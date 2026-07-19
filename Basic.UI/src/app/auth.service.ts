import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { STS_URL } from './api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  readonly username = signal<string | null>(localStorage.getItem('username'));

  get token(): string | null {
    return localStorage.getItem('token');
  }

  login(username: string, password: string) {
    return this.http
      .post<{ token: string; refreshToken: string; username: string }>(
        `${STS_URL}/api/auth/login`,
        { username, password },
      )
      .pipe(
        tap((r) => {
          localStorage.setItem('token', r.token);
          localStorage.setItem('refreshToken', r.refreshToken);
          localStorage.setItem('username', r.username);
          this.username.set(r.username);
        }),
      );
  }

  register(username: string, password: string) {
    return this.http.post(`${STS_URL}/api/auth/register`, { username, password });
  }

  refresh() {
    const refreshToken = localStorage.getItem('refreshToken');
    return this.http
      .post<{ token: string }>(`${STS_URL}/api/auth/refresh`, { refreshToken })
      .pipe(tap((r) => localStorage.setItem('token', r.token)));
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('username');
    this.username.set(null);
  }
}
