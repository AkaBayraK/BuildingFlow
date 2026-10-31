// Pure helpers for the QA FAZ 04 browser run; kept apart from the CDP driver so they can be tested without a browser.
export const VIEWPORTS = [
  { name: 'desktop', width: 1280, height: 800, mobile: false },
  { name: 'mobile', width: 375, height: 812, mobile: true },
];
const PARAM = /\[[^\]]+\]/;

// Legacy addresses that intentionally forward; a different destination is still a failure.
export const EXPECTED_REDIRECTS = { '/sites': '/properties' };
const FLAG_ROUTES = [[/^\/workspaces(\/|$)/, 'sessionContexts', 'BUILDINGFLOW_SESSION_CONTEXTS_ENABLED kapalı']];

// Static pages run as-is; /properties/[id]/... use one real property; anything else needs data FAZ 10 provides.
export function plan(pages, propertyId, flags = {}, only = null) {
  return pages.filter(page => page.route !== '/login' && (!only || only.includes(page.route))).map(page => {
    const gated = FLAG_ROUTES.find(([pattern, flag]) => pattern.test(page.route) && !flags[flag]);
    if (gated) return { route: page.route, path: null, skip: gated[2] };
    if (!PARAM.test(page.route)) return { route: page.route, path: page.route, expectedPath: EXPECTED_REDIRECTS[page.route] ?? page.route };
    const property = page.route.match(/^\/properties\/\[id\](\/[a-z-]+)?$/);
    if (property && propertyId) { const path = `/properties/${propertyId}${property[1] ?? ''}`; return { route: page.route, path, expectedPath: path }; }
    return { route: page.route, path: null, skip: propertyId || !property ? 'Örnek kayıt kimliği gerekiyor (FAZ 10 demo verisi)' : 'Yönetim alanı bulunamadı' };
  });
}

export const slug = route => (route.replace(/^\//, '').replace(/\[([^\]]+)\]/g, '$1').replace(/[^a-z0-9]+/gi, '-').replace(/^-|-$/g, '') || 'root').toLowerCase();

// Evaluated inside the page: horizontal overflow and a small, deterministic accessibility rule set.
export const PAGE_AUDIT = `(() => {
  const name = el => (el.getAttribute('aria-label') || el.getAttribute('aria-labelledby') || el.getAttribute('title') || el.textContent || '').trim();
  const visible = el => { const r = el.getBoundingClientRect(); const s = getComputedStyle(el); return r.width > 0 && r.height > 0 && s.visibility !== 'hidden' && s.display !== 'none'; };
  const labelled = el => el.getAttribute('aria-label') || el.getAttribute('aria-labelledby') || el.closest('label') || (el.id && document.querySelector('label[for="' + CSS.escape(el.id) + '"]')) || el.getAttribute('title') || el.type === 'hidden';
  return {
    overflow: Math.max(0, document.documentElement.scrollWidth - document.documentElement.clientWidth),
    imagesWithoutAlt: [...document.images].filter(img => visible(img) && !img.hasAttribute('alt')).length,
    unnamedControls: [...document.querySelectorAll('button,a[href],[role=button]')].filter(el => visible(el) && !name(el) && !el.querySelector('img[alt]:not([alt=""])')).length,
    unlabelledFields: [...document.querySelectorAll('input,select,textarea')].filter(el => visible(el) && !labelled(el)).length,
    tabsWithoutState: [...document.querySelectorAll('[role=tab]')].filter(el => !el.hasAttribute('aria-selected')).length,
    headings: document.querySelectorAll('h1,h2').length,
    alerts: [...document.querySelectorAll('[role=alert]')].filter(visible).map(el => el.textContent.trim().slice(0, 120)),
    title: document.title,
  };
})()`;

// Rules turn raw observations into the pass/fail decision; console noise is reported but does not decide alone.
export function judge({ documentStatus, finalPath, expectedPath, exceptions, serverErrors, clientErrors = [], audit, viewport }) {
  const problems = [];
  if (documentStatus && documentStatus >= 400) problems.push(`sayfa HTTP ${documentStatus}`);
  if (finalPath !== expectedPath) problems.push(`yönlendirme ${finalPath}`);
  if (exceptions.length) problems.push(`${exceptions.length} JS hatası`);
  if (serverErrors.length) problems.push(`${serverErrors.length} API 5xx`);
  if (clientErrors.length) problems.push(`${clientErrors.length} API 4xx`);
  if (!audit.headings) problems.push('başlık yok');
  if (viewport === 'mobile' && audit.overflow > 1) problems.push(`yatay taşma ${audit.overflow}px`);
  for (const [key, label] of [['imagesWithoutAlt', 'alt metinsiz görsel'], ['unnamedControls', 'adsız kontrol'], ['unlabelledFields', 'etiketsiz alan'], ['tabsWithoutState', 'aria-selected yok']])
    if (audit[key]) problems.push(`${audit[key]} ${label}`);
  return { status: problems.length ? 'FAIL' : 'PASS', problems };
}

export function reportLine(index, item) {
  return `${index}. [${item.status}] ${item.route} (${item.viewport}) > ${item.action}. Beklenen: ${item.expected}. Gerçek: ${item.actual}. Kanıt: ${item.evidence || '-'}`;
}

// Self-contained player for screencast frames: no codec or package, frames keep their capture timing.
export function recordingPlayer(frames) {
  const list = JSON.stringify(frames.map(frame => ({ file: frame.file, at: frame.at })));
  return `<!doctype html><meta charset="utf-8"><title>QA ekran kaydı</title><style>body{margin:0;background:#0b2540;color:#fff;font:14px system-ui}img{max-width:100%;display:block;margin:auto}p{text-align:center}</style>`
    + `<img id="f" alt="QA ekran kaydı karesi"><p id="t"></p><script>const frames=${list};let i=0;const img=document.getElementById('f'),t=document.getElementById('t');`
    + `function show(){if(!frames.length){t.textContent='Kare yok';return;}img.src=frames[i].file;t.textContent=(i+1)+'/'+frames.length;`
    + `const next=frames[i+1];i=(i+1)%frames.length;setTimeout(show,next?Math.max(40,(next.at-frames[i===0?frames.length-1:i-1].at)*1000):1500);}show();</script>`;
}

// Explicit targets must never silently vanish because the generated inventory is stale.
export function requestedPages(pages, only, hasStaticPage) {
  if (!only) return pages;
  return [...new Set(only)].map(route => {
    const recorded = pages.find(page => page.route === route);
    if (recorded) return recorded;
    if (/^\/[a-z][a-z/-]*$/.test(route) && hasStaticPage(route)) return { route };
    throw new Error(`Requested QA route has no inventory or source page: ${route}`);
  });
}
