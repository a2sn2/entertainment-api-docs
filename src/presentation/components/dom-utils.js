export function escapeHtml(value) {
  return String(value ?? '').replace(/[&<>'"]/g, (char) => ({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[char]));
}
export function json(value) { return JSON.stringify(value, null, 2); }
export function qs(selector, root = document) { return root.querySelector(selector); }
export function qsa(selector, root = document) { return [...root.querySelectorAll(selector)]; }
export function route(root, url) {
  if (/^(https?:|mailto:|#)/.test(url)) return url;
  const prefix = root === '.' ? './' : `${root}/`;
  return `${prefix}${url}`.replace('/./', '/');
}
export function badgeClass(status) {
  const map = {'Tested':'badge-success','Observed':'badge-info','Pending':'badge-warn','Not Performed':'badge-danger','Confirmed':'badge-success','Inference':'badge-warn'};
  return map[status] ?? 'badge-info';
}
