import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from './auth.service';

// After a hard reload the token is gone from memory but the refresh cookie is still there: try it before bouncing home.
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const home = inject(Router).createUrlTree(['/']);
  if (auth.token) return true;
  return auth.refresh().pipe(
    map(() => true),
    catchError(() => of(home)),
  );
};
