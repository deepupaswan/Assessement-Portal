import { Component, OnDestroy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, AuthUser } from '../core/services/auth.service';
import { NotificationService } from '../core/services/notification.service';
import { AppRoles, AppRouteUrls } from '../constants/app.constants';
import { AuthMessages } from '../constants/auth.constants';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
})
export class RegisterComponent implements OnDestroy {
  error: string | null = null;
  loading = false;
  private readonly destroy$ = new Subject<void>();

  form = this.fb.group({
    name: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    role: [AppRoles.Candidate, Validators.required]
  });

  constructor(
    private fb: FormBuilder,
    private auth: AuthService,
    private router: Router,
    private notifier: NotificationService
  ) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = null;

    const role = this.form.value.role ?? AppRoles.Candidate;

    this.auth.register(
      this.form.value.name!,
      this.form.value.email!,
      this.form.value.password!,
      role
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (user: AuthUser) => {
        this.notifier.showSuccess('Account created successfully');
        this.router.navigate([user.role === AppRoles.Admin ? AppRouteUrls.admin : AppRouteUrls.candidate]);
      },
      error: (err: HttpErrorResponse) => {
        const msg = err.error?.message || AuthMessages.RegisterError;
        this.notifier.showError(msg);
        this.loading = false;
      },
      complete: () => {
        this.loading = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
