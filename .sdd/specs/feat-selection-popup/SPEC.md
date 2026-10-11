# F-03 / F-04 — Selection Popup and Current Interaction Protection

Date: 2026-10-11. Status: implementation authorized by owner in this chat.
Inherits REQUIREMENT.md, DESIGN.md, .sdd/constitution.md and .agents/AGENTS.md.

## Goal and scope

Deliver a loadable React 19 browser Extension that connects the existing F-01/F-02
selection boundaries to the existing anonymous POST /api/analysis contract.
No backend response/schema change, translation, authentication, learning, Web App,
history, linguistic datasets, or provider expansion belongs to this feature.

## Owner authorization

Owner requested F-02 merge/push, F-03/F-04 implementation and related documentation.
Owner separately approved react/react-dom/@types/react/@types/react-dom 19.3.0,
tailwindcss/@tailwindcss/vite 4.3.3 and locked transitive dependencies. No additional
package or browser installation is authorized. F-02 was merged/pushed to origin/dev
at e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4 before this implementation branch.

## Feature decisions

- OD-011 is refined only locally: open Shadow DOM, nonmodal dialog, retain host
  focus on automatic opening; restore host focus only if focus was in popup at close.
  Position below selection or above when needed; clamp within visual viewport, width <=380px.
  A manual native popover top layer prevents host transforms/overflow from changing
  fixed placement, with ordinary Shadow DOM fallback where the API is unavailable.
  Restore deepest accessible focus inside open host shadow roots; closed roots remain opaque.
- EXT-004: 300ms request debounce; show shell/loading immediately for valid selection.
  This prevents request debounce from being counted as popup render latency.
- PERF-004: independent 5000ms deadline from dispatch, never an infinite spinner.
- OD-014: respect existing API maximum 1000 trimmed UTF-16 code units; reject oversized
  text visibly without truncation. No sentence/page context is sent.
- Content scripts run per HTTP(S) frame. Input/textarea controls have no DOM Range
  representation and remain unsupported; DOM/contenteditable reuse F-02 exactly.
  Generate UUID v4 using getRandomValues so ordinary HTTP pages do not require
  the secure-context-only randomUUID API. BFCache pagehide clears active data while
  retaining the content instance's listeners; genuine teardown disposes all listeners.
- Content script messages background; only background fetches fixed build-configured
  backend origin. Use credentials omit, redirect error, and no content/page message bridge.
  Chromium MV3 service worker and Firefox MV3 background script are packaged separately.
- Vietnamese result sections preserve canonical IDs, matched form/reading, sense
  restrictions, gloss language, kanji provenance and each grammar occurrence/span.
  Unavailable capabilities and empty completed results have different UI states.

## Acceptance criteria and trace

| AC | Required behavior | Requirements |
|---|---|---|
| AC01 | Ruby-safe Japanese selection opens one shell; empty/non-Japanese/invalid selection sends no request | JPN-001, DOM-001, EXT-001/002 |
| AC02 | Loading renders before dispatch/response; rapid selection sends final text after 300ms | EXT-003/004, NET-001/002 |
| AC03 | Selection change invalidates identity immediately, including during debounce; attempt AbortController cancellation | ASYNC-001/002, EXT-005 |
| AC04 | Old success/error/partial, ignored abort, timeout and close never overwrite current state | ASYNC-002 |
| AC05 | One minimal envelope {text,interaction_id}; validate actual DTO shape, version and correlation | EXT-006, API-002/004, PRIV-001 |
| AC06 | 5s timeout, network/server failure and explicit Retry with fresh UUID | NET-003/004/005, PERF-004 |
| AC07 | Render vocabulary/kanji/grammar, partial/unavailable/empty truthful; retain restrictions/provenance/occurrences | NET-006, VOC-007/008, KAN-002, GRM-007/008 |
| AC08 | Shadow CSS isolation, narrow viewport, scroll/resize, Escape/keyboard and focus return; popup interactions do not create host lookup | BROWSER-003 |
| AC09 | No content logs/storage/history; configured backend permission only, no arbitrary URL fetch | PRIV-002/006, SEC-002, BROWSER-001/002 |
| AC10 | Library build preserved; packaged Chromium/Firefox artifacts; real background transport browser smoke | BROWSER-002 |

## Verification limits

Run unit/type/build/coverage, existing F-01/F-02 browser regressions and new real-browser
popup tests on installed Chrome/Edge/Brave. Firefox remains pending if absent. Performance
samples are local observations, not an approved PERF-001 benchmark certification.
Backend unit/build regressions run; PostgreSQL integration requires working local Docker.
Docker startup currently fails at dockerInference socket; do not reset/delete user data.
