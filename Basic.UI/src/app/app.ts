import { Component, ElementRef, OnInit, inject, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css',
})
export class App implements OnInit {
  protected readonly auth = inject(AuthService);
  private router = inject(Router);
  private loginDrawer = viewChild<ElementRef<HTMLDialogElement>>('loginDrawer');
  private menuDrawer = viewChild<ElementRef<HTMLDialogElement>>('menuDrawer');

  ngOnInit(): void {
    // Re-derive session state from the HttpOnly cookie — a page reload has no
    // other way to know a still-valid session exists. A 401 here just means
    // "not logged in," not an error to surface.
    this.auth.restore().subscribe({ error: () => {} });
  }

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
    this.auth.logout().subscribe(() => this.router.navigate(['/']));
  }
}
