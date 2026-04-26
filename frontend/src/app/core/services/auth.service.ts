import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
  token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'portal.auth.user';
  private readonly authApiBaseUrl = `${environment.apiBaseUrl}/api/auth`;
  private readonly userSubject = new BehaviorSubject<AuthUser | null>(null);
  readonly currentUser$ = this.userSubject.asObservable();
  readonly isAuthenticated$ = this.currentUser$.pipe(map(user => !!user?.token));

  constructor(private http: HttpClient) {
    const persistedUser = localStorage.getItem(this.storageKey);
    if (persistedUser) {
      const user = this.tryParseUser(persistedUser);
      this.userSubject.next(user);

      if (!user) {
        localStorage.removeItem(this.storageKey);
      }
    }
  }

  login(email: string, password: string): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.authApiBaseUrl}/login`, { email, password }).pipe(
      tap(user => this.setUser(user))
    );
  }

  register(name: string, email: string, password: string): Observable<AuthUser> {
    return this.http.post<AuthUser>(`${this.authApiBaseUrl}/register`, { name, email, password }).pipe(
      tap(user => this.setUser(user))
    );
  }

  setUser(user: AuthUser | null): void {
    this.userSubject.next(user);

    if (user) {
      localStorage.setItem(this.storageKey, JSON.stringify(user));
    } else {
      localStorage.removeItem(this.storageKey);
    }
  }

  getUser(): AuthUser | null {
    const user = this.userSubject.value;
    if (!user || this.isTokenExpired(user.token)) {
      this.logout();
      return null;
    }

    return user;
  }

  isAuthenticated(): boolean {
    const user = this.userSubject.value;
    return !!user?.token && !this.isTokenExpired(user.token);
  }

  logout(): void {
    this.setUser(null);
  }

  private tryParseUser(serializedUser: string): AuthUser | null {
    try {
      const parsed = JSON.parse(serializedUser) as AuthUser;
      if (!parsed?.token || this.isTokenExpired(parsed.token)) {
        return null;
      }

      return parsed;
    } catch {
      return null;
    }
  }

  private isTokenExpired(token: string): boolean {
    try {
      const parts = token.split('.');
      if (parts.length < 2) {
        return true;
      }

      const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
      const payloadJson = atob(padded);
      const payload = JSON.parse(payloadJson) as { exp?: number };

      if (!payload.exp) {
        return true;
      }

      const nowInSeconds = Math.floor(Date.now() / 1000);
      return payload.exp <= nowInSeconds;
    } catch {
      return true;
    }
  }
}
