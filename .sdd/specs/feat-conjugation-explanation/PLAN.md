---
title: "F-09 — Corpus và kiểm duyệt giải thích chia từ"
status: completed
priority: P1
branch: feat/tokenizer-deinflection
created: 2026-10-13
---

# Implementation Plan: F-09 — Conjugation Explanation

**Branch**: `feat/tokenizer-deinflection` | **Date**: 2026-10-13 | **Spec**: [SPEC.md](./SPEC.md)
**Input**: Chốt danh mục bốn nhóm gồm các thể cơ bản/nâng cao → kiểm duyệt → triển khai gap → nghiệm thu.
**Trạng thái 2026-10-14**: G1/G2/G3 được người dùng duyệt;42/42 tasks complete trong phạm vi corpus .3. [Acceptance manifest](./evidence/acceptance-manifest.json) bind candidate/report/hash và [biên nhận G3](./reviews/user-acceptance-receipt-20261014.json). Corpus60 ô/412 ca/115 rules,code/tests/review đạt;giữ .1/.2/.3 và báo cáo trước nghiệm thu nguyên vẹn. Firefox runtime/production publication riêng;không commit/merge hoặc migrate DB người dùng.

**Quyết định mới (2026-10-13)**: Người dùng chọn “Bao gồm cả cách dùng kính ngữ” của
`れる/られる` và hiển thị/giải thích cả hai nghĩa của `食べさせられる`.
Đã tạo revision .3, giữ .2 và biên nhận .1 nguyên vẹn; không còn câu hỏi hành vi này pending.
Không dùng counts/hash hoặc kết quả kiểm tra .2 để chứng nhận corpus mới.

## Summary

Kế thừa Sidecar, versioned data và API/popup đã có, nhưng mở rộng F-09 từ baseline 9
quy tắc sang [danh mục bốn nhóm, 60 ô đề xuất](./corpus/expanded-20261013.3/coverage-catalog.md).
Khóa danh mục/đáp án, review độc lập, rồi viết tests và sửa data/engine theo gap có thật.
Giữ exact-first, không đoán nghĩa mơ hồ: contract3 hiển thị mọi phân tích có chứng cứ
như phương án để người đọc chọn. Contract1/2 không đổi; không tách cả câu hay xây F-20.
Gói 9 quy tắc `f09-nine-rules-20261013.1` là draft bị thay thế, không phải G1 approval.

## Technical Context

| Hạng mục | Quyết định kế thừa |
|---|---|
| Language/Version | .NET10/C#14, React19/TypeScript, Python3.11 Sidecar |
| Primary Dependencies | EF/Npgsql/gRPC/System.Text.Json, xUnit/NSubstitute, Vitest/Playwright đã khóa; không thêm package |
| Storage | PostgreSQL16: immutable ConjugationDataset(Version, Content JSONB); corpus/review hồ sơ trong folder F-09 |
| Testing | Unit ≥85% coverage usecase bị sửa; PostgreSQL/Testcontainers + Sudachi thật; contract/browser regressions |
| Target Platform | Backend Linux Docker; Extension Chrome/Edge/Brave/Firefox |
| Project Type | Shared backend + Extension, Clean Architecture |
| Performance Goals | Giữ adapter500ms/client5s; không tự tuyên bố đạt OD-005/PERF |
| Constraints | Exact-first, half-open UTF-16, không log selected text, API1/2 compatible; contract3 nhiều phương án, immutable old versions |
| Scale/Scope | Review tất cả ô bắt buộc của 4 nhóm; 60 ô trong catalog .3, ≥1 ca đúng/ô không-exact và ≥2 đúng+1 âm/rule thực thi; 9 baseline vẫn được kiểm |

## Constitution Check

Gate thiết kế trước/sau nghiên cứu: **PASS**, không ngoại lệ. Không đồng nghĩa corpus/release approved.

| Nguyên tắc | Biện pháp |
|---|---|
| §2–3 authoritative/boundaries | Engine normalized hiện có; knowledge là data, không đẩy Sudachi labels ra client |
| §4 correctness/identity | Expected đáp án được review độc lập; stable rule/dictionary IDs, đúng span/segments; hiển thị cả potential/passive có chứng cứ, không gán bừa noun/na identity |
| §5 provenance/immutability | Source/license/editorial review + frozen hashes; version mới insert-only, không sửa migration/seed cũ |
| §6–7 async/privacy | Retry/stale/cancel/timeout regressions; anonymous, không thêm history/raw-content log |
| §8 provider isolation | Reuse provider/adapters; legacy là seed tham khảo, không copy engine |
| §9–11 approval/testing | Human G1/G2, acceptance G3, traceability, independent review trước nghiệm thu |

**Production gate pending**: parser hiện chỉ nhận provenance.status=development; dataset chưa
liên kết KnowledgeRelease. Không đổi thành approved để vượt gate; [research](./research.md)
ghi dependency publication riêng. Không mở rộng ResourceRevision/schema nền tảng trong scope này.

## Project Structure

### Documentation (this feature)

Hiện tạo: PLAN.md, [research](./research.md), [data-model](./data-model.md),
[contract](./contracts/conjugation-contract.md), [quickstart](./quickstart.md) và
[catalog mới](./corpus/expanded-20261013.3/coverage-catalog.md).
Artifacts corpus chuẩn bị đã có dưới folder này; hồ sơ duyệt/acceptance còn tiếp tục:

- `corpus/expanded-20261013.3/{manifest,cases,sources,scope-material}.json`: scope mới và expected độc lập; bản .1/.2 giữ nguyên cùng biên nhận duyệt nội dung .1, còn gói 9-rule trực tiếp dưới `corpus/` vẫn là draft chưa duyệt.
- `reviews/linguistic-review.json`: review mọi ô/quy tắc bắt buộc, gồm 9/9 baseline; nguồn/quyền sử dụng, issue resolutions.
- `evidence/acceptance-manifest.json`, `evidence/validation-report.md`: candidate/checksums và evidence.
- [TASKS.md](./TASKS.md): đã sinh ở bước speckit-tasks sau khi lập PLAN; các task triển khai vẫn pending.

### Source Code (repository root)

| File/nhóm file | Hành động tương lai |
|---|---|
| src/infra/Conjugation/Data/development-20261013.1.json | Đọc/review, không sửa resource đã dùng trong migration |
| src/infra/Conjugation/Data/<candidate-version>.json | Tạo mới khi sửa/bổ sung hoặc đóng gói candidate |
| src/infra/Conjugation/ConjugationSeedData.cs; src/infra/Infrastructure.csproj | Add reader/resource mới khi cài candidate, không đổi constant/resource cũ |
| src/infra/Persistence/Migrations/ | Migration EF mới insert candidate; giữ schema/trigger/migration cũ |
| src/usecase/Conjugation/ConjugationMatcher.cs; models; src/infra/Conjugation/ConjugationKnowledge.cs; GrpcDictionaryTokenizerAdapter.cs | Tests trước; mở rộng theo approved slots. Hiện matcher chặn head giữ nguyên base, provider có derived base/ambiguity; engine gap là khả năng cao |
| tests/unit/ConjugationMatcherTests.cs | Bổ sung cases từ đáp án duyệt độc lập |
| tests/integration/DictionaryTokenizerAdapterTests.cs; TokenizerDeinflectionIntegrationTests.cs | Candidate/corpus, old-version replay, failures, immutability |
| src/usecase/Analysis/AnalyzeTextUseCase.cs; src/interface/Endpoints/AnalysisEndpoints.cs; analysis DTOs | Thêm contract3 chọn theo header, giữ v1/v2 byte-level semantics; nhiều phương án cùng span |
| extension/src/api/analysisContract.ts; extension/src/ui/PopupShell.tsx; background handler | Contract3 validator và thẻ “các cách hiểu có thể”; không xếp hạng nghĩa |
| extension/tests/unit/conjugationContract.test.ts; extension/tests/browser/conjugation-popup.test.ts | Candidate identity/content, v1/v2 regressions và v3 alternatives |
| Domain entity/repository/DI, Sidecar/proto | Giữ nguyên mặc định, không viết lại; sửa chỉ khi approved case chứng minh lỗi |

Không thêm project/table review/public endpoint. Editorial artifacts trong feature; runtime
knowledge ở Infra/data; core normalized matcher ở Application.

## A — Chốt corpus MVP (US1; FR-001–003)

**Phụ trách**: nhóm triển khai chuẩn bị; PO duyệt scope/chỉ định reviewer.

1. **A1 Kiểm kê**: 9 rule IDs/versions,13 ca live, 60 ô catalog .3 và nguồn;
   đối chiếu REQUIREMENT/MIGRATION/Japan Foundation. Legacy không có trong checkout thì ghi rõ.
2. **A2 Danh mục**: quyết định cho từng ô bốn nhóm, tất cả biến thể và tổ hợp đã nêu;
   giữ/sửa/bổ sung, required/excluded/needs-context, lý do và source refs. Te, potential,
   passive, causative, causative-passive, conditional thuộc scope B của người dùng;
   kính ngữ `れる/られる` được bổ sung theo trả lời 2026-10-13. Revision mới cần ca
   ba nghĩa cho `食べられる`, ca bị động/kính ngữ của godan/する và ca loại trừ
   được nguồn xác nhận; không suy ra kính ngữ chỉ từ hậu tố hoặc mở rộng toàn bộ keigo.
3. **A3 Đáp án**: ≥1 ca đúng mỗi ô không-exact, ≥2 đúng+1 âm mỗi quy tắc, thêm ca
   đa phương án potential/passive, phân biệt noun/na, bất quy tắc, Unicode/fault. Ghi dictionary fixtures,
   expected base/meaning/segments/spans/status; output runtime không là oracle.
4. **A4 Khóa scope**: corpus version mới/SHA-256, PO/ngày duyệt và reviewer là người dùng. G1/G2 có thể được xác nhận cùng một phản hồi cho gói đầy đủ; hệ thống vẫn kiểm và lưu từng gate riêng.

**G1**: 100% ô catalog/biến thể có quyết định,9 rules đều đối chiếu, PO duyệt đúng gói
version/hash. Gói cũ không đáp ứng G1; chưa bổ sung code trước khi G1/G2 đạt.

## B — Kiểm duyệt (US2; FR-004–005)

**Phụ thuộc**: G1; reviewer có chuyên môn, độc lập với kết quả chương trình.

1. **B1 Review tất cả ô/quy tắc**: base/meaning/observed segmentation, VI tổng thể và
   từng phần, nguồn/license/attribution; gồm 9 baseline và mọi mục dự kiến loại.
2. **B2 Hồ sơ**: accepted/changes_requested/rejected, reviewer/date, version/hash tài liệu
   reviewed, issues và đề xuất sửa. Nguồn/quyền sử dụng unresolved chặn acceptance.
3. **B3 Sửa và duyệt lại**: correction thành revision mới và bug regression.
   Đổi expected/scope phải cập nhật corpus và G1, không giảm scope để che ca lỗi.
4. **B4 Gap matrix**: đã đúng/cần sửa data/thiếu rule hoặc mapping/engine gap;
   khóa expected answers và source dossier trước viết tests bổ sung.

**G2**: 100% ô/quy tắc có kết luận; required items có nội dung/đáp án/source hợp lệ accepted,
blocking issue=0. AI/test runner không tự ký thay reviewer.

## C — Bổ sung có điều kiện và nghiệm thu (US3; FR-006–010)

**Phụ thuộc**: G1+G2 và người dùng đồng ý triển khai; không thực hiện ở lượt lập PLAN.

1. **C1 Tests trước**: nạp corpus đã duyệt vào tests; chạy baseline xác định gap từng ô.
2. **C2 Nếu có gap**: JSON candidate version mới, IDs ổn định, tăng rule_version khi
   đổi meaning/pattern; VI/mappings là data. Matcher/adapter được sửa đúng ca approved
   khi cần nhận copula head giữ nguyên, resolve derived base hoặc liệt kê các phân tích
   có chứng cứ. Không thêm suffix heuristics hay leak raw provider POS. Contract3 trả
   nhiều phương án độc lập cùng span, gắn nhãn “có thể”; v2 giữ one-or-none, v1 giữ empty.
   Không đủ chứng cứ cho một phương án thì không tạo phương án giả.
3. **C3 Nếu không gap**: report no-op, không thêm rule/viết lại engine; có thể giữ
   runtime version cũ nếu byte-identical và review chấp thuận, hoặc đóng gói bản candidate mới.
4. **C4 Cài candidate mới**: freeze bytes/hash + compare approved rules; additive EF migration
   và resource mới. Không UPDATE/DELETE/TRUNCATE; thử Testcontainers, không mutate DB người dùng.
5. **C5 Verification**: [quickstart](./quickstart.md); all corpus+full regressions, ≥85%
   coverage usecase bị sửa, old-version replay, exact/alternatives/VI/base/spans,
   cancel/privacy/API1/2/3/UI.
   JSONB đối chiếu semantic JSON, hash source file theo bytes, không hash raw DB serialization.
6. **C6 Acceptance**: independent review, freeze acceptance manifest bind candidate/corpus/review/report
   versions+hashes và counts. SC-001–005 đạt mới nghiệm thu scoped corpus.
   Production activation có gate phát hành riêng; local install/pin không phải publication.

**G3**: 100% expected cases đạt; đủ1 đúng/ô không-exact và2 đúng+1 âm/rule; missing-required=0; replay/immutability/
regression sạch; nguồn/review/hashes khớp. No-op vẫn cần G1/G2/G3, không dùng test lịch sử thay thế.

## Verification & Traceability

| SPEC | Phần thực hiện | Bằng chứng |
|---|---|---|
| FR-001/FR-002; SC-001 | A1/A2/A4 | 4 nhóm/60 ô .3 được quyết định,9 baseline, approved version/hash/PO/date |
| FR-003; SC-003 | A3/B3/C1/C5 | Per-slot1 positive; per-rule2 positive+1 negative; 4 nhóm/advanced/irregular coverage |
| FR-004/FR-005; SC-002/SC-005 | B1–B4 | Review100% required slots/rules, independent answers, source/rights, resolved issues |
| FR-006; SC-003 | C2/C3 | Missing-required=0 hoặc no-op report, stable IDs |
| FR-007/FR-008; SC-003/SC-004 | C1/C5 | Positives/negatives/faults, UTF-16, exact/missingVI/base/ambiguity |
| FR-009; SC-005 | C4/C6 | Frozen source hash, semantic DB equality, acceptance manifest |
| FR-010; SC-004/005 | C5/C6 | Anonymous/no-log/async regressions, publication boundary |

Kế thừa class fixture chung để seed không lặp. Ca lỗi/ambiguous/Unicode/gap và concurrency
không được skip; Firefox missing executable ghi riêng, build manifest không là runtime evidence.

## Risks & Dependencies

- G1/G2/G3 đã có human receipts đúng .3/candidate/report;G3 là lời duyệt thật,không suy từ Proceed.
- Legacy/source chưa có hoặc license unresolved: record rõ, không tự nghiệm thu.
- JSONB reserialization và hash cycles: xử lý theo research/data-model, giữ immutable snapshots.
- Head không đổi (`静か/学生`), derived base (`書ける`), khác biệt Sudachi POS/segmentation
  và bề mặt đa nghĩa cần engine/test và contract3/UI thay đổi; không hứa data-only.
- Reviewed dossier không đổi runtime status development thành production authorization.
- Publication eligibility/dataset→release integration thiếu: phụ thuộc F-20/gate riêng,
  không tự thêm bảng/resource enum ngoài SPEC.
- Dirty working tree: bảo toàn changes; không checkout/reset/merge để lập PLAN.
- Không quyết định số rule bổ sung trước scope freeze; không dự đoán thời gian human review.

## Planning Deliverables

Thiết kế theo B,kính ngữ/hai nghĩa sai khiến;G1/G2/G3 approved. Scoped F09 complete,42/42;không production authorization hoặc Firefox certification. Hồ sơ/frozen materials và hash-bound validation report giữ nguyên;acceptance manifest ghi sự kiện duyệt sau đó.

## Issues Encountered — 2026-10-14

- Sandbox Vitest mất file tạm: chạy lại ngoài sandbox, không cài dependency; suite đạt.
- Docker Engine chưa chạy: người dùng bật lại, tiếp tục Testcontainers riêng.
- Sense applicability dùng ID sets độc lập: regression red trước sửa; chuyển sang cặp form/reading hợp lệ, units đạt.
- Test upgrade dùng DB class fixture rồi rollback qua forward-only migration: mỗi ca sở hữu DB sạch riêng, giữ migration lịch sử.
- Scout phát hiện generic godan nhận v5r-i chưa được review: loại POS khỏi regular rows/candidate, thêm live negative; không bịa mẫu bất quy tắc mới.
- Corpus harness đọc span ở alternative thay vì expected: sửa schema path của harness, không đổi corpus.
- HTTP→client parser harness: Node strip-only không hỗ trợ TS parameter properties; dùng flag transform-types có sẵn, không đổi runtime chỉ để phục vụ test.
- Independent review P1: dedupe trước canonical POS làm mất godan-r passive/honorific (`帰られる`). Regression red→green; giữ class variants đến khi kiểm chứng dictionary rồi mới dedupe public identity, không union POS/segments khác nhau.
