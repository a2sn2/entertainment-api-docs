const state = {
  manifest: null,
  activeGroup: "overview",
  filter: "all",
  theme: localStorage.getItem("foundationkit-theme") || "dark"
};

const root = document.getElementById("appRoot");
const groupNav = document.getElementById("groupNav");
const searchInput = document.getElementById("searchInput");
const searchOverlay = document.getElementById("searchOverlay");
const paletteInput = document.getElementById("paletteInput");
const paletteResults = document.getElementById("paletteResults");
const sidebar = document.getElementById("sidebar");
const menuButton = document.getElementById("menuButton");

const kindIcons = {
  ui: "▣",
  api: "⌁",
  package: "◇",
  document: "≡",
  guide: "◎",
  automation: "↻",
  tool: "⌘"
};

const runtimeLabels = {
  static: "STATIC",
  library: "LIBRARY",
  "static-and-local": "STATIC + LOCAL",
  "local-write": "LOCAL DATA",
  local: "LOCAL",
  github: "GITHUB",
  "local-and-ci": "LOCAL + CI"
};

document.documentElement.dataset.theme = state.theme;

async function initialize() {
  bindEvents();

  try {
    const response = await fetch("portal-manifest.json", { cache: "no-store" });
    if (!response.ok) throw new Error(`HTTP ${response.status}`);
    state.manifest = await response.json();
    renderNavigation();
    route();
  } catch (error) {
    root.innerHTML = `
      <div class="error-state">
        <strong>تعذر تحميل خريطة المستودع.</strong>
        <div>${escapeHtml(error.message)}</div>
      </div>`;
  }
}

function bindEvents() {
  window.addEventListener("hashchange", route);

  document.getElementById("themeButton").addEventListener("click", () => {
    state.theme = state.theme === "dark" ? "light" : "dark";
    document.documentElement.dataset.theme = state.theme;
    localStorage.setItem("foundationkit-theme", state.theme);
  });

  menuButton.addEventListener("click", () => {
    const open = sidebar.classList.toggle("open");
    menuButton.setAttribute("aria-expanded", String(open));
  });

  searchInput.addEventListener("focus", openSearch);
  searchInput.addEventListener("input", event => {
    openSearch();
    paletteInput.value = event.target.value;
    renderPalette(event.target.value);
  });

  paletteInput.addEventListener("input", event => renderPalette(event.target.value));
  document.getElementById("closeSearch").addEventListener("click", closeSearch);
  document.getElementById("overlayBackdrop").addEventListener("click", closeSearch);

  document.addEventListener("keydown", event => {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
      event.preventDefault();
      openSearch();
    }

    if (event.key === "Escape") closeSearch();
  });
}

function renderNavigation() {
  const groups = state.manifest.groups;
  groupNav.innerHTML = `
    <div class="nav-title">REPOSITORY MAP</div>
    ${groups.map(group => {
      const count = state.manifest.pages.filter(page => page.group === group.id).length;
      return `
        <button class="group-link" type="button" data-group="${group.id}" aria-label="${escapeHtml(group.title)}">
          <span class="group-icon">${escapeHtml(group.icon)}</span>
          <strong>${escapeHtml(group.title)}</strong>
          <span class="count">${count}</span>
        </button>`;
    }).join("")}`;

  groupNav.querySelectorAll("[data-group]").forEach(button => {
    button.addEventListener("click", () => {
      location.hash = button.dataset.group === "overview"
        ? "#/overview-home"
        : `#/group/${button.dataset.group}`;
      closeMobileMenu();
    });
  });
}

function route() {
  if (!state.manifest) return;

  const hash = location.hash || "#/overview-home";
  const segments = hash.replace(/^#\//, "").split("/").filter(Boolean);

  if (segments[0] === "group") {
    const group = state.manifest.groups.find(item => item.id === segments[1]);
    if (group) {
      state.activeGroup = group.id;
      renderGroup(group);
      updateActiveNavigation();
      window.scrollTo({ top: 0, behavior: "smooth" });
      return;
    }
  }

  const pageId = segments[0] || "overview-home";
  const page = state.manifest.pages.find(item => item.id === pageId);

  if (!page) {
    location.hash = "#/overview-home";
    return;
  }

  state.activeGroup = page.group;
  if (page.id === "overview-home") renderOverview();
  else renderDetail(page);

  updateActiveNavigation();
  closeMobileMenu();
  window.scrollTo({ top: 0, behavior: "smooth" });
}

function updateActiveNavigation() {
  groupNav.querySelectorAll("[data-group]").forEach(button => {
    button.classList.toggle("active", button.dataset.group === state.activeGroup);
  });
}

function renderOverview() {
  const pages = state.manifest.pages;
  const uiPages = pages.filter(page => page.kind === "ui");
  const apiPages = pages.filter(page => page.kind === "api");
  const documents = pages.filter(page => page.kind === "document");
  const packages = pages.filter(page => page.kind === "package");
  const groups = state.manifest.groups.filter(group => group.id !== "overview");

  root.innerHTML = `
    <section class="page-hero">
      <div class="hero-copy-block">
        <div class="eyebrow">FOUNDATIONKIT REPOSITORY ATLAS</div>
        <h1 class="hero-title">افهم المستودع كاملًا قبل أن تفتح أول ملف.</h1>
        <p class="hero-summary">${escapeHtml(state.manifest.notice)}</p>
        <div class="hero-actions">
          <button class="primary-button" type="button" data-go-group="athar">استكشف منصة أثَر <span>←</span></button>
          <button class="ghost-button" type="button" data-go-group="core">افهم الكور</button>
          <a class="ghost-button" href="https://github.com/a2sn2/foundationkit-dotnet" target="_blank" rel="noreferrer">السورس على GitHub ↗</a>
        </div>
      </div>
      <div class="hero-orbit" aria-hidden="true">
        <div class="hero-core">FK</div>
        <span class="orbit-tag one">Blazor</span>
        <span class="orbit-tag two">SQL Server</span>
        <span class="orbit-tag three">Clean Core</span>
      </div>
    </section>

    <section class="stats-strip" aria-label="إحصاءات الخريطة">
      ${statCard(packages.length, "حزم كور عامة")}
      ${statCard(uiPages.length, "صفحات Blazor فعلية")}
      ${statCard(apiPages.length, "واجهات نظام موثقة")}
      ${statCard(documents.length, "أدلة قراءة وتشغيل")}
    </section>

    <section class="section-block">
      <div class="section-heading">
        <div>
          <div class="eyebrow">ONE REPOSITORY, CLEAR ROLES</div>
          <h2>كل جزء موجود لسبب واضح</h2>
          <p>اختر أي قسم لرؤية جميع صفحاته ومساراته وفائدته العملية.</p>
        </div>
        <span class="section-count">${pages.length} عنصرًا موثقًا</span>
      </div>
      <div class="cards-grid">
        ${groups.map(group => groupCard(group)).join("")}
      </div>
    </section>

    <section class="section-block">
      <div class="section-heading">
        <div>
          <div class="eyebrow">ACTUAL BLAZOR ROUTES</div>
          <h2>كل صفحات الواجهة داخل الحل</h2>
          <p>هذه المسارات مأخوذة من ملفات Razor نفسها. المعاينة هنا ثابتة؛ العمليات الحقيقية تعمل محليًا مع API وSQL Server.</p>
        </div>
      </div>
      <div class="cards-grid">
        ${uiPages.map(pageCard).join("")}
      </div>
    </section>
  `;

  root.querySelectorAll("[data-go-group]").forEach(button => {
    button.addEventListener("click", () => location.hash = `#/group/${button.dataset.goGroup}`);
  });
  bindPageCards();
}

function renderGroup(group) {
  state.filter = "all";
  const pages = state.manifest.pages.filter(page => page.group === group.id);
  const kinds = [...new Set(pages.map(page => page.kind))];

  root.innerHTML = `
    <section class="group-header">
      <div>
        <div class="eyebrow">FOUNDATIONKIT / ${escapeHtml(group.id.toUpperCase())}</div>
        <h1>${escapeHtml(group.title)}</h1>
        <p>${escapeHtml(group.summary)}</p>
      </div>
      <div class="group-symbol" aria-hidden="true">${escapeHtml(group.icon)}</div>
    </section>

    <div class="filter-row" aria-label="تصفية العناصر">
      <button class="filter-button active" type="button" data-filter="all">الكل · ${pages.length}</button>
      ${kinds.map(kind => `
        <button class="filter-button" type="button" data-filter="${kind}">
          ${kindLabel(kind)} · ${pages.filter(page => page.kind === kind).length}
        </button>`).join("")}
    </div>

    <section class="section-block" style="margin-top: 24px">
      <div class="cards-grid" id="groupCards">
        ${pages.map(pageCard).join("")}
      </div>
    </section>
  `;

  root.querySelectorAll("[data-filter]").forEach(button => {
    button.addEventListener("click", () => {
      state.filter = button.dataset.filter;
      root.querySelectorAll("[data-filter]").forEach(item => item.classList.toggle("active", item === button));
      const filtered = state.filter === "all" ? pages : pages.filter(page => page.kind === state.filter);
      document.getElementById("groupCards").innerHTML = filtered.map(pageCard).join("");
      bindPageCards();
    });
  });

  bindPageCards();
}

function renderDetail(page) {
  const group = state.manifest.groups.find(item => item.id === page.group);
  const runtimeWarning = ["local", "local-write"].includes(page.runtime)
    ? "هذه الوظيفة تحتاج تشغيل المشروع محليًا؛ GitHub Pages يعرض شرحًا ومعاينة فقط ولا يملك API أو SQL Server."
    : "هذا العنصر قابل للقراءة من البوابة، بينما يبقى السورس هو المرجع التنفيذي النهائي.";

  root.innerHTML = `
    <section class="detail-layout">
      <article class="detail-panel">
        <div class="detail-breadcrumbs">
          <button type="button" data-back-group="${escapeHtml(page.group)}">${escapeHtml(group?.title || page.group)}</button>
          <span>/</span>
          <span>${escapeHtml(page.title)}</span>
        </div>

        <div class="detail-title-row">
          <div>
            <div class="eyebrow">${escapeHtml(kindLabel(page.kind).toUpperCase())}</div>
            <h1>${escapeHtml(page.title)}</h1>
            <div class="detail-label">${escapeHtml(page.label || page.route)}</div>
          </div>
          <span class="kind-icon" aria-hidden="true">${kindIcons[page.kind] || "•"}</span>
        </div>

        <p class="detail-benefit">${escapeHtml(page.benefit)}</p>

        <ul class="detail-list">
          ${page.details.map(detail => `<li>${escapeHtml(detail)}</li>`).join("")}
        </ul>

        <div class="flow-track" aria-label="تدفق الصفحة">
          ${page.flow.map((node, index) => `
            <span class="flow-node">${escapeHtml(node)}</span>
            ${index < page.flow.length - 1 ? '<span class="flow-arrow">→</span>' : ""}
          `).join("")}
        </div>

        <div class="source-box">
          <small>المصدر داخل المستودع</small>
          <code>${escapeHtml(page.source)}</code>
          <a class="ghost-button" href="${sourceUrl(page)}" target="_blank" rel="noreferrer">فتح الملف على GitHub ↗</a>
        </div>

        <div class="runtime-warning">${escapeHtml(runtimeWarning)}</div>
      </article>

      <aside class="preview-panel">
        ${renderPreview(page)}
      </aside>
    </section>

    <section class="section-block">
      <div class="section-heading">
        <div>
          <div class="eyebrow">RELATED IN THE SAME SECTION</div>
          <h2>صفحات مرتبطة</h2>
        </div>
      </div>
      <div class="cards-grid">
        ${state.manifest.pages
          .filter(item => item.group === page.group && item.id !== page.id)
          .slice(0, 3)
          .map(pageCard)
          .join("")}
      </div>
    </section>
  `;

  root.querySelector("[data-back-group]")?.addEventListener("click", () => {
    location.hash = page.group === "overview" ? "#/overview-home" : `#/group/${page.group}`;
  });
  bindPageCards();
}

function pageCard(page) {
  return `
    <article class="page-card" tabindex="0" role="link" data-page-id="${escapeHtml(page.id)}">
      <div class="card-head">
        <span class="kind-icon" aria-hidden="true">${kindIcons[page.kind] || "•"}</span>
        <div class="card-badges">
          <span class="badge ${escapeHtml(page.runtime)}">${escapeHtml(runtimeLabels[page.runtime] || page.runtime)}</span>
          <span class="badge">${escapeHtml(kindLabel(page.kind))}</span>
        </div>
      </div>
      <h3>${escapeHtml(page.title)}</h3>
      <div class="route">${escapeHtml(page.label || page.route)}</div>
      <p>${escapeHtml(page.benefit)}</p>
      <div class="card-footer">
        <span class="source-path">${escapeHtml(page.source)}</span>
        <span class="card-arrow" aria-hidden="true">←</span>
      </div>
    </article>`;
}

function groupCard(group) {
  const pages = state.manifest.pages.filter(page => page.group === group.id);
  return `
    <article class="page-card" tabindex="0" role="link" data-group-id="${escapeHtml(group.id)}">
      <div class="card-head">
        <span class="kind-icon">${escapeHtml(group.icon)}</span>
        <span class="badge">${pages.length} عناصر</span>
      </div>
      <h3>${escapeHtml(group.title)}</h3>
      <p>${escapeHtml(group.summary)}</p>
      <div class="card-footer">
        <span class="source-path">${pages.slice(0, 3).map(page => page.label).join(" · ")}</span>
        <span class="card-arrow">←</span>
      </div>
    </article>`;
}

function bindPageCards() {
  root.querySelectorAll("[data-page-id]").forEach(card => {
    const open = () => location.hash = `#/${card.dataset.pageId}`;
    card.addEventListener("click", open);
    card.addEventListener("keydown", event => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        open();
      }
    });
  });

  root.querySelectorAll("[data-group-id]").forEach(card => {
    const open = () => location.hash = `#/group/${card.dataset.groupId}`;
    card.addEventListener("click", open);
    card.addEventListener("keydown", event => {
      if (event.key === "Enter" || event.key === " ") {
        event.preventDefault();
        open();
      }
    });
  });
}

function renderPreview(page) {
  const address = `${page.app ? `${page.app}.local` : "repository"}${page.route.startsWith("/") ? page.route : `/${page.route}`}`;
  return `
    <div class="browser-frame">
      <div class="browser-bar">
        <span class="browser-dot"></span>
        <span class="browser-dot"></span>
        <span class="browser-dot"></span>
        <span class="browser-address">${escapeHtml(address)}</span>
      </div>
      <div class="preview-canvas">
        ${previewBody(page)}
      </div>
    </div>`;
}

function previewBody(page) {
  switch (page.preview) {
    case "overview":
      return `
        <span class="preview-badge">REPOSITORY MAP</span>
        <h3>كور واحد، وأمثلة واضحة، ومنتجات مستقلة.</h3>
        <p class="preview-muted">لا تختلط قواعد المنتج بالحزم العامة.</p>
        <div class="repo-map">
          <div class="repo-node primary"><strong>src/FoundationKit.*</strong><small>Reusable production baseline</small></div>
          <div class="repo-node"><strong>samples/Workbench</strong><small>Architecture learning path</small></div>
          <div class="repo-node"><strong>examples/Athar</strong><small>Complete Arabic product</small></div>
          <div class="repo-node"><strong>apps/</strong><small>Future real products</small></div>
          <div class="repo-node"><strong>tests + deploy</strong><small>Proof and operations</small></div>
        </div>`;

    case "core":
      return `
        <span class="preview-badge">CLEAN DEPENDENCY FLOW</span>
        <h3>${escapeHtml(page.title)} داخل الكور</h3>
        <p class="preview-muted">كل طبقة تعرف ما تحتاجه فقط.</p>
        <div class="layer-stack">
          <div class="layer"><strong>Blazor</strong><small>UI state + typed client</small></div>
          <div class="layer"><strong>WebApi</strong><small>HTTP + Problem Details</small></div>
          <div class="layer"><strong>Infrastructure</strong><small>EF adapters + events</small></div>
          <div class="layer"><strong>Application</strong><small>Use cases + contracts</small></div>
          <div class="layer"><strong>Domain</strong><small>Business truth</small></div>
        </div>`;

    case "workbench-home":
      return `
        <span class="preview-badge">DUAL FULL STACK</span>
        <h3>مساران يلتقيان في حالة واحدة</h3>
        <div class="preview-grid">
          <div class="preview-card"><strong>User Full Stack</strong><small>إنشاء الطلب ومتابعته</small></div>
          <div class="preview-card"><strong>Admin Full Stack</strong><small>قائمة العمل والقرار</small></div>
          <div class="preview-card"><strong>Shared Status</strong><small>submitted → approved</small></div>
          <div class="preview-card"><strong>SQL Server</strong><small>BuildBriefs + AdminReviews</small></div>
        </div>
        <div class="flow-track"><span class="flow-node">User</span><span class="flow-arrow">→</span><span class="flow-node">Workflow</span><span class="flow-arrow">→</span><span class="flow-node">Admin</span></div>`;

    case "workbench-user":
      return `
        <span class="preview-badge">USER PORTAL</span>
        <h3>طلب مشروع جديد</h3>
        <p class="preview-muted">نموذج مطابق للعقد مع معاينة JSON.</p>
        <div class="preview-form">
          <div class="fake-input"></div><div class="fake-input"></div><div class="fake-input large"></div>
          <div class="preview-grid"><div class="preview-card"><strong>Domain Events</strong><small>✓ selected</small></div><div class="preview-card"><strong>Typed Results</strong><small>+ add</small></div></div>
          <div class="fake-button">إرسال الطلب</div>
        </div>`;

    case "workbench-admin":
      return `
        <span class="preview-badge">ADMIN QUEUE</span>
        <h3>طلبات بانتظار القرار</h3>
        <div class="preview-list">
          ${previewRow("منصة عمليات الفروع", "بانتظار المراجعة")}
          ${previewRow("نظام خدمة العملاء", "بانتظار المراجعة")}
        </div>
        <div class="preview-grid">
          <div class="fake-button">اعتماد</div><div class="fake-button secondary">رفض</div>
        </div>`;

    case "athar-home":
      return `
        <span class="preview-badge">من الفكرة إلى الأثر</span>
        <h3>مبادرات مجتمعية بمسار قرار واضح</h3>
        <p class="preview-muted">تسجيل، إرسال، مراجعة، ثم نتيجة قابلة للتتبع.</p>
        <div class="repo-map">
          <div class="repo-node primary"><strong>أثَر</strong><small>Arabic full-stack product</small></div>
          <div class="repo-node"><strong>فكرة</strong><small>بيانات المبادرة</small></div>
          <div class="repo-node"><strong>مراجعة</strong><small>Admin decision</small></div>
          <div class="repo-node"><strong>تدقيق</strong><small>Audit trail</small></div>
          <div class="repo-node"><strong>نتيجة</strong><small>User visibility</small></div>
        </div>`;

    case "athar-account":
      return `
        <span class="preview-badge">IDENTITY + COOKIE</span>
        <h3>ابدأ حسابك في أثَر</h3>
        <div class="preview-form">
          <div class="fake-input"></div><div class="fake-input"></div><div class="fake-input"></div>
          <div class="fake-button">إنشاء الحساب</div>
        </div>
        <div class="code-window">CSRF token ✓\nPassword policy ✓\nLockout policy ✓\nSecure cookie ✓</div>`;

    case "athar-initiatives":
      return `
        <span class="preview-badge">USER WORKSPACE</span>
        <h3>مبادراتي</h3>
        <div class="preview-grid">
          <div class="preview-form"><div class="fake-input"></div><div class="fake-input large"></div><div class="fake-button">إرسال المبادرة</div></div>
          <div class="preview-list">${previewRow("مختبر تعلم متنقل", "قيد المراجعة")}${previewRow("حديقة الحي", "معتمدة")}</div>
        </div>`;

    case "athar-admin":
      return `
        <span class="preview-badge">ADMIN DECISION CENTER</span>
        <h3>مركز قرار المبادرات</h3>
        <div class="metric-row"><div class="metric"><strong>8</strong><small>قيد المراجعة</small></div><div class="metric"><strong>21</strong><small>معتمدة</small></div><div class="metric"><strong>4,280</strong><small>مستفيد</small></div></div>
        <div class="preview-list">${previewRow("مختبر تعلم متنقل", "قيد المراجعة")}</div>
        <div class="preview-form"><div class="fake-input large"></div><div class="preview-grid"><div class="fake-button">اعتماد</div><div class="fake-button secondary">رفض</div></div></div>`;

    case "api":
      return `
        <span class="preview-badge">OPENAPI</span>
        <h3>عقود يمكن فحصها وتجربتها</h3>
        <div class="code-window">GET  /swagger/v1/swagger.json\nPOST /api/v1/auth/login\nPOST /api/v1/initiatives\nGET  /api/v1/admin/initiatives\nPOST /api/v1/admin/initiatives/{id}/review</div>`;

    case "security":
      return `
        <span class="preview-badge">ANTI-CSRF</span>
        <h3>كل كتابة تبدأ برمز جلسة صحيح</h3>
        <div class="pipeline"><div class="pipeline-step" data-step="1">GET security/antiforgery</div><div class="pipeline-step" data-step="2">حفظ Cookie + Request Token</div><div class="pipeline-step" data-step="3">إرسال X-CSRF-TOKEN</div><div class="pipeline-step" data-step="4">التحقق قبل التنفيذ</div></div>`;

    case "health":
      return `
        <span class="preview-badge">HEALTH PROBE</span>
        <h3>هل التطبيق حي وجاهز؟</h3>
        <div class="code-window">HTTP/1.1 200 OK\n{\n  "status": "ready",\n  "database": "connected",\n  "service": "${escapeHtml(page.app || "foundationkit")}"\n}</div>`;

    case "pipeline":
      return `
        <span class="preview-badge">AUTOMATION</span>
        <h3>من Commit إلى نتيجة قابلة للثقة</h3>
        <div class="pipeline"><div class="pipeline-step" data-step="1">تحقق من حدود المستودع</div><div class="pipeline-step" data-step="2">Restore + Release Build</div><div class="pipeline-step" data-step="3">Unit Tests + Publish</div><div class="pipeline-step" data-step="4">Docker + SQL Server Smoke</div><div class="pipeline-step" data-step="5">Artifact / Pages Deployment</div></div>`;

    case "document":
      return `
        <span class="preview-badge">DOCUMENTATION</span>
        <h3>${escapeHtml(page.title)}</h3>
        <p class="preview-muted">شرح قابل للقراءة مع رابط مباشر إلى المصدر.</p>
        <div class="code-window"># ${escapeHtml(page.label)}\n\n✓ لماذا يوجد هذا الجزء؟\n✓ أين يقع داخل الحل؟\n✓ ما حدوده واعتماداته؟\n✓ كيف تشغله وتتحقق منه؟</div>`;

    case "tool":
      return `
        <span class="preview-badge">EXECUTABLE PROOF</span>
        <h3>${escapeHtml(page.title)}</h3>
        <div class="pipeline"><div class="pipeline-step" data-step="1">إعداد البيئة</div><div class="pipeline-step" data-step="2">تشغيل الخدمة</div><div class="pipeline-step" data-step="3">تنفيذ السيناريو</div><div class="pipeline-step" data-step="4">فحص النتيجة والتنظيف</div></div>`;

    default:
      return `<span class="preview-badge">FOUNDATIONKIT</span><h3>${escapeHtml(page.title)}</h3><p class="preview-muted">${escapeHtml(page.benefit)}</p>`;
  }
}

function previewRow(title, status) {
  return `<div class="preview-row"><span><strong>${escapeHtml(title)}</strong><small>تفاصيل مترابطة مع قاعدة البيانات</small></span><em class="status-pill">${escapeHtml(status)}</em></div>`;
}

function openSearch() {
  if (!state.manifest) return;
  searchOverlay.hidden = false;
  paletteInput.value = searchInput.value;
  renderPalette(paletteInput.value);
  requestAnimationFrame(() => paletteInput.focus());
}

function closeSearch() {
  searchOverlay.hidden = true;
  searchInput.value = "";
}

function renderPalette(query) {
  if (!state.manifest) return;
  const normalized = normalize(query);
  const pages = state.manifest.pages.filter(page => {
    if (!normalized) return true;
    const haystack = normalize([
      page.title,
      page.label,
      page.route,
      page.source,
      page.benefit,
      ...page.details
    ].join(" "));
    return haystack.includes(normalized);
  }).slice(0, 15);

  paletteResults.innerHTML = pages.length
    ? pages.map(page => `
        <button class="palette-item" type="button" data-palette-id="${escapeHtml(page.id)}">
          <span class="kind-icon">${kindIcons[page.kind] || "•"}</span>
          <span><strong>${escapeHtml(page.title)}</strong><small>${escapeHtml(page.benefit)}</small></span>
          <code>${escapeHtml(page.label || page.route)}</code>
        </button>`).join("")
    : '<div class="empty-results">لا توجد نتيجة مطابقة. جرّب: Domain أو مبادرات أو Swagger.</div>';

  paletteResults.querySelectorAll("[data-palette-id]").forEach(button => {
    button.addEventListener("click", () => {
      closeSearch();
      location.hash = `#/${button.dataset.paletteId}`;
    });
  });
}

function closeMobileMenu() {
  sidebar.classList.remove("open");
  menuButton.setAttribute("aria-expanded", "false");
}

function statCard(value, label) {
  return `<div class="stat-card"><strong>${value}</strong><small>${escapeHtml(label)}</small></div>`;
}

function kindLabel(kind) {
  return ({
    ui: "واجهة",
    api: "API",
    package: "حزمة",
    document: "وثيقة",
    guide: "دليل",
    automation: "أتمتة",
    tool: "أداة"
  })[kind] || kind;
}

function sourceUrl(page) {
  const path = page.source.replace(/^\/+/, "");
  const fileLike = /\.[a-z0-9]+$/i.test(path);
  return `https://github.com/a2sn2/foundationkit-dotnet/${fileLike ? "blob" : "tree"}/main/${encodeURI(path)}`;
}

function normalize(value) {
  return String(value || "")
    .toLowerCase()
    .normalize("NFKD")
    .replace(/[ًٌٍَُِّْـ]/g, "")
    .trim();
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

initialize();
