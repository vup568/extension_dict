---
title: "Recover local legacy database migration history"
status: completed
priority: P1
effort: medium
branch: feat/tokenizer-deinflection
tags: [postgresql, recovery, windows]
created: 2026-10-14
---

# Local legacy database recovery

## Overview

User approved Proceed for schema comparison,backup,clone-first repair and local verification while preserving data. PostgreSQL confirmed dictionary_entries already exists with an empty EF migration history;17 legacy application tables exist. Startup now correctly refuses service instead of swallowing the database error.

## Phases

- [x] Scout schema origin,existing migrations/tests,local dictionary source and runtime boundaries.
- [x] Snapshot schema/counts,create ignored private backup,restore into isolated clone and establish reviewed baseline compatibility.
- [x] Implement guarded explicit recovery and regression tests;no startup auto-baseline or blind history insertion.
- [x] Prove repair on clone,independent safety review PASS,then apply approved repair to original local DB with backup retained.
- [x] Verify actual startup twice and HTTP/gRPC;79 units/164 integration/251 frontend pass,33 targeted tests;accepted12 F09 hashes intact. Dictionary import deferred,not lexical success claimed.
- [x] Update docs/memory/journal/report and handoff;no automatic commit/push/merge.

## Dependencies and scope

Existing Docker images/.NET packages only. Read migrations/schema/startup/scripts;conditional edits in scripts/,src/interface/,tests/integration/,docs/ and memory. Backup under ignored data/local-recovery/,never source/commit/logs. No delete/reset of original database/schema/volume or canonical artifacts. Native Tasks unavailable,persistent phases substitute.

## Success criteria

Known legacy schema is fully compared before baseline adoption;unknown schemas/history fail closed. Backup is restorable. Original data counts preserved through repair. Pending EF migrations execute normally and startup is usable rather than falsely healthy. No secrets/raw user data in evidence. No linguistic corpus modifications.

## Risks

Legacy manually-created schema may differ in constraints/index names/types/defaults. Schema equivalence and source-preserving rename/additive normalization require proof before history entry. Recovery must target the Compose database explicitly,not an arbitrary overridden connection. Dictionary_entries is empty;real dictionary population may need separate approved import/source and must not be faked with test fixtures.

## Issues encountered

Prior readonly here-doc probe printed useful results but CRLF caused trailing delimiter syntax error;no writes performed. Subsequent binary-safe process stdin probe completed READ ONLY and confirmed duplicate-table error/history0/tables18 including EF history. Existing Ubuntu test limitation and missing artifact hook remain outside this Windows repair.

EF SQL generation initially used WebApi startup project,which lacks EF Design. Retried with Infrastructure startup project and its existing design factory;SQL generated without installing a package or updating a database. User explicitly defers dictionary import;this task only repairs startup/schema compatibility,not lexical population.

Known legacy compatibility:retain owned serial sequences/high-water and stronger existing unique constraints/COALESCE uniqueness;normalize proven names/add missing indexes/align five JSONB nullabilities. Validate these intentional compatibility differences explicitly instead of claiming byte-identical schema.

Coordinated Release build passed0warnings/errors. Targeted helper test found CopyToAsync/WriteAsync GetAwaiter.GetResult emitting internal VoidTaskResult into PowerShell output;recovery execution paused and user asked to approve narrow output suppression before retry. No original database repair executed.

User approved helper output fix;31 targeted tests passed. Real rehearsal preflight then rejected ambiguous Get-Command docker results (Desktop ships docker.exe plus extensionless docker). Diagnosed FileName parameter binding before database mutation;user approved selecting explicit Windows exe and adding regression. Original history still unchanged.

Default rehearsal passed2026-10-14:restorable backup f51612addd35d1b78f08b879cac0a8aef3d5f246b8c81f1ca0eeaaad3ee8effd;clone/reference4e112962e005432ca2f673c9b8842625;all17 row fingerprints/counts preserved,4 migration IDs verified.32 targeted tests and79 units pass;full integration running. Independent review strengthens both directions of inheritance detection before Apply;original still unchanged.

Resumed completion2026-10-14:user explicitly asks finish original recovery/verification. Final targeted33/33 includes external-child guard;independent local-apply review PASS9.3/no blocker. Fresh solution build0warnings/errors,79unit and251frontend/typecheck pass. Full164 integration attempt failed106Docker-dependent cases because Linux Engine is stopped;58 non-Docker cases passed,not full-suite success. Docker Desktop start requested using installed CLI;waiting Engine,user notified. Original Apply remains pending and no data changed in this resumed attempt.

## Completed outcome

Docker restored;guarded Apply exited0 with all17 old-table counts/fingerprints preserved through baseline and official migrations. Backup retained under ignored data/local-recovery/recovery-20261014-584453a58c114105bd80f7111e97c4a6/database.dump,hash3c9dd1895f809a1c140822915dba31ec3da07a84d5c9af58c3bf7aeaf269f5cf. Original has4 migration IDs,grammar5,dictionary0,conjugationversions2.

First full rerun during concurrent recovery had3 F09 null/empty-alternative failures,161passes;500ms provider deadline unchanged. Quiet rerun after recovery passed164/164 without source/timeout changes;timing/resource contention strongly supported but exact RPCstatus not captured. This does not claim latency certification. First original-script startup health200/APIv1-v3 200/blank400;second startup again reports no migrations applied/listens5000. Agent stopped only its two test processes;user runs start-local.ps1 for own session. gRPC synthetic fixture verifiesbaseform. A final repeated-probe shell had a parse error before execution;it is not separate HTTP proof.

Documentation/artifact hooks absent;direct field/link/hash checks are fallback,not original hook success. Ubuntu CI remains previously excluded. No data reset/import/dependency install/Git delivery. See reports/validation-report.md.
