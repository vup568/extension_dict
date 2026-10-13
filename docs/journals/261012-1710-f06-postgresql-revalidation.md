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

- Production Dictionary deinflection remains unconnected: Dictionary still
  registers `NullTokenizerAdapter`. Unified morphology and F-09 conjugation
  require their own integration/implementation and validation.
- Final P95 and throughput acceptance remains pending the `OD-005` benchmark
  decision; passing functional tests does not satisfy that performance gate.

## Correction — OD-010 Alignment (2026-10-12)

The original next-step note incorrectly said no tokenizer sidecar/container had
been approved or implemented and treated OD-010 as pending. [F-08 SPEC §1.4 and
the 2026-08-24 clarification](../../.sdd/specs/feat-grammar-detection/SPEC.md#14-key-decision-tokenizer-od-010--resolved)
already resolve OD-010 as **Python + Sudachi via gRPC**. Commit `82dc015`
(2026-08-30) contains the Sidecar implementation under `sidecars/tokenizer/`,
including `app.py`, `requirements.txt`, `Dockerfile` and `proto/tokenizer.proto`.
Grammar uses `GrpcGrammarTokenizerAdapter`; Dictionary retains
`NullTokenizerAdapter` in `src/infra/DependencyInjection.cs`.

The test counts above remain the historical PostgreSQL/backend validation
results. This session did not verify a live Sudachi Sidecar; Compose currently
defines only the `db` service. The remaining gap is Dictionary/Unified/F-09
integration and live runtime verification, rather than a technology decision.
