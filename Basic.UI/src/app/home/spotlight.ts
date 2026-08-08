// Cursor position relative to the tracked element's own box, in whole px — the unit
// radial-gradient(circle at var(--mouse-x) var(--mouse-y), ...) needs.
export function spotlightPosition(
  clientX: number,
  clientY: number,
  originX: number,
  originY: number,
): { x: string; y: string } {
  return {
    x: `${Math.round(clientX - originX)}px`,
    y: `${Math.round(clientY - originY)}px`,
  };
}

const POINTER_QUERY = '(hover: hover) and (pointer: fine)';
const MOTION_QUERY = '(prefers-reduced-motion: reduce)';

// Desktop/pointer-only, motion-allowed-only cursor spotlight: sets --mouse-x/--mouse-y
// on `el` from a rAF-throttled mousemove listener. No-ops (and attaches nothing) on
// touch or under reduced motion, per the same guard style as reveal.ts/splash-parallax.ts.
export function observeSpotlight(el: Element | null): () => void {
  if (!el || !matchMedia(POINTER_QUERY).matches || matchMedia(MOTION_QUERY).matches) {
    return () => {};
  }

  const target = el as HTMLElement;
  target.classList.add('spotlight-ready');

  let ticking = false;
  let lastX = 0;
  let lastY = 0;

  const apply = () => {
    ticking = false;
    const rect = target.getBoundingClientRect();
    const { x, y } = spotlightPosition(lastX, lastY, rect.left, rect.top);
    target.style.setProperty('--mouse-x', x);
    target.style.setProperty('--mouse-y', y);
  };

  const onMove = (e: MouseEvent) => {
    lastX = e.clientX;
    lastY = e.clientY;
    if (ticking) return;
    ticking = true;
    requestAnimationFrame(apply);
  };

  window.addEventListener('mousemove', onMove, { passive: true });
  return () => {
    window.removeEventListener('mousemove', onMove);
    target.classList.remove('spotlight-ready');
  };
}
