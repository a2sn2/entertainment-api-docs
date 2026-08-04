import { route } from './dom-utils.js';
const icons = {
  menu:'<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M4 6h16v2H4V6Zm0 5h16v2H4v-2Zm0 5h16v2H4v-2Z"/></svg>',
  search:'<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="m20.7 19.3-4.1-4.1a7.5 7.5 0 1 0-1.4 1.4l4.1 4.1 1.4-1.4ZM5 10.5a5.5 5.5 0 1 1 11 0 5.5 5.5 0 0 1-11 0Z"/></svg>',
  theme:'<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M12 3a9 9 0 1 0 9 9c0-.46-.04-.92-.1-1.36A7 7 0 0 1 13.36 3.1C12.92 3.04 12.46 3 12 3Zm0 2h.12A9 9 0 0 0 19 11.88V12a7 7 0 1 1-7-7Z"/></svg>',
  print:'<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M7 3h10v4h1a3 3 0 0 1 3 3v6h-4v5H7v-5H3v-6a3 3 0 0 1 3-3h1V3Zm2 2v2h6V5H9Zm0 10v4h6v-4H9Zm9-6H6a1 1 0 0 0-1 1v4h2v-1h10v1h2v-4a1 1 0 0 0-1-1Z"/></svg>',
  logo:'<svg class="icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M7.5 4A3.5 3.5 0 0 0 4 7.5v9A3.5 3.5 0 0 0 7.5 20h9a3.5 3.5 0 0 0 3.5-3.5v-9A3.5 3.5 0 0 0 16.5 4h-9Zm0 2h9A1.5 1.5 0 0 1 18 7.5v9a1.5 1.5 0 0 1-1.5 1.5h-9A1.5 1.5 0 0 1 6 16.5v-9A1.5 1.5 0 0 1 7.5 6Zm1.25 3v6h2v-2h2.5v2h2V9h-2v2h-2.5V9h-2Z"/></svg>'
};
export function renderShell({ root, page, repository }) {
  const doc = repository.getDocumentControl();
  const nav = repository.getNavigation();
  document.body.insertAdjacentHTML('afterbegin', `
    <a class="skip-link" href="#page-root">Skip to main content</a>
    <header class="app-topbar">
      <button class="icon-btn mobile-menu" id="menuButton" aria-label="Open navigation" aria-expanded="false">${icons.menu}</button>
      <a class="brand" href="${route(root,'index.html')}"><span class="brand-mark">${icons.logo}</span><span>Entertainment APIs<small>Developer Portal</small></span></a>
      <div class="topbar-search"><span>${icons.search}</span><label class="sr-only" for="globalSearch">Search documentation</label><input id="globalSearch" type="search" placeholder="Search APIs, fields, rules…" autocomplete="off"><span class="kbd">Ctrl K</span></div>
      <div class="toolbar"><button class="icon-btn" id="themeButton" aria-label="Switch theme">${icons.theme}</button><button class="icon-btn print-btn" id="printButton" aria-label="Print page">${icons.print}</button></div>
    </header>
    <aside class="app-sidebar" id="sidebar"><div class="doc-chip"><strong>${doc.reference}</strong><small>Version ${doc.version} · ${doc.environment} · For Review</small></div><nav>
      <div class="nav-label">Documentation</div>
      ${nav.slice(0,7).map((item)=>`<a class="nav-link ${item.page===page?'active':''}" href="${route(root,item.url)}">${item.label}</a>`).join('')}
      <div class="nav-label">Control</div>
      ${nav.slice(7).map((item)=>`<a class="nav-link ${item.page===page?'active':''}" href="${route(root,item.url)}">${item.label}</a>`).join('')}
    </nav></aside>
    <div class="drawer-backdrop" id="drawerBackdrop"></div>
    <div class="command-palette" id="commandPalette" aria-hidden="true"><div class="palette-panel"><div class="palette-search">${icons.search}<input id="paletteInput" type="search" placeholder="Search documentation…" autocomplete="off"><span class="kbd">Esc</span></div><div class="palette-results" id="paletteResults"></div></div></div>
  `);
  document.body.insertAdjacentHTML('beforeend', `<footer class="site-footer">${doc.reference} | Version ${doc.version} | ${doc.classification}</footer><div class="toast" id="toast" role="status" aria-live="polite"></div>`);
}
