const PENDING = 'reveal-pending';
const SEEN = 'reveal-seen';

// Toggles the two reveal classes for one scroll-into-view entrance. Removing SEEN
// before re-adding it (rather than just re-adding) is what lets the CSS animation
// replay on every re-entry instead of only firing once.
export function armReveal(el: Element, isIntersecting: boolean): void {
  if (isIntersecting) {
    el.classList.remove(PENDING);
    el.classList.remove(SEEN);
    void (el as HTMLElement).offsetWidth; // force reflow so the removed animation can restart
    el.classList.add(SEEN);
  } else {
    el.classList.remove(SEEN);
    el.classList.add(PENDING);
  }
}

const IDLE_REARM_MS = 400;

// Watches `targets` and arms/re-arms their entrance animation as they cross the
// viewport. Leaving the viewport waits out an idle window before re-arming, so a
// quick scroll wobble right at the edge doesn't flicker the animation.
export function observeReveals(targets: Iterable<Element>): () => void {
  const timers = new Map<Element, ReturnType<typeof setTimeout>>();

  const observer = new IntersectionObserver(
    (entries) => {
      for (const entry of entries) {
        const el = entry.target;
        clearTimeout(timers.get(el));
        if (entry.isIntersecting) {
          armReveal(el, true);
        } else {
          timers.set(
            el,
            setTimeout(() => armReveal(el, false), IDLE_REARM_MS),
          );
        }
      }
    },
    { threshold: 0.15 },
  );

  for (const el of targets) {
    el.classList.add(PENDING);
    observer.observe(el);
  }

  return () => {
    observer.disconnect();
    timers.forEach((id) => clearTimeout(id));
  };
}
