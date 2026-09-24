# F-01 Implementation Approval

**Approved by:** Project owner
**Approval source:** Conversation instruction on 2026-09-20 to begin implementation after reviewing the exact dependency proposal
**Branch:** feat/japanese-text-detection
**Base:** origin/dev at d4671b3fccc83e77635ce9b16fa2e31dadfa25b3

## Approved workspace

- Package manager: npm 10.9.2 with a committed package-lock.json.
- Runtime: Node.js 22.15.0; package engines require Node.js 22.12 or newer.
- Workspace: extension/ as a private package.
- Scope: framework-free TypeScript core and test/build harness for F-01. No UI, DOM extraction, Backend changes, database changes, storage, network runtime dependency, or authentication.

## Approved dev dependencies

| Package | Exact version | Purpose |
|---|---:|---|
| typescript | 7.0.2 | Static typing and type-checking |
| vite | 8.3.0 | Approved client build tool |
| vitest | 5.0.1 | Unit and corpus tests |
| @vitest/coverage-v8 | 5.0.1 | Unit coverage evidence |
| @playwright/test | 1.63.0 | Browser parity and side-effect harness |

No additional package may be installed without another explicit approval.

## Browser availability at approval

- Chrome: available locally.
- Edge: available locally.
- Brave: unavailable locally; evidence remains pending.
- Firefox: unavailable locally; evidence remains pending unless an approved browser installation is provided.
