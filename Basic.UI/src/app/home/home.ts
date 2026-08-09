import { HttpClient } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { environment } from '../../environments/environment';
import { observeReveals } from './reveal';
import { parseRequirements, RequirementItem } from './requirements';
import { observeShaderHero } from './shader-hero';
import { observeSplashParallax } from './splash-parallax';
import { observeSpotlight } from './spotlight';

@Component({
  selector: 'app-home',
  templateUrl: './home.html',
  standalone: false,
})
export class Home implements OnInit, AfterViewInit, OnDestroy {
  private http = inject(HttpClient);
  private el = inject(ElementRef<HTMLElement>);
  private stopReveals?: () => void;
  private stopParallax?: () => void;
  private stopSpotlight?: () => void;
  private stopShaderHero?: () => void;

  readonly spartanItAboutUrl = environment.spartanItAboutUrl;

  // Requirements coverage is authored in requirements-coverage.md so editing the
  // requirements doesn't require touching this component or its template.
  readonly reqItems = signal<RequirementItem[]>([]);
  readonly reqLoading = signal(true);
  readonly reqError = signal(false);

  ngOnInit(): void {
    this.http.get('requirements-coverage.md', { responseType: 'text' }).subscribe({
      next: (md) => {
        this.reqItems.set(parseRequirements(md));
        this.reqLoading.set(false);
      },
      error: () => {
        this.reqLoading.set(false);
        this.reqError.set(true);
      },
    });
  }

  ngAfterViewInit(): void {
    // Self-guards on pointer type + reduced motion; safe to call unconditionally.
    this.stopSpotlight = observeSpotlight(this.el.nativeElement.querySelector('.home-layout'));
    this.stopShaderHero = observeShaderHero(this.el.nativeElement.querySelector('.shader-canvas'));

    if (matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    const targets = this.el.nativeElement.querySelectorAll('.build-facts, .about, .req-coverage');
    this.stopReveals = observeReveals(targets);
    this.stopParallax = observeSplashParallax(this.el.nativeElement.querySelector('.splash'));
  }

  ngOnDestroy(): void {
    this.stopReveals?.();
    this.stopParallax?.();
    this.stopSpotlight?.();
    this.stopShaderHero?.();
  }
}
