---
type: tester
date: 2026-10-14
---

# Local legacy database recovery validation

## Summary

Authorized local recovery completed without resetting/deleting original data. Original had17 legacy tables and empty history;now4 official migrations,5 grammar records,2 conjugation versions. Dictionary remains0 as import explicitly deferred. Backend startup twice reports DB up-to-date;first live health200/API contracts1/2/3 200/blank400. Agent stopped only its test servers;user starts own session with scripts/start-local.ps1.

## Results

| Verification | Result |
| --- | --- |
| Release solution build,existing dependencies | 0 warnings/errors |
| Targeted recovery/startup tests | 33/33,0 failure/skip |
| Backend units | 79/79,0 failure/skip |
| Final quiet full integration | 164/164,0 failure/skip |
| Frontend unit/typecheck | 251/251,15 files,typecheck pass |
| Actual clone/Apply fingerprints | All17 old tables counts+content hashes preserved |
| Independent local recovery review | PASS,score9.3,0 blockers |
| Actual API | health200 Healthy,v1/v2/v3 analysis200,blank400 |
| Actual gRPC | Synthetic approved example returns lexical base form |
| Accepted F09 byte bindings | All12 SHA256 intact |

No new dependencies,canonical seed edits,corpus edits,dictionary import,volume reset or Git delivery. Source migrations unchanged;local schema normalization intentionally reconciles known legacy names/indexes/nullable fields. Serial values/high-water,stronger uniqueness and verified timestamp triggers retained. Only InitialCreate baseline inserted after strict comparison;pending official migrations run normally.

## Traceability

| Requirement | Implementation/test |
| --- | --- |
| Refuse unknown schema before changes | LegacySchemaNormalizer and15 isolated cases,including rules,both-side inheritance,namespace spoof,deferred uniqueness |
| Preserve existing records and sequences | Serial high-water regression;17-table binary-stream fingerprint checks before/after clone/Apply |
| Explicit local-only operator action | DatabaseRecoveryCommand,Program switch,5 CLI tests;no HTTP/autostart recovery |
| Verified backup before Apply | Binary CreateNew/restore/SHA guards,LocalRecoveryScriptTests9 cases |
| Original issue resolves | Actual start-local.ps1 twice,initial no duplicate CREATE,HTTP proof |
| Contracts/F09 preserved | Full164 integration,251client tests,12 hash bindings |

## Retained recovery evidence

Private backup:data/local-recovery/recovery-20261014-584453a58c114105bd80f7111e97c4a6/database.dump. SHA256:3c9dd1895f809a1c140822915dba31ec3da07a84d5c9af58c3bf7aeaf269f5cf. Restore was verified into clone before original Apply. Backup ignored by Git;do not share its contents. Generated clone/reference databases retained,not deleted;they can be reviewed separately before any later cleanup.

## Encountered failures and limitations

- Initial helper test emitted internal VoidTaskResult;user-approved output-only suppression fixed it,without changing backup bytes/error checking.
- Desktop has docker.exe and extensionless docker;ambiguous command selection failed before DB writes. User-approved exe selection regression now passes.
- Earlier EF tooling command used WebApi without EFDesign;using existing Infrastructure design factory solved SQL generation without installing dependencies.
- Resumed full suite with Docker off:106failed/58passed. Docker restored;not represented as a successful run.
- Full suite concurrent with recovery:161passed/3failed in corpus V04-P1 and unlisted-word generalization for two roots,with null/empty alternatives. Unchanged500ms provider deadline and resource contention strongly supported;exact RPCstatus not logged. Quiet rerun164/164 passes without code/deadline modification. Latency certification not claimed.
- Final repeated HTTP/hash probe had a shell parser error before executing;not counted as HTTP proof. First actual HTTP check and both startup logs remain evidence;hash recheck performed separately.
- Original workflow-artifact hook is absent (MODULE_NOT_FOUND);direct artifact field/link/hash validation is fallback,not a passing original hook. Ubuntu CI remains explicitly excluded;no clean cross-platform delivery claim.
- Recovery requires disconnected local backend/clients;idle checks cannot prevent a future outside writer. Not a production concurrent recovery tool.
- HTTP200 is not evidence of useful dictionary results:canonical dictionary absent,import postponed by user.

## Next steps

User runs start-local.ps1 then reloads Extension. Plan/import approved canonical dictionary separately;do not seed test corpus as user data. No pending original recovery action or automatic commit/push/merge.
