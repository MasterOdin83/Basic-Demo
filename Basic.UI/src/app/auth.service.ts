import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { STS_URL } from './api';

interface Session {
  email: string;
  role: string;
  token: string;
}

// The STS is TurboEmpresa's API: one STS for every site (Héctor, 2026-09-17). The access token lives in
// memory only (never localStorage); the refresh token is an HttpOnly cookie scoped to the STS's /api/auth,
// so login/refresh/logout go withCredentials. On reload, the guard (and App) rebuild the session from that cookie.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  readonly email = signal<string | null>(null);
  token: string | null = null;

  // captchaToken: Turnstile token from the login form; the STS verifies it with Cloudflare.
  login(email: string, password: string, captchaToken: string) {
    return this.http
      .post<Session>(`${STS_URL}/api/auth/login`, { email, password, captchaToken }, { withCredentials: true })
      .pipe(tap((s) => this.start(s)));
  }

  register(email: string, password: string, captchaToken: string) {
    return this.http.post(`${STS_URL}/api/auth/register`, { email, password, captchaToken }, { withCredentials: true });
  }

  refresh() {
    return this.http
      .post<Session>(`${STS_URL}/api/auth/refresh`, {}, { withCredentials: true })
      .pipe(tap((s) => this.start(s)));
  }

  logout(): void {
    this.token = null;
    this.email.set(null);
    // Fire-and-forget: only the STS can clear its HttpOnly cookie.
    this.http.post(`${STS_URL}/api/auth/logout`, {}, { withCredentials: true }).subscribe({ error: () => {} });
  }

  private start({ token, email }: Session): void {
    this.token = token;
    this.email.set(email);
  }
}
