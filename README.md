# Japanese Dictionary Popup

Chrome (Manifest V3) extension: **select Japanese text on any web page and instantly see the word's reading and English meaning** in a floating popup. Fully offline after setup — no server, no network calls at lookup time.

- Exact-match lookup over the full **JMdict** dictionary (~218k entries), indexed by both kanji form and kana reading
- **Deinflection** via the kuromoji morphological analyzer: selecting 食べました resolves to 食べる, 高くない resolves to 高い
- **Longest-prefix matching** for phrase selections, plus a **clickable token list** so you can look up any word in a selected sentence
- Popup rendered in a **closed Shadow DOM** — host-page CSS can never break it, its CSS never leaks out; light & dark mode
- Dictionary lives **once** in IndexedDB inside the service worker — not per tab

## Prerequisites

- Node.js 20+ (tested with 22) and npm
- Chrome 120+ (or any recent Chromium)
- ~350 MB free disk during setup (source JSON cache + generated data + `dist`)

## Setup

```bash
npm install

# 1. Fetch + convert the dictionary data (one-time; requires network):
#    - downloads the latest jmdict-simplified English release (~11 MB zip),
#      caches it in data/, converts it to compact chunks in public/dict/
#    - copies the kuromoji IPADIC files (~96 MB) to public/kuromoji/
npm run prepare-data

# 2. Build the extension into dist/
npm run build
```

Then load it in Chrome:

1. Open `chrome://extensions`, enable **Developer mode** (top-right).
2. Click **Load unpacked** and select the **`dist`** folder (not the project root).
3. Watch the extension's toolbar icon: a **percentage badge** shows the one-time import of the dictionary into IndexedDB (interrupting it is fine — it resumes). When the badge disappears, you're done.
4. Open any Japanese page (e.g. NHK News, ja.wikipedia.org) and select a word.

After this, everything works with the network fully disabled. Subsequent browser starts skip the import entirely.

Variants: `npm run prepare-dict -- --common` uses the smaller common-words-only edition; `npm run prepare-dict -- --file=path/to.json` uses a local jmdict-simplified JSON. Re-run `npm run build` after changing data, then click the reload arrow on the extension card.

## Permissions — why each one

| Manifest entry | Why |
|---|---|
| `content_scripts` on `<all_urls>`, `all_frames: true` | The entire product is "select text on any page", so the selection listener must run on every page and inside iframes. This is a host grant, not an API permission. |
| `permissions: []` | Empty on purpose. IndexedDB, `runtime.sendMessage`, and the action badge need no permissions. No `storage`, `tabs`, `activeTab`, or `scripting`. |

## How it works

```
content script (every page/frame)          service worker (single instance)
┌────────────────────────────┐  message   ┌─────────────────────────────────┐
│ selectionchange (debounced)│──────────► │ router (typed contracts)        │
│ Japanese-char gate         │            │  ├ core/dictionary  IndexedDB   │
│ popup in closed Shadow DOM │ ◄──────────│  │   entries + multiEntry index │
└────────────────────────────┘  entries   │  └ core/language    LanguagePack│
                                          │      └ japanese: kuromoji +     │
                                          │        deinflection (lazy init) │
                                          └─────────────────────────────────┘
```

Lookup resolution order (in `core/language/japanese/japanese-pack.ts`):

1. **Exact match** of the whole selection (catches dictionary forms and multi-word entries like idioms).
2. **Tokenize with kuromoji**, then **longest-prefix** over token boundaries — each prefix is tried literally and with its last token swapped for kuromoji's `basic_form`. That swap is the deinflection step (食べました → 食べ+まし+た → 食べる).
3. Nothing matched → an explicit **"No match found"** popup (plus the token list, so you can still tap individual words).

Non-obvious implementation notes:

- **IndexedDB schema** (`core/dictionary/db.ts`): one `entries` store keyed by JMdict id with a precomputed `f` array of all kanji+kana forms and a **multiEntry index** over it — one index query serves both 学生 and がくせい. The import commits each 5000-entry chunk **and** its progress counter in the same transaction, so a killed MV3 worker resumes instead of restarting.
- **Shadow DOM isolation** (`content/popup-controller.ts`, `ui/popup.css`): closed shadow root (the only reference is held in the controller), `:host { all: initial }` severs inherited page styles, critical positioning styles are inline on a `<jpdict-popup>` host element with maximum z-index.
- **kuromoji in a service worker** (`core/language/japanese/tokenizer.ts`): uses the `@aiktb/kuromoji` fork (fetch-based loader — stock kuromoji uses `XMLHttpRequest`, which doesn't exist in workers). The `dicPath` must be root-relative (`/kuromoji`) because kuromoji collapses `//` in URLs. Init (~1–2 s) is lazy and never blocks exact-match lookups.

## Sentence translation

Selecting a multi-word phrase or sentence adds a **Translate sentence** button to the popup (default target: **Vietnamese**, with a VI|EN toggle; the choice is remembered). It uses **Chrome's built-in Translator API** — translation runs on-device:

- Requires **Chrome 138+** on desktop. On browsers without the API (Firefox, older Chrome) the button simply never appears.
- The first translation per language pair downloads a Chrome-managed language pack (a few hundred MB, shared by all sites and extensions — not part of this extension). Progress is shown in the popup; after that it works **fully offline**. Japanese→Vietnamese pivots through English, so it needs two packs.
- Because the Translator API is not exposed to workers, this is the one feature that runs in the **content script** instead of the service worker (see `src/core/translation/sentence-translator.ts`). The first download also legitimately requires a user gesture — which the button click provides.
- Installed packs are visible at `chrome://on-device-translation-internals/`.

## Performance

Target: **< 50 ms per lookup** once initialized. Verify from the service-worker console (`chrome://extensions` → *service worker*):

```js
await jpdictDebug.bench('食べました')   // e.g. "3.20 ms average over 20 lookups"
await jpdictDebug.lookup('高くなかった') // full resolution result
await jpdictDebug.status()              // dictionary state
```

Note: the *first* deinflected lookup after a service-worker cold start pays kuromoji's ~1–2 s init once; exact matches are always fast.

## Troubleshooting

- **Badge shows `!`** — the dictionary import failed; hover the icon for the reason. Usually `public/dict` was missing at build time: run `npm run prepare-data && npm run build`, then reload the extension.
- **Popup says "No match found (tokenizer unavailable)"** — `public/kuromoji` wasn't packaged; same fix as above.
- **Selections do nothing after reloading the extension** — refresh the page; the old content script lost its messaging channel.

## Project structure

```
src/
  background/       service worker: dictionary + tokenizer + message router
  content/          selection detection, popup mount/positioning
  ui/               Preact popup components (closed Shadow DOM)
  core/
    dictionary/     packed format, IndexedDB schema, import, queries
    language/       LanguagePack interface; japanese/ implementation
  shared/           cross-layer types + typed message contracts
scripts/            prepare-dict.ts, prepare-tokenizer.ts (data generation)
public/dict/        generated dictionary chunks   (gitignored)
public/kuromoji/    IPADIC tokenizer data         (gitignored)
data/               cached jmdict-simplified JSON (gitignored)
```

Adding another language later = implementing `LanguagePack` (detect/tokenize/resolve) for it and giving the prepare script a new data source; the content script and UI are language-agnostic. The same seam is where Vietnamese glosses would plug in (a ja→vi dictionary source mapped into the same packed format).

## Data licenses & attribution

- **JMdict** — property of the [Electronic Dictionary Research and Development Group](https://www.edrdg.org/), used under its [CC BY-SA 4.0 licence](https://www.edrdg.org/edrdg/licence.html), via the [jmdict-simplified](https://github.com/scriptin/jmdict-simplified) JSON distribution.
- **kuromoji.js** (`@aiktb/kuromoji` fork) — Apache License 2.0.
- **IPADIC** — the bundled morphological dictionary carries its own permissive licence (NAIST); see `node_modules/@aiktb/kuromoji/NOTICE.md`.

## Firefox portability notes

Not tested in this MVP, but nothing hard-blocks it: swap `background.service_worker` for `background.scripts` (MV3 event page), and the `chrome.*` calls used (`runtime.onMessage/sendMessage/getURL`, `action.setBadgeText`) all exist under `browser.*`. The core (`src/core/**`) is browser-API-free by design — URLs and side effects are injected from the entry points.
