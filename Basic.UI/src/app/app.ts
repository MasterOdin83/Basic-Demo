import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css',
})
export class App {
  protected readonly auth = inject(AuthService);
  private router = inject(Router);
  private loginDrawer = viewChild<ElementRef<HTMLDialogElement>>('loginDrawer');
  protected readonly menuOpen = signal(false);

  openLogin(): void {
    this.loginDrawer()?.nativeElement.showModal();
  }

  logout(): void {
    this.menuOpen.set(false);
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
