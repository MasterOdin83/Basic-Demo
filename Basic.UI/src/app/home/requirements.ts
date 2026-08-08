export type Segment =
  | { type: 'text'; value: string }
  | { type: 'bold'; value: string }
  | { type: 'link'; value: string; href: string };

export interface RequirementItem {
  label: string;
  description: Segment[];
  pending: boolean;
}

const PENDING_MARKER = '::pending';
const INLINE = /\*\*(.+?)\*\*|\[([^\]]+)\]\(([^)]+)\)/g;

// Minimal inline markdown: **bold** and [text](url) only — this content never needs more.
function parseInline(text: string): Segment[] {
  const segments: Segment[] = [];
  let lastIndex = 0;

  for (const match of text.matchAll(INLINE)) {
    if (match.index > lastIndex) {
      segments.push({ type: 'text', value: text.slice(lastIndex, match.index) });
    }
    segments.push(
      match[1] !== undefined
        ? { type: 'bold', value: match[1] }
        : { type: 'link', value: match[2], href: match[3] },
    );
    lastIndex = match.index + match[0].length;
  }
  if (lastIndex < text.length) {
    segments.push({ type: 'text', value: text.slice(lastIndex) });
  }
  return segments;
}

// One requirement per `- Label — description` list line; everything else in the file is ignored,
// so headings/comments/blank lines are free-form documentation for whoever edits the .md.
export function parseRequirements(markdown: string): RequirementItem[] {
  return markdown
    .split('\n')
    .map((line) => line.trim())
    .filter((line) => line.startsWith('- '))
    .map((line) => {
      let text = line.slice(2).trim();
      const pending = text.endsWith(PENDING_MARKER);
      if (pending) {
        text = text.slice(0, -PENDING_MARKER.length).trim();
      }

      const sep = text.indexOf(' — ');
      const label = sep === -1 ? text : text.slice(0, sep);
      const description = sep === -1 ? '' : text.slice(sep + 3);

      return { label: label.trim(), description: parseInline(description), pending };
    });
}
