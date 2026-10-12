---
date: 2026-10-12
session: f06-postgresql-revalidation
status: verified-with-known-limits
---

# Journal — F-06 PostgreSQL Revalidation

## Context

Docker became available again, allowing F-06 Dictionary Lookup to be revalidated
against PostgreSQL 16 through both the local Compose service and Testcontainers.
The review also checked whether the API reported tokenizer capability truthfully.

## What Happened

- Docker Engine and the Compose PostgreSQL 16 service were healthy.
- The full backend gates passed: 53/53 unit tests and 51/51 PostgreSQL
  Testcontainers integration tests.
- The focused F-06 slices passed: 13/13 unit tests and 6/6 integration tests.
- Exact written-form and kana-reading lookup, Vietnamese-first gloss fallback,
  provenance, empty results, validation, and unavailable-provider degradation
  were exercised against the implemented path.

## Reflection

The capability status had been derived from per-call deinflection output. This
made the default `NullTokenizerAdapter` look completed when it returned an empty
result, while the exact-match path skipped the adapter entirely and could not
observe provider availability. The issue was capability truthfulness, not the
PostgreSQL query itself.

## Decisions

- Expose configured availability through the internal tokenizer contract and
  retain per-call provider availability in `DeinflectionResult`.
- Report morphology as `unavailable` whenever no provider is configured,
  including successful exact lookups; preserve `completed` for an available
  provider that legitimately finds no deinflection.
- Keep the public HTTP contract and database schema unchanged.

## Next

- Production deinflection remains pending `OD-010` because no tokenizer
  sidecar/container or HTTP adapter has been approved and implemented.
- Final P95 and throughput acceptance remains pending the `OD-005` benchmark
  decision; passing functional tests does not satisfy that performance gate.
