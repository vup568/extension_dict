---
title: "F09 local Sidecar IPv4 stability"
status: completed
priority: P1
effort: small
branch: feat/tokenizer-deinflection
tags: [f09, windows, grpc]
created: 2026-10-15
---

# F09 local Sidecar IPv4 stability

## Overview and approval

User explicitly approved the scoped Shadow Plan with “Proceed”: fix local Sudachi defaults, add regression tests and verify fresh startup/API/packaged popup. No timeout increase, linguistic changes, DB reset/import, dependency install, Git delivery or deployment. This is step 1 of the agreed sequence; dictionary import SPEC/PLAN follows only after this stability gate.

## Phases

- [x] Scout and diagnose unchanged code; preserve prior dirty recovery/demo work and frozen F09 artifacts.
- [x] Add red regression tests for launcher, application configuration and DI fallback/explicit override: 4 expected failures, 12 passing cases before source patch.
- [x] Align three local defaults with Compose's IPv4-only published port; focused green 16/16, explicit address overrides retained.
- [x] Run full regression, fresh-process exact→inflected tests and real packaged popup; independent review.
- [x] Record evidence, user guide/memory/journal and hand off; keep original DB/accepted artifacts unchanged.

## Files and boundaries

Read: F09 SPEC, adapter/usecases, DI, Compose, launcher, configuration, related tests and existing demo report. Modify only production defaults in `src/infra/DependencyInjection.cs`, `src/interface/appsettings.json`, `scripts/start-local.ps1`; add/update tests in `tests/integration/LocalTokenizerAddressTests.cs` and `LocalBackendScriptTests.cs`. Update current README/local guides/memory and create scoped reports/journal. Leave historical hash-bound F09 corpus/datasets/reports, migrations, API contracts and 500ms adapter deadline untouched.

## Diagnosis and pre-fix evidence

Historical symptom: the real popup's second selection, polite past, had backend conjugation `unavailable` instead of `completed`. Exact-first base-word unavailable conjugation is expected and is not a failure.

Current unchanged-code reproduction on existing demo, after Docker startup:

| Experiment | First exact | First polite past | Later result |
|---|---|---|---|
| A: fresh backend, localhost Sidecar | 1305ms, grammar failed | 626ms, grammar failed/conjugation unavailable, 0 dictionary matches | 11 later past requests succeed |
| B: fresh backend, IPv4 literal Sidecar | 741ms, grammar completed | 142ms, completed, one base match/explanation | All 4 rounds succeed |
| A reversal: fresh backend, localhost | 1352ms, grammar failed | 612ms, grammar failed/conjugation unavailable, 0 matches | Second past 468ms succeeds; remaining rounds pass |

`localhost` resolves `::1` before `127.0.0.1`. Direct TCP probe to `::1:50051` failed at the 2-second bound; IPv4 connected in 2ms. Compose publishes only `127.0.0.1:50051`. Sidecar reported tokenization in ordinary milliseconds. Controlled address reversal supports a local transport/address-family startup fault, not incorrect conjugation data. The exact RPC status for the historical popup failure was not captured; do not invent it or claim global latency certification. Existing channel creation does not wire app logger factory, so setting gRPC Debug alone yielded no RPC metadata. No diagnostic logging implementation is needed for this narrowly established address fix.

## Test strategy and success criteria

Tests fail against old defaults and pass after changes; DI still honors explicitly configured remote/container/IPv6 addresses. Launcher exports IPv4 and restores previous process variables on failure/exit. Run existing cancellation/unavailable/corpus/API compatibility and recovery/demo regression tests without weakening assertions. Fresh backends must handle the exact→past sequence on the first inflected request, without prewarming, retries or Sidecar restart. Real packaged test uses installed Brave (Chromium); automated Chrome loading remains unsupported, so no automatic Chrome claim. Original DB table fingerprints and 12 accepted F09 hash bindings remain unchanged.

## Risks and issues

Runtime changes are local defaults only: deployments may override the existing address setting. Existing Windows-only test/Ubuntu limitation and absent workflow validator remain disclosed. Native Task tools unavailable; track all phases here. Prior dirty changes are user-owned and remain uncommitted. TCP timeouts alone are not gRPC error-code proof; exact-first unavailable is intentional. Keep validation workloads serial around 500ms live tests to avoid resource-contention noise.

## Final evidence

Build 0 warnings/errors; 79 units, 209 integration tests, 251 frontend tests/typecheck/build pass. Focused regression goes 12-pass/4-expected-fail before the patch → 16/16 after; scoped formatting and the 16 cases pass again after whitespace-only test normalization. Full integration predates those whitespace-only edits, with no runtime logic change afterward.

Two fresh default-path Release processes pass 48 real API requests; first polite-past request completed at 121ms/112ms without tokenizer warmup. Actual demo launcher followed by 3 fresh packaged Brave profiles passes 18 requests, no retry/skip. Original 20 public-table fingerprints and `.env` unchanged; all 12 accepted F09 hashes unchanged. Only agent-owned backends stopped; port 5000 free. Independent source/execution review: PASS, no scoped P1/P2; no Git delivery.

See [verification/traceability](reports/validation-report.md), [review](reports/final-review.md), [actual popup proof](reports/popup-report.md), [guide](../../docs/local-demo-testing.md), and [journal](../../docs/journals/261015-f09-local-sidecar-stability.md). Official workflow/doc hook absent: direct artifact/reference checks are a disclosed fallback, not official hook approval.

## Next step

After verified stability, prepare a separate dictionary-import SPEC/PLAN using verified source/license choices and V1 knowledge. Never infer approval for GPL-derived Vietnamese data, source downloads or writing real data into an existing DB from this local-address fix.
