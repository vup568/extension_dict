---
title: "Local isolated dictionary demo"
status: completed
priority: P1
effort: medium
branch: feat/tokenizer-deinflection
tags: [demo, dictionary, windows]
created: 2026-10-14
---

# Local isolated dictionary demo

## Overview and approval

User approved the five-step chat plan and explicitly asked to implement it. Create `jp_dict_demo` in the existing PostgreSQL Docker instance, install unchanged migrations/candidate dataset, seed exactly 17 synthetic `dictionary_fixtures`, and exercise the existing backend/extension. Approved fixture POS is reused; expected test answers are not runtime data. Chrome manual acceptance remains a user handoff; packaged automation used installed Brave because Chrome ignores its extension-loading flags.

## Phases

- [x] Inspect frozen17 fixtures/current scripts/migrations/API;preserve existing uncommitted recovery work.
- [x] Add guarded explicit seed CLI and transactionally idempotent fixture loader with synthetic provenance.
- [x] Add prepare-demo.ps1/start-demo.ps1 helpers; keep demo separate from .env database and caller environment.
- [x] Test guards/idempotency/real dictionary+F09 API; independent review; rebuild and check packaged Brave popup using real backend.
- [x] Create actual demo, prove original DB unchanged, write guide/examples and finalize evidence/memory/journal. Agent-owned backend stopped; port 5000 free.

## Files and contracts

Read:cases.json dictionary_fixtures,AppDbContext/EF repositories,source/gloss entities,start-local.ps1,Program.cs,existing script helpers/API/browser harness.
Create:src/infra/Demo/DemoDictionarySeeder.cs,src/interface/DemoDictionaryCommand.cs,scripts/prepare-demo.ps1,scripts/start-demo.ps1,tests/integration/DemoDictionaryTests.cs andDemoScriptTests.cs,docs/local-demo-testing.md.
Modify:Program CLI dispatch andIntegrationTests.csproj linked command;README/local guide/memory. Existing API response shapes,adapter deadlines,linguistic engine/candidate/corpus/migrations remain unchanged. No new dependency,container,production importer,auth,Git writes or reset/delete.

## Success criteria

Only loopback `jp_dict_demo` accepts explicit seed confirmation. Wrong/hash-altered corpus and non-demo targets are rejected before migration; unknown ownership/catalog objects or conflicting dictionary fixtures fail closed. Seed 17 words with forms/readings/POS/VI and synthetic source provenance, never `cases.expected`. Repeated preparation preserves counts/IDs. Real API and popup show meanings/F09 alternatives; unknown words are empty and exact-first remains intact. Ctrl+C then start-local returns to original configuration; .env stays unchanged. All **20 current original public-table data fingerprints** match the pre-demo baseline (including the 17 legacy tables).

## Risks and checks

DB name alone is insufficient ownership proof;fresh database or matching source marker required. Demo seed is opt-in CLI,not normal startup or HTTP. Existing same-name DB not reset;stop on unknown schema/data. Chrome can remain on old unpacked build:check currentpackage/rebuildwithinstalleddeps and exercise actual HTTP popup. Generic current DB dictionary remains0;demo17 includes deliberately synthetic boundary/exact-first fixtures,not a complete dictionary. Test serially to avoid known500ms gRPC resource-contention flakiness.

## Issues encountered

Missing docs/code-standards.md: follow AGENTS and adjacent conventions. Prior recovery changes stay uncommitted. Native Task tools, official artifact hook and doc validator are absent; direct checks are explicitly a fallback. Existing Windows-only CI limitation remains.

Guard review found unsupported schema/function/sequence/composite-type detection gaps; fixed with regression tests before actual installation. Initial EF mutation/PowerShell inspection test-fixture failures were corrected without weakening assertions. One actual popup request returned unavailable conjugation; direct API and unchanged popup rerun passed, but exact cause is unproven and the existing 500ms deadline was not relaxed. Chrome manual acceptance and real dictionary import remain separate.

## Final evidence and handoff

Build 0 warnings/errors; 79 unit, 202 integration, 251 frontend tests and typecheck pass. Scoped formatting passes. Actual seed/reseed: 17 entries, 17 source records, one manifest, four migrations; row hash stable. Original 20 table fingerprints and 12 accepted F09 bindings unchanged. Real packaged Brave: one passing test/six selections, first transient failure retained. Review: PASS_WITH_RISK; no outstanding scoped P1/P2, no Git writes.

- [Validation and traceability](reports/validation-report.md)
- [Independent review/Git audit](reports/final-review.md)
- [Test-fixture RCA](reports/debugger.md)
- [User guide](../../docs/local-demo-testing.md)
- [Journal](../../docs/journals/261014-local-demo-dictionary.md)
