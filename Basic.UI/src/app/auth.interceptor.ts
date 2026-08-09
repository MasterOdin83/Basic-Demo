import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Every call, to either API, rides on the session cookie — there's no token of any
// kind to attach, so there's nothing API-specific left for this interceptor to branch
// on. A 401 means the session itself is gone; there's no "refresh and retry" step
// because there's nothing left to refresh (see AuthController — no more access/
// refresh tokens exist anywhere in this flow, just the cookie + server-side session).
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);

  return next(req.clone({ withCredentials: true })).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && !req.url.includes('/api/auth/')) {
        auth.logout().subscribe();
        // No dedicated /login page — send them home, where the login drawer lives.
        router.navigate(['/']);
      }
      return throwError(() => err);
    }),
  );
};
