# Tasks: Ruby-Safe DOM Extraction (F-02)

**Input**: [SPEC.md](SPEC.md), [PLAN.md](PLAN.md), [research.md](research.md), [data-model.md](data-model.md), [local contract](contracts/ruby-safe-dom-extraction.md), [quickstart.md](quickstart.md)  
**Branch**: `feat/ruby-safe-dom-extraction`  
**Status**: Complete — 46/46 tasks implemented and validated  
**Organization**: Tasks are grouped by the three prioritized user stories in SPEC.md. Automated tests are mandatory because exact DOM/ruby behavior, privacy, browser parity, and host-page side effects are acceptance requirements.

## Format

Every task uses: checkbox, sequential task ID, optional `[P]` marker, required story label inside story phases, an actionable description, and an exact file path.

- `[P]` means the task writes a different file and can proceed without an incomplete adjacent task.
- `[US1]`, `[US2]`, and `[US3]` map to the three user stories in SPEC.md.
- Tests for a story must be written and observed failing for the intended missing behavior before that story's runtime implementation.
- No task may add a package, popup, selection listener, debounce, Backend request, persistence, authentication, surrounding-page context, or OD-014 length policy.

## Phase 1: Setup and governance

**Purpose**: Make the existing Extension toolchain ready for a browser-only boundary without adding dependencies or weakening evidence.

- [X] T001 Record that F-02 reuses the five already approved Extension dev dependencies and introduces no package/version change in .sdd/specs/001-ruby-safe-dom-extraction/implementation-approval.md
- [X] T002 Update extension/tsconfig.json so the F-02 browser harness/tests and performance test are type-checked without enabling non-strict settings
- [X] T003 Update extension/vitest.config.ts to exclude extension/src/browser/** only from Node coverage and document that mandatory Playwright coverage is the compensating boundary evidence

**Checkpoint**: Tooling remains locked, strict, and explicit about the Node/browser coverage split.

---

## Phase 2: Foundational contracts and pure assembly

**Purpose**: Establish shared result/token contracts and deterministic text assembly before DOM behavior.

**CRITICAL**: All user stories depend on this phase.

### Tests

- [X] T004 [P] Add failing exhaustive success/error shape and no-content error tests in extension/tests/unit/rubySafeSelectionContract.test.ts
- [X] T005 [P] Add failing token assembly tests for exact text, explicit breaks, pending/coalesced block boundaries, and leading/trailing suppression in extension/tests/unit/selectedTextAccumulator.test.ts
- [X] T006 [P] Add failing source-boundary tests proving DOM/browser globals stay outside extension/src/core/** and unrelated boundaries in extension/tests/unit/browserBoundaryIsolation.test.ts

### Implementation

- [X] T007 Implement the discriminated result and exact six-code error union in extension/src/boundary/rubySafeSelectionResult.ts
- [X] T008 Implement browser-independent extraction tokens and deterministic assembly in extension/src/core/selectedTextAccumulator.ts
- [X] T009 [P] Define the reviewed, CSS-independent HTML semantic block set in extension/src/browser/selection/semanticBlockElements.ts
- [X] T010 [P] Define stable corpus case IDs, DOM fixture descriptors, range descriptors, and independent expected outcomes in extension/tests/corpus/ruby-safe-selection-corpus.ts

**Checkpoint**: Contract/accumulator/isolation tests pass; no DOM extraction implementation exists yet.

---

## Phase 3: User Story 1 — Extract clean Japanese base text (Priority: P1) 🎯 MVP slice

**Goal**: Return exact selected base text while excluding every selected HTML `rt`/`rp` descendant, including partial annotation boundaries.

**Independent Test**: Run the ruby-focused browser corpus against the dedicated harness; plain/ruby/rp/repeated-text/partial-boundary/annotation-only/Unicode cases must return their exact success text or `no-base-text`.

### Tests for User Story 1

- [X] T011 [P] [US1] Create the dedicated host page with plain, ruby, rp, nested-inline, repeated-text, partial-boundary, and annotation-only fixtures in extension/tests/browser/ruby-safe-selection-harness.html
- [X] T012 [P] [US1] Add F02-AC-001–008, F02-AC-012, and F02-AC-028 ruby/base-text cases with independent expected values in extension/tests/corpus/ruby-safe-selection-corpus.ts
- [X] T013 [US1] Add failing Playwright tests for exact ruby exclusion, original ancestry, partial start/end clipping, Unicode preservation, and annotation-only behavior in extension/tests/browser/ruby-safe-selection-ruby.test.ts

### Implementation for User Story 1

- [X] T014 [US1] Implement one-range original-DOM traversal, selected text-node clipping, and HTML-namespace rt/rp subtree exclusion in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T015 [US1] Map successful complete text and annotation-only empty output to the F-02 result contract in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T016 [US1] Expose deterministic fixture selection creation and F-02 extraction functions to Playwright in extension/tests/browser/ruby-safe-selection-harness.ts
- [X] T017 [US1] Export the public F-02 result types and extraction boundary without changing F-01 exports in extension/src/index.ts
- [X] T018 [US1] Run the focused US1 unit/browser subset and record exact case totals and outcomes in .sdd/specs/001-ruby-safe-dom-extraction/evidence/corpus-results.md

**Checkpoint**: US1 independently proves clean ruby-safe base text but is not release-ready without structural and safe-failure stories.

---

## Phase 4: User Story 2 — Preserve meaningful DOM structure (Priority: P2)

**Goal**: Preserve inline order, partial selection, Unicode/whitespace, `br`, semantic block boundaries, direction, and contenteditable values without mutation.

**Independent Test**: Run structure fixtures and compare exact strings, including `LF` placement, consecutive explicit breaks, forward/backward equivalence, and unmodified contenteditable state.

### Tests for User Story 2

- [X] T019 [US2] Add F02-AC-009–014 and F02-AC-026–027 structure/contenteditable/long cases to extension/tests/corpus/ruby-safe-selection-corpus.ts
- [X] T020 [P] [US2] Add failing Playwright tests for inline, br, consecutive-br, nested block, existing-whitespace, forward/backward, and partial-range semantics in extension/tests/browser/ruby-safe-selection-structure.test.ts
- [X] T021 [P] [US2] Add failing Playwright tests for contenteditable, supplementary CJK, decomposed kana, variation selectors, replacement characters, and malformed UTF-16 preservation in extension/tests/browser/ruby-safe-selection-contenteditable-unicode.test.ts

### Implementation for User Story 2

- [X] T022 [US2] Emit explicit-break and semantic block-boundary tokens during selected DOM traversal in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T023 [US2] Apply the reviewed semantic block policy without computed-style dependence in extension/src/browser/selection/semanticBlockElements.ts
- [X] T024 [US2] Complete forward/backward document-order, element-boundary, existing-whitespace, and contenteditable extraction semantics in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T025 [US2] Extend the harness with deterministic block, direction, editable, Unicode, and 100,000-code-unit selection builders in extension/tests/browser/ruby-safe-selection-harness.ts
- [X] T026 [US2] Run US1+US2 regression and append structure case totals and exact-output results to .sdd/specs/001-ruby-safe-dom-extraction/evidence/corpus-results.md

**Checkpoint**: US1 and US2 produce complete exact text for supported selections; error/frame/privacy hardening remains.

---

## Phase 5: User Story 3 — Fail safely in hostile or changing contexts (Priority: P3)

**Goal**: Return content-free errors for invalid, multi-range, empty-base, stale, inaccessible, or unexpected cases; remain frame-local and create zero host/privacy side effects.

**Independent Test**: Exercise every error category, same-frame iframe locality, privacy markers, hostile host state, and environmental invariance; no partial or unrelated content may escape.

### Tests for User Story 3

- [X] T027 [P] [US3] Add failing no-selection, collapsed, multi-range, stale, inaccessible-reader, no-base, and unexpected-failure contract cases in extension/tests/browser/ruby-safe-selection-errors-frames.test.ts
- [X] T028 [US3] Add failing allowed-iframe, parent marker, and sibling-frame marker locality cases in extension/tests/browser/ruby-safe-selection-errors-frames.test.ts
- [X] T029 [P] [US3] Add failing network/storage/history/log/retention marker assertions for success and every error path in extension/tests/browser/ruby-safe-selection-privacy-side-effects.test.ts
- [X] T030 [US3] Add failing DOM/style/focus/selection/editable/clipboard/navigation/event-default invariance assertions in extension/tests/browser/ruby-safe-selection-privacy-side-effects.test.ts
- [X] T031 [P] [US3] Add offline/online, locale, page-language, font, login-state, and repeated-call invariance tests in extension/tests/browser/ruby-safe-selection-environment.test.ts

### Implementation for User Story 3

- [X] T032 [US3] Implement single-acquisition reader validation and no/collapsed/multi-range mapping before content access in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T033 [US3] Implement connected same-document snapshot validation, stale classification, and no-partial-output rollback in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T034 [US3] Implement inaccessible-reader and unexpected-failure mapping with content-free diagnostics in extension/src/browser/selection/extractRubySafeSelection.ts
- [X] T035 [US3] Complete frame-local harness execution without parent/sibling aggregation or permission broadening in extension/tests/browser/ruby-safe-selection-harness.ts
- [X] T036 [US3] Run the complete US3 suite and record error/frame/privacy/side-effect outcomes in .sdd/specs/001-ruby-safe-dom-extraction/evidence/browser-parity-privacy-report.md

**Checkpoint**: All three user stories are independently verifiable and integrated under the local contract.

---

## Phase 6: Performance, parity, regression, and delivery evidence

**Purpose**: Validate the complete approved scope without inventing missing browser or latency evidence.

- [X] T037 Add cold/warm correctness-asserting profiles for small plain, nested ruby, multi-block, contenteditable, and 100,000-code-unit selections in extension/tests/browser/ruby-safe-selection-performance.test.ts
- [X] T038 Run the F-02 performance profiles on each available approved browser and record OS, browser/version, input shape/length, sample counts, cold time, and warm p95 without a pass threshold in .sdd/specs/001-ruby-safe-dom-extraction/evidence/performance-report.md
- [X] T039 Run the complete corpus on Chrome, Edge, Brave, and Firefox where available and record exact pass/fail/pending rows without substituting one Chromium browser for another in .sdd/specs/001-ruby-safe-dom-extraction/evidence/browser-parity-privacy-report.md
- [X] T040 Run npm ci, unicode verification, TypeScript type-check, Node coverage, Vite build, and the complete available-browser suite using commands in .sdd/specs/001-ruby-safe-dom-extraction/quickstart.md
- [X] T041 Run existing .NET Release build and unit regression checks, and run PostgreSQL integration tests only when Docker is available; record environment-blocked checks explicitly in .sdd/specs/001-ruby-safe-dom-extraction/evidence/final-review.md
- [X] T042 Review runtime imports and built artifacts for prohibited network/storage/logging/popup/auth/Backend dependencies and record findings in .sdd/specs/001-ruby-safe-dom-extraction/evidence/final-review.md
- [X] T043 Update .sdd/specs/001-ruby-safe-dom-extraction/quickstart.md only where actual verified commands or prerequisites differ from the planned guide
- [X] T044 Map every F02-FR, F02-NFR, F02-AC, and F02-SC to implementation, tests, evidence, and pass/fail/pending state in .sdd/specs/001-ruby-safe-dom-extraction/evidence/final-traceability.md
- [X] T045 Complete scope, privacy, browser, performance, dependency, migration, and constitutional review in .sdd/specs/001-ruby-safe-dom-extraction/evidence/final-review.md
- [X] T046 Update F-02 implementation status, verified limitations, and encountered issues in PLAN.md after final evidence is complete

---

## Dependencies & Execution Order

### Phase dependencies

1. **Phase 1** starts immediately.
2. **Phase 2** depends on Phase 1 and blocks all user stories.
3. **US1** depends on Phase 2.
4. **US2** depends on the traversal/result slice delivered by US1 but remains independently testable through its own exact-output fixtures.
5. **US3** depends on the boundary created by US1; its error/frame/privacy suite can be developed alongside late US2 work when files do not overlap.
6. **Phase 6** depends on all three stories.

### Story dependency graph

```text
Setup
  └─ Foundation
       └─ US1 Ruby-safe base text
            ├─ US2 Structure/contenteditable
            └─ US3 Errors/frame/privacy
                  └─ Final parity/performance/evidence
```

### Within each user story

1. Add corpus/fixture data.
2. Write the story's tests.
3. Run the focused tests and confirm failure for missing behavior.
4. Implement the smallest contract-compliant behavior.
5. Run the story plus all earlier story regression.
6. Record evidence only from actual execution.

## Parallel Opportunities

- T004, T005, and T006 use different unit-test files.
- T009 and T010 use different source/corpus files after result/token concepts are agreed.
- T011 and T012 can proceed in parallel.
- T019, T020, and T021 use corpus and separate browser-test files; coordinate only the shared corpus append.
- T027–T031 target independent behavior groups, although T027/T028 share one file and must be serialized with each other.
- Documentation tasks T043–T045 may be prepared in parallel only after final command outputs exist.
- Browser runs for different configured projects may run independently if local CPU/memory allows and each report preserves the browser identity.

## Parallel Examples

### User Story 1

```text
Task T011: Create dedicated ruby-selection browser harness HTML
Task T012: Add independently expected ruby corpus cases
```

After both complete, T013 writes the failing browser tests.

### User Story 2

```text
Task T020: Add structure and line-boundary browser tests
Task T021: Add contenteditable and Unicode browser tests
```

Both depend on T019 corpus IDs but write different files.

### User Story 3

```text
Task T029: Add privacy marker observers
Task T031: Add environment-invariance tests
```

These write separate files and can run alongside serialized error/frame test work.

## Implementation Strategy

### MVP development slice

1. Complete setup and foundational contracts.
2. Implement US1 only.
3. Demonstrate exact ruby stripping and partial annotation handling in a real browser harness.

US1 is a useful development checkpoint, but F-02 is not release-ready until US2, US3, parity, privacy, and final evidence are complete.

### Incremental delivery

1. **US1**: trustworthy base text without ruby annotation.
2. **US2**: exact structure, Unicode, direction, and editable content.
3. **US3**: safe errors, frame locality, privacy, and host invariance.
4. **Evidence**: performance, named-browser status, full regression, and traceability.

## Stop Conditions

- Stop if implementation needs a new package or browser permission; obtain explicit owner approval and update PLAN.md first.
- Stop and amend SPEC.md if correct behavior requires a different line-boundary, iframe, multi-range, truncation, or surrounding-context policy.
- Do not move selection listening, debounce, popup, requests, or stale asynchronous response behavior from F-03/F-04 into F-02.
- Record unavailable browser/Docker evidence as pending or environment-blocked; never infer pass.
- Do not use a Node coverage exclusion unless the corresponding Playwright boundary tests exist and run.

## Definition of Done

F-02 is done only when T001–T046 are complete, the exact corpus and privacy/side-effect checks pass, all four named browsers have honest evidence status, performance is measured without a fabricated threshold, existing regressions remain green, and final traceability shows no hidden scope or constitutional violation.

