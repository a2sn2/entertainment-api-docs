const form = document.querySelector('#idea-form');
const input = document.querySelector('#idea-input');
const counter = document.querySelector('#idea-counter');
const resultSection = document.querySelector('#result-section');
const ideaOutput = document.querySelector('#idea-output');
const analysisTags = document.querySelector('#analysis-tags');
const layerGrid = document.querySelector('#layer-grid');
const progressFill = document.querySelector('#progress-fill');
const startOver = document.querySelector('#start-over');
const copySummary = document.querySelector('#copy-summary');
const contactLink = document.querySelector('#contact-link');
const currentYear = document.querySelector('#current-year');

const repository = 'a2sn2/foundationkit-dotnet';
let latestSummary = '';

const categories = [
  {
    id: 'ai',
    label: 'AI product',
    terms: ['ذكاء', 'ai', 'agent', 'rag', 'نموذج', 'مساعد', 'تحليل البيانات'],
    focus: ['معرفة ومصادر موثوقة', 'حدود واضحة بين الذكاء وقواعد العمل', 'تقييم ومراقبة جودة النتائج']
  },
  {
    id: 'fintech',
    label: 'Fintech system',
    terms: ['مالي', 'fintech', 'محفظة', 'دفع', 'صرافة', 'تحويل', 'امتثال', 'kyc'],
    focus: ['صلاحيات دقيقة', 'سجل تدقيق', 'سلامة المعاملات والأخطاء']
  },
  {
    id: 'operations',
    label: 'Internal operations',
    terms: ['داخلي', 'موظف', 'مهام', 'عمليات', 'إدارة', 'تقارير', 'workflow', 'صلاحيات'],
    focus: ['أدوار ومسؤوليات', 'سير عمل وحالات', 'لوحات متابعة وتقارير']
  },
  {
    id: 'automation',
    label: 'Automation',
    terms: ['أتمتة', 'automation', 'متكرر', 'تكامل', 'ربط', 'webhook'],
    focus: ['أحداث قابلة لإعادة المحاولة', 'تكاملات معزولة', 'تتبع وفشل آمن']
  },
  {
    id: 'commerce',
    label: 'Digital platform',
    terms: ['متجر', 'منصة', 'اشتراك', 'عميل', 'حجز', 'بيع', 'خدمة'],
    focus: ['رحلة مستخدم واضحة', 'كتالوج أو عروض', 'دفع ودعم وتشغيل']
  }
];

const baseLayers = [
  ['01', 'Domain', 'المفاهيم وقواعد العمل والحالات التي يجب أن تبقى صحيحة دائمًا.', '#9d7bff'],
  ['02', 'Application', 'حالات الاستخدام والأوامر والاستعلامات وحدود تنفيذ كل عملية.', '#2ee6c5'],
  ['03', 'API & Security', 'عقود HTTP والمصادقة والصلاحيات والأخطاء المتوقعة.', '#ffbd66'],
  ['04', 'Data', 'النموذج التخزيني والمهاجرات والتدقيق وسياسة التزامن.', '#69a8ff'],
  ['05', 'Experience', 'واجهة المستخدم والحالات المرئية والوصول والأداء.', '#ff7a95'],
  ['06', 'Quality', 'اختبارات الوحدات والتكامل والمسار الكامل والمراقبة التشغيلية.', '#b7f36b']
];

function normalize(value) {
  return value.trim().replace(/\s+/g, ' ');
}

function analyzeIdea(idea) {
  const haystack = idea.toLocaleLowerCase('ar');
  const ranked = categories
    .map(category => ({
      ...category,
      score: category.terms.reduce((score, term) => score + (haystack.includes(term) ? 1 : 0), 0)
    }))
    .sort((a, b) => b.score - a.score);

  const primary = ranked[0].score > 0 ? ranked[0] : {
    id: 'product',
    label: 'Custom digital product',
    focus: ['فهم المشكلة والمستخدمين', 'حدود المنتج وإصداره الأول', 'تشغيل قابل للقياس والتحسين']
  };

  const secondary = ranked.filter(item => item.score > 0 && item.id !== primary.id).slice(0, 2);
  return { primary, secondary };
}

function renderTags(analysis) {
  analysisTags.replaceChildren();
  [analysis.primary.label, ...analysis.secondary.map(item => item.label), 'FoundationKit candidate']
    .forEach(label => {
      const tag = document.createElement('span');
      tag.textContent = label;
      analysisTags.append(tag);
    });
}

function renderLayers(analysis) {
  layerGrid.replaceChildren();
  const focus = analysis.primary.focus;

  baseLayers.forEach(([index, title, description, color], position) => {
    const card = document.createElement('article');
    card.className = 'layer-card';
    card.style.setProperty('--card-color', color);
    card.style.animationDelay = `${position * 70}ms`;

    const marker = document.createElement('span');
    marker.textContent = index;
    const heading = document.createElement('h3');
    heading.textContent = title;
    const copy = document.createElement('p');
    copy.textContent = position < 3 ? `${description} التركيز الأول: ${focus[position]}.` : description;

    card.append(marker, heading, copy);
    layerGrid.append(card);
  });
}

function buildSummary(idea, analysis) {
  return [
    `Project idea: ${idea}`,
    `Initial classification: ${analysis.primary.label}`,
    'Foundation focus:',
    ...analysis.primary.focus.map(item => `- ${item}`),
    '',
    'Suggested layers: Domain, Application, API & Security, Data, Experience, Quality.',
    'Prepared with the FoundationKit interactive showcase.'
  ].join('\n');
}

function buildContactUrl(idea, analysis) {
  const title = `[Project idea] ${analysis.primary.label}: ${idea.slice(0, 70)}`;
  const params = new URLSearchParams({ template: 'project-idea.yml', title });
  return `https://github.com/${repository}/issues/new?${params}`;
}

async function copyText(text) {
  if (navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(text);
    return;
  }

  const helper = document.createElement('textarea');
  helper.value = text;
  helper.setAttribute('readonly', '');
  helper.style.position = 'fixed';
  helper.style.opacity = '0';
  document.body.append(helper);
  helper.select();
  document.execCommand('copy');
  helper.remove();
}

form.addEventListener('submit', event => {
  event.preventDefault();
  const idea = normalize(input.value);
  if (!idea) {
    input.focus();
    return;
  }

  const analysis = analyzeIdea(idea);
  ideaOutput.textContent = idea;
  renderTags(analysis);
  renderLayers(analysis);
  latestSummary = buildSummary(idea, analysis);
  contactLink.href = buildContactUrl(idea, analysis);

  resultSection.hidden = false;
  requestAnimationFrame(() => {
    progressFill.style.width = '100%';
    resultSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
  });
});

input.addEventListener('input', () => {
  counter.textContent = `${input.value.length} / ${input.maxLength}`;
});

document.querySelectorAll('[data-idea]').forEach(chip => {
  chip.addEventListener('click', () => {
    input.value = chip.dataset.idea ?? '';
    input.dispatchEvent(new Event('input'));
    input.focus();
  });
});

startOver.addEventListener('click', () => {
  resultSection.hidden = true;
  progressFill.style.width = '0';
  input.value = '';
  input.dispatchEvent(new Event('input'));
  input.focus();
  window.scrollTo({ top: 0, behavior: 'smooth' });
});

copySummary.addEventListener('click', async () => {
  if (!latestSummary) return;
  const original = copySummary.textContent;
  try {
    await copyText(latestSummary);
    copySummary.textContent = 'تم النسخ ✓';
  } catch {
    copySummary.textContent = 'تعذر النسخ';
  }
  window.setTimeout(() => { copySummary.textContent = original; }, 1800);
});

currentYear.textContent = new Date().getFullYear().toString();

if ('serviceWorker' in navigator && location.protocol.startsWith('http')) {
  window.addEventListener('load', () => navigator.serviceWorker.register('./sw.js').catch(() => {}));
}
