# Đợt code đầu trải nghiệm đọc theo Yomitan

Ngày 08/11/2026. Feature 005 có popup theo từng lần xuất hiện của từ, ghim/resize, scan Shift opt-in, và đường phát âm Gemini qua `/api/speech`. Scan dùng pipeline từ điển hiện có. Speech kiểm đúng mục từ và cặp dạng viết–cách đọc ở server.

Kiểm tra extension đóng gói trên Edge phát hiện CSP của trang chặn media Blob. Playback chuyển sang Web Audio và phát lại từ RAM. Các thao tác quét, dịch và nghe kiểm tra sự kiện trình duyệt để sự kiện giả từ trang không kích hoạt gửi request.

Checklist đạt 14/28. Việc tiếp nối gồm kiểm chứng PostgreSQL/Sudachi thật, probe và nghe review Gemini, duyệt nguồn/đáp án của gap catalog rồi mới mở rộng F09. Xem [báo cáo kiểm chứng](../../plans/261104-yomitan-reading-experience/evidence/test-report.md).
