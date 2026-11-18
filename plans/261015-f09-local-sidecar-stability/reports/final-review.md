# Independent review — F09 local Sidecar IPv4 stability

Date: 2026-10-15. Scope: user-approved local-address fix; existing recovery/demo work is not re-reviewed or authorized for Git delivery here.

## Verdict

**PASS for the scoped local-address correction — no P1/P2 findings.** Source/spec review and the execution-evidence review below are complete. The initial checkpoint had left execution pending; the final addendum records which artifacts were independently inspected and which live observations were supplied by the executing agents. This is not a production/latency certification.

## Reviewed changes

- `src/infra/DependencyInjection.cs`: missing-setting fallback changes from `http://localhost:50051` to `http://127.0.0.1:50051`; explicit configured values still win through the existing null-coalescing operator.
- `src/interface/appsettings.json`: shipped default matches the host port published by Compose.
- `scripts/start-local.ps1`: temporary launcher environment uses the same IPv4 literal; existing snapshot/restore logic and database selection are unchanged.
- `tests/integration/LocalTokenizerAddressTests.cs`: missing-setting fallback, shipped configuration/Compose alignment and four explicit overrides are checked without opening a network or database connection.
- `tests/integration/LocalBackendScriptTests.cs`: both Windows PowerShell 5 and PowerShell 7 check the address seen by the child backend and restoration after backend failure, preserving prior assertions.

Reviewed context: F09/F06 specifications, active plan, full DI registration, both gRPC adapters, Compose binding, Sidecar listener, launchers, API composition and adjacent test infrastructure. Existing accepted corpus/datasets and historical reports are outside the change scope.

## Stage 1 — specification compliance

| Requirement | Result | Evidence |
|---|---|---|
| Fix all three host-local defaults | PASS | Actual diff has three address substitutions plus one explanatory comment. |
| Preserve explicit deployment address override | PASS | DI still reads `configuration["GrpcTokenizerAddress"] ?? fallback`; tests retain container DNS, HTTPS remote, IPv6 literal and explicit localhost. |
| Preserve F09 linguistic behavior and API contracts | PASS (source) | No matcher, dataset, use-case, DTO, endpoint or popup runtime changes in the scoped diff. |
| Preserve 500 ms deadline/cancellation | PASS (source) | Both adapters retain their existing linked cancellation and 500 ms bounds; no retry/prewarm added. |
| Keep original DB and frozen artifacts unchanged | PASS (scope/evidence) | No migration, seed, database-selection or corpus changes in the address patch. Reviewer independently verified all 12 accepted artifact bindings; main reports matching original 20-table inventory/row fingerprints and `.env` hash. |
| Test old failure then corrected path | PASS | Reviewer inspected current red/green/formatted-green TRX counters; live API/popup evidence is detailed below. |

No unjustified feature expansion found. Current local README/backend guidance consistently identifies the IPv4 default, restarts the backend without DB reset and preserves existing deployments' explicit configuration. Previously dirty recovery/demo documentation is outside the scope of this fix review.

## Stage 2 — side-effect checks (a–e)

(a) **Reachability and deployment:** `compose.yaml` publishes tokenizer only as `127.0.0.1:50051:50051`, so the new host-local default targets the actual published family. A backend running in another container still needs its existing explicit service-DNS setting; this fix does not add deployment discovery.

(b) **Shared consumers and timing:** Dictionary and Grammar use the same singleton gRPC client/channel, so the transport correction applies to both. Their dataset opt-in, unavailable/failure semantics, cancellation and time budget remain unchanged. Exact-first dictionary behavior is untouched.

(c) **Configuration precedence:** Direct backend configuration overrides remain intact, including explicitly selected localhost/IPv6. The local launcher already temporarily replaced `GrpcTokenizerAddress` before this patch; retaining that behavior is not a new override regression. Existing process environment is restored in `finally`.

(d) **Launcher propagation:** `start-demo.ps1` delegates to `start-local.ps1 -SkipDependencies`, so the actual demo inherits the correction without duplicate defaults or changes to its guarded database/provenance logic. Normal local launch retains existing dependencies, migration behavior and secrets handling.

(e) **Data, privacy and contracts:** Three endpoint defaults do not alter SQL, accepted linguistic content, contract versions, raw-text logging, browser permissions or persistence. No new dependency, retry, timeout inflation, DB reset/import or Git operation is introduced.

## Diagnosis assessment

The recorded controlled A/B/A fresh-process experiment supports a local transport/address-family startup fault: localhost produced the first inflected miss, IPv4 literal removed it, and reverting localhost reproduced it. Independent source inspection confirms the IPv4-only host binding. DNS order and a bounded failing IPv6 TCP probe provide supporting evidence.

This does **not** establish the exact RPC status of the historical popup failure, prove all localhost configurations defective, eliminate every possible future 500 ms timeout, or certify latency. The in-container healthcheck's localhost is a separate network namespace; Sidecar binds `[::]`, so leaving that healthcheck unchanged is appropriate.

## Final execution-evidence review

Independently read the current XML counters/timestamps in `tests/TestResults/f09-ipv4/`:

| Artifact | Observed result |
|---|---|
| `red.trx` | 16 executed: 12 passed, 4 failed before source correction. |
| `green.trx` | 16/16 passed, zero failure/skip/abort. |
| `green-formatted.trx` | 16/16 passed after test-only whitespace formatting, zero failure/skip/abort. |
| `units.trx` | 79/79 passed, zero failure/skip/abort. |
| `integration.trx` | 209/209 passed, zero failure/skip/abort. |

Reviewer independently recomputed SHA256 for all 12 bindings in the accepted F09 manifest: all match. `git diff --quiet` confirmed the two gRPC adapters, Application/Domain runtime, Sidecar application and Compose have no changes versus HEAD in the inspected paths; both adapters retain 500 ms cancellation. The current scoped source diff remains the three defaults plus comment, address tests and whitespace-only formatting in their touched test files.

Read the browser tester's actual `popup-report.md` and the generated Playwright last-run status (`passed`, no failed tests). Browser execution: three fresh isolated Brave profiles, 18 actual selections/requests, zero failures/skips/retries. Cases cover exact-first, polite past, all three potential/passive/honorific interpretations, な-adjective, noun/copula and unknown input. The tester explicitly records no tokenizer prewarm before the first profile, no API mocking and no timeout increase.

Main agent supplied execution observations: Release build has zero warnings/errors; frontend 251 unit tests, typecheck and build pass; two fresh Release backends were started without a `GrpcTokenizerAddress` override, separately exercising root-CWD DI fallback and interface-CWD appsettings. Twelve exact→past pairs per backend produced 48 passing API requests in total; their first past requests completed in 121/112 ms with grammar completed. A fresh Debug backend through actual `start-demo.ps1` was used for the packaged popup. Original 20-table inventory/row hashes and `.env` hash match the private baseline. Those private DB fingerprints and full console sessions were not re-executed by this readonly reviewer; their provenance is the main agent's check, not a second independent live DB audit.

## Disclosures and delivery limits

- Automated packaged Brave is not automated Chrome; report browser identity accurately.
- Known Windows-only tests/Ubuntu limitation and absent workflow-validator hook are unchanged; no clean cross-platform CI or workflow-hook approval is implied.
- Controlled local experiments support this correction; the historical exact RPC status remains unproven, and another future resource/connection timeout remains possible under the unchanged 500 ms budget.
- No commit, push, merge, real dictionary import or production publication is authorized by this review.
