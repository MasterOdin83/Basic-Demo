import { Directive, ElementRef, EventEmitter, OnDestroy, OnInit, Output, inject } from '@angular/core';
import { environment } from '../environments/environment';

interface Turnstile {
  render(el: HTMLElement, options: Record<string, unknown>): string;
  reset(widgetId: string): void;
  remove(widgetId: string): void;
}

// Cloudflare Turnstile widget (script tag in index.html, render=explicit). Emits the token on
// success and '' when it expires or errors. Tokens are single-use: callers reset() after every
// submit, successful or not.
@Directive({ selector: '[appTurnstile]', standalone: false })
export class TurnstileDirective implements OnInit, OnDestroy {
  @Output() token = new EventEmitter<string>();
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);
  private widgetId?: string;
  private timer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    // The script loads async; poll until it's there (10 s max), then render once.
    let tries = 0;
    this.timer = setInterval(() => {
      const turnstile = this.api();
      if (!turnstile) {
        if (++tries > 100) clearInterval(this.timer);
        return;
      }
      clearInterval(this.timer);
      this.widgetId = turnstile.render(this.el.nativeElement, {
        sitekey: environment.turnstileSiteKey,
        theme: 'dark',
        callback: (t: string) => this.token.emit(t),
        'expired-callback': () => this.token.emit(''),
        'error-callback': () => this.token.emit(''),
      });
    }, 100);
  }

  reset(): void {
    if (this.widgetId) this.api()?.reset(this.widgetId);
    this.token.emit('');
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
    if (this.widgetId) this.api()?.remove(this.widgetId);
  }

  private api(): Turnstile | undefined {
    return (globalThis as { turnstile?: Turnstile }).turnstile;
  }
}
