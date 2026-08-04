import { escapeHtml } from './dom-utils.js';
export function codeBlock(value, label = 'JSON') {
  const text = typeof value === 'string' ? value : JSON.stringify(value, null, 2);
  return `<div class="code-block"><div class="code-head"><span>${escapeHtml(label)}</span><button class="code-copy" type="button" data-copy-code>Copy</button></div><pre><code>${escapeHtml(text)}</code></pre></div>`;
}
