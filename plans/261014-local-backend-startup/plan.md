---
title: "Fix local backend database startup"
status: completed
priority: P1
effort: medium
tags: [startup, local-config, windows]
branch: feat/tokenizer-deinflection
created: 2026-10-14
---

# Local backend startup fix

## Overview

User approved Shadow Plan with Proceed. Missing DefaultConnection skips AppDbContext registration while EF repositories stay registered;Program swallows initialization failure and serves health200/analysis500. Compose reads .env but direct dotnet run does not. Earlier supplied commands omitted the required connection variable;integration tests injected it explicitly.

## Phases

- [x] Scout:DI/Program/config/Compose/README,dependent repositories and composition tests;two independent read-only scouts confirmed cause.
- [x] Diagnose:exact AppDbContext resolution exception;live pre-fix health200/analysis500. Failure occurs before SQL authentication/gRPC.
- [x] Regression tests red then targeted fix:missing/blank config fail-fast;startup migration failure blocks serving;explicit local helper builds safely quoted connection configuration.
- [x] Verify:Release build,17 targeted tests,79 unit/135 integration and251 frontend tests plus typecheck pass on Windows;only isolated DBs,never user migration/restart.
- [x] Independent review/adversarial validation:PASS_WITH_RISK for Windows handoff;Ubuntu CI finding explicitly excluded by user,not resolved. Artifacts document this delivery blocker and unavailable hook.
- [x] Docs/memory/journal/handoff;retain F09 accepted artifacts;no Git operations.

## Scope and dependencies

Modify src/infra/DependencyInjection.cs,src/interface/Program.cs,add testable database startup helper and scripts/start-local.ps1 with literal .env parser. Existing packages only. Add tests under tests/integration;update README/root PLAN/SDD. Native task tools unavailable;this persistent plan tracks all phases.

## Success criteria

Missing config fails before listening with a safe actionable error;valid config registers transient contexts;startup DB failure cannot falsely appear usable;local helper explicitly reads known dotenv keys and never prints/persists credentials;original analysis boundary works on a real isolated DB. Contracts/schema/linguistic corpus unchanged.

## Risks and authority

Credentials handled only in process environment;dotenv never evaluated as code. Script execution by user starts existing Docker services and backend's existing migrations,clearly documented. Agent verifies with ValidateOnly/mocks/isolated Testcontainers and does not stop/restart user's process,install packages or migrate user DB. Accepted F09 report/dataset/corpus hashes remain untouched. Fail-fast startup is an intentional behavior correction,not public API evolution.

## Issues encountered

First linked-source build required explicit DI/logging usings (Web SDK global usings are not inherited by the integration project). Fixed imports;stale --no-build test output after compile failure was not valid post-fix evidence. Windows PowerShell5 writes module-preparation progress as CLIXML on stderr;test child process suppresses only progress,not errors. Hook workflow-artifact-gate.cjs is absent from configured/local paths;finalization must report that limitation and validate artifact fields explicitly without claiming the missing hook passed.

Frontend sandbox attempt failed on temporary Vitest cache creation;the escalated rerun passed251 tests. Independent review found Windows executable names in new tests incompatible with Ubuntu CI. User explicitly chose Windows-only and declined the Ubuntu fix;no clean cross-platform CI or Git-delivery claim. Documented artifact hook execution returned MODULE_NOT_FOUND;fallback checks validate evidence structure and retain the known hard-delivery blocker,not replace it with a false gate approval.

## Completion boundary

Windows local fix complete under the user's narrowed scope. Independent review PASS_WITH_RISK,zero in-scope blockers. User must stop old backend and execute scripts/start-local.ps1;agent has not modified the user's running service/database. Git delivery,Ubuntu CI remediation and production remain outside this task. Detailed traceability and results:reports/validation-report.md.

## Subsequent local checkpoint authorization

On2026-10-14 the user explicitly authorized local commits for F06/F09 and this fix,without push/merge. This follow-up does not authorize CI remediation or ship/deployment. Scoped Git attributes preserve accepted byte hashes;immutable corpus/review whitespace remains unchanged. Generated harness JSON stays outside the commits. Historical reports retain their original no-Git boundary and unavailable-hook/Ubuntu limitations.
