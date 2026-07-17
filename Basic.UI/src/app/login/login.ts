import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.html',
  standalone: false,
})
export class Login {
  private auth = inject(AuthService);
  private router = inject(Router);

  mode: 'login' | 'register' = 'login';
  username = '';
  password = '';
  error = '';
  info = '';
  busy = false;

  toggleMode(): void {
    this.mode = this.mode === 'login' ? 'register' : 'login';
    this.error = '';
    this.info = '';
  }

  submit(): void {
    this.error = '';
    this.info = '';
    this.busy = true;

    if (this.mode === 'login') {
      this.auth.login(this.username, this.password).subscribe({
        next: () => this.router.navigate(['/']),
        error: (e) => {
          this.busy = false;
          this.error = e.error?.error ?? 'Login failed.';
        },
      });
    } else {
      this.auth.register(this.username, this.password).subscribe({
        next: () => {
          this.busy = false;
          this.mode = 'login';
          this.info = 'Account created — you can log in now.';
        },
        error: (e) => {
          this.busy = false;
          this.error = e.error?.error ?? 'Registration failed.';
        },
      });
    }
  }
}
