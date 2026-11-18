# Local demo dictionary verification — 2026-10-14

## Delivered scope

Existing Compose PostgreSQL now contains separate `jp_dict_demo`: 17 synthetic dictionary entries, 17 source records, one synthetic source manifest and four unchanged EF migrations. Only the pinned corpus `dictionary_fixtures` are loaded; test cases and expected explanations are not runtime dictionary data. Normal API/F06/F09 logic, migrations, datasets and dependency versions are unchanged. No commit, push or merge.

## Verification

| Check | Evidence |
|---|---|
| Release build | 0 warnings/errors, existing dependencies only |
| Backend units | 79/79; `tests/TestResults/local-demo/full/local-demo-units.trx` |
| Integration | 202/202, including 28 demo seeder and 10 CLI/script cases; latest final run is `tests/TestResults/local-demo/full/local-demo-final-integration.trx` |
| Frontend | Typecheck and 251/251 units pass; package rebuilt with `npm run build` |
| Formatting | Scoped C# whitespace verification and scoped `git diff --check` pass |
| Actual Docker preparation | First install and repeated preparation succeed; entries remain 17, source records 17, manifest 1, migration history 4 |
| Actual reseed stability | SHA256 of dictionary-entry row JSON, including IDs/timestamps, unchanged before/after preparation; isolated tests also compare all fixture child/source IDs |
| Original database | Same 20 public tables; every table's privately streamed row-content SHA256 matches the pre-demo baseline |
| Frozen F09 artifacts | All 12 acceptance-manifest SHA256 bindings unchanged |
| Real API | Health 200; v1/v2 retain zero ambiguous explanations, v3 returns all three for `食べられる`; `書ける` retains exact fixture 016 and no deinflection; blank input 400 |
| Packaged popup | Installed Brave, real background HTTP/API and demo DB: one passing test with six selections; no API mock or automatic retries |
| Handoff | Only agent-owned backend stopped; port 5000 free, Docker services/DB retained |

## Acceptance traceability

| Approved requirement | Implementation | Proof |
|---|---|---|
| Separate local DB; preserve original | `prepare-demo.ps1`, exact loopback/database guard | Original 20-table fingerprint comparison; wrong-target tests |
| Load 17 fixture words, not expected answers | `DemoDictionarySeeder` fixture projection + synthetic source records | SQL lookup/provenance tests; actual 17/17/1 counts |
| Safe repeat; refuse unowned or altered contents | Advisory lock, transaction, marker/graph/catalog checks | Concurrent/idempotent/corruption/schema tests and actual stable row hash |
| Run demo and return to original configuration | `start-demo.ps1` temporarily overrides connection then calls existing launcher | Script environment/restoration/error tests; guide's Ctrl+C/start-local procedure |
| Meaning + F09 explanations in extension | Unchanged backend engine and rebuilt MV3 package | Six actual API-backed popup selections: exact word, past verb, ambiguity, na adjective, noun, unknown word |

Popup examples passed: `食べる` gives Vietnamese meaning; `食べました` gives `食べる` plus past/polite explanation; `食べられる` displays potential/passive/honorific alternatives; `静かでした` and `学生です` explain adjective/noun forms; an unregistered synthetic word returns no dictionary match. F09 may require scrolling within the popup.

## Failures and limitations retained

- Initial script tests: 6 pass/4 fail because the **test fixture** assigned `DbConnectionStringBuilder.ConnectionString` through PowerShell's IDictionary adapter. Explicit CLR setter fixed inspection only; final 10 pass without weakening criteria. Earlier seeder mutation test used JSON braces with EF's format parser; raw Npgsql mutation fixed that fixture. See [debugger report](debugger.md).
- Initial frontend sandbox run had Vitest cache ENOENT; unchanged escalated run passed all 251 tests. Docker Engine went down after the interrupted session; user restarted it. No dependency/image downloads or resets were used.
- The first actual popup attempt returned conjugation `unavailable` for the second selection, instead of an explanation. Subsequent direct API checks and an unchanged-criteria popup rerun passed. Provider/deadline/resource contention is only a hypothesis: exact RPC cause was **not captured**, and the backend had already processed other inflected requests. Do not describe this as proven cold start or certify error-free latency. The existing 500ms deadline was not relaxed. This remains an operational follow-up risk, not an undisclosed test skip.
- A manual diagnostic initially supplied a non-UUID interaction ID and correctly received validation 400; requests using valid UUIDs passed. No API contract was changed to accommodate that input.
- Installed Chrome ignores automation's extension-loading flags; automated proof is **Brave (Chromium)**. User's Chrome unpacked acceptance is separate. Firefox and Ubuntu CI are not certified; the existing Windows-only CI choice is unchanged.
- Ownership checks verify synthetic dictionary/provenance graphs and supported catalog guards, not all grammar/schema integrity. Normal grammar initialization may populate its existing rules in demo; this is not a real dictionary importer or production publication.
- Workflow artifact/doc validator scripts are absent in checkout. Direct field/reference checks are a disclosed fallback, not official validator approval. Native Task tools unavailable: all approved phases are tracked in [plan](../plan.md).

## User handoff

See [demo guide](../../../docs/local-demo-testing.md). Run `pwsh -NoProfile -File .\scripts\start-demo.ps1` from project root, reload Chrome's unpacked extension and the reading page. To return, Ctrl+C then run `start-local.ps1`. `.env` remains unchanged; synthetic demo DB is retained. Review/journal: [final review](final-review.md), [journal](../../../docs/journals/261014-local-demo-dictionary.md).
