const normalize = (value) => String(value ?? '').toLowerCase().replace(/\s+/g, ' ').trim();

export function buildSearchIndex(repository) {
  const items = [];
  repository.getNavigation().forEach((item) => items.push({ title: item.label, section: 'Page', url: item.url, text: normalize(`${item.label} ${item.keywords ?? ''}`) }));
  repository.getApis().forEach((api) => {
    items.push({ title: `${api.ref} — ${api.name}`, section: 'API Reference', url: `pages/api-reference.html#${api.ref.toLowerCase()}`, text: normalize(`${api.ref} ${api.name} ${api.purpose} ${api.path} ${api.group}`) });
    [...api.requestFields, ...api.responseFields].forEach((field) => items.push({ title: field.field, section: api.ref, url: `pages/api-reference.html#${api.ref.toLowerCase()}`, text: normalize(`${field.field} ${field.type} ${field.description} ${field.example}`) }));
    [...api.rules, ...api.errors].forEach((rule) => items.push({ title: rule, section: api.ref, url: `pages/api-reference.html#${api.ref.toLowerCase()}`, text: normalize(rule) }));
  });
  repository.getOpenQuestions().forEach((question) => items.push({ title: question, section: 'Open Questions', url: 'pages/open-questions.html', text: normalize(question) }));
  repository.getErrorScenariosArray().forEach(([name, data]) => items.push({ title: name, section: 'Error Assistant', url: 'pages/error-assistant.html', text: normalize(`${name} ${Object.values(data).join(' ')}`) }));
  return items;
}

export function searchDocumentation(index, query, limit = 16) {
  const q = normalize(query);
  if (q.length < 2) return [];
  return index
    .map((item) => {
      const title = normalize(item.title);
      const score = title === q ? 100 : title.startsWith(q) ? 70 : title.includes(q) ? 45 : item.text.includes(q) ? 20 : 0;
      return { ...item, score };
    })
    .filter((item) => item.score > 0)
    .sort((a, b) => b.score - a.score || a.title.localeCompare(b.title))
    .slice(0, limit);
}
