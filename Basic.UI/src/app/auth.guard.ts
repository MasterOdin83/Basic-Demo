import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  if (auth.username()) return true;

  // Hard page reload landing directly on a guarded route: no in-memory state
  // yet (that's by design — see auth.service.ts), so ask the server whether the
  // session cookie is still valid instead of assuming logged-out.
  const router = inject(Router);
  // No dedicated /login page — send them home, where the login drawer lives.
  return auth.restore().pipe(
    map(() => true),
    catchError(() => of(router.createUrlTree(['/']))),
  );
};
