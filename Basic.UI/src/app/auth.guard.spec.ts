import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { firstValueFrom, Observable } from 'rxjs';
import { STS_URL } from './api';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('allows an already-established session without a network call', () => {
    TestBed.inject(AuthService).username.set('demo');

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('reload with a still-valid session cookie: restores state and allows', async () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never)) as Observable<boolean | UrlTree>;
    const settled = firstValueFrom(result);

    httpMock.expectOne(`${STS_URL}/api/auth/me`).flush({ id: 1, username: 'demo', token: 'a-token' });

    expect(await settled).toBe(true);
  });

  it('reload with no valid session: redirects home, where the login drawer lives', async () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never)) as Observable<boolean | UrlTree>;
    const settled = firstValueFrom(result);

    httpMock.expectOne(`${STS_URL}/api/auth/me`).flush(null, { status: 401, statusText: 'Unauthorized' });

    const resolved = await settled;
    expect(resolved instanceof UrlTree).toBe(true);
    expect((resolved as UrlTree).toString()).toBe('/');
  });
});
