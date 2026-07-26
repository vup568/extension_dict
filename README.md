# Japanese Dictionary Popup

Chrome (Manifest V3) extension: **select Japanese text on any web page and instantly explore it in a tabbed popup** — Từ vựng (vocabulary) | Hán tự (kanji) | Ngữ pháp (grammar) | Dịch (translation). Dictionary, kanji, and grammar work fully offline after setup; UI labels and grammar explanations are in Vietnamese.

- **Từ vựng** — exact-match lookup over the full **JMdict** dictionary (~218k entries), indexed by both kanji form and kana reading; **deinflection** via the kuromoji morphological analyzer (食べました → 食べる, 高くない → 高い); **longest-prefix matching** plus a **clickable token strip** grouped into grammar-aware units
- **Hán tự** — one card per kanji in the selection from **KANJIDIC2**: prominent **Hán Việt (Sino-Vietnamese) reading** (学 → HỌC), on/kun readings, English meanings, stroke count, JLPT level, school grade, newspaper frequency
- **Ngữ pháp** — extracts the **whole sentence containing the selection**, highlights the selection, and lists every **JLPT N5–N4 grammar pattern** detected in it (102 offline rules, Vietnamese explanations), plus per-part conjugation breakdowns
- **Dịch** — hybrid sentence translation (on-device Chrome pack or online), Vietnamese by default
- Popup rendered in a **closed Shadow DOM** — host-page CSS can never break it, its CSS never leaks out; light & dark mode; **draggable** and pinnable
- Dictionary + kanji data live **once** in IndexedDB inside the service worker — not per tab

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
#    - downloads the kanjidic2-en release (~1.3 MB zip) and packs the kanji
#      store (readings incl. Hán Việt, meanings, strokes, JLPT)
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

Variants: `npm run prepare-dict -- --common` uses the smaller common-words-only edition; `npm run prepare-dict -- --file=path/to.json` uses a local jmdict-simplified JSON. Note `prepare-dict` wipes `public/dict/` entirely — run `npm run prepare-kanji` after it (or just use `prepare-data`, which runs everything in order). Re-run `npm run build` after changing data, then click the reload arrow on the extension card.

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

- **IndexedDB schema** (`core/dictionary/db.ts`): one `entries` store keyed by JMdict id with a precomputed `f` array of all kanji+kana forms and a **multiEntry index** over it — one index query serves both 学生 and がくせい; plus a `kanji` store (schema v2) keyed by the character for the Hán tự tab. The import commits each chunk **and** its progress counter in the same transaction, so a killed MV3 worker resumes instead of restarting; the kanji chunks import after the entries under the same badge percentage.
- **Tabbed popup, lazy tabs** (`ui/Popup.tsx`, `content/content-script.ts`): kanji data, sentence-grammar analysis, and single-word translations are fetched only when their tab is first opened; every async patch is guarded by a selection sequence number so late responses can't resurrect an outdated popup.
- **Sentence extraction** (`content/sentence.ts`): two auxiliary Ranges cover the text before/after the selection inside its block container, each furigana-stripped and cut at the nearest sentence terminator (。！？…), 100 chars max per side — that is how the Ngữ pháp tab gets the full sentence even when you select a single word.
- **Grammar rules** (`core/language/japanese/grammar-patterns.ts`): 102 N5–N4 patterns as token-sequence matchers over IPADIC features (surface / basic_form / pos / pos_detail_1 / conjugated_form), scanned longest-match-first so 〜たことがある beats plain 〜た. `npm run test-grammar` asserts every rule's example sentence still triggers it through the real tokenizer.
- **Shadow DOM isolation** (`content/popup-controller.ts`, `ui/popup.css`): closed shadow root (the only reference is held in the controller), `:host { all: initial }` severs inherited page styles, critical positioning styles are inline on a `<jpdict-popup>` host element with maximum z-index.
- **kuromoji in a service worker** (`core/language/japanese/tokenizer.ts`): uses the `@aiktb/kuromoji` fork (fetch-based loader — stock kuromoji uses `XMLHttpRequest`, which doesn't exist in workers). The `dicPath` must be root-relative (`/kuromoji`) because kuromoji collapses `//` in URLs. Init (~1–2 s) is lazy and never blocks exact-match lookups.

## Sentence translation

Selecting a multi-word phrase or sentence **translates it automatically** (shown in the Dịch tab; single words translate when you open the tab). Default target: **Vietnamese**, VI|EN toggle, choice remembered. A hybrid engine picks the best available path per call (`src/content/translation-flow.ts`):

1. **On-device** (Chrome's built-in Translator API, Chrome 138+): used automatically once its language pack is installed — private and fully offline. Because this API is not exposed to workers, it runs in the content script; the pack download legitimately requires a user click, which the **"⬇ Download offline pack"** link provides. Installed packs: `chrome://on-device-translation-internals/`.
2. **Online fallback** (zero setup): the key-less `translate.googleapis.com` gtx endpoint — the same one popular dictionary extensions use. Instant from the first use, but: it needs internet, **the selected text is sent to Google**, and the endpoint is unofficial (rate-limited per IP; Google could change or block it). It is fetched from the service worker under the narrow `https://translate.googleapis.com/*` host permission.

Results are cached per (text, target). Hover the translated text to see which engine produced it. To keep everything local, download the offline pack once — on-device then wins everywhere. Word/kanji lookups never touch the network either way.

Selection handling details:

- **Furigana-safe**: ruby annotations (`<rt>`/`<rp>`, as used by NHK Easy News) are stripped from selections before tokenizing, lookup, and translation — reading aids never pollute results.
- **Long selections** (over ~500 characters, up to 2000): the popup switches to a translation-only view — no tabs, token strip, or dictionary lookup, just the translated passage.
- **Token strip cleanup**: pure digit/punctuation tokens are hidden; noun runs that form a JMdict compound are merged (IPADIC splits 熱中症 into 熱中+症; the strip shows the compound); and a verb/adjective absorbs its auxiliaries — including the passive/causative suffixes られる/させる — into one grammar unit (なり+まし+た → なりました, looked up as なる).
- **Conjugation breakdown**: selecting or tapping a conjugated unit shows a "Cách chia" section explaining each part offline in Vietnamese (なり — gốc của なる, まし — đuôi lịch sự, た — quá khứ), driven by `src/core/language/japanese/grammar.ts`; the Ngữ pháp tab repeats these breakdowns for every conjugated unit in the sentence.
- **Draggable popup**: press and hold the grip bar (or any empty popup area — headword included) to move the popup anywhere; it stays pinned there until the next selection. Button, tab, token-chip, and text areas keep their normal behavior so glosses stay copyable.
- **JLPT numbers on kanji cards**: KANJIDIC2 carries the pre-2010 JLPT 1–4 scale; the packer maps it to the modern N-scale (old 4→N5, 3→N4, 2→N2, 1→N1 — the old scale had no N3), so treat N-levels as approximate.

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
  content/          selection detection, sentence extraction, popup control
  ui/               Preact popup components (closed Shadow DOM, tabbed)
  core/
    dictionary/     packed format, IndexedDB schema, import, word + kanji queries
    language/       LanguagePack interface; japanese/ implementation
                    (tokenizer, grammar units, grammar-patterns + matcher)
  shared/           cross-layer types + typed message contracts
scripts/            prepare-dict/kanji/tokenizer (data generation),
                    test-grammar (rule self-test), dev-tokenize (dev dump)
public/dict/        generated dictionary + kanji chunks (gitignored)
public/kuromoji/    IPADIC tokenizer data               (gitignored)
data/               cached source JSON                  (gitignored)
```

Adding another language later = implementing `LanguagePack` (detect/tokenize/resolve) for it and giving the prepare script a new data source; the content script and UI are language-agnostic. The same seam is where Vietnamese glosses would plug in (a ja→vi dictionary source mapped into the same packed format).

## Data licenses & attribution

- **JMdict** and **KANJIDIC2** — property of the [Electronic Dictionary Research and Development Group](https://www.edrdg.org/), used under its [CC BY-SA 4.0 licence](https://www.edrdg.org/edrdg/licence.html), via the [jmdict-simplified](https://github.com/scriptin/jmdict-simplified) JSON distributions. KANJIDIC2's Vietnamese (Hán Việt) readings were contributed to it by Minh Chau Pham. SKIP codes (a separately-licensed, non-commercial part of KANJIDIC2) are **not** imported.
- **Grammar patterns** (`src/core/language/japanese/grammar-patterns.ts`) — detection rules and Vietnamese explanations written for this project; JLPT point coverage and formations cross-checked against [jkindrix/japanese-language-data](https://github.com/jkindrix/japanese-language-data) (CC BY-SA 4.0). That data file is accordingly shared under CC BY-SA 4.0.
- **kuromoji.js** (`@aiktb/kuromoji` fork) — Apache License 2.0.
- **IPADIC** — the bundled morphological dictionary carries its own permissive licence (NAIST); see `node_modules/@aiktb/kuromoji/NOTICE.md`.

## Firefox portability notes

Not tested in this MVP, but nothing hard-blocks it: swap `background.service_worker` for `background.scripts` (MV3 event page), and the `chrome.*` calls used (`runtime.onMessage/sendMessage/getURL`, `action.setBadgeText`) all exist under `browser.*`. The core (`src/core/**`) is browser-API-free by design — URLs and side effects are injected from the entry points.
