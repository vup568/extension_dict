# Tasks: F-09 — Conjugation Explanation

**Date**: 2026-10-13 | **Status**: 42/42 complete;G1/G2/G3 accepted_for_scoped_corpus. Firefox runtime/production publication are separate limitations,not authorized by acceptance.
**Input**: [SPEC](./SPEC.md), [PLAN](./PLAN.md), [research](./research.md),
[data-model](./data-model.md), [contract](./contracts/conjugation-contract.md), [quickstart](./quickstart.md).
**Organization**: Setup → foundation → US1(scope) → US2(review) → US3(gaps/acceptance) → handoff.
Đây là danh sách công việc tương lai; tạo TASKS không cho phép tự chạy code, ký duyệt hoặc deploy.

**Scope update 2026-10-14**: User approved G3:“ok tôi duyệt nghiệm thu nhé”;acceptance-manifest.json binds unchanged corpus/candidate/review/report to reviews/user-acceptance-receipt-20261014.json. G1/G2 receipts retained;runtime data still development,production/Firefox separate.

## Format & rules

- Mỗi task: `- [ ] T001 [P nếu độc lập] [US1 nếu thuộc story] mô tả + đường dẫn`; ID tăng liên tục.
- Đường dẫn tương đối repo; file corpus/reviews/evidence là đầu ra cần tạo khi thực hiện.
- `[P]` chỉ cho phép song song sau khi các phụ thuộc chung đã xong, không chạy xuyên G1/G2.
- Tests được yêu cầu bởi SPEC/PLAN: viết từ expected đã duyệt, test gap trước implementation.
  Chỉ ghi fail khi có lỗi thật; không ép case đang đúng thành fail để làm TDD.
- Task conditional có thể đóng bằng **N/A có bằng chứng/lý do**, không bỏ qua im lặng.
  Human approval task chỉ tick khi người có thẩm quyền thật sự duyệt đúng version/hash.
- Không package mới, không sửa seed/migration cũ, không DB người dùng, không auto commit/push/merge.

## Phase 1 — Setup (T001–T002)

**Goal**: Ghi nhận phạm vi và môi trường, không scaffold hệ thống đã có.

- [X] T001 [P] Ghi baseline branch/working-tree và phạm vi chỉ thực hiện F-09 trong `.sdd/specs/feat-conjugation-explanation/evidence/start-state.md`; đối chiếu SPEC/PLAN, bảo toàn changes hiện có, ghi quyền triển khai chưa được suy ra từ việc tạo TASKS.
- [X] T002 [P] Kiểm tra .NET/Node/dependencies đã restore, Docker và browser executables; ghi availability/missing evidence trong `.sdd/specs/feat-conjugation-explanation/evidence/environment.md`, không tự cài package/browser.

**Checkpoint**: Có trạng thái đầu vào và môi trường; chưa động vào runtime data.

## Phase 2 — Foundational (T003–T004)

**Depends on**: T001/T002. Chỉ chuẩn bị conventions/hồ sơ, không thêm bảng hay endpoint.

- [X] T003 [P] Ghi conventions corpus/review theo data-model trong `.sdd/specs/feat-conjugation-explanation/corpus/README.md`: unique IDs, outcomes, ≥2 đúng+1 âm/rule, review states, UTF-16, source-byte SHA-256 lowercase, freeze/revision order và không checksum self-reference.
- [X] T004 [P] Freeze source fingerprint của `src/infra/Conjugation/Data/development-20261013.1.json`, seed reader/migration và danh sách tests hiện có vào `.sdd/specs/feat-conjugation-explanation/evidence/baseline.json`; giữ source bytes/hash/version để so sánh replay về sau, không coi baseline là linguistic approval.

**Checkpoint**: T003/T004 xong mới bắt đầu stories. Runtime schema/seed giữ nguyên.

## Phase 3 — US1: Chốt corpus bốn nhóm (Priority P1; T005–T012)

**Goal**: Corpus có phạm vi rõ, frozen examples và PO duyệt; không mặc định 9 rules là đủ.
**Independent Test**: Đọc manifest/cases/approval material, kiểm tra coverage/ref/hash/PO/date;
không cần sửa backend/popup. Maps PLAN A1–A4; FR-001/FR-002/FR-003, SC-001/SC-003.

### Prepare, validate and approve

- [X] T005 [US1] Kiểm kê 9 rule IDs/versions, 13 positives cũ và 60 ô đề xuất bốn nhóm trong `corpus/expanded-20261013.3/coverage-catalog.md`; hoàn thiện inventory biến thể/tổ hợp, tình trạng keep/revise/add/needs-context và source refs trong `corpus/expanded-20261013.3/manifest.json`. Không dùng manifest 9-rule cũ làm G1. Depends T003/T004.
- [X] T006 [P] [US1] Lập `corpus/expanded-20261013.3/sources.json`: Japan Foundation edition/link/checksum khi có, editorial provenance/quyền dùng; legacy chưa xác minh ghi pending, không claim đã import. Depends T005.
- [X] T007 [P] [US1] Soạn `corpus/expanded-20261013.3/cases.json`: ≥1 supported mỗi ô không-exact bắt buộc, ≥2 supported+1 false-positive/unavailable mỗi rule, ca ba phương án potential/passive/honorific và hai phương án causative-passive/potential, copula/na-noun, phái sinh/bất quy tắc, Unicode, exact/missing-base/fault; expected VI/base/segments/spans/status độc lập runtime. Depends T005.
- [X] T008 [US1] Hoàn thiện manifest/coverage matrix cho 60 ô đề xuất, mọi biến thể/tổ hợp và 9 baseline: slot/rule/case/source refs resolve, quyết định required/excluded/needs-context không mâu thuẫn; proposed answers chưa là reviewed. Depends T006/T007.
- [X] T009 [US1] Kiểm schema/unique refs, 4 nhóm và advanced forms, ≥1 đúng/ô không-exact, ≥2 đúng+1 âm/rule, positive spans/segments, ambiguity negatives/privacy; ghi `evidence/expanded-corpus-validation.md`. Thiếu thì sửa T006–T008. Depends T008.
- [X] T010 [US1] Freeze `corpus/expanded-20261013.3/scope-material.json` cùng cases/source hashes, chọn version mới; ghi material ref/hash vào manifest, không self-hash. Gói 9-rule cũ vẫn giữ nguyên như unapproved draft. Depends T009.
- [X] T011 [US1] **Human G1**: trình đúng gói B/version/hash cho PO và chỉ định reviewer chuyên môn; chỉ khi có xác nhận thật mới ghi approver/date/approved state trong `corpus/expanded-20261013.3/manifest.json`. Người dùng đã duyệt nội dung bản .1 và xác nhận hai quyết định hành vi mới; .3 bổ sung câu chữ/thành phần kính ngữ nên cần xác nhận đúng đáp án snapshot cuối. PO và reviewer là người dùng đã chỉ định; một phản hồi rõ ràng có thể xác nhận scope G1 và nội dung G2 cùng gói, lưu hồ sơ riêng sau khi kiểm chứng từng gate. Depends T010.
- [X] T012 [US1] Xác minh G1 sau T011: actual expanded-file hashes, coverage, PO/date và reviewer refs; ghi checkpoint trong `.sdd/specs/feat-conjugation-explanation/evidence/g1-check.md`; đảm bảo files không đổi sau ký. Sai hash/version phải tạo revision và xin duyệt lại.

**G1 checkpoint**: 100% ô/biến thể có quyết định, 9 rules được đối chiếu, scope thật sự approved.
US2 không bắt đầu nếu T011/T012 chưa đạt. US1 chỉ hoàn tất hồ sơ phạm vi, không phải MVP production.

## Phase 4 — US2: Kiểm duyệt toàn bộ ô/quy tắc (Priority P1; T013–T020)

**Depends on**: G1(T012). **Goal**: Đáp án/nội dung được người có chuyên môn duyệt độc lập.
**Independent Test**: Kiểm hồ sơ mọi ô/quy tắc bắt buộc và 9/9 baseline, packet/hash, nguồn/quyền sử dụng và issues;
không dùng kết quả chương trình chứng minh ngôn ngữ đúng. Maps B1–B4, FR-004/FR-005, SC-002/SC-005.

- [X] T013 [US2] Chuẩn bị và freeze proposed slot/rule/answer packets từ expanded scope đã duyệt trong `.sdd/specs/feat-conjugation-explanation/reviews/review-materials.json`; kèm slot/rule IDs/versions và source refs, tạo hồ sơ draft `.sdd/specs/feat-conjugation-explanation/reviews/linguistic-review.json` theo data-model, không tự gắn accepted.
- [X] T014 [P] [US2] Yêu cầu reviewer T011 kiểm mọi ô/quy tắc bắt buộc và 9/9 baseline, kể cả mục loại/mơ hồ: base form, semantics, observed segmentation, VI tổng thể/từng phần; ghi kết luận có thật, reviewer/date/hash, issues vào `.sdd/specs/feat-conjugation-explanation/reviews/linguistic-review.json`. Depends T013; pending nếu người duyệt chưa phản hồi.
- [X] T015 [P] [US2] Đối chiếu publisher/version/checksum/license/notice/attribution hoặc editorial provenance trong `.sdd/specs/feat-conjugation-explanation/corpus/expanded-20261013.3/sources.json`; ghi bằng chứng hoặc unresolved issue, không tự khẳng định quyền dùng nguồn. Depends T013; nguồn chưa rõ chặn G2.
- [X] T016 [US2] Tổng hợp T014/T015 thành `.sdd/specs/feat-conjugation-explanation/reviews/issues.md`: accepted/changes_requested/rejected, blocking reason, correction cần làm và owner; đảm bảo mỗi baseline rule có record, không loại required rule chỉ để che lỗi.
- [X] T017 [US2] Sửa proposed materials/đáp án theo issues, tạo revision mới trong `.sdd/specs/feat-conjugation-explanation/reviews/review-materials.json` và `corpus/expanded-20261013.3/cases.json` hoặc folder revision mới; giữ frozen snapshot cũ có version trong hồ sơ. Nếu scope/answers đổi, lặp T008–T012 để tái duyệt G1; lỗi cũ thành regression case, không update runtime seed.
- [X] T018 [US2] Lập `.sdd/specs/feat-conjugation-explanation/evidence/gap-matrix.md`: already-correct/data correction/missing rule hoặc mapping/possible engine gap; phân biệt triage với gap đã được runtime kiểm chứng. Mỗi gap liên kết approved case, source/review issue; input cho C1. Depends T017 và G1 hiện hành.
- [X] T019 [US2] **Human G2**: reviewer duyệt lại material/answers đúng revision/hash, source-rights resolved và mọi ô/quy tắc required accepted; ghi hồ sơ cả 9 baseline vào `.sdd/specs/feat-conjugation-explanation/reviews/linguistic-review.json`. Chưa có reviewer signoff hoặc còn blocking issue thì dừng, không tự tick. Depends T018.
- [X] T020 [US2] Freeze review dossier/hashes, xác minh G1/G2 cùng corpus revision, 100% required và 9/9 baseline có kết luận, blocking issue=0; ghi `.sdd/specs/feat-conjugation-explanation/reviews/review-summary.md`. Đổi material/source/answer sau freeze phải mở revision/review lại, không sửa chữ ký cũ.

**G2 checkpoint**: Có chuyên môn duyệt thực tế, không phải AI/test runner tự chứng nhận.
Nếu không thiếu rule vẫn cần G2; sửa nội dung đã cài phải là version mới, không ghi đè baseline.

## Phase 5 — US3: Bổ sung có điều kiện và nghiệm thu (Priority P2; T021–T038)

**Depends on**: G1/G2 và người dùng cho phép triển khai. **Goal**: Missing-required=0,
frozen dataset/test evidence khớp hồ sơ duyệt, không rewrite runtime nếu không cần.
**Independent Test**: 100% approved cases và faults đạt, old-version replay/immutability,
API1/2 giữ nguyên, API3 alternatives/UI regressions, hashes/source/review đầy đủ. Maps C1–C6, FR-006–010, SC-003–005.

### Authorization and tests first

- [X] T021 [US3] Đối chiếu quyền triển khai đã được người dùng cấp trong hội thoại với corpus/review versions sau T020; ghi `.sdd/specs/feat-conjugation-explanation/evidence/implementation-authorization.md`. Dùng lại xác nhận đã có khi bao phủ đúng scope; không yêu cầu lặp Proceed. Quyền triển khai không là quyền publish/cài DB người dùng.
- [X] T022 [P] [US3] Mở rộng `tests/unit/ConjugationMatcherTests.cs` bằng normalized rule/token fixtures và expected đã review: head giữ nguyên base + copula, derived-base mapping có chứng cứ, full-chain, nhiều phương án hợp lệ/không bịa phương án, missingVI, gaps/overlaps/UTF-16; inputs độc lập Infrastructure, không biến actual thành expected. Depends T021.
- [X] T023 [P] [US3] Nối frozen corpus/selected dataset version vào `tests/integration/TokenizerDeinflectionIntegrationTests.cs` và fixture copy trong `tests/integration/IntegrationTests.csproj` khi cần; đủ dictionary fixtures, real Sudachi/PostgreSQL, class fixture không seed lặp. Không hardcode lại development-only cases làm bằng chứng corpus mới. Depends T021.
- [X] T024 [US3] Thêm guard tests trong `tests/integration/ConjugationEvidenceValidationTests.cs`: missing approval/source/ref, version/hash mismatch, thiếu per-rule cases, self-hash/cycle hoặc actual overwrite expected phải bị từ chối nghiệm thu; giữ validator ở test/evidence boundary, không invent production approved-state. Depends T023.
- [X] T025 [US3] Chuẩn bị candidate/expected-derived v3 UI fixtures đúng contract trong `extension/tests/unit/analysisFixtures.ts`, gồm hai và ba phương án cùng span, giữ fixtures v1/v2 và corpus identity; không nhúng quy tắc vào FE runtime. Depends T021.
- [X] T026 [P] [US3] Mở rộng `extension/tests/unit/conjugationContract.test.ts` cho v3 nhiều phương án, candidate identity, vi/base-entry/status và invalid spans/segments/version/content; giữ v1/v2 acceptance hiện có. Depends T025.
- [X] T027 [P] [US3] Mở rộng `extension/tests/browser/conjugation-popup.test.ts` cho hai/ba thẻ “có thể” để người đọc chọn, approved examples/candidate render, Retry fresh identity, stale/close; dùng popup/harness cũ. Depends T025.
- [X] T028 [US3] Chạy approved baseline cases/guard tests, đối chiếu T018; cập nhật `.sdd/specs/feat-conjugation-explanation/evidence/gap-matrix.md` và freeze quyết định needs-data-change/engine-gap/no-op, target candidate version/path tại `evidence/change-decision.json`. Depends T022–T027; report lỗi thật, không ép tests đang pass thành fail.

### Conditional data/code work

- [X] T029 [US3] Nếu cần sửa/bổ sung/đóng gói: tạo đúng file `src/infra/Conjugation/Data/<candidate-version>.json` đã resolve trong T028, giữ schema1/status development, stable rule IDs và tăng rule_version khi đổi meaning/pattern; content/provenance đúng review. Nếu byte-identical/no-gap, ghi N/A và version giữ lại trong `evidence/change-decision.json`, không sửa baseline. Depends T028.
- [X] T030 [US3] Theo approved cases, sửa `src/usecase/Conjugation/ConjugationMatcher.cs`, `src/infra/Conjugation/ConjugationKnowledge.cs` hoặc `src/infra/Adapters/GrpcDictionaryTokenizerAdapter.cs` cho copula/na/noun, derived base và tập nhiều phân tích có chứng cứ; thêm contract3 trong `src/usecase/Analysis/AnalyzeTextUseCase.cs`, `src/interface/Endpoints/AnalysisEndpoints.cs`/DTOs và `extension/src/api/analysisContract.ts`, `extension/src/ui/PopupShell.tsx`/background. Giữ v1/v2, không suffix heuristic hoặc xếp hạng nghĩa. Depends T029.
- [X] T031 [US3] Khi candidate mới cần cài, thêm reader/resource mới trong `src/infra/Conjugation/ConjugationSeedData.cs` và `src/infra/Infrastructure.csproj`, giữ constant/resource cũ không đổi; xác nhận file không bị ignore. Nếu giữ version cũ, ghi N/A bằng chứng trong `evidence/change-decision.json`. Depends T029/T030.
- [X] T032 [US3] Nếu có candidate mới, sinh migration EF forward-only `src/infra/Persistence/Migrations/<timestamp>_AddConjugationCandidate.cs` (timestamp do EF sinh) cùng designer/snapshot, chỉ INSERT candidate vào schema hiện có; không sửa migration cũ/trigger, không chạy DB người dùng. Nếu không cần migration, ghi N/A trong `evidence/change-decision.json`. Depends T031.
- [X] T033 [US3] Hoàn thiện candidate integration/replay checks trong `tests/integration/TokenizerDeinflectionIntegrationTests.cs`: source hash khớp artifact, JSONB semantic-equal, old-version còn nguyên, UPDATE/DELETE/TRUNCATE vẫn bị chặn, dataset pin không là publication. Dùng Testcontainers riêng và giữ class fixture ownership. Depends T032 hoặc N/A có bằng chứng.

### Verification and acceptance

- [X] T034 [US3] Chạy Sidecar unit suite, Release/unit/integration và existing Microsoft.CodeCoverage theo quickstart; ghi command/results/corpus coverage vào `.sdd/specs/feat-conjugation-explanation/evidence/backend-validation.md`. Assert all required cases, caller cancel/500ms timeout, unavailable/DB failure, exact-first, missing-base/VI, two/three-alternative ambiguity, Unicode/privacy, v1 empty/v2 one-or-none/v3 alternatives/unknown400; usecase bị sửa coverage≥85%. Depends T033, không reuse counts lịch sử.
- [X] T035 [US3] Chạy FE typecheck/coverage/build/Unicode và Chrome/Edge/Brave/packaged, Firefox nếu executable phù hợp đã có; ghi cases/versions/limitations trong `.sdd/specs/feat-conjugation-explanation/evidence/client-validation.md`. Không auto download/browser install; build Firefox không là parity evidence, thiếu gate nào phải ghi pending rõ. Depends T033; không chạy build .NET đồng thời T034.
- [X] T036 [US3] Tổng hợp `.sdd/specs/feat-conjugation-explanation/evidence/validation-report.md`: required/covered/excluded, positives/negatives/fault counts, missing-required=0, review/source refs, candidate/corpus/review hashes và command bằng chứng; bản no-op vẫn cần fresh validation. Depends T034/T035; không gọi scoped corpus acceptance là production-ready.
- [X] T037 [US3] Yêu cầu independent code/data review và ghi `.sdd/specs/feat-conjugation-explanation/evidence/final-review.md`: all FR/SC, correct VI/material/hash, immutable old data, no contract/async/privacy regression hoặc raw provider leakage; findings blocking phải giải quyết và test/review lại. Depends T036.
- [X] T038 [US3] **Gate G3**: sau reviewer/PO xác nhận nghiệm thu, tạo/freeze `.sdd/specs/feat-conjugation-explanation/evidence/acceptance-manifest.json` bind đúng versions/source hashes/report hash/counts/reviewer/date và state accepted_for_scoped_corpus; 100% expected đạt, required missing=0 và blocking issues=0. Không tự ký hoặc ghi production_approved; chưa đủ evidence thì giữ candidate. Depends T037.

**Conditional rule**: `<candidate-version>` lấy từ T028; `<timestamp>` lấy từ EF-generated migration,
không tự chọn target ngoài workspace. Khi đổi source/answer/review cần invalidate approval liên quan,
chạy lại gates; N/A là công việc đã đánh giá có evidence, không checkbox được tick sẵn.

## Phase 6 — Polish & Handoff (T039–T042)

**Depends on**: G3 scoped acceptance hoặc báo cáo rõ gate còn pending; không giả full completion.

- [X] T039 [P] Đồng bộ status/evidence links trong `docs/FEATURE_MAP.md` và `README.md`, giữ counts lịch sử, phân biệt corpus nghiệm thu với production activation; ghi rõ contract3 mới và v1/v2 vẫn tương thích, không thêm route mới.
- [X] T040 [P] Ghi `.sdd/specs/feat-conjugation-explanation/evidence/release-readiness.md`: candidate/pin không phải publication, dataset→KnowledgeRelease eligibility là gate/F-20 dependency riêng; không tự mở rộng ResourceRevision/schema hoặc publish vì F-09 tests pass.
- [X] T041 Đối chiếu tasks/FR/SC và human gate receipts, cập nhật `.sdd/specs/feat-conjugation-explanation/PLAN.md`, `TASKS.md`, root `PLAN.md` và `.agents/SDD.md` đúng trạng thái thực tế; không tick human approval/conditional task nếu thiếu evidence. Depends T039/T040.
- [X] T042 Kiểm tra links/diff/secret hygiene, source/data snapshots và đóng handoff trong `.sdd/specs/feat-conjugation-explanation/evidence/handoff.md`; liệt kê completed/pending/N/A và publication/browser limitations. Commit/push/PR/merge chỉ khi được yêu cầu riêng, không thực hiện mặc định. Depends T041.

## Dependencies & Execution Order

```text
Setup(T001,T002) → Foundation(T003,T004)
 → US1(T005–T012) → G1(T011,T012)
 → US2(T013–T020) → G2(T019,T020)
 → Implementation authorization(T021)
 → US3 tests/triage(T022–T028)
 → conditional candidate/code/install(T029–T033)
 → validation/review/acceptance(T034–T038) → G3
 → Handoff(T039–T042)
```

- T006/T007 phụ thuộc T005, không phụ thuộc nhau: source catalog và proposed cases chuẩn bị riêng;
  T008 mới join refs/coverage. T014/T015 sau T013; T016 join semantic/rights findings.
- T022/T023 sau T021 làm khác test files; T024 sau fixture/copy T023. T025 cũng sau T021;
  T026/T027 sau T025 không cùng sửa fixture. T028 chờ tất cả tests/preparation T022–T027.
- T029→T030→T031→T032→T033 tuần tự hoặc N/A có bằng chứng. T034/T035 không build
  cùng project/output directories; T036 chờ cả hai. T039/T040 khác file, sau T038.
- Review correction ở T017 tạo **revision mới**; lặp T008–T012/G1 với revision đó rồi mới G2,
  không thêm dependency cycle cho cùng một frozen artifact, không sửa chữ ký đã có.
- Các tasks runtime/CLI cần user implementation approval; source/linguistic/publication gate
  thiếu input thì báo và dừng đúng checkpoint, không bịa phê duyệt.

## Parallel Examples per User Story

- **US1**: Sau T005, T006(source catalog) + T007(proposed cases); T008 join và validate sau.
- **US2**: Sau T013, reviewer làm T014(meaning/VI) và người phụ trách nguồn làm T015(rights);
  T016/T019 không hoàn tất trước cả hai hoặc reviewer có thật chưa duyệt.
- **US3**: Sau T021, T022(unit) + T023(live corpus integration) có file riêng. Sau T025,
  T026(FE validator tests) + T027(browser tests) không đổi shared fixtures; builds serial theo project.
  `[P]` không tự cho phép spawn team, thêm dependency hoặc chạy qua human gates.

## Traceability

| SPEC | Tasks | Exit evidence |
|---|---|---|
| FR-001/FR-002; SC-001 | T005–T006,T008–T012 | 4 nhóm/60 ô .3 và 9 baseline có quyết định + G1 version/hash/PO/date |
| FR-003; SC-003 | T007,T009,T022–T028,T034–T036 | ≥1 positive/ô không-exact, ≥2 positive+1 negative/rule, advanced/irregular coverage |
| FR-004/FR-005; SC-002/SC-005 | T013–T020 | Independent review mọi ô/rule bắt buộc và 9 baseline, sources/rights, issues resolved/re-review |
| FR-006; SC-003 | T028–T033 | Data-first gap resolution/no-op proof, stable IDs, append-only install |
| FR-007/FR-008; SC-003/SC-004 | T022–T027,T033–T037 | Corpus results + contract/span/error/async/privacy regressions |
| FR-009; SC-005 | T004,T010,T020,T033,T036–T038 | Source hashes + semantic DB/replay + acceptance binding |
| FR-010; SC-004/SC-005 | T021,T034–T042 | Anonymous/no-log, real receipts, publication boundary/handoff |

## Implementation Strategy & Summary

**Increment đầu tiên**: Setup/foundation + US1 tạo bộ scope/đáp án đề xuất để PO duyệt;
không gọi US1 là MVP production. US2 hoàn tất review; US3 chỉ bổ sung gap thật sau quyền triển khai.
Khóa dữ liệu/evidence từng revision, kiểm independent outcomes trước qua checkpoint.

| Nhóm | Tasks | Count |
|---|---|---|
| Setup/foundation | T001–T004 | 4 |
| US1 — scope | T005–T012 | 8 |
| US2 — linguistic review | T013–T020 | 8 |
| US3 — conditional completion | T021–T038 | 18 |
| Handoff | T039–T042 | 4 |
| Total | T001–T042 | 42 |

2026-10-14: G1/G2/G3 real user receipts;42/42 tasks complete. T017 N/A(no correction requested);code/test/review/handoff and412case mapping verified. Accepted manifest binds exact candidate/report hashes;does not imply production/Firefox certification or Git/userDB writes. T022 uses separate paradigm/candidate tests;T023/T033 uses corpus integration;T031 separate candidate reader preserves historical bytes.
Kiểm tra cấu trúc corpus không phải runtime tests hay phê duyệt ngôn ngữ/production.
