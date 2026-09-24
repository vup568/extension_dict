# Validation Guide: Ruby-Safe DOM Extraction (F-02)

**Feature**: [SPEC.md](SPEC.md)  
**Contract**: [contracts/ruby-safe-dom-extraction.md](contracts/ruby-safe-dom-extraction.md)  
**Data model**: [data-model.md](data-model.md)

This guide describes the commands and observable results verified by the F-02 implementation.

## Prerequisites

- Node.js 22.12 or newer and npm.
- Run commands from `extension/`.
- Install only the existing lockfile dependencies with `npm ci`.
- Chrome and Edge may be addressed through their existing Playwright channels.
- Set `BRAVE_EXECUTABLE_PATH` when Brave is installed.
- Use the Playwright-compatible Firefox executable through `FIREFOX_EXECUTABLE_PATH` when available.

F-02 requires no Backend, database, Docker, login, network service, or new npm dependency.

## Validation Commands

```powershell
cd E:\jp-dict-extension\extension
npm ci
npm run unicode:verify
npm run typecheck
npm run test:coverage
npm run build
npm run test:browser
```

Run the correctness-asserting F-02 performance profiles with the existing Playwright toolchain:

```powershell
npm run test:browser -- tests/browser/ruby-safe-selection-performance.test.ts
```

## Focused Browser Validation

After implementation, run the F-02 browser suite by its test-name or file filter:

```powershell
npm run test:browser -- ruby-safe-selection
```

Run a specific available browser:

```powershell
npm run test:browser -- ruby-safe-selection --project=chrome
npm run test:browser -- ruby-safe-selection --project=edge
$env:BRAVE_EXECUTABLE_PATH = "C:\path\to\brave.exe"
npm run test:browser -- ruby-safe-selection --project=brave
$env:FIREFOX_EXECUTABLE_PATH = "C:\path\to\playwright-compatible-firefox.exe"
npm run test:browser -- ruby-safe-selection --project=firefox
```

Missing Brave/Firefox executables remain pending; do not report them as pass.

## Required Scenario Groups

### 1. Ruby/base-text correctness

- Plain Japanese text remains unchanged.
- `<ruby>日本<rt>にほん</rt></ruby>` returns exactly `日本`.
- `rp` fallback punctuation is excluded.
- Partial selections starting/ending inside annotation return only eligible base text.
- Annotation-only selection returns `no-base-text`.
- Identical reading text outside `rt`/`rp` remains present.

### 2. Structure and Unicode

- Adjacent inline nodes concatenate without an invented separator.
- Selected `br` becomes `LF`.
- Selected adjacent blocks have one logical `LF`.
- Consecutive `br` elements preserve consecutive breaks.
- Forward/backward selection yields document-order output.
- Whitespace, decomposed kana, supplementary CJK, variation selectors, replacement characters, and malformed UTF-16 remain unchanged.
- Contenteditable values and selection remain unchanged.

### 3. Errors and frame locality

- No/collapsed selection returns `no-selection`.
- Multiple ranges return `unsupported-multi-range` without reading the first range.
- Detached/incoherent boundaries return `stale-selection`.
- Inaccessible selection acquisition returns `inaccessible-context`.
- Unexpected traversal failure returns `unexpected-failure`.
- An allowed iframe extracts only its own selection.
- Parent and sibling-frame content never appears.

### 4. Privacy and host safety

Use unique synthetic text markers on success and every error path. Verify:

- no request contains the marker;
- no local/session storage or history contains the marker;
- no raw-content log/console output contains the marker;
- no DOM, style, focus, selection, editable value, clipboard, navigation, or event-default state changes;
- no DOM/Range/Node reference is retained in the public result.

### 5. Performance evidence

Measure cold and warm p95 for:

- small plain selection;
- nested-ruby selection;
- multi-block selection;
- contenteditable selection;
- 100,000-code-unit selection with meaningful content at the end.

Record:

- OS and browser/version;
- policy/spec version;
- input length and DOM shape;
- warmup and measured sample counts;
- cold duration and warm p95;
- exact correctness assertion for every measured sample.

F-02 has no standalone approved latency threshold. Report measurements as evidence toward PERF-001; do not call the threshold passed until a feature allocation is approved.

## Expected Completion Evidence

- Exact corpus results mapped to F02-AC-001 through F02-AC-030.
- Node coverage remains ≥85% for code executable under Node.
- Real-browser tests cover the excluded browser-only boundary.
- Chrome and Brave are prioritized for the owner's manual validation.
- Chrome, Edge, Brave, and Firefox each have an explicit pass/fail/pending row.
- Final review confirms no popup, debounce, request, Backend, persistence, authentication, context expansion, or linguistic analysis was added.

