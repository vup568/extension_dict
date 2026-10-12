# F-06 Dictionary Lookup Verification — 2026-10-12

## Summary

The implemented exact-lookup slice passes its .NET and PostgreSQL 16 gates. The
default `NullTokenizerAdapter` now reports morphology as `unavailable`, matching
AC-011 and the runtime limitation. Production deinflection remains pending OD-010.

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
| Production Sidecar/deinflection | PENDING — blocked by OD-010 |
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

- No production tokenizer container or Sidecar HTTP adapter is implemented.
- AC-010 cannot be claimed from mocked unit behavior alone.
- P95/throughput targets require the separately approved benchmark gate.
- The standalone endpoint is wired and has a happy-path HTTP equivalence test in
  the unified-analysis suite; dedicated 400/503/error-envelope HTTP integration
  tests for that standalone route remain a test gap.
- Control-character policy, endpoint-owned 504 timeout and rate-limit/429 behavior
  from the full SPEC are not implemented or verified by this exact-lookup slice.
