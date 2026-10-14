# F-06 Dictionary Lookup Verification — 2026-10-12

> Historical exact-lookup evidence. Update2026-10-13: F-06 gRPC deinflection and
> initial F-09/API2/popup implementation now exist on the feature branch.
> New runtime gates/results are tracked in the
> [Tokenizer validation report](../../feat-tokenizer-deinflection/evidence/validation-report.md).
> The counts and pending rows below describe the 2026-10-12 checkout, not the new code.

## Summary

The implemented exact-lookup slice passes its .NET and PostgreSQL 16 gates. The
default `NullTokenizerAdapter` now reports morphology as `unavailable`, matching
AC-011 and the runtime limitation. OD-010 is already resolved as Python + Sudachi
over gRPC (F-08 SPEC §1.4). Dictionary deinflection remains unintegrated.

## Results

| Gate | Result |
|---|---|
| Docker Engine | PASS — client/server 29.6.1 |
| Compose PostgreSQL | PASS — PostgreSQL 16 healthy on localhost |
| Release solution build | PASS — 0 warnings, 0 errors |
| Full unit suite | PASS — 53/53 |
| F-06-focused unit slice | PASS — 13/13 |
| Full PostgreSQL Testcontainers suite | PASS — 51/51 |
| F-06-focused integration slice | PASS — 6/6 |
| OD-010 technology choice | RESOLVED — Python + Sudachi/gRPC, F-08 SPEC §1.4 |
| Dictionary Sidecar/deinflection integration | PENDING — Dictionary DI still uses NullTokenizerAdapter |
| Approved performance benchmark | PENDING — blocked by OD-005 |

## Regression Proof

Before the capability fix, the focused PostgreSQL regression failed with actual
`"completed"` versus expected `"unavailable"` when `NullTokenizerAdapter` handled
an unknown word. The initial correction also exposed the exact-match path, which
did not call the adapter. The final internal contract exposes configured provider
availability and keeps per-call availability in `DeinflectionResult`; regression
tests now cover exact match, unavailable, available-empty and successful provider
states. Public HTTP shapes and database schema are unchanged.

## Verified behavior

- Exact written-form and kana-reading lookup against PostgreSQL.
- Vietnamese gloss priority with English fallback.
- Empty lookup result without request failure.
- Blank/length input validation and restriction provenance.
- Graceful degradation when tokenizer capability is unavailable.

## Reproduction commands

- F-06 unit slice: `dotnet test tests/unit/UnitTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~LookupWordUseCaseTests|FullyQualifiedName~DictionaryEntryTests"`
- F-06 PostgreSQL slice: `dotnet test tests/integration/IntegrationTests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~DictionaryLookupIntegrationTests"`
- Full backend gates: Release solution build, full unit project and full integration project with PostgreSQL Testcontainers.

## Remaining limits

- Sidecar server/Dockerfile/proto already exist in `sidecars/tokenizer/`, and Grammar
  uses `GrpcGrammarTokenizerAdapter`. Dictionary has no gRPC deinflection adapter.
- Compose currently defines only PostgreSQL; this report's PostgreSQL tests do not
  certify live Sidecar runtime or deployment. OD-010 is not an open decision.
- AC-010 cannot be claimed from mocked unit behavior alone.
- P95/throughput targets require the separately approved benchmark gate.
- The standalone endpoint is wired and has a happy-path HTTP equivalence test in
  the unified-analysis suite; dedicated 400/503/error-envelope HTTP integration
  tests for that standalone route remain a test gap.
- Control-character policy, endpoint-owned 504 timeout and rate-limit/429 behavior
  from the full SPEC are not implemented or verified by this exact-lookup slice.
