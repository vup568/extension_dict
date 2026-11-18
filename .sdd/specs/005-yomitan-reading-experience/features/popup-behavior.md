# YPO — Vị trí, kích thước và vòng đời popup

Ngày: 2026-11-04. Phiên bản: 0.2. Trạng thái: Draft để review, chưa triển khai. Thuộc [đặc tả tổng](../SPEC.md).

## 1. Context & Goal

Popup đủ linh hoạt để đọc kết quả dài mà vẫn cho tương tác ổn định với trang. [F03/F04](../../feat-selection-popup/SPEC.md) đã có Shadow DOM, vị trí theo selection, top-layer fallback, clamp viewport, focus, Escape và width tối đa 380 px. Resize và pin trong spec này là mở rộng phạm vi riêng; không ghi chúng là chức năng đang có.

### Tham khảo và điểm tích hợp

Yomitan revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`: `ext/js/app/popup.js`, `ext/js/display/display-resizer.js`, `ext/css/popup-outer.css`. Tham khảo resize theo pointer và giữ popup khi đang tương tác; không chuyển nguyên iframe/custom API của Yomitan vào dự án.

Điểm tích hợp: [createPopupView.tsx](../../../../extension/src/browser/popup/createPopupView.tsx), [startSelectionPopup.ts](../../../../extension/src/browser/popup/startSelectionPopup.ts), [PopupShell.tsx](../../../../extension/src/ui/PopupShell.tsx), [popup.css](../../../../extension/src/ui/popup.css).

## 2. Actors & Roles

| Actor | Vai trò và quyền |
|---|---|
| Người đọc ẩn danh | Resize, ghim, unpin, điều khiển bàn phím và đóng popup; không cần login. |
| Content script/React view | Quản lý một popup mỗi frame, focus/viewport và lifecycle; không sửa văn bản hoặc chặn tương tác trang tùy tiện. |
| Browser/host page | Cung cấp viewport, pointer/focus/top-layer; có thể thiếu API hoặc quyền frame. |
| Background | Hủy request đúng namespace khi phiên bị thay hoặc đóng; resize không tạo API request mới. |

## 3. Functional Requirements

| ID | Hành vi bắt buộc |
|---|---|
| YPO-01 | THE system SHALL giữ một popup current mỗi frame trong Shadow DOM; dùng top layer khi có và fallback an toàn, không yêu cầu sửa CSS/text của trang. |
| YPO-02 | WHEN mở popup mới, hệ thống SHALL dùng kích thước mặc định đề xuất 380×480 px hoặc preference hợp lệ; width người dùng chọn từ 280–640 px, luôn clamp với lề viewport 12 px. |
| YPO-03 | WHILE resize, hệ thống SHALL clamp chiều cao tối thiểu 160 px và tối đa viewport trừ lề; nếu viewport nhỏ hơn min thì viewport được ưu tiên. Nội dung cuộn bên trong, nút đóng vẫn truy cập được. |
| YPO-04 | WHEN bắt đầu/kết thúc resize, hệ thống SHALL quản lý pointer capture và cleanup khi pointer-up, cancel, window blur hoặc đóng popup; thao tác resize không tạo selection/lookup mới. |
| YPO-05 | WHEN người dùng vào popup từ điểm hover, hệ thống SHALL cho phép tương tác mà không bị scan các ký tự trong popup. Pin giữ popup hiện hành trong phiên khỏi việc rê chuột; không tạo lịch sử hay giữ raw text qua phiên. |
| YPO-06 | WHEN pin được bật, pointer scan mới SHALL không thay popup. Selection chủ động mới hoặc Escape vẫn được phép thay/đóng và phải hủy công việc cũ. Unpin giữ current result cho tới một ý định quét mới hợp lệ. |
| YPO-07 | THE system SHALL cung cấp Escape/Đóng, thao tác resize qua bàn phím và focus có thể dự đoán. Tự mở popup không cướp focus; khi đóng chỉ khôi phục focus nếu focus đang ở popup và mục gốc còn tồn tại. |
| YPO-08 | WHEN scroll, zoom hoặc visual viewport thay đổi, hệ thống SHALL cập nhật vị trí và clamp lại; không để popup mất khỏi viewport hoặc pointer resize treo. |
| YPO-09 | THE system SHALL chỉ lưu width/height preference, không lưu nội dung, key, interaction ID hoặc trạng thái pin. Giá trị preference sai/quá lớn phải được bỏ hoặc clamp. |
| YPO-10 | WHEN đổi selection, đóng, pagehide hoặc tháo content script, hệ thống SHALL hủy requests/timers, dừng audio và giải phóng tài nguyên; response cũ không được tái mở popup. |

## 4. Non-functional Requirements

- **Khả năng truy cập:** popup nonmodal, không focus trap, nút có tên truy cập và keyboard resize; tự mở không cướp focus (YPO-07).
- **Responsive:** default đề xuất 380×480 px, width 280–640 px, height min160 và clamp viewport với lề12 px; viewport nhỏ ưu tiên khả năng đọc/đóng (YPO-02/03/08).
- **Cách ly/tương thích:** Shadow DOM, top layer khi có và fallback; không rò CSS, hỗ trợ frame được phép và ghi rõ browser chưa kiểm chứng (YPO-01).
- **Độ tin cậy:** blur/pointercancel kết thúc capture và listener của resize; chỉ khi thay interaction, đóng hoặc pagehide mới hủy request/timer thuộc interaction và dừng audio. Pin giữ current khi chỉ rê chuột (YPO-04/05/06/10).
- **Riêng tư:** chỉ persist preference kích thước hợp lệ, không text/key/ID/pin (YPO-09).
- **Hiệu năng:** resize/open detail không phát lại API; đo lại shell latency theo SPEC tổng sau thay bố cục, không lấy số đo trước làm chứng cứ bản mới.

## 5. Data Model

| Dữ liệu | Trường/state | Vòng đời |
|---|---|---|
| Popup session | Current interaction, anchor, pin, focus trước mở | Tại frame hiện hành, không history |
| Resize state | Pointer ID, size, capture và callback cleanup | Chỉ trong thao tác; kết thúc trên up/cancel/blur |
| Size preference | popup.width, popup.height hợp lệ | Có thể lưu extension storage; không schema backend |
| Resource ownership | Pending namespaces, audio/Blob hiện hành | Giải phóng khi thay/đóng/pagehide |

### Interface và hợp đồng liên quan

Không thay API dữ liệu chỉ vì resize/pin. Các settings và namespace trong [contracts.md](../contracts.md) dùng chung cho YUI/YSC/YAU. State pin thuộc phiên popup, không là bản sao dữ liệu từ điển. Resize và mở mục chi tiết chỉ cập nhật view, không phát lại analysis/translation/speech.

Keyboard resize đề xuất: nút tăng/giảm chiều rộng và chiều cao có accessible name, bước 20 px, cùng clamp như pointer. Không cần keyboard trap vì popup nonmodal. Trong viewport hẹp, lề và khả năng đóng được ưu tiên hơn min size. Resize không kéo thả vị trí tự do trong đợt này.

## 6. Error Handling

| Trường hợp | Phản hồi và cách phục hồi |
|---|---|
| Không có top-layer API | Dùng Shadow DOM fallback và clamp; không làm popup vô hình. |
| Preference sai hoặc ngoài viewport | Bỏ hoặc clamp; vẫn truy cập nút đóng. |
| Pointercancel/window blur trong resize | Kết thúc capture/listener, giữ kích thước hợp lệ, không gửi lookup mới. |
| Anchor/focus gốc bị gỡ khỏi DOM | Tránh dereference node mất hiệu lực; chỉ phục hồi focus nếu node còn tồn tại. |
| Đổi/đóng/pagehide khi API hoặc audio đang chờ | Hủy đúng resource và bỏ response cũ; không tái mở popup. |
| Frame hoặc nội dung không có quyền | Không đọc hoặc vẽ xuyên frame giả; ghi giới hạn thực tế trong báo cáo browser. |

## 7. Acceptance Criteria

| AC | Tình huống và kết quả cần quan sát | Yêu cầu |
|---|---|---|
| YPO-AC01 | Trang có CSS transform/overflow/reset mạnh vẫn hiển thị popup và đọc/đóng được; shadow CSS không rò ra trang. | YPO-01 |
| YPO-AC02 | Viewport 320 px và zoom 200%: popup nằm trong viewport với lề 12 px khi đủ chỗ; không cố giữ width 280 px nếu viewport nhỏ hơn. | YPO-02/03/08 |
| YPO-AC03 | Kéo resize vượt mép màn hình rồi blur/pointercancel: resize kết thúc, không request mới hoặc selection bị đổi. | YPO-03/04 |
| YPO-AC04 | Di chuột từ từ đang tra vào popup có thể chọn nghĩa/nút nghe; scan không đọc chữ bên trong popup. | YPO-05 |
| YPO-AC05 | Pin rồi rê qua từ khác không thay kết quả; bôi đen vùng mới vẫn thay current, Escape luôn đóng và hủy audio/request. | YPO-06/10 |
| YPO-AC06 | Tab và keyboard resize hoạt động; mở tự động giữ focus host, đóng từ trong popup trả focus đúng phần tử còn tồn tại. | YPO-07 |
| YPO-AC07 | Reload trang chỉ phục hồi kích thước hợp lệ; pin/text/interaction ID không được lưu trong storage. | YPO-09 |
| YPO-AC08 | A đang dịch/đọc, đóng hoặc pagehide; response A về sau không hiện lại popup và không phát audio. | YPO-10 |
| YPO-AC09 | Text dọc và same-origin/cross-origin frame có content script được phép: popup định vị trong viewport frame, không bịa overlay xuyên frame. Frame không có quyền không được đọc. | YPO-01/08 |
| YPO-AC10 | Pin khi audio/dịch đang tải hoặc đang phát rồi rê qua chữ khác: kết quả/audio hiện hành được giữ; selection mới hoặc Escape mới thay/đóng và hủy đúng. | YPO-05/06/10 |

Liên hệ: BROWSER-001/002/003, EXT-003/005, ASYNC-001/002, PRIV-006 và SPEC F03/F04. Kiểm thử trên Chrome/Edge/Brave/Firefox cài sẵn; thiếu browser runtime phải ghi chưa xác minh, không suy từ build thành parity.

## 8. Out of Scope

Không thêm nhiều popup lồng nhau, kéo thả vị trí tự do, popup lịch sử, đồng bộ preference qua tài khoản hoặc biến pin thành lưu từ. Pin/resize không được giữ dữ liệu khi user chọn vùng mới. Phải đo lại shell latency sau thay bố cục; số đo trước đây không là bằng chứng cho bản mới.
