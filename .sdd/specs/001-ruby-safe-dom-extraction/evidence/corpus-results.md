# F-02 Corpus Results

**Date**: 2026-09-24  
**Branch**: `feat/ruby-safe-dom-extraction`

## Foundation and US1

| Check | Cases | Result |
|---|---:|---|
| Result contract, accumulator, and boundary-isolation unit tests | 16 | PASS |
| Ruby/base-text corpus on installed Google Chrome | 11 | PASS |
| Ruby/base-text corpus on installed Microsoft Edge | 11 | PASS |
| Ruby/base-text corpus on installed Brave | 11 | PASS |

Commands executed:

```text
npm test -- --run tests/unit/rubySafeSelectionContract.test.ts tests/unit/selectedTextAccumulator.test.ts tests/unit/browserBoundaryIsolation.test.ts
npm run test:browser -- tests/browser/ruby-safe-selection-ruby.test.ts
BRAVE_EXECUTABLE_PATH=<installed Brave executable> npm run test:browser -- tests/browser/ruby-safe-selection-ruby.test.ts
```

The browser corpus covers F02-AC-001–008, F02-AC-012, and F02-AC-028.
Every executed case returned its independently declared exact text or `no-base-text`
outcome. Firefox was not executed because no installed executable was found; it remains
pending and is not represented by a Chromium substitute.

No browser or package was downloaded for these runs.

## US2 structure and editable content

The corpus was expanded by 11 independently expected US2 cases for explicit/consecutive
`br`, semantic blocks, existing `LF`, exact whitespace, forward/backward selection,
contenteditable, Unicode, a 100,000-code-unit selection, and exclusion of generated or
neighboring content. The combined 22-case corpus and focused structure/editable suites ran
on all three installed browsers:

| Browser | Combined corpus | Focused structure/editable Playwright tests | Result |
|---|---:|---:|---|
| Google Chrome | 22 | 2 | PASS |
| Microsoft Edge | 22 | 2 | PASS |
| Brave | 22 | 2 | PASS |

The US1+US2 regression command completed with 9/9 Playwright test entries passing. No
browser or package was downloaded. Firefox remains pending because it is not installed.

The final corpus adds two edge-policy regressions: a foreign-namespace element named `rt`
remains eligible base text, and a programmatically supplied U+0000 code unit is preserved.
The final complete corpus therefore contains 24 cases per browser.
