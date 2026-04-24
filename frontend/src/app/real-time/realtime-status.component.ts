import { Component, OnDestroy, OnInit } from '@angular/core';
import { SignalRService } from '../core/services/signalr.service';
import { AuthService } from '../core/services/auth.service';
import { Subscription } from 'rxjs';
import { environment } from '../../environments/environment';

@Component({
  selector: 'app-realtime-status',
  template: `
    <div *ngIf="connectionState !== 'connected'" class="alert alert-warning py-2 px-3 mb-3">
      Real-time: {{ connectionState }}
    </div>
    <div *ngIf="connectionState === 'connected'" class="alert alert-success py-2 px-3 mb-3">
      Real-time: Connected
    </div>
  `
})
export class RealtimeStatusComponent implements OnInit, OnDestroy {
  connectionState = 'disconnected';
  private sub?: Subscription;

  constructor(private signalR: SignalRService, private auth: AuthService) {}

  ngOnInit() {
    const user = this.auth.getUser();
    if (user) {
      this.signalR.startConnection(environment.signalRHubUrl, user.token);
      this.sub = this.signalR.connectionState$.subscribe(state => this.connectionState = state);
    }
  }

  ngOnDestroy() {
    this.signalR.stopConnection();
    this.sub?.unsubscribe();
  }
}
