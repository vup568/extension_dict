# Research: F-09 — Quyết định thiết kế

**Date**: 2026-10-13 | [SPEC](./SPEC.md), [PLAN](./PLAN.md)
Nghiên cứu code/tài liệu checkout; không cài package, không thực hiện review ngôn ngữ.
Phạm vi B đã chọn; catalog 59 ô là đề xuất US1, không giả định đã được duyệt.

## 1. Reuse runtime, bổ sung theo gap

- **Decision**: Giữ stack/provider, schema1 và phần không liên quan của API/popup;
  data-first, code sửa theo approved cases. Contract3/UI alternatives là thay đổi đã được
  người dùng chọn, còn contract1/2 giữ nguyên.
- **Rationale**: Whole-chain, UTF-16, unavailable, v1/v2 đã có evidence; giới hạn
  unique-match hiện tại phải mở cho v3 khi nhiều phân tích đều có chứng cứ.
- **Alternatives considered**: Viết lại matcher/UI, đưa linguistic constants lên FE:
  loại do overlap và trái authoritative/versioned knowledge.
- **Evidence**: `src/usecase/Conjugation/ConjugationMatcher.cs`, `src/infra/Conjugation/`,
  [báo cáo lịch sử](../feat-tokenizer-deinflection/evidence/validation-report.md).

## 2. Scope/review/acceptance là ba hồ sơ khác nhau

- **Decision**: PO duyệt corpus version/hash; reviewer có chuyên môn duyệt meaning/answers;
  acceptance bind candidate với đúng corpus/review và fresh validation report.
- **Rationale**: 9 rules/13 live positives không tự đủ ≥2 positive+1 negative từng rule.
  Runtime output chỉ là actual, không dùng làm expected oracle. Gói 9 quy tắc cũ
  chưa được phê duyệt và không thể làm scope packet cho lựa chọn B.
- **Alternatives considered**: Auto-approved vì tests pass hoặc AI tự ký: loại vì vòng
  lặp kiểm chứng và không tuân human gates.
- **Evidence**: SPEC FR-001–005; JSON ghi production_review=pending. MIGRATION §5.6/6.8
  coi mô tả Việt/legacy là seeds; không tìm thấy legacy implementation trong checkout này.
  Nguồn/version legacy chưa có phải ghi rõ, không claim đã import/review.

## 3. Reviewed candidate không tự thành production

- **Decision**: Giữ runtime `provenance.status=development`; hồ sơ editorial dùng state riêng.
  Có thể thêm evidence refs vào provenance nhưng chúng không phải authorization.
- **Rationale**: `ConjugationKnowledge.Parse` chỉ chấp nhận development và chưa kiểm
  reviewer/license/checksum/production_review. Config pin chọn version, không duyệt version.
  Dataset chưa liên kết KnowledgeRelease; ResourceRevision chỉ có dictionary/kanji/grammar.
- **Alternatives considered**: Đổi status=approved, nới parser status hoặc resource-type
  CHECK đơn lẻ: không bảo đảm eligibility, vượt scope F-09 review/data.
- **Production prerequisite**: Cần publication binding version/hash/nguồn/review với
  release hợp lệ. Nếu chưa có, phụ thuộc F-20 và xin thiết kế/phê duyệt riêng; các publication
  tests cũ không chứng nhận candidate conjugation. Local acceptance không tuyên bố published/current.
- **Evidence**: `ConjugationKnowledge.cs`, `GrpcDictionaryTokenizerAdapter.cs`,
  `DependencyInjection.cs`, schema/migrations và SPEC ngoài scope pipeline toàn nền tảng.

## 4. Cài version mới, không sửa lịch sử

- **Decision**: Candidate mới dùng JSON/resource/reader và EF migration mới insert-only
  vào bảng hiện có. Giữ development seed/resource/migration cũ; không thêm bảng review.
- **Rationale**: `20261013020721_AddConjugationDatasets.cs` dùng embedded seed và triggers
  chặn UPDATE/DELETE/TRUNCATE; sửa seed khiến replay migration cũ thay đổi dữ liệu.
- **Alternatives considered**: UPDATE old row hoặc thay migration cũ bị loại; installer/
  CLI mới chưa cần vì EF migration dùng dependency hiện có.
- **Evidence**: Migration trên, `ConjugationSeedData.cs`, `Infrastructure.csproj`,
  live immutability/new-version test trong `TokenizerDeinflectionIntegrationTests.cs`.

## 5. Hash và JSONB

- **Decision**: SHA-256 chính xác UTF-8 bytes source artifact; lowercase64hex trong hồ sơ.
  Check DB JSONB bằng semantic JSON equality, không checksum raw DB serialization.
- **Rationale**: JSONB đổi whitespace/key order dù semantic bằng nhau. Không self-hash;
  không candidate↔report checksum cycle.
- **Alternatives considered**: Self-hash, raw DB hash hoặc thêm canonical-JSON library:
  không cần cho workflow source-frozen + semantic DB check này.
- **Freeze order**: Scope material/cases → approval manifest → proposed rule/answer materials →
  review records → candidate → validation report → acceptance manifest. Approval không chứa
  hash toàn chính artifact; candidate không tham chiếu report tương lai.

## 6. Khả thi kỹ thuật của phạm vi B

- **Nguồn ngữ pháp**: Dùng [Japan Foundation Irodori Grammar Notes](https://www.irodori.jpf.go.jp/assets/data/Grammar_all.pdf)
  và [bảng plain/polite bốn nhóm](https://www.irodori.jpf.go.jp/assets/data/pre-intermediate/pdf/ZZ_L09.pdf)
  làm tài liệu đối chiếu, không sao chép câu giải thích. Reviewer xác nhận từng ca/biến thể.
- **Quan sát Sidecar**: `静かだ` và `学生です` có head giữ nguyên base, rồi token `だ/です`;
  matcher hiện loại trường hợp này trước khi so rule. `書ける` được Sudachi xem là base
  `書ける`, nên không thể suy `書く` bằng cùng đường xử lý 9 rule. `食べられる` có thể
  mang nghĩa khả năng/bị động. Cần tests trước rồi sửa matcher/adapter theo chứng cứ
  và dữ liệu được duyệt; không dùng suffix heuristic.
- **Ranh giới**: Một surface mơ hồ không thể gán nghĩa duy nhất bằng word-unit hiện tại;
  người dùng chọn hiển thị mọi phân tích có chứng cứ như phương án trong contract3.
  Contract2 tiếp tục unavailable thay vì chọn bừa một nghĩa. Các thể
  kết hợp (bị động/sai khiến + quá khứ/phủ định/lịch sự) cần inventory ca riêng.
- **Evidence**: [catalog](./corpus/expanded-20261013.1/coverage-catalog.md),
  [scope change](./evidence/scope-change.md), `ConjugationMatcher.cs`,
  `GrpcDictionaryTokenizerAdapter.cs` và quan sát Docker Sidecar 2026-10-13.

## Kết luận

Đã chốt hành vi đa phương án ở mức sản phẩm; cấu trúc contract3 là thiết kế draft cần
được kiểm chứng bằng tests và review khi triển khai. Constitution pre/post-design PASS,
không ngoại lệ.
G1 scope, G2 linguistic signoff, G3 acceptance và production publication là các gate tương lai.
Không source/license hoặc corpus cụ thể nào được auto-approved trong lượt lập PLAN.
