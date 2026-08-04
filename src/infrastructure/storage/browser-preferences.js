const safeStorage = {
  get(key) { try { return localStorage.getItem(key); } catch { return null; } },
  set(key, value) { try { localStorage.setItem(key, value); } catch { /* storage may be disabled */ } }
};

export const BrowserPreferences = Object.freeze({
  getTheme() { return safeStorage.get('ent-doc-theme'); },
  setTheme(theme) { safeStorage.set('ent-doc-theme', theme); },
  getLastPage() { return safeStorage.get('ent-doc-last-page'); },
  setLastPage(page) { safeStorage.set('ent-doc-last-page', page); }
});
