import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';
import { TurnstileDirective } from '../turnstile';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  standalone: false,
})
export class Login {
  protected auth = inject(AuthService);
  private router = inject(Router);
  private captcha = viewChild(TurnstileDirective);

  // Zoneless app: state written in subscribe callbacks must be signals or it never renders.
  readonly mode = signal<'login' | 'register'>('login');
  readonly error = signal('');
  readonly info = signal('');
  readonly busy = signal(false);
  readonly avatarFailed = signal(false);
  readonly showPassword = signal(false);
  // Turnstile token: '' until the widget passes, and again after every submit (tokens are single use).
  readonly captchaToken = signal('');
  email = '';
  password = '';

  toggleMode(): void {
    this.mode.set(this.mode() === 'login' ? 'register' : 'login');
    this.error.set('');
    this.info.set('');
    this.resetCaptcha();
  }

  submit(): void {
    this.error.set('');
    this.info.set('');

    if (this.mode() === 'register' && this.password.length < 8) {
      this.error.set('Password must be at least 8 characters.');
      return;
    }

    this.busy.set(true);
    const captchaToken = this.captchaToken();

    if (this.mode() === 'login') {
      this.auth.login(this.email, this.password, captchaToken).subscribe({
        next: () => this.router.navigate(['/tasks']),
        error: (e: HttpErrorResponse) => {
          this.busy.set(false);
          this.error.set(this.describe(e, 'Login failed. Check your credentials and try again.'));
          this.resetCaptcha();
        },
      });
    } else {
      this.auth.register(this.email, this.password, captchaToken).subscribe({
        next: () => {
          this.busy.set(false);
          this.mode.set('login');
          this.info.set('Account created — you can log in now.');
          this.resetCaptcha();
        },
        error: (e: HttpErrorResponse) => {
          this.busy.set(false);
          this.error.set(this.describe(e, 'Registration failed.'));
          this.resetCaptcha();
        },
      });
    }
  }

  private describe(e: HttpErrorResponse, fallback: string): string {
    if (e.status === 403) return 'Captcha check failed. Try again.';
    if (e.status === 429) return 'Too many attempts. Wait a minute and try again.';
    return fallback;
  }

  private resetCaptcha(): void {
    this.captcha()?.reset();
  }
}
