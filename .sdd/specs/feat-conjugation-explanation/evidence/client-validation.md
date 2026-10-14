# F-09 — Client validation, 2026-10-14

## Summary

Extension requests contract3,accepts1/2/3;v3 renders all evidenced interpretations as separate “Cách hiểu có thể” cards. Client imports no corpus or linguistic rules. Existing popup/interaction state machine reused.

## Commands and results

From `extension/`,existing dependencies only:

```powershell
npm.cmd run typecheck
npm.cmd run test:coverage
npm.cmd run unicode:verify
npm.cmd run build
# BRAVE_EXECUTABLE_PATH points to the existing installed Brave executable.
npm.cmd run test:browser -- --project=chrome --project=edge --project=brave --project=packaged
```

| Gate | Result |
|---|---|
|Typecheck|PASS|
|Vitest|251/251;15 suites;0 failures/skips|
|Coverage|97.11% statements,94.53% branches,98.38% functions,98.07% lines|
|Unicode policy|25 ranges;SHA256 b46a981ebcc331220a81dafbc8c1f35bf17b1859cf016fa52a8c0dd6fec39bf9|
|Build|library+Chromium+Firefox manifests/bundles;PASS|
|Playwright|136/136:Chrome45,Edge45,Brave45,packagedMV3 Brave1;0 failures/skips/flaky|

Browser result: `extension/test-results/browser-results.json` (ignored machine artifact). Popup TSX is validated by browser tests; TS unit coverage is not claimed to measure TSX.

## Verified behavior

- Approved cases 食べられる(3),食べさせられる(2),学生です/静かです/𠮷です. Typed base/VI/rule/dataset and exact UTF16 reconstruction validated;duplicate identity,mixed dataset,blank VI,half-surrogates,unverified root/blank canonical identity/fabricated match rejected.
- Genuine pending v3 late-success after replacement or close is discarded;Retry has fresh interaction. Existing v1/v2,ruby-safe,minimal envelope,5s timeout,stale error/success,Shadow DOM/focus/privacy regressions retained.
- Packaged content→background POST requests header3 with only text/interaction_id,no cookies/page context/storage. Independent backend live HTTP tests feed actual responses for contracts1/2/3 to this same production parser,not fixture-only interoperability.
- First sandbox run failed on Vitest temporary cache ENOENT;outside-sandbox rerun and full suite passed. No code/dependency workaround used.

## Remaining limitations

Firefox executable absent in standard installation paths;no auto download/install. Firefox build is not runtime parity evidence. Existing whole-selection word-unit,unavailable generic morphology and publication boundary unchanged. G3 remains for the user;no production/Git delivery claimed.
