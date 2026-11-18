# YUI — Nội dung popup phục vụ đọc

Ngày: 2026-11-04. Phiên bản: 0.2. Trạng thái: Draft để review, chưa triển khai. Thuộc [đặc tả tổng](../SPEC.md).

## 1. Context & Goal

Người đọc nhìn thấy từ trong ngữ cảnh, chọn đúng mục từ và xem nghĩa mà không phải cuộn qua toàn bộ kết quả. Dự án đã có React popup, các occurrence theo vị trí, JMdict Nhật–Anh, kanji, giải thích thể chia và dịch theo yêu cầu. Đợt này cải thiện cách trình bày dữ liệu hiện có, không tạo nguồn từ điển mới.

### Tham khảo và điểm tích hợp

Yomitan tại `E:/Road To N3/Yomitan`, revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`: `ext/templates-display.html`, `ext/js/display/display-generator.js`, `ext/css/display.css`. Tham khảo bố cục headword/reading/definition; viết component React phù hợp DTO dự án. Code Yomitan không phải React component có thể gắn trực tiếp.

Điểm tích hợp: [PopupShell.tsx](../../../../extension/src/ui/PopupShell.tsx), [analysisContract.ts](../../../../extension/src/api/analysisContract.ts), [TranslationPanel.tsx](../../../../extension/src/ui/TranslationPanel.tsx). Nếu lấy code thực tế, tuân thủ bảng nguồn và quyết định tái sử dụng trong SPEC tổng.

## 2. Actors & Roles

| Actor | Vai trò và quyền |
|---|---|
| Người đọc ẩn danh | Chọn từ, đọc nghĩa, mở chi tiết và chủ động yêu cầu dịch/nghe; không cần đăng nhập. |
| Content script và React UI | Nhận kết quả đã xác thực, quản lý mục hiện hành; không tự suy ra nghĩa hoặc quyền lưu dữ liệu. |
| Background và backend | Background gọi API được cấu hình; backend cung cấp canonical entry, restrictions và trạng thái từng khả năng. |

## 3. Functional Requirements

| ID | Hành vi bắt buộc |
|---|---|
| YUI-01 | WHEN có các occurrence, hệ thống SHALL hiển thị danh sách từ theo thứ tự xuất hiện; mỗi mục được định danh bằng span và các kết quả bên trong bằng canonical ID cùng cặp dạng viết/cách đọc. |
| YUI-02 | WHEN người dùng chọn một occurrence, hệ thống SHALL hiển thị chi tiết occurrence đó; lần đầu chọn mục đầu tiên có kết quả hoặc mục tiêu từ thao tác quét chuột. |
| YUI-03 | WHILE hiển thị chi tiết, hệ thống SHALL đặt surface, dạng từ điển và cách đọc trong một cụm rõ ràng; không lặp lại cùng một tiêu đề khi surface và dạng từ điển giống nhau. |
| YUI-04 | WHEN surface đã chia khác dạng gốc, hệ thống SHALL hiển thị riêng mối liên hệ giữa hai dạng; chỉ trình bày giải thích thể chia khi backend cung cấp chứng cứ tương ứng. |
| YUI-05 | THE system SHALL giữ đầy đủ nghĩa, từ loại, giới hạn dạng viết/cách đọc và nguồn/provenance đang có. Phần chi tiết được thu gọn phải mở được bằng chuột và bàn phím. |
| YUI-06 | WHILE có nhiều mục từ đồng âm hoặc nhiều phân tích hợp lệ, hệ thống SHALL cho xem từng mục; không gộp thành một nghĩa chắc chắn hoặc chọn nghĩa bằng vị trí danh sách. |
| YUI-07 | WHEN cùng một từ xuất hiện nhiều lần, hệ thống SHALL giữ mỗi vị trí riêng. Chọn từ thứ hai phải hiển thị surface và phân tích của đúng lần xuất hiện đó. |
| YUI-08 | THE system SHALL cung cấp các mục Từ vựng, Kanji, Ngữ pháp và Dịch câu; mục dịch ban đầu thu gọn, mở mục dịch không tự gửi API, chỉ nút Dịch mới gửi. |
| YUI-09 | IF capability lỗi, unavailable, completed-empty hoặc truncated, hệ thống SHALL thể hiện trạng thái tương ứng tại đúng mục; không gắn nhãn mọi dữ liệu thất bại chỉ vì một capability unavailable. |
| YUI-10 | THE system SHALL giữ giao diện Việt, nghĩa Nhật–Anh hiện có và giải thích Việt. Dữ liệu chỉ đọc không yêu cầu tài khoản. |

## 4. Non-functional Requirements

- **Tính đúng đắn:** giữ canonical ID, thứ tự occurrence, cặp viết–đọc và sense restrictions theo YUI-01/04/05/06/07; không đánh đổi dữ liệu để có bố cục gọn.
- **Khả năng truy cập:** thao tác mục/chi tiết bằng bàn phím, tên nút và lang rõ theo YUI-05/10; hành vi focus/resize kế thừa YPO.
- **Bảo mật và riêng tư:** render dữ liệu như text, không lưu raw text/history hay tự gửi tới provider; giữ ranh giới giao tiếp của SPEC tổng.
- **Hiệu năng:** kế thừa mục tiêu shell và analysis của SPEC tổng; đổi mục trong kết quả đã có không phát lại analysis. Đo lại sau thay bố cục, không lấy số đo cũ làm nghiệm thu.
- **Khả năng phục hồi:** lỗi một capability không xóa các phần đã có kết quả (YUI-09).

## 5. Data Model

| Dữ liệu | Trường/định danh chính | Vòng đời |
|---|---|---|
| Analysis result | normalized_text, dictionary_matches, vocabulary_occurrences, capabilities | Chỉ giữ cho interaction hiện hành |
| Occurrence đang chọn | span UTF-16 và index hiển thị; canonical ID trong từng match | Đổi selection thì vô hiệu |
| Chi tiết từ | writtenForms, readings, senses, restrictions, conjugations | Dữ liệu backend; không thêm schema DB |
| Trạng thái trình bày | active occurrence, phần đang mở/thu gọn | State giao diện trong phiên |

### Interface và hợp đồng liên quan

Sử dụng `AnalysisResultDto` v3 và các interface trong [contracts.md](../contracts.md); không đổi canonical ID hay tự dựng nghĩa ở client. Span occurrence tính trên `normalized_text`; span giải thích con tính trên surface occurrence. Luồng từ đơn không có occurrences vẫn hiển thị hợp lệ. Dữ liệu kanji và ngữ pháp vẫn truy cập được dù đang chọn chi tiết một từ.

Reading được hiển thị nguyên vẹn. Chỉ thêm ruby khi ánh xạ kanji–kana được xác minh; nếu chưa có ánh xạ thì hiển thị kana riêng, không ghép furigana bằng số ký tự. Nút nghe theo [YAU](ai-pronunciation.md) luôn chọn cặp mục từ/cách đọc cụ thể.

## 6. Error Handling

| Trường hợp | Phản hồi và cách phục hồi |
|---|---|
| Kết quả sai contract/correlation/span | Không render dữ liệu chưa xác thực; báo lỗi riêng và cho retry nếu hợp lệ. |
| completed nhưng không có mục từ | Hiển thị không tìm thấy; vẫn cho xem Kanji/Ngữ pháp khi có. |
| Capability failed/unavailable hoặc truncated | Giữ đúng nhãn tại phần liên quan; phần thành công vẫn đọc được. |
| Chỉ có lemma, thiếu giải thích thể chia | Hiển thị dạng gốc và nghĩa có chứng cứ; không dựng explanation. |
| Dịch hoặc speech lỗi | Giữ chi tiết từ đang đọc và trạng thái lỗi riêng; retry chỉ theo thao tác người dùng. |
| Phản hồi cũ sau đổi/đóng | Bỏ phản hồi, không mở lại popup hoặc thay mục hiện hành. |

## 7. Acceptance Criteria

| AC | Tình huống và kết quả cần quan sát | Yêu cầu |
|---|---|---|
| YUI-AC01 | `猫、猫。` có hai vị trí chọn riêng; không mất mục do trùng chữ hay key React. | YUI-01/02/07 |
| YUI-AC02 | Tra `電気代` chỉ có một tiêu đề dạng chữ; reading và nghĩa vẫn xem được. | YUI-03/05 |
| YUI-AC03 | `食べました` có surface và dạng gốc `食べる`; fixture fallback lemma không tự sinh giải thích. | YUI-04 |
| YUI-AC04 | Fixture `はし` có cầu/đũa và sense bị giới hạn; từng nghĩa giữ đúng cặp viết–đọc, không gộp theo reading. | YUI-05/06 |
| YUI-AC05 | Mở/đóng Dịch câu phát zero translation request; nhấn Dịch phát đúng một request current và không thay mục tra đang chọn. | YUI-08 |
| YUI-AC06 | Kanji completed, grammar unavailable và vocabulary truncated được phân biệt; mọi phần đã có kết quả vẫn đọc được. | YUI-09 |
| YUI-AC07 | Tab/Enter/Space đổi mục và mở chi tiết; tiêu đề, nhãn tiếng Việt và nội dung Anh được gắn ngôn ngữ phù hợp. | YUI-02/05/10 |
| YUI-AC08 | Response v1/v2 hoặc v3 từ đơn không có occurrences vẫn render được, không giả định mọi kết quả đều là câu. | YUI-01/04 |

Các AC kiểm bằng component/browser fixtures kết hợp response backend thực tế cho từ đơn, câu và Unicode. Liên hệ baseline: EXT-003/006, VOC-007/008, ASYNC-001/002, NET-006, DOM-001 trong REQUIREMENT.md; kiểm thử hiện có tiếp tục là regression gate.

## 8. Out of Scope

Không bổ sung pitch accent, tần suất từ vựng, JLPT suy đoán, dữ liệu phát âm hay nút Lưu chưa có backend. Thu gọn UI không được làm mất nghĩa/restriction. Bố cục cuối cần kiểm tra trên màn hình hẹp và trang thật cùng [YPO](popup-behavior.md); không coi ảnh chụp mẫu là bằng chứng đã chạy.
