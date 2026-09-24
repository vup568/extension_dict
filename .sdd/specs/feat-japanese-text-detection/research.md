# Research: Japanese Text Detection (F-01)

**Feature:** [Japanese Text Detection specification](SPEC.md)
**Date:** 2026-09-20
**Scope:** Design evidence only. No package, toolchain, source data, or runtime code is installed by this document.

## R-01 — Freeze Unicode semantics in checked-in data

**Decision:** Generate a static, sorted, non-overlapping range table from Unicode 17.0.0 UCD inputs and commit it with a provenance manifest.

**Rationale:** SPEC.md fixes behavior to Unicode 17.0.0. Browser Unicode-property regular expressions depend on the browser's shipped Unicode/ICU data, so they cannot by themselves guarantee the same membership on all four target browsers. A generated table makes the policy reviewable and reproducible.

**Inputs to record:** Scripts.txt; PropList.txt; UnicodeData.txt; Blocks.txt, all from Unicode 17.0.0. The manifest must record each canonical URL, downloaded checksum, license/notice, generator revision, generated-table checksum and policy version.

**Alternatives considered:**

- Runtime Unicode-property regular expressions: rejected as semantic authority because browser data can drift from Unicode 17.0.0.
- Broad block/range checks: rejected because blocks contain symbols, radicals and unassigned code points that SPEC.md explicitly rejects.
- Reusing the backend CJK/Kanji helper: rejected because its broad legacy-oriented ranges are not the approved F-01 policy and F-01 must not alter Backend scope.

## R-02 — Use code-point scanning with table membership

**Decision:** Scan every code point in the JavaScript string and test it against the generated table using a deterministic sorted-range lookup.

**Rationale:** This covers supplementary Han and Kana while preserving the full-input guarantee. JavaScript iteration by code point handles valid surrogate pairs; an isolated surrogate produces a code unit outside all trigger ranges and inspection continues to later characters, matching F01-AC-024 and F01-AC-025.

**Alternatives considered:**

- BMP-only character iteration: rejected because U+20B9F, U+20000, U+2F800 and U+1B000 are mandatory positive cases.
- Normalizing, decoding entities, or repairing mojibake before scanning: rejected by F01-FR-005 and F01-AC-019 to F01-AC-020.
- Limiting input length: rejected by F01-FR-012 and F01-AC-021.

## R-03 — Keep core and boundary separate

**Decision:** Place a pure detector in extension/src/core/ and a thin unknown-input validation boundary in extension/src/boundary/.

**Rationale:** The core needs a simple string-to-boolean contract to remain deterministic and free of side effects. The boundary is where non-string input and unexpected failure can be represented distinctly from a valid false classification. This follows the constitutional browser-isolation and structured-error requirements without introducing a Browser API abstraction where none is needed.

**Alternatives considered:**

- Accept arbitrary values in the core and coerce them: rejected by F01-AC-023.
- Return false for every failure: rejected because consumers must distinguish false from input or execution error.
- Put DOM/selection handling into F-01: rejected; this belongs to F-02.

## R-04 — Corpus is the policy oracle for tests

**Decision:** Store a declarative corpus with stable IDs, independent expected outcomes and explicit code-point/code-unit representation for ambiguous cases.

**Rationale:** A fixture authored by calling the production detector cannot catch policy regressions. Explicit representation prevents a source editor, serializer or test runner from silently changing lone surrogates, supplementary characters or variation selectors.

**Alternatives considered:**

- Handwritten inline test strings only: rejected because malformed UTF-16 and invisible code points require unambiguous representation.
- Generating expected values from the range table: rejected because it makes the test oracle share the implementation's decision path.

## R-05 — Privacy and browser parity require separate evidence

**Decision:** Test pure classification in an environment without browser globals, then run a minimal corpus/side-effect harness separately on Chrome, Edge, Brave and Firefox.

**Rationale:** Unit success does not prove cross-browser parity or that a future integration avoids network, storage, DOM and logging side effects. The required evidence must use synthetic markers, record browser/OS versions and mark an unavailable browser pending.

**Alternatives considered:**

- One Chromium run for all Chromium-family browsers: rejected by F01-NFR-003.
- Real selected page text in test logs: rejected by the privacy rules.
- Claiming browser tests pass before a runner exists: rejected by F01-AC-028.

## R-06 — Tooling remains an explicit human approval gate

**Decision:** Do not choose or install a package manager, Vite bootstrap, unit-test package or browser-automation package in planning.

**Rationale:** The repository currently has no frontend workspace, package manifest or browser runner, while AGENTS.md prohibits dependency installation without explicit approval. The spec fixes behavior, not a dependency set.

**Alternatives considered:**

- Silently bootstrap the common frontend stack: rejected by the repository governance rule.
- Reuse .NET xUnit to implement the TypeScript browser detector: rejected because it would not exercise the intended client runtime or browser matrix.
