# Research: Ruby-Safe DOM Extraction (F-02)

**Date**: 2026-09-24  
**Status**: Complete — no unresolved technical clarification  
**Scope**: Planning evidence only; no runtime implementation

## Decision 1 — Reuse the approved Extension toolchain

**Decision**: Implement F-02 inside the existing `extension/` TypeScript workspace and validate it with the existing Vitest and Playwright setup. Add no package.

**Rationale**:

- The workspace already targets modern ECMAScript with DOM typings and strict TypeScript checks.
- F-02 needs real browser Selection/Range behavior. Playwright already provides that boundary.
- Adding a simulated DOM package would create a second DOM behavior source and require new dependency approval without improving browser confidence.

**Alternatives considered**:

- Add a simulated DOM package for Node tests: rejected because Range/Selection compatibility is the main risk and a simulation is weaker evidence.
- Create a separate Extension project: rejected because F-02 directly extends the existing F-01 client boundary.

## Decision 2 — Isolate browser access from deterministic text assembly

**Decision**: Place Selection/Range traversal in an explicit browser boundary and place structural text assembly in a browser-independent accumulator. The browser boundary emits semantic tokens: exact text, explicit line break, and block boundary.

**Rationale**:

- The Constitution and BROWSER-001 require browser-vendor APIs to stay outside core logic.
- Token assembly can be exhaustively unit-tested under Node without a DOM simulator.
- Real DOM selection, iframe locality, contenteditable behavior, privacy, and side effects remain covered in actual browsers.

**Alternatives considered**:

- Put the complete algorithm in one browser module: simpler initially, but it weakens unit coverage for whitespace and boundary semantics.
- Create an abstraction around every DOM type: rejected as unnecessary complexity; only the boundary and the pure accumulator need separation.

## Decision 3 — Traverse selected original nodes rather than trust rendered selection text

**Decision**: Traverse nodes intersecting the one selected Range in document order, clip boundary text nodes to the selected offsets, skip HTML `rt`/`rp` subtrees, and feed remaining content into the accumulator.

**Rationale**:

- Selection stringification is defined around rendered text and may include ruby annotations.
- Removing `rt`/`rp` only after cloning the selected fragment loses ancestry when a range begins or ends inside annotation text. Traversal of original nodes preserves the ancestry needed to identify those partial annotation selections.
- Direct traversal reads only the selected/intersecting range and does not serialize or execute markup.
- The W3C Selection API specifies one Selection per document, live Range association, document-local selection, direction, and mutation behavior: https://w3c.github.io/selection-api/
- The DOM Standard defines Range and `cloneContents()`, but cloning is not used as the semantic authority for annotation ancestry: https://dom.spec.whatwg.org/

**Alternatives considered**:

- `Selection.toString()`: rejected because ruby readings may pollute the result and output is browser-rendering dependent.
- Clone selected content, delete `rt`/`rp`, then read `textContent`: retained as useful legacy evidence but rejected as the complete algorithm because partial boundary clones can lose the excluded ancestor.
- Temporarily mutate the host DOM: prohibited because it changes the page and risks selection/focus side effects.

## Decision 4 — Use a deterministic structural-boundary policy

**Decision**:

- An intersected HTML `br` emits an explicit line-break token.
- Selected standard HTML text-container elements with block semantics emit a block-boundary token after their selected content.
- The block policy is a fixed reviewed set, independent of computed CSS and browser heuristics.
- The accumulator preserves every explicit break, coalesces nested/adjacent block boundaries, and suppresses artificial leading/trailing block breaks.

The initial reviewed block set is:

`address`, `article`, `aside`, `blockquote`, `dd`, `div`, `dl`, `dt`, `fieldset`, `figcaption`, `figure`, `footer`, `form`, `h1`–`h6`, `header`, `hgroup`, `hr`, `li`, `main`, `nav`, `ol`, `p`, `pre`, `search`, `section`, `table`, `tbody`, `td`, `tfoot`, `th`, `thead`, `tr`, and `ul`.

**Rationale**:

- The owner selected structural `LF` behavior.
- Computed CSS can be hostile, mutable, and browser-dependent. A fixed semantic set gives repeatable output.
- The HTML Standard models `br` as a newline and defines ruby annotation semantics: https://html.spec.whatwg.org/multipage/rendering.html and https://html.spec.whatwg.org/multipage/text-level-semantics.html

**Alternatives considered**:

- Use computed `display`: rejected because hostile CSS could change linguistic input and cross-browser parity.
- Concatenate all nodes: rejected because text from adjacent blocks can be merged into a different token sequence.
- Convert all boundaries to spaces: rejected by the owner's Q1 decision.

## Decision 5 — Exclude HTML ruby annotation by ancestry

**Decision**: Skip text beneath HTML-namespace `rt` or `rp` elements, including when the selected boundary begins or ends inside the annotation. Preserve base content under `ruby` and any same-looking text outside excluded ancestry.

**Rationale**:

- DOM-001 requires base text and prohibits annotation pollution.
- Ancestry is precise: text equality cannot distinguish a legitimate base occurrence from the same string used as a reading.
- The HTML Standard identifies `rt` as ruby text and child `rp` as fallback annotation punctuation.

**Alternatives considered**:

- Remove matching reading strings from the final text: rejected because it would delete legitimate repeated base text.
- Exclude all descendants of `ruby`: rejected because it removes the base text.

## Decision 6 — Keep the public outcome content-safe

**Decision**: Use a discriminated outcome:

- success: `status=ok` with the complete extracted plain text;
- non-success: `status=error` with one stable code: `no-selection`, `unsupported-multi-range`, `no-base-text`, `stale-selection`, `inaccessible-context`, or `unexpected-failure`.

No error carries raw selection, partial text, DOM, markup, URL fragments, selectors, or stack details.

**Rationale**:

- Callers must distinguish a missing/invalid extraction from valid non-Japanese text.
- Content-free errors satisfy privacy and prevent partial analysis.
- The shape follows the already proven F-01 boundary pattern without coupling the two result types.

**Alternatives considered**:

- Throw every error: rejected because browser integration needs predictable non-content outcomes.
- Return an empty string for every failure: rejected because it erases the difference between no selection, annotation-only content, stale state, and unexpected failure.

## Decision 7 — Enforce one range and frame-local operation

**Decision**:

- Accept exactly one non-collapsed Range.
- Reject multiple ranges before reading any range content.
- Obtain Selection through a caller-supplied reader bound to the current document/frame.
- Never walk parent, child, or sibling frame documents.
- Map access failure before selection acquisition to `inaccessible-context`.

**Rationale**:

- This implements the owner's Q2 and Q3 decisions.
- The Selection API associates selection with a document, including nested documents separately.
- A reader seam makes frame locality explicit and allows inaccessible-context tests without expanding permissions.

**Alternatives considered**:

- Use only the first range: rejected because it silently discards reader-selected content.
- Aggregate ranges or frames: rejected because it broadens collection and produces ambiguous separators.
- Ignore iframe selection: rejected because the approved migration corpus includes relevant iframe cases.

## Decision 8 — Treat stale selection as a snapshot-coherence failure

**Decision**: Before traversal, validate that captured boundary nodes still belong to the expected connected document and that the range remains non-collapsed. During the synchronous traversal, any Range/DOM consistency exception returns `stale-selection` when it indicates lost selection coherence; unrelated faults return `unexpected-failure`. No retry scans the page.

**Rationale**:

- Selection Ranges are live and browser mutation rules can move their boundaries.
- A fresh full-page fallback violates minimization and can analyze text the reader did not select.
- F-03 will own event stability and debounce; F-02 only validates the extraction snapshot it receives.

**Alternatives considered**:

- Retry using current `Selection.toString()`: rejected because it may represent a different interaction and reintroduces ruby pollution.
- Return the text accumulated before failure: rejected by the no-partial-output requirement.

## Decision 9 — Split Node coverage from browser-boundary evidence

**Decision**:

- Vitest covers the pure accumulator, result model, dependency isolation, and non-browser logic.
- Playwright covers Selection/Range traversal, ruby ancestry, partial boundaries, blocks, contenteditable, iframe locality, privacy, errors, host-page side effects, and browser parity.
- The Node coverage gate excludes the browser-only directory and remains ≥85% for code executable in that environment.
- Existing Chrome CI automatically executes the new browser tests; Edge, Brave, and Firefox evidence follows the approved local/available-browser reporting policy.

**Rationale**:

- Meaningful Range behavior must be verified in real engines.
- A Node DOM simulation would require an unapproved package and duplicate behavior.
- Excluding browser-only code from Node coverage is transparent only because equivalent Playwright coverage is mandatory.

**Alternatives considered**:

- Count unexecuted browser files in Node coverage: rejected because it makes the percentage misleading.
- Add a DOM simulator: rejected under Decision 1.

## Decision 10 — Measure performance without inventing an F-02 threshold

**Decision**: Measure cold and warm p95 in browser for simple, nested-ruby, multi-block, contenteditable, and 100,000-code-unit selection profiles. Record browser/OS, fixture length, sample count, and result. Do not call F02-SC-006 a latency pass until a feature allocation under PERF-001 is approved.

**Rationale**:

- PERF-001 applies to the complete stable-selection-to-popup-shell flow.
- F-02 must provide evidence for its contribution, but allocating an arbitrary share would be an undocumented product decision.

**Alternatives considered**:

- Reuse the F-01 Node benchmark: rejected because F-02 is DOM/browser work.
- Claim the whole 150 ms for extraction: rejected because it leaves no budget for F-03 orchestration and rendering.

## Legacy Evidence Review

Legacy `origin/extension_v1:src/content/selection.ts` confirms that ruby annotation polluted `Selection.toString()` and that cloning/removing `rt`/`rp` was previously useful. The following legacy choices are intentionally not inherited:

- trimming selected text;
- raw and normalized hard length caps;
- silently taking only `getRangeAt(0)`;
- debounce and event watching inside extraction;
- clone/remove as the only annotation-boundary mechanism.

Those behaviors either conflict with the F-02 specification, belong to F-03/OD-014, or lack complete partial-selection coverage.

## Research Resolution

All Technical Context decisions required for planning are resolved. No `NEEDS CLARIFICATION` item remains, no package approval is needed, and no constitutional exception is proposed.

