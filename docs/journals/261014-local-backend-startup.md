---
date: 2026-10-14
session: local-backend-startup-fix
---

# Journal: 2026-10-14 — Local Backend Startup

## Context

Người dùng tự thử Extension sau nghiệm thu F09; popup báo lỗi phân tích.
Hướng dẫn bốn lệnh trước đó thiếu cấu hình PostgreSQL cho tiến trình .NET.
Người dùng đã duyệt Shadow Plan bằng “Proceed”.

## What Happened

- Thiếu `ConnectionStrings__DefaultConnection` khiến đăng ký `AppDbContext` bị bỏ qua.
- Startup cũ nuốt lỗi khởi tạo: `/health/live` vẫn 200 nhưng `/api/analysis` trả 500.
- DI nay từ chối cấu hình thiếu/rỗng; giữ EF context transient cho các capability chạy song song.
- `DatabaseStartup.InitializeAsync` hoàn tất migration hiện có trước khi mở HTTP; thất bại dừng backend với exit code 1.
- Log startup chỉ chứa loại lỗi và tên cấu hình, không raw exception hay connection string.
- Helper PowerShell đọc literal `.env` có allowlist, quote an toàn, ưu tiên connection string đã đặt và khôi phục biến môi trường khi kết thúc.
- Khi người dùng tự chạy helper, Compose đợi `db`/`tokenizer` với `--no-build --wait`; backend áp dụng migration hiện có.

## Reflection

Test với fixture có đủ cấu hình không thay thế kiểm tra bootstrap thật bằng hướng dẫn local.
Một liveness 200 không chứng minh API dùng database hoạt động, nhất là khi startup bỏ qua lỗi.
Hướng dẫn khởi chạy phải đưa đủ cấu hình và nói rõ migration, không yêu cầu người dùng in secrets để chẩn đoán.

## Decisions Made

| Decision | Rationale | Impact |
|----------|-----------|--------|
| Fail trước khi listen | Không phục vụ API với DB chưa khởi tạo | Lỗi sớm, có chỉ dẫn cấu hình |
| `.env` literal, không evaluate | Giữ secrets ngoài code và ngăn thực thi nội dung cấu hình | Helper kiểm tra an toàn, không echo credentials |
| Giữ snapshot F09 nghiệm thu | Sửa bootstrap không đổi tri thức ngôn ngữ | Corpus/dataset và hồ sơ hash-bound được bảo toàn |

## Verification and Next Steps

- Đã báo cáo đạt: 17 test mục tiêu, 79 backend unit và 251 frontend unit.
- Hồi quy cuối: 135/135 integration đạt, không failure/skip; Release build không warning/error.
- Review PASS_WITH_RISK cho Windows; người dùng không yêu cầu sửa test PowerShell cho Ubuntu, nên CI Linux còn rủi ro chưa giải quyết.
- Không restart backend, migrate DB người dùng, cài dependency hoặc commit/merge trong bước này.
- Hướng dẫn người dùng dừng backend cũ rồi tự chạy helper và thử lại popup; chưa khẳng định process cũ đã hết lỗi.
