# F-02 Performance Evidence

**Date**: 2026-09-24  
**OS**: Windows  
**Policy**: F-02 specification dated 2026-09-24  
**Method**: One correctness-asserted cold invocation, followed by 30 correctness-asserted
warm samples per profile and browser. Durations use the page `performance.now()` clock.

| Browser | Profile | UTF-16 length | Cold ms | Warm p95 ms |
|---|---|---:|---:|---:|
| Chrome 153.0.8010.53 | Small plain | 7 | 0.4 | 0.1 |
| Chrome 153.0.8010.53 | Nested ruby | 5 | 0.0 | 0.2 |
| Chrome 153.0.8010.53 | Multi-block | 3 | 0.0 | 0.1 |
| Chrome 153.0.8010.53 | Contenteditable ruby | 6 | 0.2 | 0.1 |
| Chrome 153.0.8010.53 | Long selection | 100,000 | 0.0 | 0.0 |
| Edge 153.0.4234.48 | Small plain | 7 | 0.5 | 0.1 |
| Edge 153.0.4234.48 | Nested ruby | 5 | 0.1 | 0.1 |
| Edge 153.0.4234.48 | Multi-block | 3 | 0.0 | 0.1 |
| Edge 153.0.4234.48 | Contenteditable ruby | 6 | 0.1 | 0.1 |
| Edge 153.0.4234.48 | Long selection | 100,000 | 0.0 | 0.1 |
| Brave 153.0.8010.53 | Small plain | 7 | 0.4 | 0.1 |
| Brave 153.0.8010.53 | Nested ruby | 5 | 0.1 | 0.1 |
| Brave 153.0.8010.53 | Multi-block | 3 | 0.1 | 0.0 |
| Brave 153.0.8010.53 | Contenteditable ruby | 6 | 0.1 | 0.0 |
| Brave 153.0.8010.53 | Long selection | 100,000 | 0.0 | 0.1 |
| Firefox | All profiles | — | — | PENDING — executable not installed |

All 465 measured invocations (31 samples × 5 profiles × 3 browsers) returned the exact
expected result. Values displayed as `0.0` are below the timer/reporting resolution after
rounding to three decimals; they do not mean zero computational work.

These figures are contribution evidence toward PERF-001 only. F-02 has no independently
approved latency allocation, so this report does not claim a standalone performance pass.
No raw selected text was logged; output contained profile ID, input length, browser metadata,
sample counts, and duration only. No browser or package was downloaded.
