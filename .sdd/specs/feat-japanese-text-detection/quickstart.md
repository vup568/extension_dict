# Validation Guide: Japanese Text Detection (F-01)

**Feature:** [SPEC.md](SPEC.md)
**Design references:** [data model](data-model.md), [local contract](contracts/japanese-text-detector.md), [research decisions](research.md)

## Prerequisites

- Node.js 22.12 or newer and npm.
- Run from the extension/ directory.
- Chrome and Edge are auto-detected by their Playwright channels.
- Set BRAVE_EXECUTABLE_PATH and FIREFOX_EXECUTABLE_PATH to approved local executables to add those target projects. Missing targets remain pending.

Install exactly the locked dependencies:

    npm ci

## Verified commands

    npm run unicode:verify
    npm run typecheck
    npm run test
    npm run test:coverage
    npm run build
    npm run test:browser
    npm run benchmark

## Validation order

1. Run the generated-policy reproducibility check. Confirm source release 17.0.0, source checksums, license/notice, generator revision and generated-table checksum match the provenance manifest.
2. Run the unit/corpus suite. All cases F01-AC-001 through F01-AC-026 must pass, with independent expected outcomes.
3. Run isolation/privacy checks with synthetic markers. Confirm no DOM or selection changes, no browser/vendor global dependency, no storage/network use, no raw-input logging and no retained input reference.
4. Run the identical corpus on Chrome, Edge, Brave and Firefox. Record OS, browser version, runner version, policy version, case count and outcome for every browser. A missing browser remains pending.
5. Run the benchmark profiles from F01-AC-032. Record device, browser/version, input lengths, positive-first/positive-last/negative/supplementary shape, cold/warm sample counts and p95. Compare only with the separately approved OD-005 detector budget.
6. Complete the traceability review in PLAN.md section 7 against every acceptance and success criterion. Do not convert pending browser or benchmark evidence into a pass.

## Expected observable outcomes

- Japanese trigger members, including standalone supplementary U+20B9F, classify true.
- Chinese Han text also classifies true by the approved presence policy; F-01 is not language identification.
- Plain Latin, Hangul, radicals, symbols, emojis, isolated marks, entity/escape literals and the literal mojibake fixture classify false unless another trigger member is present.
- Non-string values are invalid input, not false.
- Lone surrogates do not crash or match, and valid later/earlier trigger characters still determine the result.
- No test requires authentication, network access, a Backend service, a database, real user page text, DOM extraction, popup UI or persistence.

## Evidence to retain for review

Retain the provenance manifest, generator validation output, unit/corpus report, isolation/privacy report, four-browser parity report, benchmark report and final acceptance traceability. These artifacts are evidence for delivery; they are not user-content storage.
