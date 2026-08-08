import { armReveal } from './reveal';

describe('armReveal', () => {
  it('marks an element seen when it enters the viewport', () => {
    const el = document.createElement('div');
    el.classList.add('reveal-pending');

    armReveal(el, true);

    expect(el.classList.contains('reveal-seen')).toBe(true);
    expect(el.classList.contains('reveal-pending')).toBe(false);
  });

  it('re-arms an element back to pending when it leaves the viewport', () => {
    const el = document.createElement('div');
    el.classList.add('reveal-seen');

    armReveal(el, false);

    expect(el.classList.contains('reveal-pending')).toBe(true);
    expect(el.classList.contains('reveal-seen')).toBe(false);
  });
});
