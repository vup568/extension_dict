# F-02 Browser Parity and Privacy Report

**Date**: 2026-09-24  
**OS**: Windows  
**Branch**: `feat/ruby-safe-dom-extraction`

## US3 error, frame, privacy, and host-safety evidence

| Browser target | Local executable | US3 tests | Status |
|---|---|---:|---|
| Google Chrome 153.0.8010.53 | Installed | 6/6 | PASS |
| Microsoft Edge 153.0.4234.48 | Installed | 6/6 | PASS |
| Brave | Installed and launched through `BRAVE_EXECUTABLE_PATH` | 6/6 | PASS |
| Firefox | Not installed | 0 | PENDING |

The 18 executed Playwright entries verified:

- all six content-free error categories, including multi-range rejection before content access;
- coherent-selection invalidation returns `stale-selection` without partial output;
- one allowed event frame is read without parent or sibling marker aggregation;
- success and every error path create no marker-bearing request, storage, history, or console entry;
- DOM, inline style, focus, active selection, contenteditable markup, clipboard sentinel,
  navigation, and event default state remain unchanged;
- offline/online, page language, font, login-state marker, and repeated calls preserve the
  same exact result.

Commands used the already installed browser executables. No browser or package was
downloaded. Firefox remains pending rather than being represented by another Chromium
result.

## Final complete regression

After `npm ci`, the complete Extension Playwright suite passed **48/48**:

| Browser | Complete suite | Final F-02 corpus | Status |
|---|---:|---:|---|
| Chrome 153.0.8010.53 | 16/16 | 24/24 | PASS |
| Edge 153.0.4234.48 | 16/16 | 24/24 | PASS |
| Brave 153.0.8010.53 | 16/16 | 24/24 | PASS |
| Firefox | 0 | 0 | PENDING — executable not installed |

The complete suite includes existing F-01 browser regressions, every F-02 story, the
F-02→F-01 clean-text handoff, privacy/side effects, and correctness-asserting performance
profiles.
