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
      .post<{ token: string; username: string }>(`${STS_URL}/api/auth/login`, { username, password })
      .pipe(
        tap((r) => {
          localStorage.setItem('token', r.token);
          localStorage.setItem('username', r.username);
          this.username.set(r.username);
        }),
      );
  }

  register(username: string, password: string) {
    return this.http.post(`${STS_URL}/api/auth/register`, { username, password });
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('username');
    this.username.set(null);
  }
}
