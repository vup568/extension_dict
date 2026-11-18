# YSC — Tra từ bằng vị trí con trỏ

Ngày: 2026-11-04. Phiên bản: 0.2. Trạng thái: Draft để review, chưa triển khai. Thuộc [đặc tả tổng](../SPEC.md).

## 1. Context & Goal

Người dùng có thể bật quét từ, giữ Shift và dừng con trỏ trên chữ để tra. Luồng bôi đen hiện tại tiếp tục dùng đúng vùng chọn. Hover là capability mới có lựa chọn bật riêng, vì [F-02](../../001-ruby-safe-dom-extraction/SPEC.md) chỉ cho phép trích DOM trong active selection. Ngoại lệ đọc context giới hạn dưới đây chỉ áp dụng cho hover sau khi người dùng bật chức năng.

### Tham khảo và điểm tích hợp

Yomitan revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`: `ext/js/language/text-scanner.js`, `ext/js/dom/text-source-generator.js`, `ext/js/dom/dom-text-scanner.js`, `ext/js/dom/text-source-range.js`. Tham khảo modifier, dwell, giới hạn nguồn text và hủy timer; viết adapter DOM tương thích kiến trúc dự án.

Các điểm hiện có: [startSelectionPopup.ts](../../../../extension/src/browser/popup/startSelectionPopup.ts), [extractRubySafeSelection.ts](../../../../extension/src/browser/selection/extractRubySafeSelection.ts), [analysisController.ts](../../../../extension/src/interaction/analysisController.ts), [SentenceVocabularyUseCase.cs](../../../../src/usecase/Dictionary/SentenceVocabularyUseCase.cs). Client không tải tokenizer hoặc từ điển canonical.

## 2. Actors & Roles

| Actor | Vai trò và quyền |
|---|---|
| Người đọc ẩn danh | Bật/tắt quét; Shift và dwell là ý định tra, selection chủ động vẫn ưu tiên. |
| Content script trong frame được phép | Chỉ đọc buffer DOM có giới hạn tại điểm hợp lệ; không truy cập input/password/editable hoặc nội dung ngoài quyền. |
| Background | Xác thực extension/tab/frame/interaction, gọi endpoint cố định và hủy theo namespace. |
| Backend | Kiểm payload/offset và chọn occurrence từ tokenizer/từ điển tập trung. |

## 3. Functional Requirements

| ID | Hành vi bắt buộc |
|---|---|
| YSC-01 | THE system SHALL mặc định tắt quét con trỏ và giải thích rằng khi bật, context nhỏ quanh con trỏ có thể được gửi tới backend sau thao tác Shift. Chỉ lưu preference, không lưu nội dung. |
| YSC-02 | WHEN quét được bật, người dùng giữ Shift và con trỏ ổn định 300 ms trên text Nhật hợp lệ, hệ thống SHALL tạo interaction quét; di chuột bình thường không gửi request. |
| YSC-03 | THE system SHALL giới hạn buffer trong cùng khối DOM ở tối đa 64 đơn vị UTF-16, gồm tối đa 16 đơn vị trước điểm và 48 từ điểm trở đi; ranh giới surrogate phải nguyên vẹn. |
| YSC-04 | WHILE trích buffer, hệ thống SHALL bỏ `rt/rp`, giữ base text và ánh xạ điểm DOM sang offset UTF-16 sau trim. Không vượt block hoặc tự đọc toàn câu/trang. |
| YSC-05 | IF điểm thuộc input, textarea, password, editable hoặc popup của extension, hệ thống SHALL không đọc text và không gửi scan request. DOM không truy cập được phải trả không hỗ trợ một cách an toàn. |
| YSC-06 | WHEN backend trả kết quả, hệ thống SHALL chỉ chọn occurrence có `start <= offset < end`; có thể bao gồm đầu từ nằm trước con trỏ. Không đoán từ ở client hoặc chỉ tra một kanji khi đã có cụm phù hợp. |
| YSC-07 | THE system SHALL giới hạn một scan request đang chờ mỗi frame và không dispatch nhanh hơn một lần mỗi 300 ms. Tái sử dụng chỉ kết quả current cùng buffer và offset; không tích lũy lịch sử. |
| YSC-08 | WHEN điểm hoặc modifier thay đổi ngoài popup chưa ghim, hệ thống SHALL hủy scan còn chờ và bỏ response scan cũ dù abort thất bại. Nhả Shift không xóa kết quả đã hiện; vào popup hoặc đang ghim thì hủy pending scan và bỏ quét mới, giữ dịch/audio hiện hành. Chỉ mục quét mới được chấp nhận, selection chủ động mới hoặc đóng mới thay interaction và hủy dịch/audio cũ. |
| YSC-09 | THE system SHALL giữ deadline phân tích 5 giây, báo lỗi/retry rõ và không tự gọi translation hay speech từ thao tác hover. |
| YSC-10 | IF vocabulary completed nhưng không có mục từ chứa offset, hệ thống SHALL hiển thị không tìm thấy tại điểm quét, không chọn tùy ý occurrence cạnh đó. IF capability failed/unavailable hoặc truncated, hệ thống SHALL giữ trạng thái tương ứng, không chuyển null thành kết luận không có từ. |

## 4. Non-functional Requirements

- **Quyền riêng tư:** hover là opt-in, mặc định tắt; giới hạn 64 UTF-16 trong cùng block, bỏ rt/rp và không gửi context sang dịch/TTS (YSC-01/03/04/05/09).
- **Hiệu năng và tải:** dwell 300 ms, dispatch tối thiểu 300 ms, một request đang chờ mỗi frame; deadline client 5 giây (YSC-02/07/09). Quota server đề xuất theo contracts, chưa là số đã nghiệm thu.
- **Tính đúng đắn:** bảo toàn surrogate, map DOM→UTF-16 sau trim, target trùng một occurrence chứa offset; không chọn từ bên cạnh để thay no-match (YSC-03/04/06/10).
- **Độ tin cậy:** pending scan và interaction đã commit có vòng đời riêng theo YSC-08; pin và vào popup giữ dịch/audio hiện hành.
- **Tương thích:** selection-only baseline được giữ; hover là capability mới có ngoại lệ context tường minh, không mở rộng quyền browser. Chưa bảo đảm mọi vị trí hoặc từ dài đều nhận đúng.

## 5. Data Model

| Dữ liệu | Cấu trúc | Vòng đời |
|---|---|---|
| Scan request | text ≤64 UTF-16, offset, interaction_id | Ephemeral; không có URL/title/cookie |
| Scan response | analysis v3, target_span hoặc null; meta contract scan v1 | Chỉ áp dụng cho scan current |
| DOM anchor map | DOM Range ↔ offset UTF-16, không cắt surrogate | Tại frame hiện hành, hủy khi nguồn mất hiệu lực |
| Preference | scan.enabled=false, scan.modifier=Shift | Chỉ preference; không schema từ điển/lịch sử |

### Interface và hợp đồng liên quan

[contracts.md](../contracts.md) định nghĩa mới `POST /api/scan` v1 với `text`, `offset`, `interaction_id`; `offset` tính trên text đã trim. Backend kiểm tra chiều dài, surrogate và offset trước khi gọi sentence service. Kết quả chứa analysis v3 cùng `target_span` hoặc null; meta riêng contract scan v1. Analysis và scan không được dùng nhầm version.

Hành động highlight, nếu có, chỉ đánh dấu vùng current bằng overlay hoặc API không sửa text DOM; không thay selection gốc, thêm wrapper hay làm hỏng ruby. Không gửi buffer hover sang dịch hoặc TTS. Trong kết quả hover chỉ cho nghe mục từ đã chọn; muốn dịch hoặc đọc câu, người dùng phải bôi đen vùng văn bản đó trước.

## 6. Error Handling

| Trường hợp | Phản hồi và cách phục hồi |
|---|---|
| DOM/điểm không hỗ trợ hoặc không có quyền | Không đọc hoặc gửi text; giữ luồng bôi đen khi trang cho phép. |
| Offset/payload/Unicode sai | Backend từ chối trước tokenizer; client báo lỗi hợp đồng, không highlight sai. |
| Không có target khi vocabulary completed | Hiển thị không tìm thấy tại điểm; không chọn occurrence cạnh đó. |
| failed/unavailable/truncated | Hiển thị đúng trạng thái khả năng; null không bị diễn giải thành không có từ. |
| Timeout/network/429 | Kết thúc loading, giữ thông báo/retry phù hợp; không tự dịch hay lặp API. |
| Nhả Shift, vào popup, pin hoặc scan lỗi thời | Hủy pending scan và bỏ response cũ; giữ kết quả đã commit theo YSC-08. |

## 7. Acceptance Criteria

| AC | Tình huống và kết quả cần quan sát | Yêu cầu |
|---|---|---|
| YSC-AC01 | Quét tắt, hoặc bật nhưng không Shift: rê qua 100 vị trí không tạo scan request. | YSC-01/02 |
| YSC-AC02 | Giữ Shift trên `語` trong `日本語`: trả đúng occurrence `日本語`, kể cả đầu từ nằm trước điểm. | YSC-03/06 |
| YSC-AC03 | Ruby `<ruby>日本<rt>にほん</rt></ruby>語` và emoji trước từ: buffer không có annotation và target span trỏ đúng UTF-16. | YSC-03/04 |
| YSC-AC04 | Điểm trên password/input/editable/popup không bị đọc; không vượt sang paragraph kế tiếp hoặc closed shadow root. | YSC-04/05 |
| YSC-AC05 | Rê nhanh A→B; A trả sau B hoặc sau đóng popup: chỉ B có thể hiển thị, một request current/frame và không vượt nhịp 300 ms. | YSC-07/08 |
| YSC-AC06 | Bôi đen câu khi hover đang chờ: selection mở popup theo luồng cũ; response hover không ghi đè. | YSC-08 |
| YSC-AC07 | API giả trả không có occurrence chứa offset; UI không tự chọn từ khác và không gọi dịch/đọc. | YSC-06/09/10 |
| YSC-AC08 | Offset ngoài buffer hoặc giữa surrogate bị từ chối trước tokenizer; timeout 5 giây có trạng thái lỗi. | YSC-03/09 |
| YSC-AC09 | Thả Shift rồi đưa chuột vào popup: scan đang chờ bị hủy, kết quả đã commit vẫn tương tác được; khi ghim, rê qua từ khác không dừng audio hoặc dịch của mục current. | YSC-08 |

Liên hệ: DOM-001, EXT-001–006, ASYNC-001/002, PRIV-001/002/006. Phải bổ sung riêng thử nghiệm privacy của hover; regression selection-only không được thay bằng kết quả hover.

## 8. Out of Scope

Không hỗ trợ OCR, canvas, PDF viewer, Google Docs canvas, closed shadow DOM, nhập liệu hoặc dò nội dung trong iframe không có quyền. Context ngắn có thể cắt từ dài hoặc thiếu ngữ cảnh; thông báo trung thực và cho phép dùng bôi đen để tra lại. Không tuyên bố mọi vị trí con trỏ luôn nhận đúng từ.
