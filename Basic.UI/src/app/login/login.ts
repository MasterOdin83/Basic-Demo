import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  standalone: false,
})
export class Login {
  protected auth = inject(AuthService);
  private router = inject(Router);

  // Zoneless app: state written in subscribe callbacks must be signals or it never renders.
  readonly mode = signal<'login' | 'register'>('login');
  readonly error = signal('');
  readonly info = signal('');
  readonly busy = signal(false);
  readonly avatarFailed = signal(false);
  username = '';
  password = '';

  toggleMode(): void {
    this.mode.set(this.mode() === 'login' ? 'register' : 'login');
    this.error.set('');
    this.info.set('');
  }

  submit(): void {
    this.error.set('');
    this.info.set('');

    if (this.mode() === 'register' && this.password.length < 8) {
      this.error.set('Password must be at least 8 characters.');
      return;
    }

    this.busy.set(true);

    if (this.mode() === 'login') {
      this.auth.login(this.username, this.password).subscribe({
        next: () => this.router.navigate(['/tasks']),
        error: () => {
          this.busy.set(false);
          this.error.set('Login failed. Check your credentials and try again.');
        },
      });
    } else {
      this.auth.register(this.username, this.password).subscribe({
        next: () => {
          this.busy.set(false);
          this.mode.set('login');
          this.info.set('Account created — you can log in now.');
        },
        error: (e) => {
          this.busy.set(false);
          this.error.set('Registration failed.');
        },
      });
    }
  }
}
