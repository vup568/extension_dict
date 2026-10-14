# Contract: F-09 — Giao tiếp và bằng chứng nghiệm thu

**Status**: Draft contract 3 bổ sung, giữ contract 1/2 | [SPEC](../SPEC.md), [PLAN](../PLAN.md)

## Public contract — version mới tương thích ngược

- `GET /api/dictionary/lookup?q=...`: exact-first, deinflect chỉ khi miss; giữ canonical
  matches/restrictions/VI-first và optional conjugation Detail hiện có.
- `POST /api/analysis`: absent header hoặc `X-Contract-Version: 1` giữ conjugations=[]/
  capability unavailable; header2 giữ tối đa một typed explanation như hiện tại.
  Header3 mới trả danh sách phương án có thể; unknown version structured400.
- Giữ interaction_id echo, request_id và structured errors; không expose raw POS/provider protocol.
- Morphology unavailable/tokens=[]; không sentence vocabulary segmentation/URL version mới.
- Cùng surface có nhiều nghĩa hợp lệ (ví dụ `食べられる`) không tự gán một nghĩa;
  contract3 trả mọi phương án đã có chứng cứ (khả năng/bị động/kính ngữ).
  `食べさせられる` có riêng hai giải thích sai khiến-bị động/sai khiến-khả năng theo lựa chọn người dùng. Contract2 không đổi cardinality cũ:
  nếu đa nghĩa, không trả một phương án tùy tiện mà báo unavailable.

## Typed occurrence v2/v3

surface, base_form, explanation, language_tag=vi, span(start/end/unit=utf16),
rule_id, rule_version>0, dataset_version, segments(surface/explanation/span).
Span half-open relative trimmed normalized_text; segments continuous/non-overlap và
concat đúng observed text **cho từng phương án**. Mỗi phương án gắn Dictionary match
có base form thực; client không đoán hay tái phân tích. Contract3 giữ shape occurrence
hiện có nhưng `conjugations` có thể chứa >1 phần tử cùng surface/span, phân biệt bằng
rule_id/version và nghĩa. Thứ tự ổn định theo rule ID, không hàm ý xác suất/ưu tiên;
UI có nhãn “Các cách hiểu có thể”, mỗi thẻ riêng để người đọc chọn.

| Trường hợp | Conjugations | Conjugation capability |
|---|---|---|
| Supported một nghĩa + có base entry/VI | 1 typed occurrence | completed |
| Contract3: nhiều nghĩa có chứng cứ | N typed occurrences, mỗi phương án đầy đủ | completed |
| Contract2: nhiều nghĩa có chứng cứ | [] — không chọn một nghĩa tùy tiện | unavailable |
| Supported thiếu base entry | [] | completed |
| Unsupported/không đủ chứng cứ/missing dataset/VI/provider timeout | [] | unavailable |
| Exact hit | [] | unavailable (tokenizer skipped) |
| Internal/repository error ở vocabulary | [] | failed trong analysis partial isolation |
| Request cancel | Không đổi thành success partial | Cancellation propagates, không render stale |

Contract1 luôn unavailable/[] như cũ; không đồng nhất completed-empty với unavailable.
Contract3 không được trả một tập phương án chưa đủ nguồn/identity chỉ để đạt coverage.

## Editorial contract — không phải API mới

Producer: nhóm triển khai; approvers: PO(scope), reviewer chuyên môn(ngôn ngữ).
Hồ sơ ở [data-model](../data-model.md); actual so với expected đã freeze, không tạo oracle.
G1 scope bốn nhóm/60 ô đề xuất trong corpus .3 + cases hashes và PO/date; G2 review toàn bộ ô/quy tắc
bắt buộc, gồm 9 baseline, cùng source/rights/answers; G3 candidate/report hashes
và all cases pass. Thiếu/đổi version/hash hoặc blocking issue: không nghiệm thu, không giảm
corpus để bỏ ca lỗi. Không thêm admin endpoint, review table/UI trong scope này.

## Publication boundary và verification

Install/config pin để test không phải publication; candidate runtime status development.
Review state trong hồ sơ không authorization. Production cần gate bind version/hash tới
release hợp lệ; không nới parser status/ResourceRevision enum đơn lẻ để tự coi approved.

Replay v1/v2/v3/unknown400, found/missing base/exact, unavailable/failure/cancel;
UTF-16 supplementary/segment bounds; VI/canonical provenance/candidate identity;
FE malformed shape rejection; popup render/Retry/stale/close và background header3.
Test contract2 vẫn từ chối >1 occurrence, contract3 chấp nhận các phương án cùng span
nhưng từ chối trùng rule identity, lệch surface/span/base-entry hoặc phương án thiếu VI.
Corpus mới cần evidence mới; Firefox build không thay runtime verification.
