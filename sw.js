const CACHE='entertainment-api-docs-v3-0';
const ROOT=new URL('./',self.location.href);
const assets=[
  './','./index.html','./manifest.webmanifest',
  './pages/quick-start.html','./pages/purchase-flow.html','./pages/api-reference.html','./pages/playground.html','./pages/error-assistant.html','./pages/test-coverage.html','./pages/platform-architecture.html','./pages/governance.html','./pages/known-limitations.html','./pages/open-questions.html',
  './assets/css/tokens.css','./assets/css/base.css','./assets/css/layout.css','./assets/css/components.css','./assets/css/pages.css',
  './src/domain/documentation-model.js','./src/domain/api-contracts.js','./src/domain/purchase-flow.js','./src/domain/quality-data.js',
  './src/application/use-cases/build-purchase-requests.js','./src/application/use-cases/search-documentation.js','./src/application/use-cases/resolve-error-action.js','./src/application/use-cases/filter-test-scenarios.js',
  './src/infrastructure/repositories/static-documentation-repository.js','./src/infrastructure/storage/browser-preferences.js',
  './src/presentation/main.js','./src/presentation/components/dom-utils.js','./src/presentation/components/code-block.js','./src/presentation/components/data-table.js','./src/presentation/components/app-shell.js','./src/presentation/components/command-palette.js','./src/presentation/components/interactions.js',
  './src/presentation/pages/home-page.js','./src/presentation/pages/quick-start-page.js','./src/presentation/pages/purchase-flow-page.js','./src/presentation/pages/api-reference-page.js','./src/presentation/pages/playground-page.js','./src/presentation/pages/error-assistant-page.js','./src/presentation/pages/test-coverage-page.js','./src/presentation/pages/platform-architecture-page.js','./src/presentation/pages/governance-page.js','./src/presentation/pages/known-limitations-page.js','./src/presentation/pages/open-questions-page.js'
].map(path=>new URL(path,ROOT).href);
self.addEventListener('install',(event)=>event.waitUntil(caches.open(CACHE).then((cache)=>cache.addAll(assets)).then(()=>self.skipWaiting())));
self.addEventListener('activate',(event)=>event.waitUntil(caches.keys().then((keys)=>Promise.all(keys.filter((key)=>key!==CACHE).map((key)=>caches.delete(key)))).then(()=>self.clients.claim())));
self.addEventListener('fetch',(event)=>{if(event.request.method!=='GET')return;event.respondWith(caches.match(event.request).then((cached)=>cached||fetch(event.request).then((response)=>{const copy=response.clone();caches.open(CACHE).then((cache)=>cache.put(event.request,copy));return response;}).catch(()=>caches.match(new URL('./index.html',ROOT).href))));});
