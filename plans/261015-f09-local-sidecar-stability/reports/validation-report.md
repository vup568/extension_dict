# Verification — F09 local Sidecar IPv4 stability

Date: 2026-10-15. Scope: approved local transport correction, not new conjugation rules or dictionary import.

## Outcome

Three host-local defaults now use `http://127.0.0.1:50051`, matching Compose's published port. DI continues honoring an explicit Sidecar address. API shapes, dictionary selection, linguistic data, cancellation and the 500ms provider bound are unchanged. No retry, prewarm, new dependency, original DB write, commit, push, merge or server deployment.

## Diagnosis: before and after

Unchanged code reproduced the first polite-past lookup with backend conjugation `unavailable`, no base match, and failed grammar. This occurred before popup rendering. `localhost` resolved IPv6 `::1` first; Compose publishes only IPv4. A direct IPv6 TCP probe failed at a 2-second bound while IPv4 connected in 2ms.

Controlled fresh-process address reversal:

| Case | First polite-past HTTP time | Result |
|---|---|---|
| Before: localhost | 626ms | unavailable, 0 matches/explanations |
| IPv4 experiment | 142ms | completed, 1 match/explanation |
| Revert experiment: localhost | 612ms | unavailable, 0 matches/explanations |
| After: DI default, fresh Release process | 121ms | completed; all 24 requests pass |
| After: appsettings default, fresh Release process | 112ms | completed; all 24 requests pass |

These are observations on this Windows/Docker host, not latency certification or proof of the exact historical RPC error code. The first exact lookup can exceed 500ms total HTTP time because SQL/JIT/other work is outside the provider timer; it must not be confused with a provider deadline regression. Exact base words intentionally have unavailable conjugation with a successful dictionary result.

## Verification evidence

| Check | Result | Artifact |
|---|---|---|
| Regression before source fix | 12 passed, 4 expected failures | `tests/TestResults/f09-ipv4/red.trx` |
| Regression after source fix | 16/16 passed | `tests/TestResults/f09-ipv4/green.trx` |
| Release build | 0 warnings/errors, no restore | Tester execution |
| Backend units | 79/79 passed | `tests/TestResults/f09-ipv4/units.trx` |
| Integration | 209/209, no failures/skips | `tests/TestResults/f09-ipv4/integration.trx` |
| Final scoped formatting | Passed | `dotnet format whitespace --verify-no-changes` on changed C# tests/DI |
| Regression after whitespace-only normalization | 16/16 passed | `tests/TestResults/f09-ipv4/green-formatted.trx` |
| Frontend | Typecheck, 251/251 units and extension build passed | Tester execution, installed dependencies only |
| Fresh DI default process | 12 exact→past pairs, 24 successful requests | Parent-controlled live verification |
| Fresh appsettings default process | 12 exact→past pairs, 24 successful requests | Parent-controlled live verification |
| Actual demo launcher + packaged popup | 3 fresh Brave profiles, 18 actual API requests, no retry/skip/failure | [Popup report](popup-report.md) |
| Original database | Same inventory and private row-content SHA256 across all 20 public tables | Parent before/after fingerprint comparison |
| Local secrets file | `.env` SHA256 unchanged | Parent private comparison, digest not published |
| Frozen F09 acceptance | 12/12 file hash bindings unchanged | Accepted manifest recheck |
| Shutdown | Only agent-owned test backends stopped; Docker/DB retained | Port 5000 checked free |

Integration 209 executed before five whitespace-only normalizations in the two test files; afterward scoped formatting and the focused 16 cases were rerun. No runtime logic changed after the full passing suite. The extension build reported its existing Node experimental type-stripping warning; existing EF multiple-collection query warnings were observed, not introduced or suppressed as a code fix.

## Acceptance traceability

| Requirement | Implementation | Proof |
|---|---|---|
| Correct local endpoint | DI fallback, appsettings and start-local use the IPv4 published address | Red→green defaults/Compose/launcher cases; fresh-process checks |
| Preserve deployment overrides | Existing configuration null-coalescing precedence retained | Explicit container hostname, HTTPS hostname, IPv6 and localhost tests |
| Preserve caller environment | Existing launcher `finally` retained | Both Windows PowerShell 5 and PowerShell 7 failure/restoration tests |
| Explain supported inflections without fabricated results | Existing F06/F09 engine/data unchanged | 209 integration suite; real popup past/ambiguity/na/noun/unknown cases |
| Preserve 500ms/cancellation and v1/v2/v3 behavior | No adapter/usecase/contract changes | Existing fault/caller-cancellation/corpus/API regression cases |
| Preserve original data/privacy | Demo-only execution; no new log path, schema or linguistic source changes | Original 20-table + `.env` comparison, 12 accepted bindings |

Fresh default verification launches separate processes with `GrpcTokenizerAddress` removed from their environment, once from repository root (DI fallback) and once from the interface directory (appsettings). Only `/health/live` is polled before the first exact→past pair; no tokenizer warmup or automatic request retry. The actual `start-demo.ps1` backend was a third fresh process, followed immediately by the unchanged packaged popup test on three fresh profiles.

## Review, limitations and next step

See [independent review](final-review.md) and [active plan](../plan.md). Automated browser proof is installed Brave/Chromium, not branded Chrome or Firefox. User can reload the unpacked Chrome package and repeat the guide. This corrects the demonstrated local routing fault; explicit misconfiguration, service outages or future resource saturation can still truthfully return unavailable.

Known Windows-only CI limitation remains per the user's previous scope choice. Official workflow/doc validator scripts are absent; invocation failure and direct-field/link fallback are disclosed, not official gate approval. Native Task tools are unavailable; the plan's complete phase checklist is the persistent tracker. Existing dirty recovery/demo work remains uncommitted and untouched outside the approved overlap.

Next is a **separate** dictionary-import SPEC/PLAN, with source/license/scope approval before downloads or loading real data. No production publication/F20 acceptance is inferred from this fix.
