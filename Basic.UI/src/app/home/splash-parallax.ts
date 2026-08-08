export interface SplashTransform {
  translateY: number;
  opacity: number;
  scale: number;
}

// 0 = splash fully at rest (top of page), 1 = scrolled a full splash-height past it.
export function splashProgress(scrollY: number, splashHeight: number): number {
  if (splashHeight <= 0) return 1;
  return Math.min(Math.max(scrollY / splashHeight, 0), 1);
}

export function splashTransformAt(progress: number): SplashTransform {
  const p = Math.min(Math.max(progress, 0), 1);
  return {
    translateY: p * -40 || 0, // avoid -0 at p === 0
    opacity: 1 - p,
    scale: 1 - p * 0.08,
  };
}

// rAF-throttled scroll listener wiring the pure math above onto the element's own
// style — kept out of the pure functions above so they stay trivially testable.
export function observeSplashParallax(el: Element | null): () => void {
  if (!el) return () => {};
  const target = el as HTMLElement;
  let ticking = false;

  const apply = () => {
    ticking = false;
    const { translateY, opacity, scale } = splashTransformAt(
      splashProgress(window.scrollY, target.offsetHeight),
    );
    target.style.transform = `translateY(${translateY}px) scale(${scale})`;
    target.style.opacity = String(opacity);
  };

  const onScroll = () => {
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(apply);
  };

  apply();
  window.addEventListener('scroll', onScroll, { passive: true });
  return () => window.removeEventListener('scroll', onScroll);
}
