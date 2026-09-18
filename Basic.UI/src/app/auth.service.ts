import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { from, switchMap, tap } from 'rxjs';
import { environment } from '../environments/environment';
import { STS_URL } from './api';

interface Session {
  email: string;
  role: string;
  token: string;
}

interface GrecaptchaEnterprise {
  ready(callback: () => void): void;
  execute(siteKey: string, options: { action: string }): Promise<string>;
}

// The STS is TurboEmpresa's API: one STS for every site (Héctor, 2026-09-17). The access token lives in
// memory only (never localStorage); the refresh token is an HttpOnly cookie scoped to the STS's /api/auth,
// so login/refresh/logout go withCredentials. On reload, the guard (and App) rebuild the session from that cookie.
// Login and register carry an invisible reCAPTCHA Enterprise token (TurboEmpresa's site key) that the STS verifies.
@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly recaptchaLoaded = this.loadRecaptcha();
  readonly email = signal<string | null>(null);
  token: string | null = null;

  login(email: string, password: string) {
    return this.credentials('login', email, password).pipe(tap((s) => this.start(s)));
  }

  register(email: string, password: string) {
    return this.credentials('register', email, password);
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

  private credentials(action: 'login' | 'register', email: string, password: string) {
    return from(this.recaptchaToken(action)).pipe(
      switchMap((captchaToken) =>
        this.http.post<Session>(`${STS_URL}/api/auth/${action}`, { email, password, captchaToken }, { withCredentials: true }),
      ),
    );
  }

  private start({ token, email }: Session): void {
    this.token = token;
    this.email.set(email);
  }

  // Tokens expire after 2 minutes, so one is requested per submit. Without the script there is no token and the STS decides.
  private async recaptchaToken(action: string): Promise<string | undefined> {
    await this.recaptchaLoaded;
    const grecaptcha = (window as unknown as { grecaptcha?: { enterprise?: GrecaptchaEnterprise } }).grecaptcha?.enterprise;
    if (!grecaptcha) return undefined;
    return new Promise((resolve) =>
      grecaptcha.ready(async () => {
        try {
          resolve(await grecaptcha.execute(environment.recaptchaSiteKey, { action }));
        } catch {
          resolve(undefined);
        }
      }),
    );
  }

  private loadRecaptcha(): Promise<void> {
    if (typeof document === 'undefined' || !environment.recaptchaSiteKey) return Promise.resolve();
    return new Promise((resolve) => {
      const script = document.createElement('script');
      script.src = `https://www.google.com/recaptcha/enterprise.js?render=${encodeURIComponent(environment.recaptchaSiteKey)}`;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => resolve();
      document.head.appendChild(script);
    });
  }
}
