import { spotlightPosition } from './spotlight';

describe('spotlightPosition', () => {
  it('translates a viewport point into element-relative px, rounded', () => {
    expect(spotlightPosition(150, 250, 100, 200)).toEqual({ x: '50px', y: '50px' });
  });

  it('rounds fractional coordinates', () => {
    expect(spotlightPosition(10.6, 20.4, 0, 0)).toEqual({ x: '11px', y: '20px' });
  });

  it('handles the cursor sitting above/left of the element origin', () => {
    expect(spotlightPosition(10, 10, 50, 50)).toEqual({ x: '-40px', y: '-40px' });
  });
});
