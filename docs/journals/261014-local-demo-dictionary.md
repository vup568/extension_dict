---
date: 2026-10-14
session: local-demo-dictionary
---

# Journal: 2026-10-14 — Isolated dictionary demo

## Context

The original backend database had no dictionary entries. The user approved a separate `jp_dict_demo` database in the existing PostgreSQL Docker instance to try real lookup responses before a future dictionary import. Existing uncommitted recovery work stays intact; no Git delivery was authorized.

## What happened

- Added an explicit, confirmed local-only seed command and PowerShell 7 preparation/launch scripts. Exactly 17 synthetic `dictionary_fixtures` from the frozen F09 corpus are persisted with source provenance; test cases/expected answers are not loaded. F09 explanations still execute the existing engine and candidate dataset.
- Target/hash/ownership guards reject non-demo connections, altered corpus, mixed fixture graphs and unsupported catalog objects. Independent review found missing non-public schema, standalone routine/sequence and composite-type checks; those guards and regression cases were added before live preparation.
- Live preparation and repeated seeding retained 17 entries, 17 source records, one manifest, four migrations and unchanged dictionary row fingerprints. The parent verified all 20 original public-table fingerprints/inventory and 12 accepted F09 bindings remained unchanged.
- Inspected final integration TRX: 202 passed, including 28 seed and 10 script tests. Parent execution also passed the Release build, unit/frontend suites and typecheck. Earlier fake-script failures came from PowerShell's adapted dictionary property setter; only the test inspection was corrected, not production safety checks.
- The real packaged Brave popup passed six selections against the real backend, including Vietnamese meanings and all three interpretations of `食べられる`. A preceding run reported conjugation unavailable for `食べました`; the unchanged rerun passed. No RPC status was captured, so its cause remains unexplained—not established as cold startup or a certified latency result.

## Reflection

An available HTTP endpoint is not proof of dictionary data or successful conjugation lookup. The demo now exercises the real SQL/API/popup path without pretending synthetic fixtures are an imported dictionary. Retaining failed evidence matters even when a subsequent unchanged execution passes.

## Decisions made

| Decision | Reason and impact |
|---|---|
| Separate opt-in demo DB; no reset or auto-seed | Preserve original data and keep normal startup behavior unchanged. |
| Frozen fixture-only installation with synthetic provenance | Enable lookup testing without hardcoding F09 answers or claiming full dictionary coverage. |
| Fail closed on ownership/content mismatch | Existing unknown data requires operator review, not automatic repair. |
| Existing engine, migrations, API and dependencies unchanged | This is demo plumbing, not a linguistic dataset expansion or production importer. |
| No commit/push/merge | Respect the user's latest explicit choice. |

## Next steps

The user can launch `start-demo.ps1` and manually reload/test the unpacked extension in Chrome. Chrome automation parity, the transient provider failure, existing Windows-only CI limitation and absent artifact hook remain disclosed limitations. Future real dictionary import needs its own approved scope. Demo ownership checks are not a complete grammar/dataset/schema audit.
