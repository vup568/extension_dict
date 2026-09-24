# F-01 Final Review

**Reviewed:** 2026-09-20
**Decision:** Implementation complete for available local evidence; not release-ready against the full SPEC

## Passed review areas

- Scope remains the pure Japanese-trigger detector and local input boundary.
- No selection listener, ruby extraction, popup, React UI, Backend endpoint, database, authentication or persistence was added.
- Runtime source has no DOM, browser-vendor, network, storage, logging or React dependency.
- Invalid input and unexpected failure are distinct from false and contain no raw input.
- Unicode sources, checksums, terms URL, generator version and generated checksum are reproducible.
- Mandatory classification/error corpus and long/malformed input behavior pass.
- Type check, build, coverage, Chrome/Edge parity, privacy and host-side-effect checks pass.
- Existing .NET Release build and unit tests pass (51/51); the integration suite could not start because the local Docker Engine pipe was unavailable.
- Existing .NET CI is preserved and a locked Extension job is added.
- No constitutional exception or hidden provider dependency was introduced.

## Explicit pending evidence

1. Brave is not installed locally; its parity/privacy row is pending.
2. Firefox is not installed locally; its parity/privacy row is pending.
3. OD-005 has not approved a detector-specific performance budget. Measurements exist, but F01-SC-005 remains pending.
4. The existing .NET integration suite remains unverified in this run because Testcontainers could not connect to `npipe://./pipe/docker_engine`; all 50 failures occurred during fixture startup before code-specific assertions.

The pending items do not invalidate the implemented core, but F-01 must not be called fully release-ready under SPEC.md until they are resolved. The Docker result is an environment prerequisite failure, not evidence of a product regression. No pending item is recorded as pass.

## Scope and migration impact

- Backend and database behavior are unchanged.
- No legacy source, migration or regression fixture was deleted.
- The backend CJK helper was not treated as F-01 policy authority.
- The private Extension package contains only the five explicitly approved dev dependencies.
