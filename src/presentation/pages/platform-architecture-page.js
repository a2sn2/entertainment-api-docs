export function renderPlatformArchitecturePage() {
  return `
  <section class="page-head"><div><span class="eyebrow">PRODUCTION FOUNDATION</span><h1>Platform Architecture</h1><p>How the static portal grows into a database-backed platform without rebuilding the core for every feature.</p></div></section>
  <section class="section">
    <div class="callout"><div>◆</div><div><h3>Architecture decision</h3><p>Use a modular monolith with Clean Architecture and DDD boundaries first. Extract services only when scale or team ownership justifies it.</p></div></div>
  </section>
  <section class="section"><h2 class="section-title">Runtime topology</h2>
    <div class="grid-4">
      ${[
        ['Client Web','Published documentation and search for authenticated or public readers.'],
        ['Admin Web','Documents, versions, approvals, users, roles, and platform settings.'],
        ['ASP.NET Core API','The only application boundary allowed to enforce use cases and authorization.'],
        ['PostgreSQL','Identity, documents, versions, workflow state, audit records, and future integrations.']
      ].map(([title,text])=>`<article class="card metric"><div class="metric-label">Layer</div><div class="metric-value">${title}</div><div class="metric-note">${text}</div></article>`).join('')}
    </div>
  </section>
  <section class="section"><h2 class="section-title">Clean Architecture dependency rule</h2>
    <div class="panel"><pre>Domain &lt;- Application &lt;- Infrastructure &lt;- API / Admin / Client</pre>
    <p>The Domain does not depend on HTTP, database, Identity, UI, or provider SDKs. Infrastructure implements interfaces declared by Application.</p></div>
  </section>
  <section class="section"><h2 class="section-title">Bounded contexts</h2>
    <div class="grid-3">
      ${[
        ['Identity & Access','Users, roles, authentication, authorization, account lifecycle.'],
        ['Documentation Catalog','Documents, references, slugs, versions, metadata.'],
        ['Publishing Workflow','Draft, review, approval, publish, archive.'],
        ['Audit & Operations','Audit trail, health, logging, support, observability.'],
        ['Integration Registry','Future environments, schemas, providers, and connection metadata.']
      ].map(([title,text])=>`<article class="panel"><h3>${title}</h3><p class="muted">${text}</p></article>`).join('')}
    </div>
  </section>
  <section class="section"><h2 class="section-title">Role model</h2>
    <div class="table-wrap"><table><thead><tr><th>Role</th><th>Primary responsibility</th></tr></thead><tbody>
      <tr><td>Administrator</td><td>Users, roles, settings, full platform control.</td></tr>
      <tr><td>Editor</td><td>Create documents, add versions, submit for review.</td></tr>
      <tr><td>Reviewer</td><td>Approve, publish, and archive reviewed content.</td></tr>
      <tr><td>Reader</td><td>Consume published documentation.</td></tr>
    </tbody></table></div>
  </section>
  <section class="section"><h2 class="section-title">Repository foundation</h2>
    <div class="code-block"><div class="code-head"><span>Structure</span></div><pre>platform/
├── src/
│   ├── EntertainmentDocs.Domain
│   ├── EntertainmentDocs.Application
│   ├── EntertainmentDocs.Infrastructure
│   └── EntertainmentDocs.Api
├── apps/
│   ├── EntertainmentDocs.Client
│   └── EntertainmentDocs.Admin
├── tests/
├── deploy/
└── docs/</pre></div>
  </section>
  <section class="section"><div class="callout callout-danger"><div>!</div><div><h3>Deployment boundary</h3><p>GitHub Pages remains a static preview. Database, identity, admin, and API features require separate application hosting.</p></div></div></section>`;
}
