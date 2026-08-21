---
date: 2026-08-21
session: knowledge-release-structural-immutability
---

# Journal: 2026-08-21 — Knowledge Release Structural Immutability

## Context

The foundation model described published knowledge releases as immutable, but the original schema still allowed direct updates, cascading deletes, post-publication membership changes, and draft releases marked current. This session strengthened the modeled release boundary without claiming full `DATA-005` compliance.

## What Happened

- Reproduced the gaps against PostgreSQL, then used TDD and independent review to cover direct SQL, rollback/retry, pointer switching, child writes, concurrency, and legacy migration upgrade.
- Replaced row-level `is_current` state with a singleton `current_knowledge_release` pointer so switching current facts does not mutate an earlier published snapshot.
- Added domain lifecycle guards, an internal publication use case/EF publication path, serializable transactions, PostgreSQL locks, restrictive foreign keys, and database triggers/transaction-local guards.
- Added a forward EF Core migration with preflight validation and legacy-current backfill; PostgreSQL Testcontainers exercise the runtime and upgrade paths.

## Reflection

Application-layer validation alone was insufficient because direct SQL could bypass it, while database triggers alone needed a controlled internal escape hatch for atomic publication. The review also exposed rollback state tracking and direct pointer deletion as important edge cases. Defense in depth is appropriate here, but its boundary must remain explicit: the current change protects modeled release metadata, revisions, and manifest membership, not every object that conceptually contributes to a release snapshot.

## Decisions Made

| Decision | Rationale | Impact |
|----------|-----------|--------|
| Store current status in a singleton pointer | Preserve historical published rows while switching the active release | Current selection changes independently from release snapshot data |
| Publish through one internal transactional path | Keep validation, publication, and pointer switch atomic | Direct ordinary mutations are rejected; retries recover cleanly after rollback |
| Enforce invariants in PostgreSQL as well as Domain/Application | EF is not the only possible writer | Published release metadata and modeled child membership remain protected from direct SQL |
| Use a forward migration with preflight checks | Avoid silently converting invalid legacy state | Deployment fails visibly when legacy data violates the new invariants |
| Defer the broader snapshot and authorization boundary | Existing schema and requirements do not yet define it fully | No claim of complete `DATA-005` or `DATA-001` implementation |

## Next Steps

- Specify how `SourceManifest` metadata, `SourceRecord`, and `EditorialMapping` are frozen or version-linked into a complete release snapshot.
- Define and validate the `DATA-001` operator authentication/database-role boundary before exposing publication operationally.
- Keep the PostgreSQL migration, publication, immutability, concurrency, and rollback/retry tests as release-governance regression gates.
