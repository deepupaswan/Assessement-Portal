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
      this.userSubject.next(JSON.parse(persistedUser) as AuthUser);
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
    return this.userSubject.value;
  }

  isAuthenticated(): boolean {
    return !!this.userSubject.value?.token;
  }

  logout(): void {
    this.setUser(null);
  }
}
