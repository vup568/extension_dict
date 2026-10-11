# F-02 Final Traceability

**Date**: 2026-09-24  
**Branch**: `feat/ruby-safe-dom-extraction`  
**Legend**: PASS = implemented and verified; PENDING = required named-browser evidence is unavailable, not inferred.

## Functional requirements

| ID | Implementation | Primary verification | State |
|---|---|---|---|
| F02-FR-001 | `extractRubySafeSelection.ts` range validation/traversal | ruby and structure browser corpus | PASS |
| F02-FR-002 | ordered original-DOM traversal | forward/backward corpus | PASS |
| F02-FR-003 | HTML-namespace `rt`/`rp` subtree exclusion | ruby, `rp`, foreign-namespace regressions | PASS |
| F02-FR-004 | eligible ruby/base descendants | exact ruby corpus | PASS |
| F02-FR-005 | `selectedTextAccumulator.ts` text tokens | accumulator unit + inline browser tests | PASS |
| F02-FR-006 | explicit-break token | `br` and consecutive-`br` tests | PASS |
| F02-FR-007 | reviewed semantic blocks + pending boundary | block/LF unit and browser tests | PASS |
| F02-FR-008 | exact text slicing, no normalization | whitespace, U+0000 and Unicode tests | PASS |
| F02-FR-009 | boundary-node clipping | partial base/annotation tests | PASS |
| F02-FR-010 | same adapter for editable DOM | contenteditable mutation tests | PASS |
| F02-FR-011 | caller-bound `SelectionReader` | same-frame iframe test | PASS |
| F02-FR-012 | reader failure mapping | inaccessible-context test | PASS |
| F02-FR-013 | zero/collapsed/multi validation | error contract tests | PASS |
| F02-FR-014 | coherent snapshot validation | stale invalidation test | PASS |
| F02-FR-015 | empty eligible assembly mapping | annotation-only test | PASS |
| F02-FR-016 | public result/export and F-01 handoff | ruby-safe extraction → F-01 test | PASS |
| F02-FR-017 | extraction-only module | runtime source/build inspection | PASS |
| F02-FR-018 | ephemeral processing | privacy marker tests | PASS |
| F02-FR-019 | read-only host interaction | host side-effect tests | PASS |
| F02-FR-020 | complete traversal without truncation | 100,000-code-unit test/profile | PASS |

## Non-functional requirements

| ID | Evidence | State |
|---|---|---|
| F02-NFR-001 | exact 24-case corpus plus error/structure suites | PASS |
| F02-NFR-002 | frame-local and neighboring-content tests | PASS |
| F02-NFR-003 | request/storage/history/log marker observers | PASS |
| F02-NFR-004 | Chrome, Edge and Brave pass; Firefox executable unavailable | PENDING |
| F02-NFR-005 | DOM/style/focus/selection/editable/clipboard/navigation/event sentinels | PASS |
| F02-NFR-006 | environment and repeated-call test | PASS |
| F02-NFR-007 | five-profile cold/warm report; no fabricated threshold | PASS |
| F02-NFR-008 | browser isolation source test and directory boundary | PASS |
| F02-NFR-009 | original-node text-only traversal and safe error tests | PASS |

## Acceptance criteria

| ID | Verification | State |
|---|---|---|
| F02-AC-001 | `F02-ruby-reading-excluded` | PASS |
| F02-AC-002 | `F02-ruby-rp-excluded` | PASS |
| F02-AC-003 | plain/multiple-ruby/foreign-namespace cases | PASS |
| F02-AC-004 | `F02-repeated-text-ancestry` | PASS |
| F02-AC-005 | partial boundary corpus | PASS |
| F02-AC-006 | base→annotation and annotation→base cases | PASS |
| F02-AC-007 | annotation-only error | PASS |
| F02-AC-008 | nested-inline exact output | PASS |
| F02-AC-009 | explicit `br` exact output | PASS |
| F02-AC-010 | adjacent blocks/existing LF | PASS |
| F02-AC-011 | consecutive `br` | PASS |
| F02-AC-012 | whitespace, combining, supplementary, variation selector, replacement, lone surrogate, U+0000 | PASS |
| F02-AC-013 | forward/backward cases | PASS |
| F02-AC-014 | contenteditable exact output and unchanged state | PASS |
| F02-AC-015 | none/collapsed errors | PASS |
| F02-AC-016 | synthetic multi-range rejected before `getRangeAt` | PASS |
| F02-AC-017 | selection invalidated during traversal | PASS |
| F02-AC-018 | event-frame selection | PASS |
| F02-AC-019 | parent/sibling private markers absent | PASS |
| F02-AC-020 | throwing selection reader | PASS |
| F02-AC-021 | injected traversal failure | PASS |
| F02-AC-022 | host invariance sentinel | PASS |
| F02-AC-023 | offline/online and environment invariance | PASS |
| F02-AC-024 | Chrome/Edge/Brave pass; Firefox unavailable | PENDING |
| F02-AC-025 | success and all error privacy markers | PASS |
| F02-AC-026 | complete 100,000-code-unit output + timing | PASS |
| F02-AC-027 | generated CSS and neighbor exclusion | PASS |
| F02-AC-028 | non-Japanese base handoff to F-01 | PASS |
| F02-AC-029 | runtime import/build/scope review | PASS |
| F02-AC-030 | this matrix maps every required identifier | PASS |

## Success criteria

| ID | Evidence | State |
|---|---|---|
| F02-SC-001 | mandatory ruby fixtures exact on three installed browsers | PASS |
| F02-SC-002 | structure/editable/Unicode/long fixtures exact | PASS |
| F02-SC-003 | three named browsers pass; Firefox unavailable | PENDING |
| F02-SC-004 | every defined error returns exact content-free code | PASS |
| F02-SC-005 | privacy and host-safety observers | PASS |
| F02-SC-006 | five-profile performance report on three browsers | PASS |
| F02-SC-007 | SPEC → implementation → tests → evidence mapping | PASS |

## Evidence locations

- `corpus-results.md`: exact corpus and focused story results.
- `browser-parity-privacy-report.md`: named-browser, error, frame, privacy, and host-safety status.
- `performance-report.md`: correctness-asserting cold/warm measurements.
- `final-review.md`: dependency, build, regression, scope, constitutional, and migration review.
