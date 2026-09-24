# Implementation Plan: Ruby-Safe DOM Extraction (F-02)

**Branch**: `feat/ruby-safe-dom-extraction` | **Date**: 2026-09-24 | **Spec**: [SPEC.md](SPEC.md)  
**Input**: Approved F-02 specification in `.sdd/specs/001-ruby-safe-dom-extraction/SPEC.md`
**Implementation Status**: Complete — see `evidence/final-review.md`; Firefox parity remains pending  

## Summary

Implement a local Extension boundary that turns exactly one current browser selection Range into deterministic ruby-safe plain text. The browser adapter traverses only nodes intersecting the selected Range, excludes HTML `rt`/`rp` descendants by their original DOM ancestry, clips partial boundary text precisely, and emits semantic text/break tokens. A browser-independent accumulator preserves exact text, explicit `br` line breaks, and coalesced semantic block boundaries.

The result is either complete non-empty base text or one content-free error category. The feature performs no selection listening, debounce, popup, Backend call, authentication, persistence, logging, or input-length policy. It reuses the approved TypeScript/Vite/Vitest/Playwright workspace and adds no dependency.

## Technical Context

**Language/Version**: TypeScript 7.0.2 in strict mode; Node.js ≥22.12 for tooling  
**Primary Dependencies**: Browser DOM Selection/Range APIs at runtime; existing Vite 8.3.0, Vitest 5.0.1, Playwright 1.63.0 for build/test only  
**Storage**: N/A — selection, DOM references, tokens, text, and errors are ephemeral  
**Testing**: Vitest for pure accumulator/contracts/isolation; Playwright for real DOM, Range, frames, privacy, parity, side effects, and performance  
**Target Platform**: Desktop Chrome, Edge, Brave, and Firefox Extension-compatible page contexts; Brave and Chrome are primary owner-validation browsers  
**Project Type**: Browser Extension client library boundary inside the existing hybrid repository  
**Performance Goals**: No network wait or intentional debounce; record cold/warm browser p95 for five approved profiles as contribution evidence to PERF-001  
**Constraints**: Exact ruby exclusion, one range, frame-local access, no normalization/truncation, no raw-content log/storage/network, no host-page mutation, no new packages  
**Scale/Scope**: One selected Range per call; corpus covers partial/nested DOM, contenteditable, frames, errors, and up to 100,000 UTF-16 code units  
**Open Clarifications**: None

## Constitution Check

*GATE: Passed before Phase 0 research; re-checked after Phase 1 design.*

| Constitutional obligation | Plan response | Pre-design | Post-design |
|---|---|---|---|
| Browser APIs isolated from core logic | DOM/Selection/Range access lives under `src/browser/selection/`; deterministic token assembly lives under `src/core/` | Pass | Pass |
| Ruby-safe linguistic correctness | Original DOM ancestry excludes HTML `rt`/`rp`; base Unicode and partial offsets are preserved | Pass | Pass |
| Cross-browser approved semantics | One frozen corpus and result contract execute on Chrome, Edge, Brave, and Firefox with explicit pending rows | Pass | Pass |
| Page-text minimization | Adapter traverses only the selected Range in the event frame; no parent/sibling/full-page fallback | Pass | Pass |
| Raw selected text remains ephemeral | No network, storage, cache, history, log, telemetry content, or public DOM reference | Pass | Pass |
| No fabricated/stale results | Unsupported, empty, inaccessible, stale, and unexpected cases return distinct content-free errors with no partial text | Pass | Pass |
| No competing knowledge/data store | F-02 creates no canonical resource, database entity, migration, provider, or persistent data | Pass | Pass |
| Meaningful regression tests | Pure rules use unit tests; browser-only behavior uses real-browser fixtures and privacy/side-effect evidence | Pass | Pass |
| Dependency governance | Existing approved packages are sufficient; no installation or package manifest change is planned | Pass | Pass |
| Explicit feature scope | Popup, debounce, analysis request, surrounding context, multi-range aggregation, auth, persistence, and OD-014 limits remain outside F-02 | Pass | Pass |

No constitutional exception or complexity waiver is required.

## Architecture and Data Flow

```text
F-03 caller in current frame (future)
        │
        ▼
Selection Reader boundary
        │ selection unavailable / inaccessible
        ├──────────────────────────────► content-free error
        ▼
Single-range validation and coherent snapshot
        │ invalid / multi / stale
        ├──────────────────────────────► content-free error
        ▼
DOM Range traversal (browser boundary)
        │
        ├─ selected eligible text ─────► text token
        ├─ selected <br> ──────────────► explicit-break token
        ├─ selected semantic block ────► block-boundary token
        └─ selected HTML <rt>/<rp> ────► skipped subtree
                                            │
                                            ▼
                              SelectedTextAccumulator (pure)
                                            │
                          empty ─────────────┼──────── complete
                            ▼               ▼
                  error:no-base-text   ok:{ text }
                                            │
                                            ▼
                                 F-01 classification
```

### Boundary ownership

- The selection reader is supplied by the current document/frame caller. It never discovers or walks other frames.
- Browser Range traversal owns DOM access, intersection checks, partial text clipping, annotation ancestry, and structural-element classification.
- The pure accumulator owns deterministic joining, explicit breaks, pending block boundaries, and leading/trailing suppression.
- The result boundary owns stable success/error shape and guarantees that errors contain no content.
- F-01 owns Japanese classification after successful extraction.
- F-03 will own event listening, debounce, interaction identity, and popup orchestration.

## Technical Design

### 1. Result and reader contract

Create a discriminated `RubySafeSelectionResult`:

- `{ status: "ok"; text: string }`
- `{ status: "error"; code: RubySafeSelectionErrorCode }`

The stable error codes are exactly those approved in SPEC §6. A `SelectionReader` callback obtains the Selection from the current frame. Access failure before acquisition maps to `inaccessible-context`; traversal failures are classified separately.

### 2. Validation order

The adapter must:

1. acquire Selection once;
2. return `no-selection` for null/empty/collapsed selection;
3. return `unsupported-multi-range` for more than one range before reading any range text;
4. capture the single range and expected document;
5. validate connected same-document boundaries and non-collapsed coherence;
6. traverse synchronously;
7. return complete assembled text or a content-free error.

This order prevents partial reads from unsupported multi-range selection.

### 3. Original-DOM traversal

Traverse the smallest useful original subtree rooted at the range common ancestor (or its parent when the common ancestor is a text node). Visit only nodes intersecting the range.

- Text nodes: clip only when the node is the start/end boundary; otherwise include the full intersecting text node.
- HTML `rt`/`rp`: skip the entire subtree before visiting text, including partial boundary cases.
- HTML `br`: emit an explicit-break token when selected/intersected.
- Reviewed semantic block elements: emit a block-boundary token after selected eligible content.
- Other elements: traverse eligible selected descendants without invented separators.
- Foreign-namespace elements named `rt` or `rp`: do not treat as HTML ruby annotations solely by local name.

No host node is removed, cloned into the page, rewritten, normalized, focused, or selected.

### 4. Deterministic text accumulator

The pure accumulator processes ordered tokens:

- append text payload exactly;
- append every explicit `LF`;
- hold a block boundary as pending until later eligible content;
- materialize at most one `LF` for pending nested/adjacent blocks;
- skip a block `LF` when output already ends with `LF`;
- discard pending block boundary at the end.

This preserves consecutive `br` breaks while avoiding outer and nested-block artifacts.

### 5. Stale and unexpected failures

Range/DOM conditions proving lost coherence map to `stale-selection`. A synthetic seam must exercise an unexpected traversal failure and verify `unexpected-failure`. Neither path returns accumulated content or raw diagnostics.

F-02 does not retry, reacquire a newer selection, or scan a larger context. A later selection is a new F-03 interaction.

### 6. Coverage policy

The Node coverage gate remains ≥85% for modules executable under Node. `src/browser/**` is explicitly excluded from Node coverage and receives mandatory Playwright coverage instead. An architecture test verifies DOM globals do not leak into `src/core/**` or unrelated boundaries.

This exclusion must be visible in review and cannot be used to omit browser tests.

## Browser and Frame Strategy

- The same harness contract runs in every configured Playwright project.
- A dedicated F-02 harness creates exact DOM and selection fixtures without altering the F-01 harness.
- Same-origin iframe tests execute the extraction boundary inside the selected frame and assert parent/sibling marker exclusion.
- Inaccessible-context behavior is exercised through the reader seam; tests must not request broader host permission.
- Multi-range behavior is tested through a conforming synthetic Selection-like boundary where an engine does not expose user-created multi-range selection.
- Brave and Chrome receive first manual validation; report rows remain mandatory for all four approved names.

## Testing Strategy

### Unit tests

- exact token accumulation and whitespace preservation;
- explicit vs block-boundary behavior;
- repeated/nested block-boundary coalescing;
- leading/trailing boundary suppression;
- discriminated result and content-free error shape;
- browser-dependency isolation;
- source-policy review for fixed structural tags.

### Browser tests

- plain/ruby/`rt`/`rp` and repeated-text ancestry;
- partial start/end inside annotation and base nodes;
- nested inline elements;
- `br`, consecutive `br`, blocks, existing whitespace;
- forward/backward ranges;
- Unicode and malformed UTF-16 preservation;
- contenteditable behavior;
- no/collapsed/multi/stale/inaccessible/unexpected errors;
- frame-local extraction;
- network/storage/log/history/retention privacy markers;
- DOM/style/focus/selection/editable/clipboard/navigation side effects;
- offline/locale/font/login-state invariance;
- long input and exact-output assertions.

### Performance evidence

Use browser timing around complete extraction and correctness validation. Profiles:

1. small plain;
2. nested ruby;
3. multi-block;
4. contenteditable;
5. 100,000-code-unit selection with eligible content at the end.

Record cold duration, warm p95, samples, exact browser version, OS, input length/shape, and correctness. Measurements remain ungraded until an approved F-02 allocation exists under PERF-001.

## Privacy and Security Review

- The public error/result types contain no page URL, selector, DOM, Range, Node, markup, partial text, or stack.
- Browser tests patch/observe fetch, XHR, WebSocket, beacon, storage, console, history, clipboard, focus, selection, and DOM sentinels.
- The traversal never evaluates scripts or inserts cloned markup.
- No permission, manifest, Backend, telemetry, cache, or persistence change belongs to F-02.
- Test fixtures use synthetic markers only.

## Project Structure

### Documentation

```text
.sdd/specs/001-ruby-safe-dom-extraction/
├── SPEC.md
├── PLAN.md
├── TASKS.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── ruby-safe-dom-extraction.md
├── checklists/
│   └── requirements.md
└── evidence/
    ├── corpus-results.md
    ├── browser-parity-privacy-report.md
    ├── performance-report.md
    ├── final-traceability.md
    └── final-review.md
```

### Source and tests

```text
extension/
├── src/
│   ├── boundary/
│   │   └── rubySafeSelectionResult.ts
│   ├── browser/
│   │   └── selection/
│   │       ├── extractRubySafeSelection.ts
│   │       └── semanticBlockElements.ts
│   ├── core/
│   │   └── selectedTextAccumulator.ts
│   └── index.ts
├── tests/
│   ├── corpus/
│   │   └── ruby-safe-selection-corpus.ts
│   ├── unit/
│   │   ├── selectedTextAccumulator.test.ts
│   │   ├── rubySafeSelectionContract.test.ts
│   │   └── browserBoundaryIsolation.test.ts
│   └── browser/
│       ├── ruby-safe-selection-harness.html
│       ├── ruby-safe-selection-harness.ts
│       ├── ruby-safe-selection-ruby.test.ts
│       ├── ruby-safe-selection-structure.test.ts
│       ├── ruby-safe-selection-errors-frames.test.ts
│       ├── ruby-safe-selection-privacy-side-effects.test.ts
│       └── ruby-safe-selection-performance.test.ts
└── vitest.config.ts
```

**Structure Decision**: Extend the existing single Extension workspace. DOM-specific files live under `src/browser/selection/`; reusable deterministic logic remains under `src/core/`; public result types remain under `src/boundary/`. No new application, service, package, manifest, database, or Backend module is created.

## Delivery Sequence

### Phase 0 — Research

Completed in [research.md](research.md):

- Selection/Range standards and live-range behavior;
- legacy ruby extraction evidence and rejected legacy behavior;
- browser/core boundary;
- annotation ancestry traversal;
- structural line policy;
- result/error contract;
- test/coverage and performance strategy.

No `NEEDS CLARIFICATION` remains.

### Phase 1 — Design and contracts

Completed:

- [data-model.md](data-model.md): ephemeral reader, snapshot, token, text, error, and result model;
- [local extraction contract](contracts/ruby-safe-dom-extraction.md): input/output/error/privacy compatibility contract;
- [quickstart.md](quickstart.md): validation commands, scenario groups, and expected evidence.

Post-design Constitution Check remains fully passing.

### Phase 2 — Test-first implementation

1. Establish result/token contracts and corpus fixtures.
2. Add failing pure accumulator tests, then implement the accumulator.
3. Add failing US1 browser tests, then implement ruby-safe traversal.
4. Add failing US2 structural/partial/editable tests, then complete boundary semantics.
5. Add failing US3 error/frame/privacy tests, then complete validation and safe failure.
6. Add performance/parity evidence and run complete regression.
7. Complete traceability and final constitutional review.

## Requirement Traceability Strategy

| Requirement group | Planned implementation | Primary verification |
|---|---|---|
| F02-FR-001–004, 008–009 | Original-range traversal and annotation ancestry | Ruby/partial/Unicode browser corpus |
| F02-FR-005–007 | Pure token accumulator + fixed block policy | Unit token tests + structure browser fixtures |
| F02-FR-010 | Same adapter for contenteditable | Contenteditable browser fixture and mutation sentinel |
| F02-FR-011–012 | Caller-bound Selection reader | Same-frame iframe and inaccessible-reader tests |
| F02-FR-013–015 | Validation and error mapping | No/collapsed/multi/stale/empty tests |
| F02-FR-016 | Public success contract and F-01 handoff fixture | Contract/integration harness test |
| F02-FR-017–019 | Scope and privacy/side-effect boundaries | Source inspection + browser observers |
| F02-FR-020 | Complete traversal | 100,000-code-unit exact-output test |
| F02-NFR-001–009 | Combined core/browser design | Coverage, parity, privacy, performance, final review |
| F02-AC-001–030 | Stable corpus IDs | Final traceability matrix |
| F02-SC-001–007 | Evidence artifacts | Final review with pass/fail/pending status |

## Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Partial selection loses ruby ancestry | Annotation leaks or base text disappears | Traverse original DOM and test boundaries inside `rt`/`rp` |
| Browser stringification differs | Cross-browser output divergence | Do not use rendered Selection string as authority |
| CSS changes block behavior | Host page controls linguistic input | Fixed semantic block set independent of computed style |
| Nested blocks create duplicate newlines | Incorrect input and offsets | Pending block token with coalescing and exact fixtures |
| Multiple ranges silently lose content | Misleading analysis | Reject before reading any range |
| Live Range changes after selection | Stale/incorrect text | Validate coherent connected snapshot and return no partial output |
| Node coverage hides DOM risk | False confidence | Explicit browser-source exclusion plus mandatory Playwright coverage |
| Raw text leaks in errors/tests | Privacy violation | Content-free union and synthetic marker observers |
| Performance target is invented | False release claim | Record measurements; retain threshold status as pending |

## Complexity Tracking

No constitutional violation requires justification. The pure accumulator is the only additional abstraction and exists to keep browser-independent text semantics deterministic and unit-testable.

## Definition of Done

- Every implementation task is complete.
- Exact corpus and error cases pass.
- Node type-check, coverage, build, and Unicode reproducibility remain green.
- Available-browser Playwright suites pass; every named browser has explicit pass/fail/pending evidence.
- Privacy and host-page observers find no prohibited effect.
- Performance profiles are recorded without a fabricated threshold claim.
- Every F02-FR, F02-NFR, F02-AC, and F02-SC maps to implementation/test/evidence.
- Final review finds no scope creep, hidden dependency, privacy violation, or constitutional exception.

