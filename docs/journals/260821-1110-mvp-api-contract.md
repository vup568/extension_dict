---
date: 2026-08-21
session: mvp-api-contract
---

# Journal: 2026-08-21 — MVP API Contract

## Context

Project owner requested a complete MVP API baseline, excluding the existing Dictionary Lookup draft. The goal is a shared review contract for Extension and Web before endpoint implementation.

## What Happened

- Created `docs/api/mvp-api-contract.md` as a proposed HTTP contract covering analysis, kanji, grammar, translation, auth, learning, review, export, and knowledge-release operations.
- New routes use `/api/` without a URL version; Dictionary Lookup remains untouched and out of scope.
- Preserved required semantics: anonymous reading, canonical IDs, interaction correlation, structured errors, server-side ownership, idempotent saves, soft delete, and selected-text privacy.

## Reflection

Keeping required behavior separate from proposed HTTP shapes makes the baseline useful without presenting unapproved route details as implemented behavior. The contract also follows the current .NET 10, React 19, PostgreSQL 16/Testcontainers baseline rather than older SQL Server/SQLite artifacts.

## Decisions Made

| Decision | Rationale | Impact |
|---|---|---|
| Use `/api/` routes without URL version | Project-owner direction | Contract versioning moves to proposed metadata/header policy. |
| Exclude Dictionary Lookup | Existing draft must not be replaced or duplicated | MVP baseline covers all other capabilities only. |
| Mark endpoint shapes as proposed | Most feature specs have not approved HTTP details | Prevents documentation from becoming an accidental runtime commitment. |

## Outstanding Decisions

- Contract compatibility and deprecation policy; span/offset convention; request/page limits and pagination.
- Session transport, identity provider, recovery/linking; translation provider, disclosure and retry policy.
- Grammar matcher projection, export mapping/job behavior, and operator authorization/ingestion/publish rollback.
- Reconcile duplicate MVP API Contract task blocks in `PLAN.md` so task tracking has one authoritative status.

## Next Steps

- Approve or revise the proposed contract through feature specifications before implementation.
- Add endpoint-level implementation and integration tests only after those approvals.
