# Nguồn tham khảo và baseline triển khai

Ngày 08/11/2026. [SPEC 005 v0.2](../../../.sdd/specs/005-yomitan-reading-experience/SPEC.md) và [hợp đồng](../../../.sdd/specs/005-yomitan-reading-experience/contracts.md) là baseline. Triển khai dùng các mặc định DD01–DD08 trong bản dự thảo, gồm Shift và 300 ms, cửa sổ 16/48 UTF‑16, resize 380×480 px, pin theo phiên và deadline speech 25/28/30/32 giây. Model và voice cần cấu hình rõ ràng trước khi gọi Gemini. Phần corpus giữ gate duyệt ngôn ngữ độc lập.

## Mã và dữ liệu dự án

- Giữ selection ruby-safe F02, controller F03/F04, hợp đồng analysis v1–v3, tra câu và dịch theo yêu cầu. Scan dùng lại `AnalyzeTextUseCase`, còn speech tái sử dụng truy vấn canonical và kiểm tra applicability của từ điển.
- Nguồn F09 đã nghiệm thu được kiểm hash và dùng làm baseline trong [bảng đối chiếu corpus](conjugation-gap-catalog.md).
- Playback dùng Web Audio với WAV đã xác thực. Thử nghiệm extension đóng gói cho thấy CSP `media-src 'none'` của trang chặn HTML audio dùng URL Blob. Web Audio giải mã từ RAM và giải phóng buffer/context theo vòng đời popup.

## Nguồn bên ngoài

Revision Yomitan tham khảo `d3c54c10264673eca0d66b970ef74cd5c8b048e3` lấy theo [khảo sát đã lưu](../../../docs/yomitan-reuse-research.md). Checkout tham khảo nằm ngoài thư mục dự án và lần kiểm tra hiện tại không đọc được Git tại đó. Danh mục file/hành vi tham khảo đã ghi trong năm spec con và [bản đồ tái sử dụng](../../../docs/yomitan-reuse-plan.md). Đợt này tự viết mã theo DTO và kiến trúc hiện có, không tích hợp code, rule, test, asset hoặc dữ liệu của Yomitan. Các nguồn corpus độc lập còn phải được ghi theo phiên bản khi duyệt T16/T17.

Gemini TTS dùng Interactions API một lượt, `store: false`, model/voice server allowlist và WAV/PCM mono 24 kHz. Mã adapter được viết theo tài liệu chính thức. [Gemini speech generation](https://ai.google.dev/gemini-api/docs/speech-generation) · [Interactions API](https://ai.google.dev/api/interactions-api) · [Interactions overview](https://ai.google.dev/gemini-api/docs/interactions-overview)
