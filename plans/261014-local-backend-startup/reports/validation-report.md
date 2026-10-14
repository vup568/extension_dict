# Windows local backend startup — validation

Date:2026-10-14. Scope:user-approved Windows local fix,not Git delivery or production.

## Root cause and outcome

The local guide omitted the required connection configuration. Compose loads .env but direct dotnet run does not. Infrastructure skipped AppDbContext while registering EF repositories;Program swallowed initialization errors and opened HTTP. The observed resolution error preceded any SQL/gRPC operation. Before:health200,analysis500. After:missing configuration fails early;database initialization failure stops startup;the same analysis input returned200 through an isolated real-PostgreSQL HTTP host.

The original user's process and database were not restarted/migrated. Actual post-fix user smoke awaits their execution of scripts/start-local.ps1. Existing migrations run when the user launches that script;no new schema or linguistic data change in this fix.

## Traceability

| Acceptance criterion | Implementation | Proof |
| --- | --- | --- |
| Missing/blank config fails before service use | infra/DependencyInjection.cs | BackendConfigurationTests;3 missing-value cases failed pre-fix,4 cases pass after |
| Startup failure does not expose healthy HTTP | interface/DatabaseStartup.cs,Program.cs | DatabaseStartupTests for DI failure,unreachable DB,cancellation;Program orders initialization before endpoints/listen |
| Valid config preserves parallel-capability lifetime | infra/DependencyInjection.cs | Distinct transient contexts in BackendConfigurationTests |
| Safe explicit local configuration | scripts/local-backend-config.ps1,start-local.ps1 | LocalBackendScriptTests with PS5/7,quoting/rejection/precedence/environment restoration;real .env ValidateOnly without services |
| Original analysis boundary works with database | interface composition/startup | DatabaseStartupHttpTests:POST contract3 returns200,health200,on isolated Testcontainers DB |
| Existing F09/business contracts preserved | No dataset/API/schema changes in this fix | Full backend/FE regression;acceptance manifest hash checks |

## Fresh verification

- Release solution build:no restore,0 warnings/errors.
- Targeted startup/config/script tests:17 passed,0 failed/skipped.
- Backend unit:79 passed,0 failed/skipped.
- Full integration:135 passed,0 failed/skipped,including17 new regression tests and118 existing tests.
- Frontend:typecheck passes;15 files/251 tests pass after escalated rerun.
- No dependency installation,no production activation,no real-user database migration,no Git mutation.
- Final read-only verification:all12 accepted F09 manifest bindings match SHA256;TRX files confirm17/79/135 complete passes;git diff --check passes. Artifact field checks pass and correctly retain hard-delivery BLOCKED.
- Build/tests used installed dependencies and existing isolated Docker images. Test results:tests/TestResults/local-startup/startup-{targeted,unit,integration}.trx.

## Review and limitations

Independent review PASS_WITH_RISK for Windows,zero in-scope blockers. Read-only adversarial password probe confirmed delimiter/quote/Host payload cannot inject a connection-string field. Reviewer's sandbox test did not complete and is not counted as separate passing evidence.

Known P1 retained:LocalBackendScriptTests calls Windows executable names and Ubuntu CI cannot execute them. User explicitly declined Ubuntu remediation and selected Windows-only. This prevents clean multi-platform CI/Git-delivery claims;not silently waived for ship/push/PR.

Encountered and corrected:explicit usings needed for linked startup source;stale no-build run after failed compilation not evidence;PS5 module-progress CLIXML required suppressing only progress in child tests;frontend sandbox temporary-cache failure required escalated rerun.

The required workflow-artifact-gate.cjs is absent;its documented invocation returned MODULE_NOT_FOUND. Fallback read-only checks validate fields/results/hash bindings and retain BLOCKED hard-delivery state. The original validator did not pass,and no generated score substitutes for it.

## Handoff

Stop the old backend with Ctrl+C,then run scripts/start-local.ps1 from project root and reload the Chrome page. README and docs/local-backend-testing.md describe prerequisites,existing migration behavior,empty dictionary caveats and troubleshooting. No automatic commit/merge.
