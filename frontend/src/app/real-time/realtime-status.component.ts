import { Component, OnDestroy, OnInit } from '@angular/core';
import { SignalRService } from '../core/services/signalr.service';
import { AuthService } from '../core/services/auth.service';
import { Subject } from 'rxjs';
import { switchMap, takeUntil } from 'rxjs/operators';
import { from } from 'rxjs';
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
  private readonly destroy$ = new Subject<void>();

  constructor(private signalR: SignalRService, private auth: AuthService) {}

  ngOnInit() {
    const user = this.auth.getUser();
    if (user) {
      // Use switchMap to: 
      // 1. Convert the Promise from startConnection() to an Observable using from()
      // 2. Switch to listening to connectionState$ after connection starts
      // 3. Automatically unsubscribe from previous subscription when switching
      from(this.signalR.startConnection(environment.signalRHubUrl, user.token))
        .pipe(
          switchMap(() => this.signalR.connectionState$),
          takeUntil(this.destroy$)
        )
        .subscribe({
          next: (state) => this.connectionState = state,
          error: (err) => console.error('SignalR connection error:', err)
        });
    }
  }

  ngOnDestroy() {
    // Do not stop the shared SignalR connection here; this avoids stopping it while
    // other parts of the app still need it. Let app-level logic stop on logout.
    this.destroy$.next();
    this.destroy$.complete();
  }
}
