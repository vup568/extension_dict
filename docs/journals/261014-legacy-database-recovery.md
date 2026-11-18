---
date: 2026-10-14
session: legacy-database-recovery
---

# Journal: 2026-10-14 — Legacy Database Recovery

## Context

Backend dừng khi EF tạo lại `dictionary_entries`: schema local cũ đã có 17 bảng nhưng lịch sử migration rỗng.
Người dùng duyệt sửa giữ dữ liệu, sao lưu và thử bản sao trước bằng “Proceed”; không duyệt reset volume hay Git delivery.

## What Happened

- Đối chiếu catalog với DB reference tạo bằng InitialCreate; không lấy số bảng hoặc DBML làm bằng chứng tương đương.
- Backup riêng được restore sang clone; rehearsal đạt, giữ 17 fingerprints dữ liệu và nâng lịch sử lên đủ 4 migrations.
- CLI phục hồi explicit; startup thông thường không tự baseline. Chỉ ghi InitialCreate sau kiểm tra/normalization trong transaction.
- Giữ serial và sequence high-water, unique constraints, index COALESCE mạnh hơn, cùng function/triggers cập nhật thời gian đã xác minh.
- Bổ sung index FK còn thiếu, đổi tên constraint tương đương và đồng bộ 5 cột JSONB nullable; migrations/dataset F09 cũ không sửa.
- Từ chối schema lạ, rewrite rules, inheritance cả hai chiều qua schema khác, function namespace giả và unique constraint deferred.
- Lỗi phục hồi chỉ báo thông tin an toàn/mã kiểm tra số; backup, credentials và raw selected text không đưa vào Git/log.

## Reflection

Testcontainers DB mới không phát hiện lịch sử EF lệch với DB local đã tạo thủ công.
Schema tương thích phải được chứng minh ở mức constraint/index/default/sequence, không chỉ tạo đủ bảng hay trả HTTP 200.

## Decisions Made

| Decision | Rationale | Impact |
|---|---|---|
| Backup → clone → kiểm chứng → Apply | Ưu tiên giữ dữ liệu | Không baseline mù hoặc reset DB |
| Giữ tương thích legacy đã khai báo | Tránh thay ID/sequence hoặc giảm uniqueness | Tương thích có kiểm soát, không giả vờ byte-exact |
| Phục hồi explicit/fail closed | Schema không rõ nguồn không được tự sửa | Transaction rollback khi ngoài allowlist |

## Verification and Next Steps

- Trước lần hồi quy cuối: 33 test mục tiêu đạt; build 0 warnings/errors, 79 unit và 251 frontend/typecheck đạt mới.
- Lần integration sau có 164 ca: 58 đạt, 106 lỗi vì Docker Engine dừng; không coi đó là pass hoặc bỏ qua failures.
- Đã yêu cầu chạy Docker Desktop trở lại; cần rerun integration và nghiệm thu Apply trên DB gốc. Tại snapshot này Apply chưa thực hiện.
- Sau Apply phải kiểm tra fingerprints, startup lần hai và API; dictionary rỗng vẫn cần import canonical riêng, không suy từ HTTP 200.

## Completion Update

- Docker được bật lại;Apply DB gốc đạt,17 fingerprints/counts giữ nguyên,history4/grammar5/dataset2/dictionary0. Backup được giữ trong ignored data/local-recovery;không reset/import.
- Lượt full test chạy song song recovery có3 lỗi F09;quiet rerun164/164 đạt không đổi code/deadline500ms. Đây là rủi ro timing/resource contention,không suy latency certification.
- Backend script khởi động2lần đều DB up-to-date;health200,analysis contracts1/2/3 200,inputtrống400. Đã dừng đúng các process test của agent;người dùng tự chạy start-local.ps1.
- 79units/251FE/typecheck/33target/build đạt;review PASS local-only. Không commit/push/merge;import dictionary còn bước riêng được user hoãn.
