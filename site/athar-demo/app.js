"use strict";

const STORAGE_KEY = "foundationkit-athar-browser-demo-v1";

const statusLabels = {
  submitted: "قيد المراجعة",
  approved: "معتمدة",
  rejected: "مرفوضة"
};

const defaultState = () => ({
  version: 1,
  currentView: "user",
  pendingOnly: false,
  initiatives: [
    {
      id: crypto.randomUUID(),
      title: "مختبر تعلم متنقل",
      description: "مبادرة تقدم جلسات تعليم رقمية وعملية للطلاب في المناطق البعيدة باستخدام حقيبة تقنية متنقلة ومدربين متطوعين.",
      category: "تعليم",
      city: "صنعاء",
      budget: 25000,
      beneficiaries: 320,
      status: "submitted",
      createdAt: new Date().toISOString(),
      reviewedAt: null,
      reviewedBy: null,
      reviewNotes: null
    }
  ]
});

let state = loadState();
let toastTimer;

const elements = {
  views: {
    user: document.getElementById("userView"),
    admin: document.getElementById("adminView"),
    about: document.getElementById("aboutView")
  },
  viewButtons: [...document.querySelectorAll("[data-view]")],
  initiativeForm: document.getElementById("initiativeForm"),
  userInitiatives: document.getElementById("userInitiatives"),
  adminInitiatives: document.getElementById("adminInitiatives"),
  userCount: document.getElementById("userCount"),
  adminMetrics: document.getElementById("adminMetrics"),
  resetDemo: document.getElementById("resetDemo"),
  showPendingOnly: document.getElementById("showPendingOnly"),
  emptyTemplate: document.getElementById("emptyTemplate"),
  toast: document.getElementById("toast")
};

function loadState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return defaultState();
    }

    const parsed = JSON.parse(raw);
    if (parsed?.version !== 1 || !Array.isArray(parsed.initiatives)) {
      return defaultState();
    }

    return parsed;
  }
  catch {
    return defaultState();
  }
}

function saveState() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function formatNumber(value) {
  return new Intl.NumberFormat("ar", { maximumFractionDigits: 0 }).format(Number(value) || 0);
}

function formatDate(value) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("ar", {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function statusClass(status) {
  return `status status-${status}`;
}

function cloneEmptyState() {
  return elements.emptyTemplate.content.cloneNode(true);
}

function showToast(message) {
  clearTimeout(toastTimer);
  elements.toast.textContent = message;
  elements.toast.classList.add("show");
  toastTimer = setTimeout(() => elements.toast.classList.remove("show"), 3200);
}

function setView(view) {
  if (!elements.views[view]) {
    return;
  }

  state.currentView = view;
  saveState();

  for (const [name, section] of Object.entries(elements.views)) {
    section.classList.toggle("active", name === view);
  }

  for (const button of elements.viewButtons) {
    button.classList.toggle("active", button.dataset.view === view);
  }

  if (view === "admin") {
    renderAdmin();
  }

  window.scrollTo({ top: document.querySelector(".hero").offsetHeight - 30, behavior: "smooth" });
}

function initiativeSummary(initiative) {
  return `
    <div class="card-topline">
      <div>
        <h4>${escapeHtml(initiative.title)}</h4>
        <small>أُنشئت في ${formatDate(initiative.createdAt)}</small>
      </div>
      <span class="${statusClass(initiative.status)}">${statusLabels[initiative.status] ?? initiative.status}</span>
    </div>
    <p class="card-description">${escapeHtml(initiative.description)}</p>
    <div class="meta-grid">
      <span>${escapeHtml(initiative.category)}</span>
      <span>${escapeHtml(initiative.city)}</span>
      <span>ميزانية ${formatNumber(initiative.budget)}</span>
      <span>${formatNumber(initiative.beneficiaries)} مستفيد</span>
    </div>
  `;
}

function reviewSummary(initiative) {
  if (!initiative.reviewedAt) {
    return "";
  }

  return `
    <div class="review-note">
      <strong>قرار الإدارة · ${escapeHtml(initiative.reviewedBy || "مسؤول منصة أثَر")}</strong>
      <span>${escapeHtml(initiative.reviewNotes || "تم تسجيل القرار دون ملاحظات إضافية.")}</span>
      <span>${formatDate(initiative.reviewedAt)}</span>
    </div>
  `;
}

function renderUser() {
  elements.userCount.textContent = String(state.initiatives.length);
  elements.userInitiatives.replaceChildren();

  if (state.initiatives.length === 0) {
    elements.userInitiatives.appendChild(cloneEmptyState());
    return;
  }

  const sorted = [...state.initiatives].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

  for (const initiative of sorted) {
    const card = document.createElement("article");
    card.className = "initiative-card";
    card.innerHTML = initiativeSummary(initiative) + reviewSummary(initiative);
    elements.userInitiatives.appendChild(card);
  }
}

function renderMetrics() {
  const submitted = state.initiatives.filter(item => item.status === "submitted").length;
  const approved = state.initiatives.filter(item => item.status === "approved").length;
  const rejected = state.initiatives.filter(item => item.status === "rejected").length;
  const approvedBeneficiaries = state.initiatives
    .filter(item => item.status === "approved")
    .reduce((sum, item) => sum + Number(item.beneficiaries || 0), 0);

  const metrics = [
    ["قيد المراجعة", submitted],
    ["معتمدة", approved],
    ["مرفوضة", rejected],
    ["مستفيدون معتمدون", formatNumber(approvedBeneficiaries)]
  ];

  elements.adminMetrics.innerHTML = metrics
    .map(([label, value]) => `<div class="metric"><span>${label}</span><strong>${value}</strong></div>`)
    .join("");
}

function renderAdmin() {
  renderMetrics();
  elements.showPendingOnly.setAttribute("aria-pressed", String(state.pendingOnly));
  elements.adminInitiatives.replaceChildren();

  const filtered = state.pendingOnly
    ? state.initiatives.filter(item => item.status === "submitted")
    : state.initiatives;

  if (filtered.length === 0) {
    elements.adminInitiatives.appendChild(cloneEmptyState());
    return;
  }

  const sorted = [...filtered].sort((a, b) => {
    if (a.status === "submitted" && b.status !== "submitted") return -1;
    if (a.status !== "submitted" && b.status === "submitted") return 1;
    return new Date(b.createdAt) - new Date(a.createdAt);
  });

  for (const initiative of sorted) {
    const card = document.createElement("article");
    card.className = "admin-card";

    const locked = initiative.status !== "submitted";
    card.innerHTML = `
      <div>
        ${initiativeSummary(initiative)}
        ${reviewSummary(initiative)}
      </div>
      <div class="review-box">
        <label>
          <span>ملاحظة القرار</span>
          <textarea data-note-for="${initiative.id}" rows="4" ${locked ? "disabled" : ""} placeholder="اكتب سببًا واضحًا للقرار...">${escapeHtml(initiative.reviewNotes || "")}</textarea>
        </label>
        <div class="decision-row">
          <button class="decision-button approve" type="button" data-action="approve" data-id="${initiative.id}" ${locked ? "disabled" : ""}>اعتماد</button>
          <button class="decision-button reject" type="button" data-action="reject" data-id="${initiative.id}" ${locked ? "disabled" : ""}>رفض</button>
        </div>
        ${locked ? `<small>تم إغلاق القرار في ${formatDate(initiative.reviewedAt)}</small>` : "<small>القرار ينعكس فورًا في مساحة المستخدم.</small>"}
      </div>
    `;

    elements.adminInitiatives.appendChild(card);
  }
}

function renderAll() {
  renderUser();
  renderAdmin();
  setView(state.currentView || "user");
}

function createInitiative(form) {
  const data = new FormData(form);
  const initiative = {
    id: crypto.randomUUID(),
    title: String(data.get("title") || "").trim(),
    description: String(data.get("description") || "").trim(),
    category: String(data.get("category") || "").trim(),
    city: String(data.get("city") || "").trim(),
    budget: Number(data.get("budget") || 0),
    beneficiaries: Number(data.get("beneficiaries") || 0),
    status: "submitted",
    createdAt: new Date().toISOString(),
    reviewedAt: null,
    reviewedBy: null,
    reviewNotes: null
  };

  state.initiatives.push(initiative);
  saveState();
  form.reset();
  form.elements.city.value = "صنعاء";
  form.elements.budget.value = "25000";
  form.elements.beneficiaries.value = "320";
  renderAll();
  showToast("تم إرسال المبادرة، وأصبحت ظاهرة في لوحة الإدارة.");
}

function reviewInitiative(id, decision) {
  const initiative = state.initiatives.find(item => item.id === id);
  if (!initiative || initiative.status !== "submitted") {
    showToast("لا يمكن تعديل قرار مبادرة تمت مراجعتها مسبقًا.");
    return;
  }

  const noteField = document.querySelector(`[data-note-for="${CSS.escape(id)}"]`);
  const note = String(noteField?.value || "").trim();

  if (note.length < 5) {
    showToast("اكتب ملاحظة قرار واضحة من خمسة أحرف على الأقل.");
    noteField?.focus();
    return;
  }

  initiative.status = decision === "approve" ? "approved" : "rejected";
  initiative.reviewedAt = new Date().toISOString();
  initiative.reviewedBy = "مسؤول منصة أثَر";
  initiative.reviewNotes = note;

  saveState();
  renderAll();
  setView("admin");
  showToast(decision === "approve" ? "تم اعتماد المبادرة." : "تم رفض المبادرة.");
}

for (const button of elements.viewButtons) {
  button.addEventListener("click", () => setView(button.dataset.view));
}

elements.initiativeForm.addEventListener("submit", event => {
  event.preventDefault();

  if (!elements.initiativeForm.reportValidity()) {
    return;
  }

  createInitiative(elements.initiativeForm);
});

elements.adminInitiatives.addEventListener("click", event => {
  const button = event.target.closest("[data-action][data-id]");
  if (!button) {
    return;
  }

  reviewInitiative(button.dataset.id, button.dataset.action);
});

elements.showPendingOnly.addEventListener("click", () => {
  state.pendingOnly = !state.pendingOnly;
  saveState();
  renderAdmin();
});

elements.resetDemo.addEventListener("click", () => {
  const confirmed = window.confirm("سيتم حذف بيانات التجربة المخزنة في هذا المتصفح وإعادتها للبداية. هل تريد المتابعة؟");
  if (!confirmed) {
    return;
  }

  state = defaultState();
  saveState();
  renderAll();
  showToast("أُعيدت التجربة إلى حالتها الأولى.");
});

renderAll();
