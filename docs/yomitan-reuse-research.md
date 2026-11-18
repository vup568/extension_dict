# Khảo sát tái sử dụng Yomitan

Tài liệu ghi nhận khảo sát cho 5 nhóm: giao diện kết quả, quét bằng chuột, xử lý thể chia, phát âm, hành vi popup. Ngày 2026-11-04, người dùng xác nhận ưu tiên tham khảo/tự viết, chỉ lấy code khi cần và ghi giấy phép; giữ backend/PostgreSQL tập trung. Người dùng chọn giọng AI Gemini cho phát âm. Bộ tài liệu tiếp nối: [kế hoạch và đặc tả](yomitan-reuse-plan.md).

| Nhóm | Source Yomitan tại `E:/Road To N3/Yomitan` | Source dự án | Nhận xét |
|---|---|---|---|
| Kết quả tra | `ext/templates-display.html`, `ext/js/display/display-generator.js`, `ext/css/display.css` | `extension/src/ui/PopupShell.tsx` | Yomitan dùng template và kiểu dữ liệu riêng; cần đối chiếu trước khi tích hợp. |
| Quét chuột | `ext/js/language/text-scanner.js`, `ext/js/dom/text-source-generator.js` | `extension/src/browser/popup/startSelectionPopup.ts` | Dự án hiện kích hoạt theo selection; quét vị trí con trỏ là luồng mới cần quản lý quyền riêng tư và tải API. |
| Thể chia | `ext/js/language/ja/japanese-transforms.js`, `ext/js/language/language-transformer.js` | `src/usecase/Dictionary/SentenceVocabularyUseCase.cs`, `.sdd/specs/feat-conjugation-explanation/SPEC.md` | Đã có tokenizer và corpus được chấp nhận trong phạm vi riêng; cần đối chiếu bằng corpus có đáp án, không suy luận phủ 100%. |
| Phát âm | `ext/js/display/display-audio.js`, `ext/js/media/audio-system.js` | Chưa thấy khả năng phát âm trong `extension/src` | Mã phát âm không đồng nghĩa có quyền sử dụng mọi nguồn âm thanh; cần xác định nguồn, điều khoản, tính khả dụng và cách kiểm thử. |
| Hành vi popup | `ext/js/app/popup.js`, `ext/js/display/display-resizer.js`, `ext/css/popup-outer.css` | `extension/src/browser/popup/createPopupView.tsx` | Dự án đã dùng Shadow DOM, định vị và điều khiển focus; cần bổ sung theo khoảng trống được xác minh. |

Yomitan khai báo GPL-3.0-or-later trong `package.json`; khả năng dùng code trực tiếp khi phân phối và điều kiện dữ liệu/âm thanh phải được đánh giá cụ thể. Đây là khảo sát, không xác nhận các nghĩa vụ pháp lý cho toàn hệ thống. Không sao chép code hay dữ liệu trong bước khảo sát này.

Đặc tả đặt tại [005-yomitan-reading-experience](../.sdd/specs/005-yomitan-reading-experience/SPEC.md). Yomitan revision khảo sát `d3c54c10264673eca0d66b970ef74cd5c8b048e3`, checkout sạch khi kiểm tra. Tài liệu này ghi nhận source và không thay thế đặc tả được duyệt.
