(() => {
  "use strict";

  const state = {
    catalog: null,
    runtime: null,
    activePackageIndex: 0,
    currentStep: 1,
    selectedIdeaId: null,
    lastSummary: ""
  };

  const elements = {
    runtimeBadge: document.querySelector("#runtime-badge"),
    packageCount: document.querySelector("#package-count"),
    capabilityCount: document.querySelector("#capability-count"),
    ideaCount: document.querySelector("#idea-count"),
    packageTabs: document.querySelector("#package-tabs"),
    packagePanel: document.querySelector("#package-panel"),
    ideasGrid: document.querySelector("#ideas-grid"),
    adoptionSteps: document.querySelector("#adoption-steps"),
    ideaOptions: document.querySelector("#idea-options"),
    capabilityOptions: document.querySelector("#capability-options"),
    persistenceNote: document.querySelector("#persistence-note"),
    form: document.querySelector("#brief-form"),
    previous: document.querySelector("#previous-step"),
    next: document.querySelector("#next-step"),
    submit: document.querySelector("#submit-brief"),
    error: document.querySelector("#form-error"),
    result: document.querySelector("#brief-result"),
    resultStatus: document.querySelector("#result-status"),
    resultSummary: document.querySelector("#result-summary"),
    contactLink: document.querySelector("#contact-link"),
    copySummary: document.querySelector("#copy-summary"),
    startOver: document.querySelector("#start-over")
  };

  document.addEventListener("DOMContentLoaded", initialize);

  async function initialize() {
    state.runtime = await detectRuntime();
    updateRuntimeUi();

    try {
      state.catalog = await loadCatalog();
      renderAll();
      bindEvents();
    } catch (error) {
      console.error(error);
      elements.runtimeBadge.className = "runtime-badge demo";
      elements.runtimeBadge.innerHTML = "<span class=\"pulse\"></span><span>تعذر تحميل كتالوج FoundationKit</span>";
    }
  }

  async function detectRuntime() {
    try {
      const response = await fetch("api/runtime", { cache: "no-store" });
      if (response.ok) {
        const runtime = await response.json();
        if (runtime.mode === "local") return runtime;
      }
    } catch {
      // Static GitHub Pages intentionally has no API runtime.
    }

    return { mode: "demo", persistence: "none" };
  }

  async function loadCatalog() {
    const url = state.runtime.mode === "local"
      ? "api/catalog"
      : "catalog/foundationkit.catalog.json";
    const response = await fetch(url, { cache: "no-store" });
    if (!response.ok) throw new Error(`Catalog request failed: ${response.status}`);
    return response.json();
  }

  function updateRuntimeUi() {
    const local = state.runtime.mode === "local";
    elements.runtimeBadge.className = `runtime-badge ${local ? "local" : "demo"}`;
    elements.runtimeBadge.innerHTML = local
      ? "<span class=\"pulse\"></span><span>وضع محلي — ASP.NET Core وSQL Server متصلان</span>"
      : "<span class=\"pulse\"></span><span>GitHub Pages Demo — واجهة ثابتة دون Backend أو قاعدة بيانات</span>";
    elements.persistenceNote.innerHTML = local
      ? "<strong>الحفظ فعّال.</strong><br>سيُحفظ الملخص في قاعدة FoundationKitWorkbench على SQL Server المحلي."
      : "<strong>وضع العرض فقط.</strong><br>لن تُرسل الإجابات أو تُحفظ حتى تضغط زر التواصل الخارجي بنفسك.";
  }

  function renderAll() {
    const capabilities = state.catalog.packages.flatMap(packageItem => packageItem.capabilities);
    elements.packageCount.textContent = state.catalog.packages.length;
    elements.capabilityCount.textContent = capabilities.length;
    elements.ideaCount.textContent = state.catalog.ideas.length;
    renderPackages();
    renderIdeas();
    renderAdoptionSteps();
    renderIdeaOptions();
    renderCapabilityOptions();
  }

  function renderPackages() {
    elements.packageTabs.innerHTML = state.catalog.packages.map((packageItem, index) => `
      <button class="package-tab ${index === state.activePackageIndex ? "active" : ""}"
              type="button" role="tab"
              aria-selected="${index === state.activePackageIndex}"
              data-package-index="${index}">
        ${escapeHtml(packageItem.packageId.replace("FoundationKit.", ""))}
      </button>`).join("");

    const packageItem = state.catalog.packages[state.activePackageIndex];
    elements.packagePanel.innerHTML = `
      <article class="package-overview">
        <span class="package-code">${escapeHtml(packageItem.packageId)}</span>
        <h3>${escapeHtml(packageItem.titleAr)}</h3>
        <p>${escapeHtml(packageItem.summaryAr)}</p>
        <span class="implemented">● ${packageItem.capabilities.length} قدرات منفذة</span>
      </article>
      <div class="capabilities-grid">
        ${packageItem.capabilities.map(capability => `
          <article>
            <h4>${escapeHtml(capability.titleAr)}</h4>
            <p>${escapeHtml(capability.descriptionAr)}</p>
            <div class="public-types">
              ${capability.publicTypes.map(type => `<code>${escapeHtml(type)}</code>`).join("")}
            </div>
          </article>`).join("")}
      </div>`;
  }

  function renderIdeas() {
    elements.ideasGrid.innerHTML = state.catalog.ideas.map(idea => `
      <article class="idea-card" data-idea-id="${escapeHtml(idea.id)}">
        <span class="idea-icon">${escapeHtml(idea.icon)}</span>
        <h3>${escapeHtml(idea.titleAr)}</h3>
        <p>${escapeHtml(idea.descriptionAr)}</p>
        <button type="button" data-use-idea="${escapeHtml(idea.id)}">استخدم هذه الفكرة ←</button>
      </article>`).join("");
  }

  function renderAdoptionSteps() {
    elements.adoptionSteps.innerHTML = state.catalog.adoptionSteps.map(step => `
      <article class="adoption-step">
        <span class="adoption-number">${step.number}</span>
        <div><h3>${escapeHtml(step.titleAr)}</h3><p>${escapeHtml(step.descriptionAr)}</p></div>
        ${step.command ? `<code>${escapeHtml(step.command)}</code>` : ""}
      </article>`).join("");
  }

  function renderIdeaOptions() {
    const options = [...state.catalog.ideas, {
      id: "custom", titleAr: "فكرة أخرى", descriptionAr: "اكتب نوع المشروع والهدف بنفسك."
    }];
    elements.ideaOptions.innerHTML = options.map((idea, index) => `
      <div class="idea-option">
        <input type="radio" id="idea-${escapeHtml(idea.id)}" name="idea" value="${escapeHtml(idea.id)}" ${index === 0 ? "checked" : ""}>
        <label for="idea-${escapeHtml(idea.id)}"><strong>${escapeHtml(idea.titleAr)}</strong><small>${escapeHtml(idea.descriptionAr)}</small></label>
      </div>`).join("");
    selectIdea(options[0].id);
  }

  function renderCapabilityOptions() {
    const capabilities = state.catalog.packages.flatMap(packageItem =>
      packageItem.capabilities.map(capability => ({ ...capability, packageId: packageItem.packageId })));
    elements.capabilityOptions.innerHTML = capabilities.map(capability => `
      <div class="capability-option">
        <input type="checkbox" id="cap-${escapeHtml(capability.id)}" value="${escapeHtml(capability.id)}">
        <label for="cap-${escapeHtml(capability.id)}"><strong>${escapeHtml(capability.titleAr)}</strong><small>${escapeHtml(capability.packageId)}</small></label>
      </div>`).join("");
    applyRecommendedCapabilities();
  }

  function bindEvents() {
    elements.packageTabs.addEventListener("click", event => {
      const button = event.target.closest("[data-package-index]");
      if (!button) return;
      state.activePackageIndex = Number(button.dataset.packageIndex);
      renderPackages();
    });
    document.addEventListener("click", event => {
      const useIdeaButton = event.target.closest("[data-use-idea]");
      if (!useIdeaButton) return;
      selectIdea(useIdeaButton.dataset.useIdea);
      document.querySelector("#builder").scrollIntoView({ behavior: "smooth" });
    });
    elements.ideaOptions.addEventListener("change", event => {
      if (event.target.name === "idea") selectIdea(event.target.value);
    });
    elements.next.addEventListener("click", () => {
      if (!validateStep(state.currentStep)) return;
      setStep(state.currentStep + 1);
    });
    elements.previous.addEventListener("click", () => setStep(state.currentStep - 1));
    elements.form.addEventListener("submit", submitBrief);
    elements.copySummary.addEventListener("click", copySummary);
    elements.startOver.addEventListener("click", resetBuilder);
  }

  function selectIdea(ideaId) {
    state.selectedIdeaId = ideaId;
    const input = document.querySelector(`#idea-${cssEscape(ideaId)}`);
    if (input) input.checked = true;
    const idea = state.catalog?.ideas.find(item => item.id === ideaId);
    if (idea) {
      document.querySelector("#project-type").value = idea.titleAr;
      applyRecommendedCapabilities(idea.recommendedCapabilityIds);
    } else {
      applyRecommendedCapabilities([]);
    }
  }

  function applyRecommendedCapabilities(recommendedIds) {
    if (!state.catalog || !elements.capabilityOptions.children.length) return;
    const idea = state.catalog.ideas.find(item => item.id === state.selectedIdeaId);
    const ids = new Set(recommendedIds ?? idea?.recommendedCapabilityIds ?? []);
    elements.capabilityOptions.querySelectorAll("input[type=checkbox]").forEach(input => {
      input.checked = ids.has(input.value);
    });
  }

  function setStep(nextStep) {
    state.currentStep = Math.min(4, Math.max(1, nextStep));
    document.querySelectorAll(".form-step").forEach(step => {
      step.classList.toggle("active", Number(step.dataset.step) === state.currentStep);
    });
    document.querySelectorAll("[data-progress]").forEach(item => {
      const progress = Number(item.dataset.progress);
      item.classList.toggle("active", progress === state.currentStep);
      item.classList.toggle("complete", progress < state.currentStep);
    });
    elements.previous.classList.toggle("hidden", state.currentStep === 1);
    elements.next.classList.toggle("hidden", state.currentStep === 4);
    elements.submit.classList.toggle("hidden", state.currentStep !== 4);
    elements.error.textContent = "";
  }

  function validateStep(step) {
    const selectorsByStep = { 1: ["#project-name", "#project-type"], 2: ["#audience", "#goal"] };
    for (const selector of selectorsByStep[step] ?? []) {
      const field = document.querySelector(selector);
      if (!field.value.trim()) {
        elements.error.textContent = "أكمل الحقول المطلوبة قبل المتابعة.";
        field.focus();
        return false;
      }
    }
    if (step === 2 && document.querySelector("#goal").value.trim().length < 10) {
      elements.error.textContent = "اكتب هدفًا أوضح لا يقل عن 10 أحرف.";
      document.querySelector("#goal").focus();
      return false;
    }
    elements.error.textContent = "";
    return true;
  }

  async function submitBrief(event) {
    event.preventDefault();
    if (!validateStep(1) || !validateStep(2)) return;
    elements.submit.disabled = true;
    elements.submit.textContent = "جارٍ تجهيز الملخص...";
    elements.error.textContent = "";
    const payload = collectPayload();
    let responsePayload = null;
    try {
      if (state.runtime.mode === "local") {
        const response = await fetch("api/build-briefs", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });
        if (!response.ok) {
          const problem = await response.json().catch(() => null);
          throw new Error(problem?.detail || "تعذر حفظ الملخص في SQL Server.");
        }
        responsePayload = await response.json();
      }
      const contactUrl = responsePayload?.contactUrl || buildContactUrl(payload);
      state.lastSummary = buildSummary(payload, responsePayload?.id);
      elements.resultSummary.textContent = state.lastSummary;
      elements.contactLink.href = contactUrl;
      elements.resultStatus.textContent = state.runtime.mode === "local"
        ? `تم حفظ الملخص محليًا في SQL Server${responsePayload?.id ? ` بالمعرّف ${responsePayload.id}` : ""}.`
        : "هذا Demo ثابت؛ لم تُرسل البيانات ولم تُحفظ. يمكنك نسخ الملخص أو فتح رابط التواصل.";
      elements.form.classList.add("hidden");
      elements.result.classList.remove("hidden");
    } catch (error) {
      elements.error.textContent = error.message || "حدث خطأ غير متوقع.";
    } finally {
      elements.submit.disabled = false;
      elements.submit.textContent = "إنشاء الملخص";
    }
  }

  function collectPayload() {
    return {
      projectName: document.querySelector("#project-name").value.trim(),
      projectType: document.querySelector("#project-type").value.trim(),
      audience: document.querySelector("#audience").value.trim(),
      goal: document.querySelector("#goal").value.trim(),
      selectedCapabilityIds: [...elements.capabilityOptions.querySelectorAll("input:checked")].map(input => input.value),
      priorities: document.querySelector("#priorities").value.trim(),
      notes: document.querySelector("#notes").value.trim()
    };
  }

  function buildSummary(payload, id) {
    const capabilityMap = new Map(state.catalog.packages.flatMap(packageItem =>
      packageItem.capabilities.map(capability => [capability.id, capability.titleAr])));
    const selected = payload.selectedCapabilityIds.map(idValue => capabilityMap.get(idValue) || idValue);
    return [
      "ملخص مشروع FoundationKit", "———————————————", id ? `المعرّف المحلي: ${id}` : null,
      `اسم المشروع: ${payload.projectName}`, `النوع: ${payload.projectType}`,
      `المستخدمون: ${payload.audience}`, `الهدف: ${payload.goal}`,
      `القدرات المقترحة: ${selected.length ? selected.join("، ") : "تُحدد بعد التحليل"}`,
      `الأولويات: ${payload.priorities || "غير محددة"}`, `ملاحظات: ${payload.notes || "لا توجد"}`, "",
      "ملاحظة: يحتاج المنتج إلى قرارات مستقلة للهوية والصلاحيات وقاعدة البيانات والتكاملات والتشغيل."
    ].filter(Boolean).join("\n");
  }

  function buildContactUrl(payload) {
    const title = `Build inquiry: ${payload.projectName}`;
    const body = `${buildSummary(payload)}\n\n> This GitHub issue is public. Do not include confidential information.`;
    return `https://github.com/a2sn2/foundationkit-dotnet/issues/new?title=${encodeURIComponent(title)}&body=${encodeURIComponent(body)}`;
  }

  async function copySummary() {
    try {
      await navigator.clipboard.writeText(state.lastSummary);
      elements.copySummary.textContent = "تم النسخ ✓";
      setTimeout(() => { elements.copySummary.textContent = "نسخ الملخص"; }, 1800);
    } catch {
      elements.copySummary.textContent = "تعذر النسخ";
    }
  }

  function resetBuilder() {
    elements.form.reset();
    elements.form.classList.remove("hidden");
    elements.result.classList.add("hidden");
    state.currentStep = 1;
    selectIdea(state.catalog.ideas[0].id);
    setStep(1);
  }

  function escapeHtml(value) {
    return String(value ?? "").replaceAll("&", "&amp;").replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#039;");
  }

  function cssEscape(value) {
    return window.CSS?.escape ? window.CSS.escape(value) : String(value).replace(/[^a-zA-Z0-9_-]/g, "");
  }
})();
