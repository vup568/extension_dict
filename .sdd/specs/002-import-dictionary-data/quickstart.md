# Quickstart kiểm chứng — sau khi triển khai

**Hiện tại chưa có importer/start-real-dictionary script.** Không chạy tên CLI dự kiến trong [contract](contracts/operator-import.md) như lệnh đã tồn tại. Bước tài liệu này không download hay import gì.

## Prerequisites và thứ tự

1. User duyệt SPEC/PLAN, tạo TASKS, Proceed triển khai; G0a duyệt nguồn dự kiến/quyền download để audit, không phải quyền canonical import.
2. Windows + .NET10, Docker Engine running, PostgreSQL16/Sudachi hiện có. PowerShell7: dùng executable cài sẵn; `pwsh` không có PATH thì gọi absolute path, không tự cài package.
3. Sau G0a: tải/cache exact files vào ignored `data/dictionary-import/<run-id>/`; verify checksums/schema/license, duyệt corpus để qua G0b. Hoàn tất implementation tests nhỏ P1–P2 và offline mapping review P3, khóa mapping hash cuối vào source lock. Không copy full data vào extension/tests/Git.
4. Chỉ khi source/license/mappings đã duyệt và P0–P3 xong, user xác nhận quyền full import/tên **DB riêng mới** P4. Prepare theo contract; lưu inventory/count/content fingerprints DB gốc/demo và checksum `.env` qua kiểm tra không in secrets.
5. Inspect → full import → validate trên unserved target; resume/no-op checks cùng pins. Bootstrap/verify F09 datasets bằng migrations nguyên bản và5 grammar rules seed hiện có trước seal/read-only launch; không lazy seed trên request đầu. Giữ normal HTTP backend của user không trỏ vào target loading. OD-009 chưa clear thì không full canonical import/activation VI và không gọi phạm vi full hoàn tất.

## Commands có sẵn để baseline/regression

Từ `E:\jp-dict-extension` (đã có dependencies, không restore/install ngoài quyền):

```powershell
dotnet build JPReadingPlatform.sln --no-restore -c Release
dotnet test tests/unit/UnitTests.csproj --no-restore -c Release
dotnet test tests/integration/IntegrationTests.csproj --no-restore -c Release
npm --prefix extension run typecheck
npm --prefix extension test
npm --prefix extension run build
```

Tên solution/projects/npm scripts trên đã đối chiếu checkout; full integration cần Docker. Lệnh importer sau triển khai theo [operator contract](contracts/operator-import.md), không dán connection string lên terminal/report.

## Acceptance scenarios bắt buộc

| Ca | Expected / cách lấy đáp án độc lập |
| --- | --- |
| Từ ngoài demo: 裁判官 và ít nhất9 mục khác trong nguồn pin | Đúng JMdict identity/reading/POS/applicable gloss, không fixture ID; nghĩa Việt chỉ khi reviewed mapping có trong nguồn, không hardcode hứa trước bản dịch. |
| Restricted written/reading/sense + no-kanji reading | Cả positive và negative pairs đúng source, API và F09 không nới restriction. |
| VI homograph, thiếu reading/sense, pending/rejected | Không gắn nghĩa sai; accepted/quarantined/rejected có report, fallback en trung thực. |
| 学/食 và supplementary kanji nguồn có hỗ trợ | Đúng scalar/reading/stroke/radical, Hán Việt tách nghĩa Việt; JLPT derived có label. Ký tự không có nguồn trả missing, không thêm fixture giả. |
| 食べました; 食べられる; 静かでした; 学生だった | Base đúng canonical/POS; F09 VI và ambiguity theo rules hiện có, exact-first không bị mất. |
| Cancel/crash giữa batch, chạy lại, file hỏng, source khác, hai writers | Không duplicate/partial publication; resume cùng pins; sai input/target bị từ chối; no secret logs. |
| Hai clean rebuild cùng pins | Cùng IDs/facts SHA256, counts đối soát; thời gian/timestamps/surrogate keys không làm khác facts hash. |

Thống kê ít nhất30 ca trong corpus user duyệt; đáp án trích nguồn+review không được lấy output importer làm oracle. Giữ nguyên corpus F09 đã nghiệm thu; real-data corpus bổ sung riêng.

## Chrome và rollback

- Chỉ sau validation/receipt user duyệt: dừng backend cũ, chạy launcher real-data trên target riêng; mở `chrome://extensions`, reload extension unpacked và reload trang tiếng Nhật.
- Bôi chọn các ca đã duyệt; chụp kết quả vocabulary, kanji và F09; kiểm tra nguồn/giấy phép, retry/stale, network payload tối thiểu. Ghi version/hashes/Chrome và actual response đã redacted; Brave automation không thay Chrome manual evidence.
- Ctrl+C rồi chạy launcher cũ (`start-local.ps1` hoặc `start-demo.ps1`) để quay lại; không đổi `.env` hay xóa DB. Đối chiếu fingerprints gốc/demo trước-sau.

## Done gate

Full source counts+identity/restriction integrity, VI disposition/coverage duyệt, license clear, tests green, real Chrome acceptance, metrics thực đo và rollback đạt. HTTP200 hoặc report pass không tự là nghiệm thu. Production F20, hosting và Git delivery vẫn ngoài phạm vi.
