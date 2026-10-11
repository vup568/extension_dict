# F-03/F-04 Acceptance Traceability

Date: 2026-10-11. Scope: feature SPEC.md AC01–AC10.

| AC | Implementation | Evidence | State |
|---|---|---|---|
| AC01 | browser/popup/startSelectionPopup.ts reuses F-02 → F-01 | browser ruby/negative/oversized; F-01/F-02 regressions | PASS |
| AC02 | interaction/analysisController.ts and synchronous createPopupView commit | Node exact debounce/loading and browser final selection | PASS |
| AC03 | sequence invalidation, UUID and AbortController before debounce | Node stale-during-debounce and browser abort observation | PASS |
| AC04 | identity guard on all async outcomes, timeout and close | Node success/error/partial/ignored abort; browser late result/error/BFCache | PASS |
| AC05 | api/analysisContract.ts and platform runtime/background | validator malformed/mixed-case/correlation/version tests; actual MV3 minimal POST | PASS |
| AC06 | controller/background bounded deadlines; fresh Retry ID | Node hung/ignored abort and browser frozen-clock exact timeout/retry | PASS |
| AC07 | ui/PopupShell.tsx sections preserve source facts | browser languages/restrictions/provenance/repeated occurrences/partial/empty | PASS |
| AC08 | open Shadow DOM manual popover top layer, clamp and focus lifecycle | browser hostileCSS/transformedhost/viewport/scroll/Escape/nestedfocus/popupselection | PASS on Chrome/Edge/Brave |
| AC09 | content-free failures, origin config, sender ownership, no storage/logging | Node origin/sender/cancel/error-code tests and browser privacy/packaged no cookie | PASS |
| AC10 | build-extension.mjs Chromium/Firefox platform packages; library preserved | production builds, actual packaged MV3 Brave smoke | PASS for build + packaged Brave; Firefox runtime pending |

No backend API shape or DB schema changed. Backend configuration now has no hardcoded
default connection string; this security correction requires documented environment config.
Firefox parity, approved p95 benchmark and PostgreSQL rerun are declared limits in
validation-report.md; no checkbox/task completion converts them into runtime evidence.
