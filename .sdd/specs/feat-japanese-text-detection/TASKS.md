# Tasks: Japanese Text Detection (F-01)

**Input:** [SPEC.md](SPEC.md), [PLAN.md](PLAN.md), [research.md](research.md), [data-model.md](data-model.md), [local contract](contracts/japanese-text-detector.md), [quickstart.md](quickstart.md)
**Task status:** All 37 implementation/evidence tasks executed
**Implementation status:** Core implementation is complete; release evidence remains pending for Brave, Firefox, and the OD-005 performance budget
**Organization:** Tasks are grouped by the three P1 user stories in SPEC.md. Test tasks are mandatory because the specification and Constitution require regression, privacy, browser-parity and performance evidence.

## Format

Every task uses: checkbox, sequential task ID, optional [P] marker, required story label inside story phases, an actionable description and exact file path.

- [P] means the task can run in parallel with adjacent eligible tasks because it writes a different file and does not depend on their incomplete output.
- [US1], [US2], and [US3] map directly to SPEC.md user stories US-01, US-02, and US-03.
- A task that installs or changes dependencies may start only after T001 records explicit owner approval.

## Phase 1: Setup and approval gate

**Purpose:** Establish the approved Extension toolchain without silently selecting packages or versions.

- [X] T001 Record explicit owner approval for the Extension workspace, package manager, exact build/test/browser packages and versions in .sdd/specs/feat-japanese-text-detection/implementation-approval.md; stop before T002 when approval is absent
- [X] T002 Create the approved TypeScript/Vite Extension workspace metadata in extension/package.json and extension/tsconfig.json using only the packages and versions recorded by T001
- [X] T003 Add the verified build, type-check, unit-test, browser-test and benchmark scripts to extension/package.json without installing unrelated UI, DOM, Backend or persistence dependencies
- [X] T004 Replace the current tooling-pending note with the verified commands and prerequisites in .sdd/specs/feat-japanese-text-detection/quickstart.md

**Checkpoint:** The repository has an explicitly approved, reproducible toolchain. No user story work begins if T001 is not satisfied.

## Phase 2: Foundational Unicode policy and shared contract

**Purpose:** Create the reproducible policy data, common result types and corpus representation required by every story.

- [X] T005 [P] Add Unicode 17.0.0 source URLs, source checksums, license/notice references, policy version and generator metadata to extension/unicode/unicode17-sources.manifest.json
- [X] T006 Implement deterministic extraction of the four SPEC.md trigger sets from the pinned UCD inputs in extension/unicode/generate-trigger-ranges.ts
- [X] T007 Add generation validation for sorted, non-overlapping ranges, policy membership and reproducible output checksum in extension/tests/unit/unicode17PolicyGeneration.test.ts
- [X] T008 Run the validated generator and review the resulting immutable policy table in extension/src/core/unicode17TriggerRanges.generated.ts
- [X] T009 [P] Define classification, invalid-input and unexpected-failure outcome types without raw-input fields in extension/src/boundary/japaneseTextDetectionResult.ts
- [X] T010 [P] Define stable corpus case IDs plus literal/code-point/UTF-16-code-unit input representations in extension/tests/corpus/japanese-text-detection-corpus.ts
- [X] T011 Verify the generated data and manifest satisfy provenance, licensing and checksum requirements, then record the review in .sdd/specs/feat-japanese-text-detection/evidence/unicode-provenance-review.md

**Checkpoint:** Unicode 17.0.0 membership is reproducible and reviewable; tests can represent supplementary and malformed UTF-16 inputs without ambiguity.

## Phase 3: User Story 1 — Recognize supported Japanese text (P1) — MVP checkpoint

**Goal:** A valid string containing at least one approved Kanji/Kana trigger returns true, including supplementary and mixed-script cases.

**Independent test:** Run only the US1-tagged corpus cases covering F01-AC-001–008 and F01-AC-017. Every case returns true without DOM, Backend, network or authentication.

### Tests for User Story 1

- [X] T012 [US1] Add independent positive corpus entries for F01-AC-001–008 and F01-AC-017, including standalone U+20B9F and explicit supplementary code points, to extension/tests/corpus/japanese-text-detection-corpus.ts
- [X] T013 [P] [US1] Add failing core tests for single-character, mixed-script, compatibility, decomposed and supplementary positives in extension/tests/unit/japaneseTextDetector.positive.test.ts
- [X] T014 [P] [US1] Add failing boundary contract tests for valid-string true outcomes and input preservation in extension/tests/unit/classifyJapaneseText.success.test.ts

### Implementation for User Story 1

- [X] T015 [US1] Implement full code-point scanning and deterministic generated-range membership with EARS traceability in extension/src/core/japaneseTextDetector.ts
- [X] T016 [US1] Implement the valid-string classification path without coercion or side effects in extension/src/boundary/classifyJapaneseText.ts
- [X] T017 [US1] Run the US1 corpus subset and record case IDs, policy version and results in .sdd/specs/feat-japanese-text-detection/evidence/us1-positive-results.md

**Checkpoint:** US1 is independently demonstrable. This is a development MVP checkpoint, not release completion for F-01.

## Phase 4: User Story 2 — Reject unsupported text and malformed input safely (P1)

**Goal:** Unsupported text returns false, invalid types/errors remain distinct from false, and the classifier never normalizes, decodes, repairs or truncates input.

**Independent test:** Run only US2 and boundary-error cases covering F01-AC-009–026. Negatives return false, invalid values report invalid-input, simulated execution failure reports unexpected-failure, and valid triggers adjacent to lone surrogates still return true.

### Tests for User Story 2

- [X] T018 [US2] Add independent negative corpus entries for F01-AC-009–020, including marks, symbols, unassigned code points, literal mojibake and literal entity/escape strings, to extension/tests/corpus/japanese-text-detection-corpus.ts
- [X] T019 [US2] Add full-input, repeated-call and malformed UTF-16 corpus entries for F01-AC-021–025 to extension/tests/corpus/japanese-text-detection-corpus.ts
- [X] T020 [P] [US2] Add failing core tests for false classifications, no normalization/decoding/repair, 100,000-character full scans, repeatability and lone-surrogate continuation in extension/tests/unit/japaneseTextDetector.negative.test.ts
- [X] T021 [P] [US2] Add failing boundary tests for non-string values and a simulated execution failure that assert non-content diagnostics in extension/tests/unit/classifyJapaneseText.errors.test.ts

### Implementation for User Story 2

- [X] T022 [US2] Complete false-path, no-truncation and malformed-surrogate behavior while preserving the exact input in extension/src/core/japaneseTextDetector.ts
- [X] T023 [US2] Complete invalid-input and unexpected-failure handling so neither can be consumed as true or false in extension/src/boundary/classifyJapaneseText.ts
- [X] T024 [US2] Run the US2 and boundary-error subsets and record case IDs, outcome categories and results in .sdd/specs/feat-japanese-text-detection/evidence/us2-negative-error-results.md

**Checkpoint:** Positive, negative and error semantics are complete and independently verifiable.

## Phase 5: User Story 3 — Cross-browser consistency and privacy (P1)

**Goal:** The same policy and corpus produce the same outcomes on Chrome, Edge, Brave and Firefox without network, storage, raw-input logs, retained input or page side effects.

**Independent test:** Execute F01-AC-027–031 with synthetic markers. All available browsers return identical classifications/error categories; every required but unavailable browser is marked pending; DOM, focus, selection, storage and request observations remain unchanged.

### Tests and harnesses for User Story 3

- [X] T025 [P] [US3] Add dependency-isolation tests proving the core imports no DOM, browser-vendor, storage, network, authentication, React or Backend modules in extension/tests/unit/noRuntimeDependencies.test.ts
- [X] T026 [P] [US3] Add the shared-corpus parity harness with policy/browser/OS version capture in extension/tests/browser/japanese-text-detection-parity.test.ts
- [X] T027 [P] [US3] Add synthetic-marker checks for success, false and error paths with request, storage, logging and retained-reference observation in extension/tests/browser/japanese-text-detection-privacy.test.ts
- [X] T028 [P] [US3] Add before/after DOM, style, focus and selection assertions in extension/tests/browser/host-page-no-side-effects.test.ts
- [X] T029 [P] [US3] Add online/offline, anonymous/authenticated, locale and page-context invariance checks in extension/tests/browser/japanese-text-detection-environment.test.ts
- [X] T030 [US3] Run the complete corpus on Chrome, Edge, Brave and Firefox and record versions, case counts, mismatches and explicit pending browsers in .sdd/specs/feat-japanese-text-detection/evidence/browser-parity-privacy-report.md

**Checkpoint:** F01-SC-002 and F01-SC-003 are passed only when all four browser rows and privacy checks contain passing evidence.

## Phase 6: Performance, CI and final evidence

**Purpose:** Complete the cross-cutting evidence needed to claim F-01 is done without inventing an unapproved performance threshold.

- [X] T031 Record the approved OD-005 detector budget, or explicitly retain its pending status, in .sdd/specs/feat-japanese-text-detection/evidence/performance-budget.md; do not claim F01-SC-005 while pending
- [X] T032 Add small, medium and 100,000-character cold/warm benchmark profiles for positive-first, positive-last, negative and supplementary inputs in extension/tests/performance/japanese-text-detection-benchmark.ts
- [X] T033 Run the benchmark and record device, OS, browser/version, policy version, sample counts and p95 without input truncation in .sdd/specs/feat-japanese-text-detection/evidence/performance-report.md
- [X] T034 Add approved Extension type-check, unit, browser and reproducibility jobs while preserving existing .NET jobs in .github/workflows/ci.yml
- [X] T035 Map every F01-FR, F01-NFR, F01-AC and F01-SC to implementation/test/evidence status in .sdd/specs/feat-japanese-text-detection/evidence/final-traceability.md
- [X] T036 Execute every verified command in .sdd/specs/feat-japanese-text-detection/quickstart.md and update that file only when an actual command or prerequisite differs
- [X] T037 Review scope, privacy, provenance, licensing, browser evidence, errors and constitutional compliance, then record release-ready or explicit pending findings in .sdd/specs/feat-japanese-text-detection/evidence/final-review.md

## Dependencies and execution order

### Phase dependencies

1. Phase 1 has no technical dependency, but T001 is a mandatory human-approval gate.
2. Phase 2 depends on the approved workspace and test runner from Phase 1.
3. US1 depends on Phase 2.
4. US2 depends on US1's core and boundary implementation because both stories exercise the same public classifier.
5. US3 depends on completed US1 and US2 semantics so browser/privacy evidence uses the final corpus and outcome contract.
6. Phase 6 depends on all three stories; T034 may be prepared after verified scripts exist, but final CI evidence waits for the complete suite.

### User-story dependency graph

    Approval and Setup
            |
      Unicode Foundation
            |
       US1 Positive
            |
    US2 Negative/Error
            |
     US3 Parity/Privacy
            |
    Performance and DoD

All three stories have P1 product priority. The execution order reflects shared-code dependency, not a reduction in priority.

### Parallel opportunities

- After T002–T004, T005, T009 and T010 can run in parallel.
- After T012, T013 and T014 can run in parallel.
- After T018–T019, T020 and T021 can run in parallel.
- After US2 is complete, T025–T029 can run in parallel because each owns a different test file.
- Documentation/evidence tasks must wait for the executions they report and must never be pre-filled as passing.

## Parallel examples

### User Story 1

- T013: positive core behavior in extension/tests/unit/japaneseTextDetector.positive.test.ts
- T014: successful boundary contract in extension/tests/unit/classifyJapaneseText.success.test.ts

### User Story 2

- T020: negative and malformed-string core behavior in extension/tests/unit/japaneseTextDetector.negative.test.ts
- T021: invalid-input and unexpected-failure contract in extension/tests/unit/classifyJapaneseText.errors.test.ts

### User Story 3

- T025: dependency isolation
- T026: browser parity
- T027: privacy
- T028: host-page side effects
- T029: environmental invariance

## Implementation strategy

### Development MVP

Complete Phase 1, Phase 2 and US1, then run the US1 subset. This proves the central positive-detection value but is not releasable F-01 because negative/error, privacy, four-browser and benchmark evidence remain mandatory.

### Incremental delivery

1. Establish approved tooling and reproducible Unicode policy.
2. Deliver and validate US1 positive recognition.
3. Add US2 negative/error behavior and rerun US1 to prevent regression.
4. Add US3 parity/privacy evidence using the final corpus.
5. Complete performance, CI, traceability and constitutional review.

### Stop conditions

- Stop before T002 if T001 does not contain explicit approval.
- Stop and amend SPEC.md/PLAN.md before implementation if a task requires different trigger semantics, input normalization, language identification, DOM extraction, Backend changes or persistence.
- Record a browser as pending rather than passed when it cannot be run.
- Record F01-SC-005 as pending rather than passed while OD-005 lacks an approved detector budget.

## Definition of done

F-01 is done only when T001–T037 are complete, every mandatory corpus case passes, all four browser results are evidenced, privacy/side-effect checks pass, provenance and licensing are reviewed, CI preserves existing .NET coverage, and the final traceability review contains no hidden constitutional violation or falsely passed pending item.
