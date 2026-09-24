# US1 Positive Detection Evidence

**Executed:** 2026-09-20
**Policy:** Unicode 17.0.0
**Result:** Pass

- 31 independent US1 corpus cases cover F01-AC-001–008 and F01-AC-017.
- Standalone supplementary Han U+20B9F is an independent case; its result is not masked by adjacent Kana.
- BMP and supplementary Kana, compatibility ideographs, U+3007, decomposed text and mixed-script inputs are represented explicitly.
- npm run test completed with 116/116 tests passing across 6 files after the final corpus update.
- npm run test:coverage passed the 85% project threshold: 93.54% statements, 86.66% branches, 100% functions and 93.33% lines at the recorded run.

The evidence proves the pure detector and local boundary behavior only. It does not claim selection capture, popup or Backend integration.
