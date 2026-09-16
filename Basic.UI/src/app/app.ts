import { Component, ElementRef, inject, viewChild } from '@angular/core';
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
  private menuDrawer = viewChild<ElementRef<HTMLDialogElement>>('menuDrawer');

  openLogin(): void {
    this.loginDrawer()?.nativeElement.showModal();
  }

  openMenu(): void {
    this.menuDrawer()?.nativeElement.showModal();
  }

  closeMenu(): void {
    this.menuDrawer()?.nativeElement.close();
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}
