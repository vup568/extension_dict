---
title: "Tokenizer deinflection and Vietnamese explanations"
description: "Verify Docker Sidecar, connect F-06 and deliver initial F-09 popup results."
status: completed
priority: P1
effort: medium
branch: feat/tokenizer-deinflection
tags: [backend, frontend, database, infra]
created: 2026-10-13
---

# Implementation Plan: Tokenizer Deinflection & F-09

Follow [SPEC](./SPEC.md); status in [TASKS](./TASKS.md). User authorized the sequence.
All16 scoped tasks complete; [local validation](./evidence/validation-report.md).
Follow-up corpus/signoff/performance and remote delivery remain separate.
F-06 task2.2b depends on this plan. F-05/F-03/F-04 consume versioned results.
No generic `plans/` directory exists; use requested SDD feature location.

## Phase 1 — Existing runtime

Commit OD-010 corrections only; build unchanged requirements and call live Docker gRPC.
Record all9 actual chains/POS and supplementary offsets. Review scope/contracts before code.
Baseline smoke: Sudachi0.7.0/core20260723.1; CP offsets need UTF-16 correction.
Do not modify unrelated services, volumes or secrets.

## Phase 2 — F-06 foundation

Modify Sidecar app/proto/Compose for UTF-16, explicit unavailable and safe concurrency.
Create pure Domain ConjugationDataset, repository port, EF JSONB configuration/migration
and immutable seed with UPDATE/DELETE rejection; `ConjugationDatasetVersion` activates a
pinned version explicitly. Unset remains unavailable; unsupported rules remain unavailable.
Infra parses/maps provider metadata; Application matches normalized full-chain patterns.
Implement/register Dictionary adapter; narrow provider failure handling, propagate caller
cancellation and secondary DB failures; preserve exact-first/provenance/glosses/restrictions.
Files: `sidecars/tokenizer/`, `src/domain/Entities/`, `src/usecase/Conjugation/`,
`src/infra/{Adapters,Persistence,DependencyInjection.cs}`, Dictionary usecase/models.

## Phase 3 — F-09 API and popup

Reuse F-06 found-entry conjugation; avoid another tokenizer call.
Extend analysis DTO/orchestrator and endpoint contract negotiation: default1 empty,
header2 typed results, unknown version structured400; tokens/morphology unchanged.
Background requests2; validator accepts1/2 and checks spans/segments/status; popup renders VI
safe text. Preserve retry/debounce/stale/close behavior.
Files: analysis models/usecase, analysis endpoint, Extension background transport,
`extension/src/api/analysisContract.ts`, `extension/src/ui/PopupShell.tsx`.

## Verification and risks

Add unit/real PostgreSQL/Sidecar/API/FE/browser tests for every SPEC AC. Run full regressions,
Release build, Extension typecheck/build and diff check; independent review resolves findings.
Record evidence and sync F-06/F-05/Feature Map/memory/tasks. Draft dataset is not approved
production corpus. Pin dictionary splits; test UTF-16 grammar regressions, thread/context
isolation, immutable migration and default1 compatibility. OD-005 benchmarks remain separate.
