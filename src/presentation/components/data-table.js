import { escapeHtml } from './dom-utils.js';
export function fieldTable(fields, request = false) {
  const head = request ? ['Field','Type','Required','Description','Example'] : ['Field','Type','Description','Example'];
  const rows = fields.map((field) => request
    ? `<tr><td><code>${escapeHtml(field.field)}</code></td><td>${escapeHtml(field.type)}</td><td>${escapeHtml(field.required)}</td><td>${escapeHtml(field.description)}</td><td><code>${escapeHtml(field.example)}</code></td></tr>`
    : `<tr><td><code>${escapeHtml(field.field)}</code></td><td>${escapeHtml(field.type)}</td><td>${escapeHtml(field.description)}</td><td><code>${escapeHtml(field.example)}</code></td></tr>`).join('');
  return `<div class="table-wrap"><table><thead><tr>${head.map((item)=>`<th>${item}</th>`).join('')}</tr></thead><tbody>${rows}</tbody></table></div>`;
}
export function simpleTable(headers, rows) {
  return `<div class="table-wrap"><table><thead><tr>${headers.map((h)=>`<th>${escapeHtml(h)}</th>`).join('')}</tr></thead><tbody>${rows.map((row)=>`<tr>${row.map((cell)=>`<td>${cell}</td>`).join('')}</tr>`).join('')}</tbody></table></div>`;
}
