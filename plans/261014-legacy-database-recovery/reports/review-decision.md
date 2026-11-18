---
type: code-reviewer
date: 2026-10-14
---

# Local recovery safety review

## Summary

Independent reviewer legacy_recovery_review returned PASS for authorized local recovery,score9.3,zero remaining Important/Critical findings. Approval permits the guarded local recovery procedure,not a claim that the original database/API already works.

## Findings and resolved checks

- Explicit CLI only,no automatic baseline or HTTP recovery endpoint.
- Strict reference InitialCreate/history/version;target history empty;serial ownership/high-water validated without resetting IDs.
- Schema comparison rejects unknown columns/tables/checks/indexes/functions/rules/inheritance on either side of public tables,external trigger-function spoofing and deferred uniqueness.
- Known serial and unique-constraint compatibility is intentional;stronger COALESCE uniqueness and reviewed timestamp triggers retained.
- Single Windows executable resolution,binary CreateNew backup,private streamed fingerprints,composite-key ordering,child-only configuration and safe CLI errors verified.
- Backup restored on clone;all17 old-table fingerprints/counts remained equal through baseline and official pending migrations;history4 verified in rehearsal.
- Targeted33/33 tests include final external-child regression;independent unconfirmed CLI preflight rejects before database access.

## Recommendations and limits

Keep backend and other database clients disconnected through recovery. Idle checks do not prevent a later external writer;this is a quiescent local operator procedure,not production concurrent recovery. Original Apply/HTTP/full final integration still require execution evidence. Dictionary import explicitly deferred;HTTP200 is not proof of populated dictionary lookup. Missing workflow hook and previously accepted Windows-only/Ubuntu CI limitation remain disclosed.

## Unresolved questions

None about the reviewed local recovery design. Runtime verification pending Docker availability.
