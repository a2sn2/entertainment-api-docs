import { escapeHtml, route } from './dom-utils.js';
export function initCommandPalette({ root, index, search }) {
  const palette = document.getElementById('commandPalette');
  const paletteInput = document.getElementById('paletteInput');
  const globalSearch = document.getElementById('globalSearch');
  const results = document.getElementById('paletteResults');
  const open = (query = '') => { palette.classList.add('open'); palette.setAttribute('aria-hidden','false'); paletteInput.value = query; paletteInput.focus(); render(query); };
  const close = () => { palette.classList.remove('open'); palette.setAttribute('aria-hidden','true'); };
  const render = (query) => {
    const matches = search(index, query);
    results.innerHTML = query.trim().length < 2 ? '<div class="empty-state">Type at least two characters.</div>' : matches.length ? matches.map((item)=>`<a class="palette-result" href="${route(root,item.url)}"><strong>${escapeHtml(item.title)}</strong><small>${escapeHtml(item.section)}</small></a>`).join('') : '<div class="empty-state">No matching documentation found.</div>';
  };
  globalSearch.addEventListener('focus', () => { globalSearch.blur(); open(); });
  globalSearch.addEventListener('click', () => open());
  paletteInput.addEventListener('input', () => render(paletteInput.value));
  palette.addEventListener('click', (event) => { if (event.target === palette) close(); });
  document.addEventListener('keydown', (event) => { if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); open(); } if (event.key === 'Escape') close(); });
}
