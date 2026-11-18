# Operator Import Contract — đề xuất, chưa triển khai

Không phải HTTP API mới. Giữ public analysis schemas1/2/3 và F09 semantics hiện có; không thêm version4. Paths có secret không được ghi vào report công khai.

## Commands và inputs

CLI dự kiến: `dotnet run --project src/interface/WebApi.csproj -- --dictionary-import <verb> ...`.

| Verb | Inputs bắt buộc | Hành vi |
| --- | --- | --- |
| inspect | `--source-lock <file>` | Read-only pins/schema/license/available-files preflight, không fetch/DB writes. |
| import | `--source-lock`, `--run-id`, `--confirm-target <exact-db-name>` | Nạp target riêng đã prepare/ownership guard và chưa phục vụ; không tự tạo/reset DB hoặc tải source. |
| resume | như import | Cùng pins/policy/mapping/schema/checkpoint mới tiếp tục; không trộn run. |
| validate | `--run-id`, `--source-lock` | Read candidate, tạo report/hash; không đổi canonical facts. |

Connection qua environment `ConnectionStrings__DefaultConnection`, không CLI string; wrapper giữ secrets trong child process environment và phục hồi finally. `--help`/unknown command không khởi động HTTP hoặc migration; help nêu mặc định an toàn.

Prepare wrapper tương lai tách fetch/download rõ khỏi DB creation: G0a cho tải đúng artifacts để audit; G0b xác minh pins/license/corpus sau khi tải. P3 hoàn thiện mapping/hash offline, rồi P4 mới full import theo final lock/target user đã duyệt. Không có destructive/reset switch. Import canonical bị chặn khi source chưa được clearance; source investigation ở artifacts không biến thành canonical import.

## Required guards

- Đọc actual target identity; allow loopback PostgreSQL và target riêng explicit user-confirmed, không original/demo/system hoặc DB đang được backend phục vụ.
- Chỉ prepare DB mới do workflow tạo; existing unknown/mixed data fail closed; ownership marker + source/control state + schema/content checks đều phải khớp.
- Schema/source hashes/size bounds/license approval được verify trước write; không network/external entities khi parse; per-DB writer lock. Cancel rollback current batch, giữ completed checkpoint, không mark ready.
- Run completed cùng pins là validated no-op; changed source/mapping/facts/state thì conflict, tạo target run mới. Target sealed không import/resume.
- Không raw selected text, connection string, password, dump binary trong stdout/stderr/report; source reject details ở restricted local artifacts, notices không bị loại khỏi gói dữ liệu được phân phối.

## Report contract (schema_version=1)

Required fields: `run_id`, `target_identity` (không credential), `source_lock_sha256`, `transformation_id`, `mapping_sha256`, `state`, `sources[]`, `canonical_counts`, `facts_sha256`, `validation_checks[]`, `metrics`, `blockers[]`.

Mỗi source: source/version/schema/license/approval refs và SHA256; `input_count`, `accepted_count`, `quarantined_count`, `rejected_count`; tổng ba nhóm bằng input_count, counts kết quả normalizer/gloss riêng. Rejected/quarantine report có source identity+reason; mapping approval/rejected không tự suy từ exit code.

Metrics: elapsed time, peak memory, input+expanded bytes, table/index/total DB bytes và free-space preflight. Summary không được gọi full-ready khi VI/license/coverage gate chưa đạt. Trạng thái `validated` không là user approval.

Exit codes dự kiến: `0` command thành công (inspect/validate có thể báo **not ready** trong state, không activation); `2` args/config/unsafe target; `3` source/hash/schema/license; `4` import conflict/DB/write error; `5` validation/coverage failed; `130` cancelled. `validate` có blocker thì exit5, không0. Error có `error_code`, `message`, `run_id` khi có, không stack/secret mặc định.

## Local launch/approval boundary

`scripts/start-real-dictionary.ps1` là tên **dự kiến**, chỉ nhận ready receipt user đã duyệt bound vào run/target/lock/facts/validation hash. Recheck receipt/content trước switch; giữ candidate không ghi và không có concurrent import. Launch với connection tạm, IPv4 Sidecar và existing F09 pin; không chỉnh `.env`/reset/drop/rename/production publish. Ctrl+C rồi launcher cũ là rollback.

## API/presentation correctness

- Exact/base lookup chỉ trả applicable pairs/senses/readings. Nguồn Việt thật chỉ phục vụ khi approved và mapping được duyệt đúng sense; null/unknown/pending/rejected không được xem là approved. Demo status giữ synthetic/local-only riêng.
- `Reading.IsNoKanji` chiếu qua mảng `ReadingDto.Info` sẵn có bằng marker `reading:no-kanji`, popup label “chỉ dùng kana”. Written-form match loại reading này; kana-only match không trả association với written form. Không encode no-kanji thành RestrictedToForms=[] rồi coi unrestricted. Giữ raw tags tách marker; integration/UI tests trên contracts1/2/3.
- Contract1 vẫn không conjugations; contract2 không tự chọn một interpretation khi mơ hồ; contract3 giữ các alternatives phù hợp.
- Attribution/source-license links phải truy cập được từ popup (không chỉ startup banner); nội dung notice lấy từ source lock/approved provenance, không claim nguồn Việt đã clear.
