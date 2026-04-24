import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

export type RealtimeConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting' | 'error';

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private hubConnection: signalR.HubConnection | null = null;
  public connectionState$ = new BehaviorSubject<RealtimeConnectionState>('disconnected');

  async startConnection(hubUrl: string, token: string): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      this.connectionState$.next('connected');
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.onclose(() => this.connectionState$.next('disconnected'));
    this.hubConnection.onreconnecting(() => this.connectionState$.next('reconnecting'));
    this.hubConnection.onreconnected(() => this.connectionState$.next('connected'));

    this.connectionState$.next('connecting');

    try {
      await this.hubConnection.start();
      this.connectionState$.next('connected');
    } catch {
      this.connectionState$.next('error');
    }
  }

  async stopConnection(): Promise<void> {
    await this.hubConnection?.stop();
    this.connectionState$.next('disconnected');
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
