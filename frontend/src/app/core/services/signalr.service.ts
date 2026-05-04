import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

export type RealtimeConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  // Encapsulated subject: keep writable subject private and expose Observable publicly
  private readonly _connectionState = new BehaviorSubject<RealtimeConnectionState>('disconnected');
  public readonly connectionState$ = this._connectionState.asObservable();
  // Guard to avoid concurrent start attempts
  private starting = false;

  async startConnection(hubUrl: string, token: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this._connectionState.next('connected');
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.onclose(() => this._connectionState.next('disconnected'));
    this.hubConnection.onreconnecting(() => this._connectionState.next('reconnecting'));
    this.hubConnection.onreconnected(() => this._connectionState.next('connected'));

    // Avoid concurrent starts
    if (this.starting) {
      return;
    }

    this._connectionState.next('connecting');
    this.starting = true;

    try {
      await this.hubConnection.start();
      this._connectionState.next('connected');
    } catch (err) {
      // surface error for diagnostics
      console.warn('SignalR startConnection failed', err);
      this._connectionState.next('error');
    } finally {
      this.starting = false;
    }
  }

  async stopConnection(): Promise<void> {
    await this.hubConnection?.stop();
    this._connectionState.next('disconnected');
  }

  on<T>(event: string, callback: (data: T) => void): void {
    this.hubConnection?.on(event, callback);
  }

  off(event: string): void {
    this.hubConnection?.off(event);
  }

  send<T = unknown>(event: string, ...args: unknown[]): Promise<T | undefined> {
    return this.hubConnection?.invoke<T>(event, ...args) ?? Promise.resolve(undefined);
  }

  isConnected(): boolean {
    return this.hubConnection?.state === signalR.HubConnectionState.Connected;
  }
}
