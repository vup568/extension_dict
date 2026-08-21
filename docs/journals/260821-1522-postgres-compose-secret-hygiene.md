---
date: 2026-08-21
area: local-infrastructure
status: completed
---

# PostgreSQL Compose Secret Hygiene

## Context

The local Compose file committed a database password and exposed PostgreSQL on every network interface.

## What happened

- Moved the actual local PostgreSQL settings to ignored `.env`.
- Made Compose fail when a required setting is absent, bound PostgreSQL to localhost, and added a database readiness healthcheck.
- Recreated the existing container without deleting its volume, then rotated the existing database role password.

## Decision

Local credentials stay only in `.env`; they are never committed. Existing data is preserved during credential rotation.

## Validation

Compose validation, container healthcheck, TCP authentication with the new local secret, solution build, and PostgreSQL Testcontainers migration test passed.

## Next

Address the remaining knowledge-release immutability invariant before implementing publish workflows.
