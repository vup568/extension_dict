---
type: tester
date: 2026-10-15
---

# Popup verification: explicit IPv4 Sidecar configuration

## Summary

Existing packaged MV3 demo smoke passed 3/3 repetitions with fresh isolated Brave profiles: 18 actual analysis requests, zero failures/skips/retries, 13.3 seconds. First sequence used the newly launched backend without a pre-test tokenizer request; the main agent performed only HTTP health readiness before handing off.

## Findings

Command, from `E:/jp-dict-extension/extension` with the installed Brave executable selected through `EXTENSION_SMOKE_EXECUTABLE_PATH`:

```powershell
npx --no-install playwright test --config playwright.demo.config.ts --reporter=line --output=test-results/f09-ipv4 --repeat-each=3 --max-failures=1
```

`--max-failures=1` would stop at the first failure; all three repetitions passed, exit code 0. No API mock, response interception, automatic retry, test criterion edit, warmup request, deadline increase, browser installation or runtime change was made by this tester. Main agent reports rebuilt extension/typecheck/251 unit tests separately; this report certifies only the executed popup scope.

Each fresh profile verified six selections against the actual demo backend:

| Selection | Verified result |
| --- | --- |
| 食べる | Canonical fixture:word:001, Vietnamese meaning Ăn, exact-first/no conjugation |
| 食べました | 食べる, Vietnamese polite-past explanation, one alternative |
| 食べられる | 食べる, all three Vietnamese interpretations: potential/passive/honorific |
| 静かでした | 静か, Vietnamese な-adjective explanation |
| 学生です | 学生, Vietnamese noun/copula explanation |
| Synthetic unregistered selection | Empty dictionary result, truthful no-conjugation status |

All repetitions checked actual MV3 background POST with contract header 3/minimal envelope, popup rendering/close, no page errors, and no local/session storage writes. Profiles were temporary and cleaned by the unchanged test. No DB writes were performed by this browser test.

Screenshots (ignored local outputs):

- `E:/jp-dict-extension/extension/test-results/f09-ipv4/demo-dictionary-smoke-actu-72d3d-dictionary-and-live-F09-API/tabemashita-popup.png`
- `E:/jp-dict-extension/extension/test-results/f09-ipv4/demo-dictionary-smoke-actu-72d3d-dictionary-and-live-F09-API-repeat1/tabemashita-popup.png`
- `E:/jp-dict-extension/extension/test-results/f09-ipv4/demo-dictionary-smoke-actu-72d3d-dictionary-and-live-F09-API-repeat2/tabemashita-popup.png`

## Recommendations

Manually reload the unpacked Chrome extension for user testing. Automated result is installed Brave/Chromium, not branded Chrome: current branded Chrome ignores automated extension-load flags. No latency, Firefox, Linux CI or production-scale certification is implied by three local repetitions.

## Unresolved questions

No failure observed in this IPv4 popup run. The historical pre-fix intermittent unavailable response remains recorded in the prior report; this run does not independently prove its historical transport status.
