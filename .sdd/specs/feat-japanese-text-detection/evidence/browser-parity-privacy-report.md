# Browser Parity and Privacy Report

**Executed:** 2026-09-20
**OS:** Microsoft Windows NT 10.0.26200.0
**Policy:** Unicode 17.0.0
**Corpus:** 103 cases
**Command:** npm run test:browser

| Target | Version | Parity corpus | Privacy/side effects/environment | Status |
|---|---|---:|---|---|
| Chrome | 153.0.8010.52 | 103/103 | Pass | Pass |
| Edge | 153.0.4234.48 | 103/103 | Pass | Pass |
| Brave | Not installed locally | Not run | Not run | Pending |
| Firefox | Not installed locally | Not run | Not run | Pending |

Playwright executed 10/10 available-browser tests successfully. For Chrome and Edge this includes the complete corpus, no post-load classification requests, unchanged local/session storage, no raw marker in console output, no DOM/style/focus/selection mutation, and invariant results across offline/online and simulated page/user state.

Brave and Firefox are explicitly pending. A Chromium-family result is not substituted for either target, and F01-SC-002 is not marked passed until both missing rows have real evidence. The harness accepts BRAVE_EXECUTABLE_PATH and FIREFOX_EXECUTABLE_PATH when approved executables become available.
