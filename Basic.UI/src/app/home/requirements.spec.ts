import { parseRequirements } from './requirements';

describe('parseRequirements', () => {
  it('splits a list line into a label and plain-text description', () => {
    const [item] = parseRequirements('- Data access layer — Basic.Data — EF Core repositories.');

    expect(item.label).toBe('Data access layer');
    expect(item.description).toEqual([{ type: 'text', value: 'Basic.Data — EF Core repositories.' }]);
    expect(item.pending).toBe(false);
  });

  it('parses **bold** spans inside the description', () => {
    const [item] = parseRequirements('- Label — before **bold** after');

    expect(item.description).toEqual([
      { type: 'text', value: 'before ' },
      { type: 'bold', value: 'bold' },
      { type: 'text', value: ' after' },
    ]);
  });

  it('parses a [text](url) link inside the description', () => {
    const [item] = parseRequirements('- Label — see [docs ↗](https://example.com/docs).');

    expect(item.description).toEqual([
      { type: 'text', value: 'see ' },
      { type: 'link', value: 'docs ↗', href: 'https://example.com/docs' },
      { type: 'text', value: '.' },
    ]);
  });

  it('strips a trailing ::pending marker and flags the item', () => {
    const [item] = parseRequirements('- Label — description ::pending');

    expect(item.pending).toBe(true);
    expect(item.description).toEqual([{ type: 'text', value: 'description' }]);
  });

  it('ignores blank lines and non-list lines', () => {
    const items = parseRequirements('# Requirements coverage\n\n- Only item — desc\n\nsome stray text');

    expect(items).toHaveLength(1);
    expect(items[0].label).toBe('Only item');
  });

  it('parses every list line in order', () => {
    const items = parseRequirements('- One — a\n- Two — b\n- Three — c');

    expect(items.map((i) => i.label)).toEqual(['One', 'Two', 'Three']);
  });
});
