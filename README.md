# JP Reading Platform v2

This branch is the clean foundation for JP Reading Platform v2. The product is
being rebuilt through Spec-Driven Development (SDD) and Agent-Driven
Development (ADD), with the approved product requirements as the primary source
of truth.

No V2 runtime has been scaffolded yet. Technology choices and the final project
layout must be approved through the Platform Foundation plan before application
code is added.

## Authoritative artifacts

- [Product Requirements](REQUIREMENT.md) - approved product scope and
  system requirements.
- [Migration Decision](MIGRATION_DECISION.md) - rules for preserving,
  migrating, reimplementing, investigating, or removing V1 assets.
- [Project Constitution](.sdd/constitution.md) - non-negotiable
  engineering and governance principles.
- [Platform Foundation Spec](.sdd/specs/feat-platform-foundation/SPEC.md) - current
  specification for the shared V2 foundation.

## Branches

- `dev` - active V2 foundation and future implementation.
- `extension_v1` - frozen, complete snapshot of the legacy browser extension.

V1 runtime code must not be copied into V2 by default. Reuse is allowed only
after classification under the migration decision and validation against the
approved regression corpus.

## Development workflow

```text
REQUIREMENT.md
      |
Constitution
      |
Feature Spec
      |
Clarify
      |
Plan
      |
Tasks
      |
Analyze
      |
Implement
      |
Converge and Validate
```

Human approval is required at the specification and planning gates. Agents must
work from the current feature artifacts and must not expand scope beyond the
approved requirements.

## Current status

The Platform Foundation specification is drafted and its quality checklist is
complete. The next gate is human review of the specification, followed by the
architecture planning workflow.

## Legacy reference

The complete V1 implementation remains available without cluttering this
branch:

```powershell
git switch extension_v1
```

Return to V2 development with:

```powershell
git switch dev
```
