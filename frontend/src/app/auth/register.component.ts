import { Component, OnDestroy } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, AuthUser } from '../core/services/auth.service';
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
    password: ['', Validators.required]
  });

  constructor(private fb: FormBuilder, private auth: AuthService, private router: Router) {}

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = null;

    this.auth.register(this.form.value.name!, this.form.value.email!, this.form.value.password!).pipe(takeUntil(this.destroy$)).subscribe({
      next: (user: AuthUser) => {
        this.router.navigate([user.role === AppRoles.Admin ? AppRouteUrls.admin : AppRouteUrls.candidate]);
      },
      error: (err: HttpErrorResponse) => {
        this.error = err.error?.message || AuthMessages.RegisterError;
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
