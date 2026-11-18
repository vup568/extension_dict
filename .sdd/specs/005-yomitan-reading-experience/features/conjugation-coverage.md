# YCO — Đối chiếu và mở rộng độ phủ thể chia

Ngày: 2026-11-04. Phiên bản: 0.2. Trạng thái: Draft để review, chưa triển khai. Thuộc [đặc tả tổng](../SPEC.md).

## 1. Context & Goal

Tận dụng danh mục quy tắc và trường hợp biên của Yomitan để tìm khoảng thiếu, rồi mở rộng bằng dữ liệu ngôn ngữ có phiên bản và đáp án được kiểm duyệt. [F09](../../feat-conjugation-explanation/SPEC.md) đã được chấp nhận cho corpus giới hạn; sentence lookup đã có fallback lemma. Các kết quả đó không đồng nghĩa đã phủ mọi ngữ pháp tiếng Nhật.

### Tham khảo và điểm tích hợp

Yomitan revision `d3c54c10264673eca0d66b970ef74cd5c8b048e3`: `ext/js/language/ja/japanese-transforms.js`, `ext/js/language/language-transformer.js`, `test/language/japanese-transforms.test.js`, `test/language-transformer-cycles.test.js`. Những quy tắc này gợi ý ứng viên và điều kiện từ loại, không tự trở thành đáp án chuẩn của dự án.

Điểm tích hợp: corpus và pipeline trong [F09](../../feat-conjugation-explanation/SPEC.md), [LookupWordUseCase.cs](../../../../src/usecase/Dictionary/LookupWordUseCase.cs), [SentenceVocabularyUseCase.cs](../../../../src/usecase/Dictionary/SentenceVocabularyUseCase.cs). Phải duy trì backend là nơi xử lý; không nhúng engine biến đổi riêng vào extension.

## 2. Actors & Roles

| Actor | Vai trò và quyền |
|---|---|
| Người đọc ẩn danh | Tra từ/câu và xem các phân tích có chứng cứ, không cần tài khoản. |
| Người biên soạn/kiểm duyệt corpus | Lập gap matrix, chọn nguồn đáp án và review từng họ quy tắc; output engine không thay quyền kiểm duyệt. |
| Backend/tokenizer/repository | Sinh và xác minh ứng viên theo dataset có phiên bản, canonical ID, POS và applicability. |
| Người vận hành | Kích hoạt hoặc quay lại dataset đã xác minh theo quy trình; không sửa tùy tiện corpus đã nghiệm thu. |

## 3. Functional Requirements

### Danh mục bắt buộc để đánh giá khoảng thiếu

| Nhóm | Ô kiểm tra cần lập |
|---|---|
| Động từ cơ bản | Khẳng định/phủ định × quá khứ/không quá khứ × thường/lịch sự; て; godan/ichidan và bất quy tắc `する/来る`. |
| Động từ nâng cao | Khả năng, bị động, tôn kính khi có chứng cứ, sai khiến, sai khiến-bị động và cách hiểu sai khiến-khả năng; điều kiện ば/たら, ý chí, mệnh lệnh, mong muốn. |
| Tính từ い | Các trục cơ bản, nối くて, trạng từ く, điều kiện; `いい/よい` và ca không hợp từ loại. |
| Tính từ な và danh từ | Hệ copula だ/です, phủ định じゃ/では, quá khứ/điều kiện; な/に chỉ áp dụng tính từ な khi hợp lệ. |
| Chuỗi trong câu | ている, ていた, ていない, てしまう, てしまった; ranh giới trợ từ/dấu câu và các lần xuất hiện lặp. |

Đây là danh mục đề xuất để review. Trước khi viết quy tắc mới phải tạo bảng ô hỗ trợ/thiếu/loại trừ cùng phiên bản và đáp án; không ghi ô chưa kiểm chứng là đã hỗ trợ.

### Yêu cầu có định danh

| ID | Hành vi bắt buộc |
|---|---|
| YCO-01 | THE team SHALL lập gap matrix từ corpus F09 hiện có và các họ quy tắc trên, ghi rõ nguồn tham khảo, đầu vào, base form, POS, phân tích chấp nhận và phân tích phải từ chối. |
| YCO-02 | BEFORE bổ sung quy tắc, corpus SHALL có đáp án độc lập với output của Yomitan và engine dự án; nguồn ngôn ngữ và review phải truy vết được. |
| YCO-03 | WHEN thêm tri thức chia thể, hệ thống SHALL dùng dataset có phiên bản và pipeline kiểm tra; không viết danh sách hậu tố mới trực tiếp trong engine C#. |
| YCO-04 | WHEN xác minh ứng viên, backend SHALL đối chiếu canonical entry, POS và cặp viết–đọc–nghĩa hợp lệ trong JMdict; exact lookup có ưu tiên theo baseline. |
| YCO-05 | IF nhiều cách hiểu đều có chứng cứ, hệ thống SHALL giữ chúng riêng với nhãn “có thể”; không tự suy ra nghĩa đúng theo vị trí hoặc độ dài quy tắc. |
| YCO-06 | IF chỉ có lemma tokenizer mà chưa đủ chứng cứ giải thích, hệ thống SHALL vẫn có thể tra mục từ nhưng không dựng chuỗi giải thích giả. |
| YCO-07 | THE system SHALL giữ span UTF-16 occurrence trên normalized text và span con trên surface riêng; không cắt surrogate hay ghép qua dấu câu/khoảng trắng ngoài quy tắc được chấp nhận. |
| YCO-08 | THE system SHALL giữ hành vi tương thích contract 1/2/3; bộ client cũ không nhận shape không hỗ trợ. |
| YCO-09 | BEFORE kích hoạt dataset mới, verification SHALL so sánh corpus cũ/mới, ca âm, cancellation, giới hạn candidate và hiệu năng; dataset cũ phải có đường quay lại. |

## 4. Non-functional Requirements

- **Đúng ngôn ngữ:** đáp án độc lập, có nguồn và review; exact-first, mọi phân tích hợp lệ có nhãn có thể, không hứa phủ mọi tiếng Nhật (YCO-01/02/04/05/06).
- **Quản trị dữ liệu:** versioned dataset và provenance, không hardcode hậu tố vào C#; giữ đường quay lại và corpus đã nghiệm thu (YCO-03/09).
- **Tương thích:** contracts 1/2/3 và UTF-16 span không thay nghĩa sau mở rộng (YCO-07/08).
- **Hiệu năng/độ tin cậy:** giữ giới hạn candidate, cancellation và deadline analysis hiện có; benchmark trên corpus cũ/mới trước kích hoạt, không tự tăng timeout (YCO-09).
- **Giấy phép:** nếu lấy rule/test trực tiếp từ Yomitan phải ghi nguồn và xử lý giấy phép theo SPEC tổng; tham khảo không thay kiểm chứng chất lượng.

## 5. Data Model

| Dữ liệu | Trường cần ghi | Vòng đời |
|---|---|---|
| Gap catalog | Nhóm/ô, input, base form, POS, phân tích nhận/từ chối, nguồn, review, version | Freeze trước viết quy tắc mới |
| Versioned rules/corpus | Dataset version, rule ID/version, cases và expected output | Bản mới riêng, không overwrite bản đã nghiệm thu |
| Phân tích trả về | surface/base_form, explanation, segments, rule/dataset version, UTF-16 span | Response ephemeral |
| Bằng chứng nghiệm thu | Kết quả regression, ca âm, review/benchmark | Hồ sơ phiên bản, không raw text người dùng |

### Interface và hợp đồng liên quan

Không cần endpoint mới cho thể chia; dùng hợp đồng analysis/scan trong [contracts.md](../contracts.md). Nội dung giải thích tiếp tục tiếng Việt, nghĩa từ hiện tại tiếng Anh. Nếu thêm field bắt buộc thì cần quyết định contract tương thích riêng trước khi triển khai; không tự mở rộng validator v3.

## 6. Error Handling

| Trường hợp | Phản hồi và cách phục hồi |
|---|---|
| Chỉ có lemma, thiếu evidence giải thích | Tra nghĩa có xác minh, không thêm chuỗi explanation (YCO-06). |
| Canonical/POS hoặc cặp viết–đọc–nghĩa không hợp | Từ chối phân tích ứng viên đó; không gán nghĩa do trùng hậu tố. |
| Nhiều phân tích có chứng cứ | Trả các alternative riêng, không tự chốt một nghĩa. |
| Dataset/corpus mới không qua validation | Không kích hoạt; tiếp tục bản đã nghiệm thu hoặc quay lại bản đã xác minh. |
| Tokenizer/DB lỗi hoặc hết thời gian | Báo capability failed/unavailable theo contract hiện có, giữ phần kết quả khác và không giả explanation. |
| Response lỗi thời/caller hủy | Hủy best effort và bỏ phản hồi; không thay interaction mới. |

## 7. Acceptance Criteria

| AC | Tình huống và kết quả cần quan sát | Yêu cầu |
|---|---|---|
| YCO-AC01 | Gap matrix có kết quả cho từng ô danh mục; không dùng output Yomitan làm đáp án duy nhất. | YCO-01/02 |
| YCO-AC02 | `食べませんでした`, `行った`, `来なかった` khớp base/POS và phân tích đã review. | YCO-03/04 |
| YCO-AC03 | `高くなかった`, `静かではなかった`, `学生でした` giải thích đúng tính từ/copula, không gọi danh từ là tự đổi đuôi. | YCO-03/04 |
| YCO-AC04 | `食べられる` và `食べさせられる` giữ mọi phân tích có chứng cứ; không gán chắc nghĩa bị động hoặc khả năng. | YCO-05 |
| YCO-AC05 | Fixture lemma-only cho dạng ngoài corpus trả nghĩa từ gốc, không tự thêm conjugation explanation. | YCO-06 |
| YCO-AC06 | `🐈食べている。食べてしまった。` giữ hai vị trí và offset đúng, chuỗi có dấu câu chen vào không bị ghép. | YCO-07 |
| YCO-AC07 | Kana trùng hậu tố nhưng POS không hợp lệ bị từ chối; restriction đọc A/B của cùng entry không bị trộn. | YCO-04 |
| YCO-AC08 | Tất cả corpus F09 đã được chấp nhận và fixture contract 1/2/3 vẫn đạt trước khi kích hoạt dataset mới. | YCO-08/09 |

Liên hệ: CONJ-001/002, VOC-002/003/004/007/008, ASYNC-002, ID-001; không thay thế nghiệm thu G1/G2/G3 của F09 cũ bằng số lượng test mới.

## 8. Out of Scope

Không hứa độ phủ 100% ngôn ngữ, không xây bộ dịch ngữ pháp câu từ quy tắc suffix, không đưa tri thức chưa review lên production. Port trực tiếp dữ liệu quy tắc GPL hoặc test Yomitan phải ghi nguồn và giấy phép; paraphrase sát mã nguồn không mặc định là viết độc lập. Chất lượng corpus mới là gate riêng với việc engine chạy được.
