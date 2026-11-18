# Tasks — Real dictionary import

Source of truth: [SPEC](SPEC.md), [PLAN](PLAN.md). Implementation authorized by the user's request on 2026-10-15; source, semantic review and activation approvals remain separate.

**Scope update 2026-10-30:** The user selected Japanese–English for the current local implementation. JMdict and KANJIDIC2 were imported and the extension was tested against the separate local database; see [execution evidence](reviews/ja-en-execution-20261030.md). The task list below describes the earlier Japanese–Vietnamese scope and is not a claim that Vietnamese data has been completed.

## P0 — Source evidence

- [x] T001 Prepare exact download/audit proposal (G0a); user authorized audit downloads during this execution. This is not G0b/canonical approval.
- [ ] T002 Pin downloaded artifacts, schemas, source/license evidence and counts; obtain G0b decisions.
- [x] T003 Prepare40 independent source cases in `reviews/acceptance-corpus.proposed.json`; owner semantic approval is pending.

## P1 — Read correctness

- [x] T004 Test and implement joint form/reading/sense restrictions, no-kanji projection and F09 filtering.
- [x] T005 Add Reading.IsNoKanji through a new forward migration; preserve existing migrations/data.
- [x] T006 Serve approved Vietnamese glosses only, preserve synthetic demo status, test API compatibility and kana label.

## P2 — Import mechanism (synthetic fixtures only until source gates clear)

- [x] T007 Implement bounded secure XML parsers for JMdict/KANJIDIC2 and lossless Vietnamese inventory parsing; full source read-only audit completed.
- [x] T008 Implement source-lock inspection, hash/approval checks and explicit CLI help/dispatch; source-clearance blockers remain enforced.
- [x] T009 Add import-run/checkpoint schema and isolated-target ownership/concurrency guards; prepare is exposed as an explicit operator command and verified with synthetic fixtures.
- [ ] T010 Implement bounded transactional batches, stable identities, resume/no-op verification and canonical facts hash.
- [ ] T011 Test malformed sources, interrupted batches, changed pins, concurrency and protected/unknown targets.

T010 foundation is implemented and fixture-tested for JMdict/KANJIDIC2. Reviewed Vietnamese mapping persistence and complete source reconciliation/metrics/readiness workflow remain unfinished; the all-component importer cannot be marked complete. A verified lock requires Vietnamese and override groups, whereas the current writer refuses those groups: full real import remains blocked until that gap is implemented without bypassing review. The focused integration suite passed 54 tests on 2026-10-30; this does not constitute full acceptance.

## P3 — Offline review

- [x] T012 Inventory all172321 Vietnamese source records and50 pinned V1 overrides with explicit quarantine dispositions; no auto-approval or truncation.
- [ ] T013 Produce semantic mapping/enrichment evidence and obtain owner decisions; bind final mapping hash. An exact-headword candidate inventory and counts are recorded in `reviews/vietnamese-candidate-audit.md`; no mapping is approved by that inventory.

## P4 — Full import and validation

- [ ] T014 Obtain final-source/target authorization; prepare a new owned database and bootstrap original migrations/grammar/F09 pins.
- [ ] T015 Import all approved sources; reconcile counts, facts hashes, sizes, elapsed time and peak memory.
- [ ] T016 Run backend/frontend/API/F09 regression suites and independent approved corpus; record original/demo preservation.

## P5 — Local cutover

- [ ] T017 Implement receipt-checked read-only launcher, attribution and rollback runbook.
- [ ] T018 Obtain target/validation-bound G1 approval and actual Chrome acceptance; no implicit activation.
- [ ] T019 Review implementation, synchronize progress, traceability, project memory and journal.

## Issues Encountered

- Source/license clearance OD-009, semantic mapping/corpus review, full import and local activation are not granted by implementation approval. Work on fixture-tested mechanisms proceeds independently.
- Complete JMdict audit finds exactly one restriction contradiction at1339160/sense2; canonical `Read` fails closed while audit retains its full raw record and continues inventory. Owner-reviewed correction/repin remains required before completion.
- OVDP original ZIP audit download is authorized; current SourceForge responses are HTML, not the archive. Mirror metadata/GPLv2 project evidence is not exact artifact clearance.
- Available CLI versus planned operator scripts and validation limitations are documented in `docs/dictionary-import.md`; final metrics/traceability live in `reviews/execution-validation.md`.
