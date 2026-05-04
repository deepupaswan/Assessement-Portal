import { Component, OnDestroy, OnInit } from '@angular/core';
import { SignalRService } from '../core/services/signalr.service';
import { AuthService } from '../core/services/auth.service';
import { Subscription, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { RealtimeStatusUi } from '../constants/realtime.constants';

@Component({
  selector: 'app-realtime-status',
  template: `
    <div *ngIf="connectionState !== 'connected'" class="alert alert-warning py-2 px-3 mb-3">
      {{ ui.Prefix }}{{ connectionState }}
    </div>
    <div *ngIf="connectionState === 'connected'" class="alert alert-success py-2 px-3 mb-3">
      {{ ui.Prefix }}{{ ui.Connected }}
    </div>
  `
})
export class RealtimeStatusComponent implements OnInit, OnDestroy {
  readonly ui = RealtimeStatusUi;

  connectionState = 'disconnected';
  private sub?: Subscription;
  private readonly destroy$ = new Subject<void>();

  constructor(private signalR: SignalRService, private auth: AuthService) {}

  ngOnInit() {
    const user = this.auth.getUser();
    if (user) {
      // Only start the connection at app-level / auth-level. Do not stop it here — other
      // components may rely on the shared hub connection. Keep this call if your app
      // requires this component to initiate startup, otherwise centralize startup.
      this.signalR.startConnection(environment.signalRHubUrl, user.token).catch(() => {});

      // Subscribe to connection state and clean up with takeUntil
      this.signalR.connectionState$.pipe(takeUntil(this.destroy$)).subscribe(state => this.connectionState = state);
    }
  }

  ngOnDestroy() {
    // Do not stop the shared SignalR connection here; this avoids stopping it while
    // other parts of the app still need it. Let app-level logic stop on logout.
    this.destroy$.next();
    this.destroy$.complete();
  }
}
