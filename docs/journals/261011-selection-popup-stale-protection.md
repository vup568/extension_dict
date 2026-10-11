---
date: 2026-10-11
session: selection-popup-stale-protection
status: verified-installed-browsers
---

# Journal — Selection Popup and Current Interaction Protection

## Context

Owner requested F-02 merge/push to `dev`, then F-03/F-04 implementation connecting
existing selection modules to unified analysis, with documentation updates.
F-02 was delivered at `e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4`; subsequent work
uses `feat/selection-popup-stale-protection`.

## Changes and decisions

- React 19/open Shadow DOM/nonmodal popup renders shell/loading before the
  300ms request debounce; background and controller bound requests to 5s.
- Immediate generation/UUID invalidation is the correctness guard; abort is a
  best-effort efficiency measure. Retry, same-text selection at a new location,
  close, timeout and page lifecycle cannot revive an old interaction.
- Selection stays frame-local and ruby-safe. Manual native popover top layer
  isolates ancestor transform/overflow, with fixed-host fallback; host focus is
  retained on automatic opening and restored conditionally on close.
- Runtime validation matches mixed-case backend DTOs. Presentation retains
  restrictions, language, provenance and each grammar occurrence while
  distinguishing failed/unavailable/completed-empty.
- Background fetch uses one build-configured origin with scoped tab/frame
  cancellation, omitted credentials and rejected redirects. No backend CORS
  expansion, selected-text history, translation or auth was added.
- Approved dependency versions were installed. Transitive source-map-js 1.2.1
  audit issue was corrected through exact override 1.2.2; audit reports 0 issues.
- Existing plaintext DB credential was removed from appsettings; backend setup
  now requires external connection-string configuration.

## Verification and limits

223/223 unit tests pass. Coverage: 96.74% statements, 93.58% branches, 98.24%
functions, 97.85% lines. Typecheck, Unicode verification and production builds
pass. .NET Release has no warnings/errors and 51/51 unit tests pass. Real
packaged MV3 smoke on Brave passed from an ordinary insecure HTTP host page,
with service-worker POST to loopback backend and only `{text, interaction_id}`;
no cookies/storage. Full browser suite passes 112/112 (Chrome37, Edge37, Brave37,
packaged MV3 Brave1), without skip/flaky/failure; independent review has no
outstanding P1/P2. See
[validation report](../../.sdd/specs/feat-selection-popup/evidence/validation-report.md).

Morphology/conjugation remain unavailable, deinflection defaults to
NullTokenizerAdapter, vocabulary is unsegmented whole-selection lookup, grammar
seed contains 5 patterns, and input/textarea selection is unsupported. Firefox
runtime and approved PERF-001 benchmark are pending. Existing Docker startup
failure prevents current PostgreSQL Testcontainers verification.

## Reflection and next steps

Separating popup render timing from request debounce preserves perceived
responsiveness. Cancellation alone cannot prove currentness; success, errors,
partial results and timeouts need the same identity guard. Documentation now
distinguishes MVP scope, implemented contracts and environment-dependent
verification. Traceability and normal feature-branch push were completed; remote
SHA `86cd930c1e05bbc8c68a590b452a63d2d3f5e8df` was verified equal to local HEAD.
The implementation remains on its feature branch for review; dev has the F-02 merge.
Next work can pursue remaining linguistic/data coverage. Firefox/Docker/approved benchmark limits
remain visible rather than being counted as passed.
