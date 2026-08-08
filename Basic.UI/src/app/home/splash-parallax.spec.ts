import { splashProgress, splashTransformAt } from './splash-parallax';

describe('splashProgress', () => {
  it('is 0 at the top of the page', () => {
    expect(splashProgress(0, 800)).toBe(0);
  });

  it('is 1 once scrolled a full splash-height past the top', () => {
    expect(splashProgress(800, 800)).toBe(1);
  });

  it('clamps to 1 once scrolled beyond the splash height', () => {
    expect(splashProgress(2000, 800)).toBe(1);
  });

  it('treats a zero-height splash as already fully scrolled past', () => {
    expect(splashProgress(0, 0)).toBe(1);
  });
});

describe('splashTransformAt', () => {
  it('is at rest at progress 0', () => {
    expect(splashTransformAt(0)).toEqual({ translateY: 0, opacity: 1, scale: 1 });
  });

  it('is fully faded, drifted and shrunk at progress 1', () => {
    const t = splashTransformAt(1);
    expect(t.opacity).toBe(0);
    expect(t.translateY).toBeLessThan(0);
    expect(t.scale).toBeLessThan(1);
  });

  it('clamps out-of-range progress into [0, 1]', () => {
    expect(splashTransformAt(-1)).toEqual(splashTransformAt(0));
    expect(splashTransformAt(2)).toEqual(splashTransformAt(1));
  });
});
