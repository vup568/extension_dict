# Đặc tả tổng: trải nghiệm đọc tham khảo Yomitan

Ngày: 2026-11-04. Phiên bản: 0.2. Cập nhật 2026-11-08: đang triển khai theo [kế hoạch](../../../plans/261104-yomitan-reading-experience/plan.md). Các mốc nghiệm thu đủ năm nhóm vẫn mở.

## 1. Context & Goal — Bối cảnh và mục tiêu

Hoàn thiện cùng một đợt gồm 5 nhóm: giao diện kết quả, quét từ bằng chuột, thể chia, phát âm và hành vi popup. Người dùng xác nhận ưu tiên tham khảo/tự viết, chỉ lấy code Yomitan khi cần và ghi giấy phép; giữ PostgreSQL/backend tập trung và web là nơi học. Người dùng yêu cầu Gemini tạo giọng đọc AI thay vì mặc định dùng giọng có sẵn trên máy.

Sau đợt khảo sát và soạn kế hoạch, người dùng đã yêu cầu bắt đầu code theo các tài liệu này. Các điều kiện kiểm thử và nghiệm thu nằm ở mục 7.

### Nguồn sự thật và bộ tài liệu

Kế thừa [REQUIREMENT](../../../REQUIREMENT.md), [DESIGN](../../../DESIGN.md), [F03/F04](../feat-selection-popup/SPEC.md), [F02](../001-ruby-safe-dom-extraction/SPEC.md), [F09](../feat-conjugation-explanation/SPEC.md), [tra câu](../003-sentence-vocabulary/PLAN.md) và [dịch câu](../004-english-sentence-translation/PLAN.md).

| Nhóm | Đặc tả | Kết quả mong muốn |
|---|---|---|
| YUI | [Nội dung popup](features/popup-content.md) | Đọc nhanh, chọn từ trong câu, xem đủ nghĩa/giới hạn khi cần |
| YSC | [Quét chuột](features/pointer-scanning.md) | Bật tùy chọn, giữ Shift và trỏ từ để tra mà không phải bôi đen |
| YCO | [Thể chia](features/conjugation-coverage.md) | Đối chiếu khoảng thiếu với Yomitan và tăng độ đúng theo corpus có đáp án |
| YAU | [Giọng đọc AI](features/ai-pronunciation.md) | Nhấn nghe từ hoặc vùng chọn tiếng Nhật qua Gemini TTS |
| YPO | [Hành vi popup](features/popup-behavior.md) | Resize, giữ popup để tương tác, kiểm soát focus/đóng/vị trí |

Các file dùng chung: [contracts](contracts.md), [PLAN](PLAN.md), [TASKS](TASKS.md), [bản đồ tái sử dụng](../../../docs/yomitan-reuse-plan.md).

### Hiện trạng được xác minh

- Extension React đã có Shadow DOM, xử lý ruby, selection 300 ms debounce, hủy/bỏ phản hồi cũ, Escape và clamp viewport; popup mặc định rộng tối đa 380 px.
- Backend .NET/PostgreSQL/Sudachi đã có tra từ đơn và từng occurrence trong câu; contract analysis v3 trả `vocabulary_occurrences` tùy chọn. F09 có corpus giới hạn đã được nghiệm thu trước đây.
- UI Việt, nghĩa JMdict Anh, giải thích thể chia Việt. Không đồng nhất từ có cùng cách đọc; không dùng bản dịch câu thay nghĩa từ điển.
- Dịch Gemini riêng theo click đã có. Quét chuột, âm thanh và resize tùy chỉnh chưa thấy trong runtime được khảo sát.
- `tokens=[]` và `morphology=unavailable` vẫn là hình dạng hiện tại; không tuyên bố tính năng hình thái đã hoàn thiện chỉ vì đã tra được câu.
- Database từ điển candidate hiện có các giới hạn phục vụ local. Đợt này không chuyển candidate thành nơi lưu dữ liệu học cá nhân.

## Clarifications

### Session 2026-11-10

- Q: Bạn muốn xác nhận chất lượng giọng đọc tiếng Nhật của Gemini theo cách nào? → A: Chủ dự án trực tiếp nghe bộ mẫu tiếng Nhật và xác nhận kết quả.

## 2. Actors & Roles — Tác nhân và quyền

| Tác nhân | Hành động và trách nhiệm | Ranh giới quyền |
|---|---|---|
| Người đọc ẩn danh | Bôi đen, bật quét, xem nghĩa, resize/ghim, chủ động dịch hoặc nghe | Không cần đăng nhập; không có thao tác lưu dữ liệu cá nhân trong đợt này |
| Content script và UI extension | Đọc đúng selection hoặc cửa sổ hover đã cho phép; hiển thị và quản lý interaction current | Không có key provider, không giữ kho từ điển canonical, không đọc vùng bị loại trừ |
| Background extension | Gọi backend đã cấu hình, kiểm source tab/frame và message identity | Không tạo cầu nối arbitrary fetch cho trang; không gửi yêu cầu dịch/nghe nếu chưa có thao tác |
| Backend | Xác minh input, canonical pair, giới hạn, từ điển/thể chia và điều phối TTS | Authority nghiệp vụ; khóa và model/voice allowlist nằm ở server |
| Google Gemini | Tạo bản dịch/giọng đọc cho dữ liệu được gửi theo thao tác người đọc | Không cấp quyền đọc trang, truy cập DB hoặc tự chọn nội dung cần gửi |
| Người vận hành/kiểm duyệt | Cấu hình khóa/quota/model và duyệt corpus, nguồn/code lấy lại | Không đưa secrets vào tài liệu/client; corpus mới phải có phiên bản và bằng chứng |

## 3. Functional Requirements — Yêu cầu chức năng

### Các thiết kế đề xuất cần review cùng bộ SPEC

| Quyết định | Thiết kế trong bản nháp | Tác động |
|---|---|---|
| DD01 | Hover là opt-in, mặc định tắt; Shift + dừng con trỏ 300 ms | Giữ thao tác bôi đen, tránh gọi API khi chỉ di chuột |
| DD02 | Chỉ đọc cửa sổ DOM tối đa 64 đơn vị UTF-16, 16 trước và 48 từ điểm trỏ | Ngoại lệ có giới hạn cho F02 selection-only; chỉ dùng tra từ |
| DD03 | Backend tìm occurrence chứa offset; FE không tự tách từ hay giữ từ điển | Giữ authority và canonical IDs ở server |
| DD04 | Popup có danh sách từ và phần chi tiết từ đang xem; dịch câu thu gọn | Giảm chiều dài nhưng không làm mất nghĩa hoặc điều kiện áp dụng |
| DD05 | Resize 280–640 px, mặc định 380 px, clamp viewport; pin chỉ trong phiên | Sửa giới hạn width của F03/F04; không thêm lịch sử |
| DD06 | Gemini TTS qua backend, nút nghe chủ động; từ đọc theo reading đã chọn | Phát âm là khả năng mới, không coi là bản ghi người bản xứ |
| DD07 | TTS có timeout riêng tối đa 32 giây ở UI | Ngoại lệ PERF-004 đề xuất, không đổi analysis 5 giây |
| DD08 | Corpus thể chia mới có catalog/version và kiểm duyệt độc lập | Yomitan là nguồn khảo sát, không tự thành đáp án đúng |

Người dùng đã chốt phạm vi, cách dùng source, kiến trúc và loại phát âm AI. Các con số, lựa chọn model/voice, bố cục và default ở bảng trên là thiết kế đề xuất trong SPEC, không ghi thành quyết định đã được người dùng phê duyệt riêng.

### Yêu cầu dùng chung và truy vết

- G01: WHEN đọc/tra/dịch/nghe, hệ thống SHALL cho phép dùng ẩn danh; không triển khai tài khoản hoặc lưu từ trong bộ 5 nhóm này.
- G02: Backend SHALL tiếp tục là authority từ điển/thể chia. Không sao chép kho từ điển sang IndexedDB; không chuyển bộ rules thành engine phía client.
- G03: IF selection hoặc mục quét mới được chấp nhận thay interaction hiện hành, hoặc đóng popup, UI SHALL vô hiệu phản hồi cũ ngay, kể cả khi abort thất bại. Nhả Shift chỉ hủy scan còn chờ, không xóa kết quả đã hiện. Khi con trỏ vào popup hoặc popup đang ghim, hủy pending scan, bỏ quét mới và giữ dịch/audio của interaction hiện hành. Hủy analysis, dịch và audio theo namespace riêng khi thực sự thay/đóng interaction.
- G04: Hệ thống SHALL không lưu/log raw text, request body có key, audio hoặc interaction history mặc định. Settings chỉ lưu preference không chứa nội dung.
- G05: Hover SHALL không tự gửi context sang dịch hay TTS; các thao tác này yêu cầu click và thông báo đúng loại văn bản sẽ gửi. Chỉ phát âm câu do người dùng bôi đen, không dùng âm thầm câu xung quanh con trỏ.
- G06: Nội dung từ điển/model SHALL được render như text an toàn; không dùng HTML provider hoặc code từ trang để thực thi. Canonical ID, occurrence, reading/form và sense restriction SHALL được giữ.
- G07: Hợp đồng analysis v1/v2/v3 và translation hiện hành SHALL tương thích. `/api/scan` và `/api/speech` có hợp đồng v1 riêng.
- G08: Phần mới SHALL có states loading/result/empty/unavailable/error độc lập; missing audio không làm mất nghĩa từ. Không hứa mọi từ có giải thích thể chia hay đúng 100% ngữ nghĩa.
- G09: Không cài dependency/model mới, sao chép dataset hay sửa source locks trong công việc tài liệu. Khi triển khai, đánh giá nhu cầu dependency thực tế và tuân thủ quy trình hiện hành.

## 4. Non-functional Requirements — Chất lượng và ràng buộc

| Khía cạnh | Điều kiện cần đạt | Truy vết |
|---|---|---|
| Hiệu năng | Shell p95≤150 ms sau ý định ổn định; analysis warm p95≤800 ms. Đo theo máy/browser/dataset/sample, không coi số mục tiêu là số đã đo | PERF-001/002, G-E, PLAN |
| Giới hạn thời gian | Analysis/scan client 5 giây; speech provider 25, backend 28, background 30, UI 32 giây là ngoại lệ đề xuất. Translation giữ deadline hiện hành; không nới analysis để chờ TTS | DD07, contracts |
| Bảo mật | Kiểm input/UUID/span/response; credentials server-only; host allowlist, no redirect và giới hạn request/response/quota | G04/06/07, contracts |
| Quyền riêng tư | Không log/store raw text/audio; hover bounded; disclosure trước gửi Google; chỉ lưu preference không chứa nội dung | G03/04/05, PRIV-001/002/004/006 |
| Tin cậy | Hủy best effort cộng kiểm currentness; từng capability có loading/result/error độc lập, không chờ vô hạn hoặc retry provider tự động | G03/08, ASYNC-001/002 |
| Khả năng truy cập | Chuột và bàn phím, focus không bị cướp, Escape, viewport hẹp/zoom và nhãn ngôn ngữ; test browser thực | YUI/YPO, G-E |
| Kiến trúc và giấy phép | Backend/PostgreSQL authority; port provider độc lập; dữ liệu versioned; provenance và license thực tế khi lấy code | G02/09, PLAN |

Rủi ro TTS sai phát âm/hết quota, context ngắn thiếu từ và xung đột lifecycle phải có bằng chứng kiểm thử theo từng spec; không hứa độ đúng hoặc khả năng phục vụ vô hạn.

## 5. Data Model — Cấu trúc dữ liệu

| Đối tượng | Trường/identity chính | Vòng đời và nguồn |
|---|---|---|
| Analysis | `normalized_text`, capabilities, `dictionary_matches`, `vocabulary_occurrences?`, `vocabulary_truncated?` | DTO từ backend v1/v2/v3; chỉ giữ interaction hiện hành |
| Occurrence | `surface`, UTF-16 `span`, canonical matches, conjugations | Occurrence span trên normalized text; span conjugation con trên surface |
| Mục từ và nghĩa | canonical ID, written form ID, reading ID, sense key và restrictions | PostgreSQL là canonical store; không gộp chỉ do đồng âm |
| Scan | text bounded 64 UTF-16, offset, interaction UUID; response analysis + target_span | Ephemeral; span target phải trùng occurrence và chứa điểm quét |
| Speech | Source dictionary IDs hoặc selected text; WAV base64/MIME/ja/source AI | Input chọn một loại; audio current RAM, bỏ khi đổi/đóng; không persist DB |
| Popup state/settings | active occurrence, pin, requests/audio current; settings width/height và scan preference | Pin/content chỉ trong phiên; preference không chứa nội dung có thể lưu local |
| Corpus thể chia | rule ID/version, dataset version, POS/đáp án/negative/source | Versioned knowledge; không sửa/xóa corpus cũ đã nghiệm thu |

[contracts.md](contracts.md) là định nghĩa field/validation/giới hạn HTTP dùng chung. Đây là thay đổi DTO/state và dataset có phiên bản được đề xuất, chưa chốt migration database mới. Implementation không được tự tạo schema audio/history/người dùng từ phần dữ liệu này.

## 6. Error Handling — Xử lý lỗi

| Tình huống | Phản hồi và phục hồi bắt buộc | Điều phải bảo toàn |
|---|---|---|
| Input/UUID/span/source pair sai | Reject trước tokenizer/provider; endpoint mới trả 400 `VALIDATION_FAILED`, thông báo an toàn | Không tự sửa ID, đoán reading hoặc gửi context khác |
| Không có match, capability lỗi hoặc truncated | Phân biệt completed-empty với failed/unavailable/partial tại đúng mục | Mọi kết quả hợp lệ khác vẫn đọc được |
| Network hoặc timeout | Loading kết thúc trong deadline; báo lỗi và cho retry chủ động khi phù hợp | Response interaction cũ không cập nhật popup |
| Hết quota | 429 `RATE_LIMITED` và hướng dẫn đợi; scan quota có Retry-After theo contracts | Không retry tự động, đổi key/model hoặc fallback tính phí |
| Thiếu key/model không dùng được/provider lỗi | 503 `SERVICE_UNAVAILABLE`, không lộ body provider hoặc secrets | Tra từ, dịch và audio không hủy nhầm nhau |
| TTS quá hạn/âm thanh sai hoặc quá giới hạn | 504 `TIMEOUT` khi hết deadline; reject WAV/base64 sai hoặc quá giới hạn, đề nghị chọn đoạn ngắn nếu cần | Không phát audio một phần rồi báo thành công |
| Hủy, đổi từ, nhả Shift, pin, pagehide | Theo G03 và spec YSC/YPO: hủy pending đúng lớp, giữ committed result khi nhả Shift/ghim; thay/đóng interaction thì giải phóng audio | Không tái mở popup hoặc phát audio cũ |

Lỗi HTTP dùng envelope theo contracts; lỗi client dùng thông báo kiểm soát, không render lỗi provider thô. Retry phải là intent hiện hành và được correlate lại. Chi tiết từng capability nằm trong mục 6 của spec tương ứng.

## 7. Acceptance Criteria — Tiêu chí nghiệm thu

| Gate | Điều kiện cần đạt | Bằng chứng phải tạo khi triển khai |
|---|---|---|
| G-A | Mọi requirement/AC của 5 spec được ánh xạ implementation và test | Traceability đầy đủ, không có mục Must bị bỏ qua |
| G-B | Selection/hover/dịch/audio không ghi đè hoặc hủy nhầm nhau | Test luồng A→B, ignored abort, close, pin, unpin |
| G-C | Ruby, Unicode, homophone, restrictions và corpus cũ không thoái lui | Unit + PostgreSQL/Sudachi + browser |
| G-D | Giới hạn payload, timeout, nguồn/giấy phép và nội dung log đạt | Contract/privacy report; provenance mọi file lấy trực tiếp |
| G-E | Popup bàn phím/zoom/narrow viewport/cross-frame đạt | Chrome/Edge/Brave/Firefox cài sẵn; browser chưa chạy phải ghi chưa xác minh |
| G-F | Kiểm thử Gemini thật xác minh quyền model, hạn mức và định dạng. Chủ dự án trực tiếp nghe và xác nhận kết quả trên bộ mẫu tiếng Nhật | Ghi riêng kết quả tự động và xác nhận nghe của chủ dự án theo mẫu. Giữ khóa trong phiên backend |
| G-G | Một gói release có đủ 5 nhóm, regressions passed | Feature flags không được dùng để gọi chức năng chưa nghiệm thu là hoàn thành |

Mục tiêu latency kế thừa: shell p95≤150 ms sau ý định ổn định, analysis warm p95≤800 ms; số liệu đo phải nêu máy/browser/dataset/sample và không biến mục tiêu thành kết quả đã đo. TTS đo riêng, không hứa miễn phí vô hạn hoặc phát ngay.

## 8. Out of Scope — Ngoài phạm vi

Không thêm Anki/export, bộ từ điển offline, pitch accent/frequency dataset, nét viết kanji, clipboard monitoring, OCR, canvas/PDF extraction, Google Docs đặc thù, popup lồng nhau, authentication, web học, deploy hoặc tối ưu startup từ điển. Đây là các dự án riêng, không phải các mục đã hoàn thành trong bộ SPEC này.

Quét mới chỉ nhắm văn bản DOM đọc được trên trang HTTP(S). Browser không cho content script truy cập trang hệ thống hoặc nội dung bị bảo vệ thì báo giới hạn, không tự thêm quyền rộng hơn.
