# Tasks: Tokenizer Deinflection & Initial F-09

**Date:** 2026-10-13 | **Status:** Scoped implementation and local verification complete (16/16)
**References:** [SPEC](./SPEC.md), [PLAN](./PLAN.md)
**Evidence:** [Validation report](./evidence/validation-report.md)

## Phase 1 — Documentation and runtime

- [x] T01 Commit reviewed OD-010 corrections separately (c0cb500).
- [x] T02 Build/run unchanged Sidecar; capture live9 token/POS chains. AC-T01.
- [x] T03 Review scoped SPEC/PLAN and intentional API version evolution; no architecture blocker.

## Phase 2 — F-06 foundation (depends on T02–T03)

- [x] T04 Correct UTF-16, missing-provider failure, concurrency and privacy. AC-T02/T03.
- [x] T05 Add provider normalization from versioned mapping data. AC-T03/X01.
- [x] T06 Add immutable ConjugationDataset JSONB/migration/seed/trigger, pinned version. AC-C03.
- [x] T07 Implement generic complete-chain matching and draft VI segments. AC-C01/C02.
- [x] T08 Register real Dictionary adapter; preserve exact/provenance and cancellation. AC-D01–D03.
- [x] T09 Verify PostgreSQL/live Sidecar/Dictionary endpoint and failure paths. AC-D01–D03.

## Phase 3 — F-09 and popup (depends on T04–T09)

- [x] T10 Reuse F-06 found-entry result; API default1/opt-in2/unknown400. AC-C04.
- [x] T11 Request2 from background; validator accepts1/2 and rejects malformed shape. AC-C05.
- [x] T12 Safe popup base/VI/segments; preserve loading/retry/stale/close. AC-C06.
- [x] T13 Live corpus/API/browser checks; no false result without base entry. AC-C01/C04/C06.

## Verification and follow-up

- [x] T14 Full Release/.NET/PostgreSQL/Extension/browser checks and diff check. AC-X01.
- [x] T15 Independent review verifies every AC and touched callers/contracts.
- [x] T16 Write evidence and synchronize F-06/F-05/Feature Map/memory/checklists.
- [ ] Follow-up: production linguistic signoff, broader corpus, general morphology,
  sentence vocabulary segmentation and OD-005 performance certification remain separate.
