# F-03/F-04 Implementation Plan

Status: implemented and verified on installed targets; delivery in progress.
Branch: feat/selection-popup-stale-protection.
Date: 2026-10-11. Reviewed against source requirements and UI designer analysis.

## Read

REQUIREMENT.md; DESIGN.md; .agents/SDD.md; F-01/F-02 contracts; AnalysisEndpoints.cs;
AnalysisDtos.cs; nested dictionary/kanji/grammar DTOs; existing build/browser harness.

## Phases

1. Lock approved dependencies; define actual API types, validation and feature decisions.
2. Implement pure interaction controller with immediate invalidation and bounded lifetime.
3. Implement frame-local selection adapter and React Shadow DOM popup.
4. Add vendor runtime/background transport and loadable Chromium/Firefox packaging.
5. Run controller/contract/transport unit tests, real-browser popup/regression tests,
   packaged Extension smoke, typecheck/build/coverage and backend regression checks.
6. Independent code review, resolve findings and update related docs/evidence.

## Risks and mitigations

Mixed API field naming -> fixtures match actual backend DTO serialization.
Abort ignored -> generation plus UUID correlation, checked before every state update.
Host CORS -> extension background fetch restricted to configured backend origin.
Host CSS/focus -> Shadow DOM, host reset, nonmodal focus policy and browser fixtures.
Text retention -> no storage/logging, clear controller and DOM on close/invalidation.
Environment -> preserve Firefox/Docker pending status rather than fabricate pass.

## Progress

- [x] Source review, concrete plan and feature-local design review.
- [x] F-02 merge/push into dev, remote SHA verified.
- [x] Approved dependency installation and contract implementation.
- [x] Controller and selection integration.
- [x] Shadow popup and rendering.
- [x] Background transport and packaging.
- [x] Tests, independent review and traceability.
- [x] Documentation, traceability and verification handoff prepared.

Implementation and local verification are complete. Authorized remote delivery is
tracked separately in the root PLAN.md; Firefox/Docker/approved benchmark gates remain
explicit limits rather than unchecked implementation work.

## Issues encountered

- Dependency audit found transitive source-map-js 1.2.1 vulnerability. Exact override
  1.2.2 fixes the approved locked dependency graph; npm audit now reports zero findings.
- Review fixed BFCache listener lifetime, visualViewport size, transformed-host placement
  using a manual top-layer popover, open-shadow host focus restoration, and HTTP-safe UUIDs.
- Current .NET build succeeds with zero warnings/errors and unit suite passes 51/51.
- Current Extension Node suite passes 223/223; statement coverage 96.74%, branches 93.58%.
- Full browser suite passes 112/112, with 19/19 additional final-polish checks;
  independent review has no remaining P1/P2 blocker. Full report and preview archived.
- Docker Desktop startup fails at dockerInference socket. PostgreSQL integration rerun
  remains environment-blocked; prior F-02 50/50 results are historical, not a current pass.
