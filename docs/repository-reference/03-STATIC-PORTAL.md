# Static GitHub Pages Portal

This chapter explains the browser-only documentation portal at the repository root. It is independent of the .NET API and SQL Server platform.

---

## 1. Runtime model

The portal is rendered entirely in the browser:

```text
HTML shell
   -> CSS files
   -> ES-module entry point
   -> static repository adapter
   -> documentation domain data
   -> page renderer
   -> DOM interactions
```

There is no server-side rendering, authentication, database call, or live purchase execution. The playground builds example JSON locally. The external API contract data is documentation content, not a runtime proxy.

---

## 2. Root files

## `index.html`

Meaningful statements:

```html
<!doctype html>
<html lang="en" dir="ltr">
```

Declares modern HTML and an English, left-to-right document.

```html
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
```

Uses UTF-8 and responsive viewport sizing, including device safe areas.

```html
<meta name="description" ...>
<meta name="theme-color" content="#071b33">
```

Supplies search/browser description and browser UI theme color.

```html
<link rel="manifest" href="manifest.webmanifest">
```

Connects PWA metadata.

The five CSS links load in dependency order:

1. tokens;
2. base styles;
3. layout;
4. components;
5. page-specific rules.

```html
<body data-page="home" data-root=".">
```

`data-page` selects the renderer. `data-root` tells shared components how to build links relative to the current HTML shell.

```html
<main ...><div ... id="page-root" ...></div></main>
```

Creates the accessibility-oriented main landmark and the JavaScript render target.

```html
<script type="module" src="src/presentation/main.js"></script>
```

Starts the ES-module application.

## `404.html`

A compact GitHub Pages fallback. It exists because static hosting cannot execute server-side route fallback logic. Its role is operational, not architectural business logic.

## `.nojekyll`

Empty file that disables Jekyll processing. It protects raw static paths and assets from Jekyll conventions.

## `manifest.webmanifest`

Defines the static portal's installable application identity, start URL, display mode, and colors. It does not grant offline functionality by itself; the service worker performs caching.

## `sw.js`

The service worker owns a named cache and static resource lifecycle. Typical responsibilities in this file are:

- cache selected shell assets during install;
- remove obsolete caches during activation;
- respond from cache or network during fetch;
- keep portal availability independent of the dynamic platform.

When cache names or root assets change, the service-worker version must change so clients receive the new resource set.

---

## 3. HTML page shells

Files:

```text
pages/api-reference.html
pages/error-assistant.html
pages/governance.html
pages/known-limitations.html
pages/open-questions.html
pages/platform-architecture.html
pages/playground.html
pages/purchase-flow.html
pages/quick-start.html
pages/test-coverage.html
```

These are intentionally thin. Each page:

- declares standard metadata;
- links CSS through `../assets/...`;
- sets a page-specific `data-page` value;
- sets `data-root=".."` because the shell is one directory below root;
- provides the same `page-root` mount point;
- loads `../src/presentation/main.js`.

The page-specific content is not duplicated in HTML. It is produced by the matching renderer in `src/presentation/pages/`.

Why this approach exists:

- GitHub Pages receives real HTML paths that can be linked directly;
- rendering and navigation remain centralized;
- each page can have a meaningful browser URL;
- relative-path differences are isolated in `data-root`.

---

# 4. CSS architecture

## `assets/css/tokens.css`

Defines CSS custom properties for:

- colors;
- spacing;
- radii;
- shadows;
- typography;
- content widths;
- transition values.

Tokens make later styles semantic. A component can use `var(--surface)` rather than repeat a literal color.

Light and dark theme values are selected through document data attributes, allowing JavaScript to change theme without rebuilding markup.

## `assets/css/base.css`

Defines global element behavior:

- box sizing;
- body margin and background;
- font inheritance;
- link and button defaults;
- focus visibility;
- readable code/pre formatting.

Base rules should remain generic and should not know page identities.

## `assets/css/layout.css`

Defines structural regions:

- application shell;
- header;
- navigation/sidebar;
- main content;
- content container;
- responsive breakpoints.

It owns where components appear, not their business meaning.

## `assets/css/components.css`

Styles reusable visual concepts such as:

- cards;
- badges;
- data tables;
- code blocks;
- alerts;
- command palette;
- buttons and form controls.

## `assets/css/pages.css`

Contains rules that are meaningful only to specific documentation pages, such as purchase-flow diagrams, playground arrangements, test matrices, or architecture panels.

---

# 5. Static domain data

## `src/domain/documentation-model.js`

### `documentControl`

An immutable object containing document governance metadata:

- title and subtitle;
- reference `API-ENT-DOC-001`;
- version;
- environment;
- status;
- classification;
- owner;
- preparer;
- issue date.

`Object.freeze` communicates that presentation code must not mutate authoritative documentation metadata.

### `responseEnvelope`

Shows the observed external API response wrapper. The misspellings `massage` and `statues` are preserved because they are contract evidence rather than internal naming recommendations.

### `contractNamingNotice`

Explicitly lists observed non-standard names so readers do not “fix” them in client requests by assumption.

### `successMatrix`

Documents that HTTP success and business success are separate checks. A `200 OK` response is not sufficient; logical fields must also be inspected.

## `src/domain/api-contracts.js`

This is the central static endpoint catalog.

Each API object includes:

- `ref`: stable documentation identifier;
- `slug`: URL/search key;
- `name` and `group`;
- HTTP `method` and `path`;
- authentication requirement;
- test status;
- purpose;
- header definitions;
- request fields;
- response fields;
- request and response examples;
- success checks;
- business/operational rules;
- error and uncertainty guidance.

Current documented operations include authentication, catalog retrieval, initial purchase, purchase execution, and purchase state checking.

The shared `headers` object avoids repeating common Content-Type, Channel, and Authorization rows.

Security rules embedded in the documentation include:

- never store credentials in shared collections/source;
- do not expose tokens in screenshots;
- use the current catalog's identifiers;
- do not reveal internal cost price to customers;
- do not repeat an uncertain non-idempotent execution;
- check state with the original `requestId`.

## `src/domain/purchase-flow.js`

Owns the ordered purchase sequence and identifier meanings.

The conceptual sequence is:

```text
Login
  -> Catalog
  -> Initial Purchase
  -> Execute Purchase
  -> Check State
```

Important identifiers:

- `serviceCode`: service selected from catalog;
- `offerCode`: offer under the selected service;
- `fieldID`: required input definition;
- `resolutionID`: temporary initialization output used by execute;
- `requestId`: client-generated idempotency/tracking identifier;
- `referenceID`: backend transaction reference.

## `src/domain/quality-data.js`

Owns:

- positive and negative test scenarios;
- error-assistant mappings;
- implementation limitations;
- open questions.

This file is important for epistemic honesty: tested facts, observations, inferred behavior, and pending backend confirmation are not silently merged into one certainty level.

---

# 6. Static application use cases

## `build-purchase-requests.js`

### `buildPurchaseRequests(values)`

```javascript
const clean = Object.fromEntries(
  Object.entries(values).map(([key, value]) =>
    [key, String(value ?? '').trim()]
  )
);
```

Execution:

1. convert the input object to key/value entries;
2. replace null/undefined with an empty string;
3. convert all values to strings;
4. trim surrounding whitespace;
5. reconstruct a normalized object.

The function then returns a frozen object containing three request bodies:

- `initial` with service, offer, and `feilds`;
- `execute` with resolution and request IDs;
- `check` with request ID.

The exact external `theType` values and misspelled field name are preserved.

### `validatePurchaseInputs(values, mode)`

Defines required keys per mode, finds empty values, and returns:

```javascript
{ valid: missing.length === 0, missing }
```

It validates presence only. It does not contact the backend or prove that identifiers exist.

## `filter-test-scenarios.js`

Returns a shallow copy for `All`, otherwise filters by exact status. Copying for `All` prevents callers from receiving the original array reference.

## `resolve-error-action.js`

Maps an error category or key to the corresponding recovery guidance from quality data. Its role is selection logic; it does not diagnose network traffic automatically.

## `search-documentation.js`

### `normalize`

Converts a value to lowercase text, collapses repeated whitespace, and trims it. This creates consistent search comparison text.

### `buildSearchIndex(repository)`

Builds index items from:

- navigation pages;
- API title/purpose/path/group;
- request and response fields;
- rules and error notes;
- open questions;
- error-assistant entries.

Each item carries title, section, destination URL, and normalized searchable text.

### `searchDocumentation(index, query, limit = 16)`

- rejects queries shorter than two normalized characters;
- scores exact title at 100;
- title prefix at 70;
- title substring at 45;
- body-text substring at 20;
- removes zero-score items;
- sorts by descending score and then title;
- limits the result count.

This is deterministic client-side search, not semantic search and not a backend index.

---

# 7. Infrastructure adapters

## `static-documentation-repository.js`

Imports domain data and exposes query methods such as:

- `getDocumentControl()`;
- `getApis()`;
- `getApi(ref)`;
- `getPurchaseFlow()`;
- `getIdentifiers()`;
- `getTestScenarios()`;
- `getLimitations()`;
- `getOpenQuestions()`;
- `getNavigation()`.

Why a repository exists even for static arrays:

- presentation code depends on an interface-like query surface instead of import knowledge;
- data organization can change without rewriting every renderer;
- the architecture demonstrates dependency separation;
- tests can later substitute a repository-like object.

The navigation array owns page labels, URLs, page keys, and search keywords.

## `browser-preferences.js`

Adapts browser storage for theme and last-page preferences. It centralizes key names and protects presentation components from direct storage calls.

Browser storage may be unavailable or restricted; robust preference adapters should tolerate storage exceptions. It must never be used for production secrets.

---

# 8. Presentation entry point

## `src/presentation/main.js`

Execution order:

1. import repository, preference adapter, use cases, shell, global components, and page renderers;
2. construct `StaticDocumentationRepository`;
3. read `data-page` and `data-root` from `<body>`;
4. choose saved theme or operating-system preference;
5. set the document theme attribute;
6. render the shared application shell;
7. map page keys to renderer functions;
8. locate `#page-root`;
9. render the selected page or home fallback;
10. initialize global interactions;
11. build the search index;
12. initialize command palette;
13. remember the current page;
14. run page-specific initialization where interactive behavior is needed;
15. register the service worker after window load when supported.

The renderer map prevents a long conditional chain and makes a new page addition explicit.

Page-specific initializers exist because markup generation and DOM event registration are separate steps.

Service-worker registration failure is intentionally caught because offline enhancement must not prevent online documentation from rendering.

---

# 9. Presentation components

## `app-shell.js`

Builds common header/navigation/theme/search shell using repository navigation. It owns active-page indication and relative-link construction using `root`.

## `code-block.js`

Escapes and formats code examples so JSON or shell content is displayed rather than interpreted as HTML. Copy-button behavior is initialized separately.

## `command-palette.js`

Owns keyboard/search overlay behavior:

- open/close state;
- input handling;
- search invocation;
- result rendering;
- keyboard selection/navigation;
- destination navigation.

## `data-table.js`

Converts structured field arrays into consistent accessible table markup. It prevents each endpoint renderer from inventing a different table format.

## `dom-utils.js`

Contains reusable DOM-safe helpers, particularly HTML escaping and element/query conveniences. Escaping is critical because documentation examples may contain angle brackets, quotes, or user-like text.

## `interactions.js`

Initializes global behaviors such as:

- theme toggle;
- mobile navigation;
- copy controls;
- disclosure/expansion behavior;
- preference persistence.

It should not own API contract facts.

---

# 10. Page renderers

## `home-page.js`

Renders document overview, control metadata, workflow summary, and entry links.

## `quick-start-page.js`

Renders the minimum integration sequence and initializes controls that guide a reader through login, catalog, initial, execute, and state check.

## `purchase-flow-page.js`

Renders transaction stages and identifier transfer between stages. It explains why `resolutionID` and `requestId` serve different purposes.

## `api-reference-page.js`

Iterates the endpoint catalog and renders:

- method/path/auth status;
- purpose;
- request/response tables;
- examples;
- success criteria;
- rules and error notes.

## `playground-page.js`

Collects local form values, validates required fields by operation mode, and shows generated JSON. It is an offline builder, not a live API console.

## `error-assistant-page.js`

Displays selected error context and recommended response using quality-data mappings.

## `test-coverage-page.js`

Filters and renders test scenarios by status, preserving the difference between tested, observed, and pending behavior.

## `platform-architecture-page.js`

Explains the dynamic .NET architecture from inside the static portal. It is descriptive documentation and does not load the dynamic Admin/Client applications.

## `governance-page.js`

Renders document control, review, approval, and revision information.

## `known-limitations-page.js`

Renders limitations explicitly so unverified behavior is not presented as complete.

## `open-questions-page.js`

Renders backend questions requiring confirmation.

---

# 11. Data and rendering safety

The portal renders repository-controlled static data, but it still needs HTML escaping because examples contain arbitrary strings. `innerHTML` is used for generated markup, so all interpolated untrusted or external-contract text should pass through escaping helpers.

The service worker can preserve old assets. After changing shell structure or bundled paths, update cache versioning and verify a hard refresh/activation path.

The portal does not secure any dynamic platform resource. GitHub Pages is public static hosting.

---

# 12. Adding a static documentation page

1. Add authoritative data under `src/domain` when new facts are needed.
2. Add an application use case only when reusable transformation/search/validation logic is needed.
3. Add a renderer under `src/presentation/pages`.
4. Add an initializer when the page needs event binding.
5. Add the page to the renderer map in `main.js`.
6. Add navigation metadata in `StaticDocumentationRepository`.
7. Add a thin `pages/<name>.html` shell with correct `data-page` and `data-root`.
8. Add page-only CSS to `pages.css`; reusable visual CSS belongs in `components.css`.
9. Update service-worker cached assets when required.
10. Test direct GitHub Pages navigation, relative links, theme, keyboard behavior, and offline/cache behavior.

---

# 13. Static portal boundaries

The portal must not:

- contain production credentials;
- send real purchase operations merely because it displays request bodies;
- claim unconfirmed backend behavior as fact;
- silently normalize misspelled external contract fields;
- become the source of truth for dynamic platform users, roles, or database state;
- embed internal cost or customer identifiers in public examples.
