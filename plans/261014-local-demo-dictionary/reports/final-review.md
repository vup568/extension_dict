---
type: code-reviewer
date: 2026-10-14
status: PASS_WITH_RISK
---

# Final review: local dictionary demo

## Summary

No outstanding P1/P2 in the scoped demo implementation after two ownership review passes. Read-only Git inspection preserved existing recovery changes and found no staged files or Git delivery. This verdict covers demo plumbing and fixture ownership, not production dictionary publication, full schema integrity or latency certification.

## Findings

| Approved requirement | Reviewed implementation/evidence |
|---|---|
| Separate explicit local demo target | CLI requires confirmation; seeder accepts only a single loopback host and exact `jp_dict_demo`. Wrong targets and altered corpus fail before migration. Program consumes valueless operator switches and rejects conflicting recovery/demo commands. |
| Exactly 17 synthetic fixture entries | Hash-pinned corpus parser reads only `dictionary_fixtures`; forms/readings/POS/VI glosses and synthetic manifest/source records are persisted. No expected test answers or new linguistic rules are seeded. |
| Safe existing DB behavior | Source marker plus exact fixture graph checks; unsupported namespaces/routines/sequences/composite types and unrelated user/release/Kanji rows are rejected. No reset, overwrite or repair path. |
| Transactional and repeatable installation | Data insertion uses a transaction; advisory session lock serializes inspection/migration/installation. Reseed only validates existing data. Parent live checks retained counts/IDs and dictionary fingerprints. |
| Original environment/data preserved | Scripts use fixed SQL identifiers, child-only seed configuration and process-environment restoration. Parent execution verified 20 original public-table fingerprints/inventory unchanged. |
| Real API/popup behavior | Parent checked health, contract compatibility, exact-first `書ける` and blank-input rejection. Inspected real-backend Brave result: one test passed, six selections, zero unexpected/skipped/flaky. |
| No runtime contract/data/dependency expansion | Git inspection found no tracked changes in Domain/usecases, migrations, conjugation data, accepted F09 packet, dependency files or Compose. Frozen corpus hash independently matches the approved checksum. |

Initial findings were actionable ownership gaps: public-only relation inspection could accept an unknown non-public schema or standalone function; arbitrary sequences were permitted; standalone composite types escaped the fresh-schema test. Latest source rejects each and adds regression cases. License/attribution are also included in marker validation.

Scoped simplifier review: existing helper separation is justified. No safe simplification warranted deleting validation or changing behavior merely to shorten the greater-than-200-line seeder.

### Verification provenance

- Independently inspected `tests/TestResults/local-demo/full/local-demo-full.trx`: 202 executed/passed, zero failures; 28 `DemoDictionaryTests` and 10 `DemoScriptTests` included.
- Independently inspected final script TRX: 10 passed, zero failures. Earlier script-fixture failures and diagnostic evidence remain retained; correction used explicit CLR connection-string setters in test mocks.
- Independently parsed `extension/test-results/local-demo/results.json`: one expected, zero unexpected/skipped/flaky, approximately 4.3 seconds. Reviewed smoke source confirms it does not intercept/mock `/api/analysis`.
- Live database/API/original-fingerprint and build/unit/frontend results are parent-executed evidence; this reviewer did not create another database or rerun concurrent Docker workloads.

### Read-only Git audit

HEAD remains `a42a323` on `feat/tokenizer-deinflection`; index is empty. `.env`, `data/local-recovery` and `extension/test-results` are ignored and absent from tracked files. Existing dirty legacy recovery files remain present. No stage, commit, push, merge, reset or checkout command was executed. New demo source uses environment-derived credentials; synthetic test sentinels are not production secrets.

## Recommendations

Hand off the existing demo launch guide and keep the original DB launch path. Clearly label the 17 words as synthetic demo data. Preserve both successful and failed browser evidence; do not silently weaken the 500ms provider deadline, install browsers/packages or commit the mixed worktree.

## Unresolved questions and risks

- The first real popup run reported unavailable conjugation for `食べました`; an unchanged rerun passed. Cause is not established without RPC status. This is not proof of cold-start behavior, eliminated intermittency or latency certification.
- Packaged automation used Brave, not Chrome. The user must still manually reload/test Chrome; no Chrome automation acceptance claim.
- Ownership validation covers the synthetic dictionary graph and supported catalog guards, not complete grammar/dataset/schema integrity. Existing grammar initialization remains the original repository behavior.
- The user's Windows-only CI choice and absent workflow artifact hook remain unchanged. No clean Ubuntu/production/ship claim.
