# Implementation Plan: Japanese Text Detection (F-01)

**Version:** 1.0.0
**Date:** 2026-09-20
**Status:** Implemented; release evidence pending for Brave, Firefox, and OD-005
**Current working branch:** feat/japanese-text-detection
**Feature specification:** [SPEC.md](SPEC.md) v0.2.0
**Governing documents:** [Constitution](../../constitution.md), [REQUIREMENT.md](../../../REQUIREMENT.md), [MIGRATION_DECISION.md](../../../MIGRATION_DECISION.md), [FEATURE_MAP.md](../../../docs/FEATURE_MAP.md)

## 1. Summary

F-01 is a local, pure classifier that answers one question for one caller-provided string: does it contain at least one character covered by the approved Japanese trigger policy? It does not identify a language, extract a browser selection, show a popup, call the Backend, normalize input, or retain text.

The implementation will freeze the policy to Unicode 17.0.0 and make membership independent of each browser's runtime Unicode tables. A thin boundary validates the input type and exposes a result/error contract; the detector itself remains independent of DOM, browser-vendor APIs, network, storage, authentication, and mutable process state.

## 2. Technical Context

| Area | Decision and evidence |
|---|---|
| Product location | A new Extension client workspace will be introduced under extension/ only after the approval gate in section 6. The existing repository currently contains .NET projects only; it has no package manifest, Vite configuration, browser manifest, or frontend test runner. |
| Language/runtime | TypeScript is the planned detector language because the approved client baseline is React 19 + TypeScript + Vite. F-01 core itself is framework-free TypeScript and does not import React. |
| Unicode baseline | Unicode 17.0.0. Policy data is generated from versioned UCD source files and committed as static, reviewable data. Runtime Unicode-property regular expressions are not authoritative because browser Unicode/ICU data may differ from the frozen baseline. |
| Trigger policy | Hiragana and Katakana characters with General_Category=Lo; Unified_Ideograph=Yes; General_Category=Lo in CJK Compatibility Ideographs blocks; and U+3007. This exactly implements SPEC section 3.2. |
| Detector algorithm | Iterate the entire JavaScript string by code point; look up each code point in sorted, non-overlapping inclusive ranges. Return true on the first member and false only after the full input is inspected. A lone UTF-16 surrogate is not a matching range but does not stop inspection. |
| Boundary/error contract | The public boundary accepts unknown input. A string produces a boolean classification. A non-string produces a distinct invalid-input result/error without coercion. Unexpected execution failure is distinct from false and diagnostics must not include raw input. |
| Persistent data | None. There is no database entity, migration, cache, history, preference, telemetry payload, or canonical linguistic resource. |
| External service/provider | None at runtime. Unicode Consortium files are build-time policy sources only; selected text is never transmitted to them. |
| Test baseline | The current test projects are .NET-only. F-01 requires a new approved TypeScript unit-test setup plus a browser harness; no existing frontend runner may be assumed. |
| Browser evidence | Chrome, Edge, Brave, and Firefox are all required. A Chromium result is not evidence for Edge and Brave. An unavailable browser is recorded pending, never passed. |
| Performance evidence | The product-level PERF-001 budget applies to selection-to-popup flow, not detector alone. F-01 will report p95 for the profiles in SPEC F01-AC-032, but no numeric detector budget is approved until OD-005 is decided. |
| Dependency policy | No package manager, package, version, or browser automation tool is selected or installed by this plan. Any such addition requires the project owner's explicit approval under AGENTS.md. |

## 3. Constitution Check

### Pre-design check

| Constitutional obligation | Plan response | Status |
|---|---|---|
| Core logic isolated from browser APIs | Keep policy lookup and classification in a pure core module. DOM/selection work remains F-02; browser lifecycle remains outside F-01. | Pass |
| Linguistic correctness over legacy parity | Use explicit Unicode 17 membership and independent fixtures, including U+20B9F alone and legacy mojibake as a negative case. Do not reuse the broad backend CJK helper as policy authority. | Pass |
| Provenance for production knowledge/data | Keep a checked-in source manifest containing UCD release, source URL, checksum, license/notice, generator version and generated-artifact checksum. | Pass, subject to implementation evidence |
| Page-text minimization and no ordinary logging | Process only caller input in memory; prohibit network, storage, raw-input logs and retained references. Test success, false and error paths with synthetic markers. | Pass |
| Cross-browser semantics | Execute one corpus and one frozen policy in all four named browsers; record versions and OS. | Pass, subject to implementation evidence |
| Explicit, validatable boundary errors | Model invalid input and unexpected failure separately from a negative classification. | Pass |
| No silent scope growth | Exclude selection capture, ruby handling, popup UI, async cancellation, Backend API and data schema work. | Pass |
| Dependency governance | Do not bootstrap/install a frontend or test dependency without explicit approval. | Gate required before code |

### Post-design check

The selected architecture adds no provider, credential, persistence, Backend contract, database migration, DOM access, or browser permission. The only planned generated data is derived from a versioned Unicode source with reproducible provenance. No constitutional exception is proposed.

## 4. Design and Delivery Sequence

### Phase A — Approval and reproducibility gate

1. Obtain explicit approval to create the Extension workspace and add the minimal build/test/browser tooling required for F-01.
2. Record the approved package manager, exact packages and versions, browser harness choice, and command names in the implementation task before installation.
3. Download or otherwise acquire the four pinned Unicode 17.0.0 UCD inputs: Scripts.txt, PropList.txt, UnicodeData.txt, and Blocks.txt.
4. Commit a provenance manifest with release, canonical URLs, source checksums, license/notice, generator revision, generated-file checksum and generation command.
5. Make generation deterministic and reviewable. A Unicode upgrade must be a separate reviewed change that updates policy version, inputs, checksums and regression evidence together.

Exit evidence: approved dependency record, reproducible generated range table, and provenance manifest. No selected page text is used as input to this process.

### Phase B — Pure policy core and public boundary

1. Add a generated static range-table module below extension/src/core/. The table is data, not heuristic ranges copied from blocks.
2. Add the pure detector below extension/src/core/. It scans all code points and performs a binary search or equivalent deterministic membership lookup over the static ranges.
3. Add a small boundary below extension/src/boundary/ that validates unknown input and returns the classification or a non-content error category.
4. Keep diagnostic content limited to error category and safe metadata. Never interpolate, log, cache, store or transmit the source text.
5. Add EARS traceability comments in implementation for the core behavior and boundary failure behavior.

Exit evidence: no import/reference to DOM, browser-vendor globals, storage, fetch/network, authentication, React, Backend contracts, or mutable module state.

### Phase C — Corpus and automated regression coverage

1. Define a declarative corpus under extension/tests/ with stable IDs, input representation, expected boolean/error category, policy baseline and source note where relevant.
2. Preserve supplementary code points, variation selectors and malformed UTF-16 code units without source-file conversion. Fixtures must store visible text only where unambiguous and store explicit code points/code units otherwise.
3. Cover all mandatory cases F01-AC-001 through F01-AC-026, including the standalone U+20B9F positive case, literal mojibake negative case, literal entity/escape negatives, 100,000-character cases, repeated calls and lone surrogates.
4. Ensure expected values are authored independently of the production detector; a test may not produce expected values by calling the detector.
5. Add isolation checks for F01-AC-027, F01-AC-029, F01-AC-030 and F01-AC-031 using only synthetic selection markers.

Exit evidence: all unit/corpus tests pass; false and error are asserted as distinct outcomes; inspection demonstrates no side effects or retained raw text.

### Phase D — Browser parity, performance and delivery evidence

1. Run the same corpus and policy in minimal harnesses on Chrome, Edge, Brave and Firefox. Record browser version, OS, policy version, runner version, executed case count and outcomes.
2. Mark any unavailable browser pending. Do not replace it with another Chromium run and do not call the cross-browser criterion passed until all required evidence exists.
3. Benchmark the exact profiles in F01-AC-032: small, medium and 100,000-character inputs; positive first, positive last, negative and supplementary inputs; cold and warm runs. Report p95, run count, device, browser and version.
4. Compare the benchmark only with the separately approved detector budget from OD-005. Until that approval, publish the measurements but do not claim F01-SC-005 passed.
5. Review final evidence against every F01-AC-001 through F01-AC-033 and F01-SC-001 through F01-SC-005. Capture pending evidence explicitly.

Exit evidence: test reports, browser-parity report, privacy/side-effect review and benchmark report linked from the implementation review.

## 5. Planned Project Structure

The following is a planned structure, not a claim that these files currently exist:

    extension/
      src/
        core/
          japaneseTextDetector.ts
          unicode17TriggerRanges.generated.ts
        boundary/
          classifyJapaneseText.ts
      tests/
        corpus/
          japanese-text-detection-corpus.ts
        unit/
          japaneseTextDetector.test.ts
          classifyJapaneseText.test.ts
        browser/
          japanese-text-detection-parity.test.ts
          host-page-no-side-effects.test.ts
        performance/
          japanese-text-detection-benchmark.ts
      unicode/
        unicode17-sources.manifest.json
          generate-trigger-ranges

No src/, tests/, or backend file outside this new Extension workspace is planned for modification by F-01. In particular, the existing backend Kanji/CJK helper is not F-01 policy authority and is out of scope.

## 6. Explicit Approval Gate Before Implementation

The plan is complete as a design artifact, but implementation cannot responsibly begin yet because the repository lacks the Extension client workspace and frontend test/browser tooling. Creating that workspace or installing any package would add dependencies, which AGENTS.md forbids without explicit human approval.

The owner must explicitly approve the proposed bootstrap and dependency set after the implementation task identifies it. This is not an open product requirement: F-01 behavior, scope, Unicode policy, privacy rules and acceptance criteria are already fixed by SPEC.md. It is a repository-governance gate preventing an undocumented toolchain choice.

## 7. Requirement Traceability

| Planned evidence | Requirements and acceptance criteria |
|---|---|
| Static UCD 17 range generation plus provenance manifest | F01-FR-001 to F01-FR-005, F01-FR-007, F01-NFR-001, F01-AC-001 to F01-020, F01-AC-033 |
| Full-input pure detector and repeated-call tests | F01-FR-003, F01-FR-004, F01-FR-007, F01-FR-009, F01-FR-012, F01-NFR-005, F01-AC-021, F01-AC-022, F01-AC-024, F01-AC-025 |
| Boundary contract tests | F01-FR-006, F01-FR-010, F01-NFR-002, F01-AC-023, F01-AC-026 |
| Dependency/isolation/privacy harness | F01-FR-008, F01-FR-010, F01-FR-011, F01-NFR-002, F01-NFR-005, F01-AC-027, F01-AC-029 to F01-AC-031 |
| Four-browser corpus report | F01-FR-009, F01-FR-011, F01-NFR-003, F01-AC-028 |
| Benchmark report without truncation | F01-FR-012, F01-NFR-004, F01-AC-021, F01-AC-032 |
| Final evidence review | F01-SC-001 to F01-SC-005 and all F01-AC-001 to F01-AC-033 |

## 8. Complexity and Exceptions

No constitutional exception or additional architecture layer is proposed. The generated Unicode table is justified by the fixed Unicode 17.0.0 policy and cross-browser parity requirement; it avoids delegating semantics to inconsistent browser runtime data. The thin boundary is justified by the explicit requirement to distinguish invalid input and unexpected errors from false.

## 9. Completion Definition

Planning is complete when this plan and its companion research, data model, contract and quickstart artifacts exist and contain no unresolved behavior placeholders. F-01 itself is complete only after the approval gate is satisfied, implementation and regression coverage are added, all required browser/privacy evidence is recorded, and each acceptance/success criterion has either passed evidence or an explicitly pending status.
