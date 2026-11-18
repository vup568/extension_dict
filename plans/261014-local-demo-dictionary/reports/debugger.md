---
type: debugger
date: 2026-10-14
---

# Demo script fixture debugging

## Summary

The initial four PowerShell script-test failures were caused by the test fixture's connection-string inspection, not demo-target routing. PowerShell adapts DbConnectionStringBuilder as IDictionary: assigning `.ConnectionString` adds a dictionary key instead of invoking the CLR property setter. Explicit `set_ConnectionString(...)` repairs only the test inspection/mock.

## Findings

- Both initial `demo-scripts.trx` and `demo-scripts-diagnostic.trx` contain 6 passed/4 failed out of 10. The diagnostic run exposes `Wrong seed target`, `Keyword not supported: 'Database'`, and the derivative `Backend not called` assertion.
- Independent read-only synthetic reproduction: property assignment produces keys `[ConnectionString]`; explicit setter produces `[host,database]` and correctly reads `127.0.0.1`/`jp_dict_demo`. No Docker, database connection, real environment secret, build or suite rerun was used for this reproduction.
- The latest `DemoScriptTests.cs` invokes the explicit setter at both affected test-only boundaries: captured seed child environment and fake start-local backend. Production builders/scripts are not weakened to accommodate a fake.
- The same 10 script cases remain. Assertions still require exact demo host/database, explicit CLI confirmation, fixed CREATE target/count, no implicit pull, unchanged caller connection, no ValidateOnly processes, secret-free output and surfaced backend failure. Nothing is skipped or converted to a success-on-failure check.
- Parsed final `demo-scripts-final.trx`: 10 passed/0 failed. Parsed full `full/local-demo-full.trx`: 202 passed/0 failed, including all latest 28 DemoDictionaryTests and 10 DemoScriptTests. These are inspected existing execution artifacts, not a new run by this debugger.
- Seeder's earlier SQL-fixture failure remains recorded separately: EF interpreted literal JSON braces in mutation SQL; Npgsql executes that fixed synthetic SQL literally. Final full suite includes the affected corruption case.

## Recommendations

Use explicit CLR setters for connection-string inspection on PowerShell IDictionary-adapted builders. Retain failed diagnostic artifacts and full-suite results; do not infer live local API/browser success from mocked script tests.

## Unresolved Questions

No remaining script-fixture failure in inspected final evidence. Docker/live demo preparation is a separate parent-owned verification step; this report does not certify it or Ubuntu compatibility.
