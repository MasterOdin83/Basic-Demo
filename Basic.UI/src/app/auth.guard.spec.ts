import { TestBed } from '@angular/core/testing';
import { GuardResult, provideRouter, UrlTree } from '@angular/router';
import { isObservable, lastValueFrom, Observable, of, throwError } from 'rxjs';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let auth: { token: string | null; refresh: () => Observable<unknown> };

  beforeEach(() => {
    auth = { token: null, refresh: () => throwError(() => new Error('no refresh cookie')) };
    TestBed.configureTestingModule({ providers: [provideRouter([]), { provide: AuthService, useValue: auth }] });
  });

  const run = () => TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  const resolve = (result: unknown) =>
    isObservable(result) ? lastValueFrom(result as Observable<GuardResult>) : Promise.resolve(result as GuardResult);

  it('redirects anonymous users home when there is no refresh cookie either', async () => {
    const result = await resolve(run());
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/');
  });

  it('allows users with a token in memory without calling refresh', () => {
    auth.token = 'a-token';
    expect(run()).toBe(true);
  });

  it('rebuilds the session from the refresh cookie after a reload', async () => {
    auth.refresh = () => of({ email: 'a@b.c', role: 'cliente', token: 't' });
    expect(await resolve(run())).toBe(true);
  });
});
