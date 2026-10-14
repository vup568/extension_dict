---
date: 2026-10-13
session: tokenizer-deinflection-and-initial-f09
---

# Journal: 2026-10-13 — Tokenizer deinflection và F-09 ban đầu

## Context

Tiếp tục đúng thứ tự đã được giao: commit tài liệu OD-010 (`c0cb500`), kiểm chứng Sidecar Docker hiện có, nối Dictionary deinflection F-06, rồi thêm giải thích tiếng Việt F-09 ban đầu. Branch: `feat/tokenizer-deinflection`; phạm vi hoàn tất là code và kiểm thử local, không phải push/merge/deploy hay phê duyệt corpus production.

## What Happened

- Python/Sudachi gRPC được pin phiên bản, khóa tokenizer dùng chung, trả span UTF-16 và lỗi unavailable trung thực; logs không chứa selected text hoặc chi tiết exception. Compose thêm profile `nlp`, giữ nguyên PostgreSQL/volume của người dùng.
- F-06 giữ exact-first; chỉ nhận toàn bộ chuỗi biến đổi khớp duy nhất, không đoán từ động từ đầu tiên. Timeout 500ms tách khỏi caller cancellation; lỗi repository không bị nuốt thành lỗi provider.
- PostgreSQL lưu 9 rules phát triển và nội dung tiếng Việt trong JSONB versioned, immutable. UPDATE/DELETE/TRUNCATE bị chặn; muốn sửa nội dung phải thêm version mới. Dataset `development-20261013.1` chỉ bật khi pin rõ cấu hình.
- Unified Analysis giữ default v1 với conjugations rỗng/unavailable; header v2 opt-in trả DTO tiếng Việt, base form, segments UTF-16 và dataset identity. Extension yêu cầu v2, kiểm tra contract và render bằng React text; morphology vẫn unavailable.
- Kiểm thử pass: Python **12**, .NET unit **58**, integration **83** (live provider/API **22**, adapter cô lập **10**, baseline **51**), FE unit **236**, browser **121** (Chrome/Edge/Brave **40** mỗi loại, packaged MV3 **1**). Release build không warnings/errors; review không còn P1/P2.
- Existing transitive Code Coverage collector đo line coverage: Dictionary **98%**, Analysis **87.30%**, matcher **93.33%**; không cài dependency mới. FE TypeScript đạt **96.74%** statements; không gán số này cho popup `.tsx`, vốn được kiểm chứng qua browser.

## Reflection

Lỗi integration ban đầu thuộc test harness: xUnit tạo instance từng test nhưng DB được chia sẻ, khiến seed lặp; literal JSON braces trong EF `ExecuteSqlRaw` bị hiểu là format placeholders. Chuyển seed/provider vào class fixture và escape braces, sau đó chạy lại toàn bộ suite; không bỏ qua test thất bại. Footnote popup cũng được sửa để không báo conjugation unavailable khi v2 đã có kết quả.

## Decisions Made

| Quyết định | Lý do / tác động |
|---|---|
| Mapping, rules và giải thích là dữ liệu versioned | Giữ engine độc lập provider; thay nội dung bằng version mới, không hardcode tri thức |
| Development dataset opt-in, không production signoff | Chứng minh luồng end-to-end mà không giả nhận corpus đã được duyệt |
| v1 default, v2 opt-in | Bảo toàn hành vi client cũ; mở rộng F-09 có contract rõ ràng |
| Chỉ giải thích kết quả có dictionary entry | Liên kết base form canonical; không hiển thị suy đoán thiếu nguồn |

## Next Steps

- Corpus đầy đủ và linguistic signoff/KnowledgeRelease, sentence segmentation, morphology tổng quát, Firefox runtime và OD-005 performance là các gate riêng.
- Rate-limit/504/control-character/error-path của F-06 vẫn chưa được chứng nhận trong slice này; chưa có bằng chứng remote CI hoặc feature merge.
- Bằng chứng và lệnh tái chạy: [validation-report](../../.sdd/specs/feat-tokenizer-deinflection/evidence/validation-report.md). Project memory: [SDD](../../.agents/SDD.md).
