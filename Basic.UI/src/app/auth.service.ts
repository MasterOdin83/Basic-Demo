import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { catchError, of, tap } from 'rxjs';
import { STS_URL } from './api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  readonly username = signal<string | null>(null);

  login(username: string, password: string) {
    return this.http
      .post<{ username: string }>(`${STS_URL}/api/auth/login`, { username, password })
      .pipe(tap((r) => this.username.set(r.username)));
  }

  register(username: string, password: string) {
    return this.http.post(`${STS_URL}/api/auth/register`, { username, password });
  }

  // Re-derives session state from the cookie — call at app startup so a page
  // reload doesn't look logged-out while the session is still valid server-side.
  restore() {
    return this.http
      .get<{ username: string }>(`${STS_URL}/api/auth/me`)
      .pipe(tap((r) => this.username.set(r.username)));
  }

  logout() {
    return this.http.post(`${STS_URL}/api/auth/logout`, {}).pipe(
      // Best-effort: clear local state even if the network call fails, so a
      // dead connection can't strand the UI in a "still logged in" state.
      catchError(() => of(null)),
      tap(() => this.username.set(null)),
    );
  }
}
