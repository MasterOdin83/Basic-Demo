import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const auth = inject(AuthService);
  // Re-reads storage per call so a retry picks up the refreshed token.
  const send = (r: HttpRequest<unknown>) => {
    const token = auth.token;
    return next(token ? r.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : r);
  };

  return send(req).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status !== 401 || req.url.includes('/api/auth/')) return throwError(() => err);
      // ponytail: parallel 401s each hit /refresh; single-flight it if that ever matters.
      return auth.refresh().pipe(
        switchMap(() => send(req)),
        catchError(() => {
          auth.logout();
          router.navigate(['/login']);
          return throwError(() => err);
        }),
      );
    }),
  );
};
