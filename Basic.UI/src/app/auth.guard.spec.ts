import { TestBed } from '@angular/core/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    localStorage.clear();
  });

  it('redirects anonymous users home, where the login drawer lives', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/');
  });

  it('allows logged-in users', () => {
    localStorage.setItem('token', 'a-token');
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBe(true);
  });
});
