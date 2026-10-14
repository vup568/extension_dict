# Data Model: F-09 — Corpus, review và acceptance

**Status 2026-10-14**: .3 approved G1/G2; candidate implemented, human G3 pending. Gói 9 quy tắc cũ vẫn là historical draft | [PLAN](./PLAN.md)
Không thêm Domain entity/bảng. Runtime vẫn `ConjugationDataset(Version, Content JSONB)`;
dưới đây là hồ sơ versioned trong folder F-09 cho G1/G2/G3.

## 1. CorpusManifest — `corpus/expanded-20261013.3/manifest.json` (corpus .3 đã tạo; hồ sơ duyệt/acceptance tiếp tục theo gate)

| Trường | Quy định |
|---|---|
| schema_version, corpus_version | Schema hồ sơ1; version mới khi đổi scope/answers |
| baseline_dataset_version | development-20261013.1, không mutate baseline |
| cases_file, cases_sha256 | Path tương đối feature; SHA source bytes lowercase64hex |
| slots[] | slot_id, word_group(verb/i_adjective/na_adjective/noun_copula), form/variant, decision(required/excluded), rationale, case_ids, source_refs, proposed rule identity |
| rules[] | rule_id/version, proposal_status, slot_ids, positive_case_ids/negative_case_ids; một slot có thể có nhiều rule cho biến thể/tổ hợp |
| approval | state(draft/approved), approver, approved_at UTC, scope_material_ref/hash |

Validation: slot/rule IDs duy nhất, 60 ô đề xuất trong [catalog](./corpus/expanded-20261013.3/coverage-catalog.md)
đều có quyết định và baseline9 được đối chiếu. Mọi ô không-exact bắt buộc ≥1 ca đúng;
mỗi quy tắc thực thi ≥2 supported +1 unavailable/false-positive; ca phân biệt cho
biến thể mơ hồ. Có đủ bốn nhóm, bất quy tắc và các thể nâng cao đã chọn; refs tồn tại.
Approved thiếu PO/date/material hash không hợp lệ. Hash approval bind frozen scope packet
riêng, không checksum toàn manifest rồi lưu checksum vào chính nó.

## 2. CorpusCase — `corpus/expanded-20261013.3/cases.json` (tương lai)

- case_id; slot_ids[], tags và input.text; rule identities nằm trong từng expected alternative. Ca âm có thể hỗ trợ một rule của nhóm khác, đồng thời ghi forbidden_rule_ids cho rule không được nhận nhầm.
- input.text là ca tổng hợp công khai, không dùng selection riêng tư của người dùng.
- dictionary_fixture: canonical ID, form/reading, required/missing/exact intent.
- expected: outcome(supported/unavailable/completed_empty/exact_first/failed/discarded/cancelled), một hoặc nhiều alternatives;
  mỗi alternative có surface/base/meaning/vi, rule identity và half-open UTF-16
  span/ordered segments cho supported.
- source refs/justification, reviewer-approved material ref, groups normal/irregular/
  unicode/negative/error. Fault/cancel cases có failure expectation riêng, không lemma giả.

Supported có base-entry fixture cho từng alternative; segments continuous/non-overlap
và concat đúng normalized input riêng mỗi alternative, VI nonblank. Alternatives cùng
span/surface không được dedupe theo surface; rule identity phải khác và thứ tự ổn định,
không biểu thị xác suất. Ca âm toàn từ không có conjugation; ca âm cho một rule có thể có alternative hợp lệ của rule khác. Cases không lệ thuộc raw Sudachi POS;
actual output không ghi đè expected. Đổi frozen answer cần corpus revision và duyệt lại.

## 3. LinguisticReview — `reviews/linguistic-review.json` (tương lai)

review_version/corpus_version, reviewed_material_ref/hash; từng record có rule_id/version,
reviewer/date UTC, source/editorial refs, checks(base/semantics/segmentation/Vietnamese/
source-rights), conclusion(accepted/changes_requested/rejected), issues và resolution refs.

Review packet proposed rules/answers đã freeze, không dựa chỉ vào runtime output.
Nguồn ngoài ghi publisher/version/checksum/license/notice/attribution khi áp dụng;
project-authored ghi author/editorial provenance và review, không giả license nguồn.
G2 cần hồ sơ mọi ô/quy tắc bắt buộc và 9/9 baseline, required accepted và blocking issues=0. Rejected/removal có lý do;
đổi required scope phải PO duyệt lại G1.

## 4. Runtime candidate — giữ schema hiện có

`version`, `schema_version=1`, `provenance.status=development`, `sources[]`,
`provider_mappings.categories/inflections`. Baseline giữ `rules[]` và token patterns.
Candidate dùng optional `paradigms[]`: stable rule_id/version, canonical_suffix,
required_base/excluded_bases, dictionary_pos, ordered components(surface_template,
explanation_vi), explanation_vi, head_base_templates/head_categories chuẩn hóa.
Exactly one leading `{stem}` placeholder; reviewed full-word templates derive a proposed
base, then canonical repository verifies POS and joint form/reading sense applicability.
Only verified alternatives reach public DTOs; runtime never imports corpus/test fixtures.
Row metadata/hash versioned separately; compiler reproduces candidate semantically.
Optional corpus/review refs vào provenance chỉ tham chiếu artifacts đã freeze;
parser hiện không xác minh approval bằng extra fields, không dùng như authorization.

Candidate version khác version đã cài nếu bytes thay đổi; không đổi constant/resource cũ.
Rule ID stable; sửa meaning/pattern tăng rule_version; nội dung không đổi có thể giữ version.
Hash candidate ở hồ sơ ngoài JSON, không checksum self-reference. Sau JSONB roundtrip,
DB phải semantic-equal source, không bắt buộc serialization bytes giống source file.

## 5. AcceptanceManifest — `evidence/acceptance-manifest.json` (tương lai)

candidate version/source_file_sha256, corpus/review versions+hashes, sources/notice refs,
validation_report_sha256, baseline/required/excluded counts, positive/negative counts,
missing_required_count, checker/reviewer/date và state(candidate/accepted_for_scoped_corpus).

accepted_for_scoped_corpus cần G1/G2/G3 cùng version/hash; không phải KnowledgeRelease
published/current hay production activation. Không gắn production_approved khi publication
gate/integration chưa có. Runtime data có thể giữ version cũ nếu unchanged và reviewer chấp thuận.

## State và bất biến

Scope draft → G1 approved → G2 reviewed → candidate frozen → verified → G3 accepted.
Mỗi đổi tạo revision mới, invalidate approval/evidence bị ảnh hưởng; không sửa snapshot
đã freeze. Candidate install chỉ INSERT; old-version replay được. Publication có quyền/gate
riêng ngoài local validation, không thêm release membership vào schema trong plan này.

## Hồ sơ chuẩn bị .2

408 ca/59 ô, 17 dictionary fixtures tổng hợp và 107 rule IDs đề xuất. 9 rule baseline được đối chiếu ID/version 1; text/component mới đề xuất version 2. Expected components là phân đoạn biên tập, có thể gộp/tách token provider nhưng phải tái tạo đúng bề mặt/spans. `evidence/expanded-sudachi-observations-20261013.2.json` giữ raw provider metadata riêng. `reviews/user-review-receipt-20261013.json` ghi đúng lời duyệt .1; .2 vẫn pending_final_review.

## Revision .3 theo quyết định mới

60 ô/412 ca/115 rule IDs; thêm V21 với8 trạng thái kính ngữ, bổ sung20 ca và4 ca loại nhận nhầm. Hai cách hiểu sai khiến được giữ theo lời xác nhận. JSON cases vẫn schema1, viết mỗi ca một dòng, runtime không đọc corpus. 369 đầu vào gRPC có artifact riêng .3; hồ sơ lời trả lời `reviews/scope-decisions-20261013.json` xác nhận hành vi, chưa chứng nhận đáp án snapshot. .1/.2 và source hashes cũ giữ nguyên.
