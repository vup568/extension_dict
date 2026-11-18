# Rà soát bộ đặc tả trải nghiệm đọc

Ngày 2026-11-04. Phạm vi: chất lượng tài liệu; chưa triển khai hoặc nghiệm thu tính năng runtime.

## Kết quả

- Đã có đặc tả tổng, 5 spec con, hợp đồng, kế hoạch và 28 công việc triển khai; mục lục đặt trong `docs/yomitan-reuse-plan.md`.
- 5 spec con có 50 yêu cầu và 44 ca nghiệm thu, mỗi ca ánh xạ tới requirement. Các gate chung còn có trong SPEC tổng.
- Đã kiểm liên kết relative của bộ tài liệu, không có liên kết hỏng. Script `.claude/scripts/validate-docs.cjs` mà skill tham chiếu không có trên repo, nên kiểm đường dẫn Markdown bằng PowerShell read-only.
- Hai reviewer đã đối chiếu source Yomitan và baseline dự án. Đã sửa xung đột Shift/pin/pending scan, yêu cầu target_span trùng occurrence và phân biệt PCM phía provider với WAV phía client.
- Đã kiểm tên model ứng viên TTS qua tài liệu model chính thức, chưa gọi TTS live bằng khóa người dùng.

## Truy vết quyết định

| Nội dung | Cơ sở | Nơi thể hiện |
|---|---|---|
| Cả 5 nhóm cùng một đợt | Người dùng chọn trong trao đổi | SPEC phạm vi, PLAN P0–P6, TASKS T01–T28 |
| Tham khảo/tự viết, copy khi cần có giấy phép | Người dùng xác nhận | PLAN chiến lược; danh mục source ở mỗi spec |
| Backend/PostgreSQL tập trung | Người dùng xác nhận và baseline kiến trúc | SPEC G02, contracts scan/speech |
| Giọng AI Gemini | Người dùng đề nghị khi làm rõ phát âm | YAU, contracts speech, TASKS T20–T24 |
| Modifier, limits, resize, deadlines/model | Thiết kế đề xuất dựa khảo sát | SPEC DD01–DD08; cần review khi chốt triển khai |

## Giới hạn kiểm chứng

### Cập nhật cấu trúc 8 mục — phiên bản 0.2

- Spec tổng và cả 5 spec con có đúng thứ tự: Context & Goal, Actors & Roles, Functional Requirements, Non-functional Requirements, Data Model, Error Handling, Acceptance Criteria và Out of Scope.
- Đã đối chiếu trước/sau: giữ nguyên toàn bộ 50 hàng yêu cầu, 44 hàng AC, 9 ràng buộc G01–G09 và 7 gate nghiệm thu tổng. Các phần thêm làm rõ vai trò, dữ liệu/vòng đời và xử lý lỗi; không mở rộng phạm vi tính năng.
- File `.sdd/specs/_template.md` trước đây trống, nay có cùng cấu trúc để dùng cho spec mới; mục không áp dụng cần giải thích ngắn, không tự tạo schema hoặc quyền không cần thiết.
- Reviewer xác nhận cấu trúc/phạm vi và chỉ ra chỗ wording cần tách cleanup resize khi blur khỏi hủy audio/requests; đã sửa theo hành vi baseline.

Không chạy build/unit/browser trong bước tài liệu này vì không thay runtime. Không lấy kết quả test của phiên bản cũ làm bằng chứng cho chức năng được đề xuất. Hiệu năng, quota TTS, quyền key, chất lượng nghe và đủ browser phải đo trong triển khai theo PLAN; chưa có bằng chứng thì không đánh dấu milestone hoàn thành.
