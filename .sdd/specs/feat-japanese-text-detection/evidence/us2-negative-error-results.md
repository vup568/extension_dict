# US2 Negative and Error Evidence

**Executed:** 2026-09-20
**Policy:** Unicode 17.0.0
**Result:** Pass

- 72 US2 corpus cases cover negatives and full-input/malformed-string scenarios in F01-AC-009–025.
- F01-AC-022 is represented by the complete six-value sequence and is repeated 100 times in the statelessness unit test.
- F01-AC-023 validates null, undefined, number, boolean, array and object as invalid-input without coercion.
- F01-AC-026 injects a synthetic detector failure and verifies unexpected-failure remains distinct from false.
- Error results contain only status and error code; tests prove the synthetic raw marker is absent.
- The 100,000-character negative and positive-at-end cases pass without prefix truncation.
- npm run test completed with 116/116 tests passing across 6 files.
