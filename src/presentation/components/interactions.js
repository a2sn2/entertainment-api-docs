export function initGlobalInteractions(preferences) {
  const menu = document.getElementById('menuButton');
  const backdrop = document.getElementById('drawerBackdrop');
  const theme = document.getElementById('themeButton');
  const print = document.getElementById('printButton');
  let toastTimer;
  const closeDrawer = () => { document.body.classList.remove('drawer-open'); menu?.setAttribute('aria-expanded','false'); };
  menu?.addEventListener('click', () => { const open = !document.body.classList.contains('drawer-open'); document.body.classList.toggle('drawer-open', open); menu.setAttribute('aria-expanded', String(open)); });
  backdrop?.addEventListener('click', closeDrawer);
  document.querySelectorAll('.app-sidebar a').forEach((link)=>link.addEventListener('click',closeDrawer));
  print?.addEventListener('click',()=>window.print());
  theme?.addEventListener('click', () => { const next = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark'; document.documentElement.dataset.theme = next; preferences.setTheme(next); });
  document.addEventListener('click', async (event) => {
    const button = event.target.closest('[data-copy-code]'); if (!button) return;
    const code = button.closest('.code-block')?.querySelector('code')?.textContent ?? '';
    try { await navigator.clipboard.writeText(code); showToast('Copied to clipboard'); } catch { showToast('Copy failed. Select the text manually.'); }
  });
  function showToast(message) { const toast = document.getElementById('toast'); toast.textContent = message; toast.classList.add('show'); clearTimeout(toastTimer); toastTimer = setTimeout(()=>toast.classList.remove('show'),1800); }
  document.addEventListener('click', (event) => { const tab = event.target.closest('[data-tab]'); if (!tab) return; const group = tab.closest('[data-tabs]'); const name = tab.dataset.tab; group.querySelectorAll('[data-tab]').forEach((button)=>button.setAttribute('aria-selected', String(button===tab))); group.querySelectorAll('[data-panel]').forEach((panel)=>panel.hidden = panel.dataset.panel !== name); });
}
