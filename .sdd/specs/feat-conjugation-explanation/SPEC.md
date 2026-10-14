# Feature Specification: F-09 — Conjugation Explanation

**Feature Branch**: `feat/tokenizer-deinflection` (nhánh làm việc hiện tại; chưa tạo nhánh mới)
**Created**: 2026-10-13 | **Version**: 0.3.2 | **Status**: Accepted for scoped corpus — user G1/G2/G3 approved;42/42 tasks complete. Firefox runtime/production publication remain separate.
**Input**: Bao phủ các dạng cơ bản và nâng cao của động từ, tính từ い/な và danh từ → kiểm duyệt corpus → bổ sung phần thiếu và nghiệm thu.
**Authority**: [REQUIREMENT §9.5](../../../REQUIREMENT.md#95-conjugation), [Constitution](../../constitution.md), [Migration §5.6/6.8](../../../MIGRATION_DECISION.md).

## Clarifications

### Session 2026-10-13

- Q: “Bao phủ hết” ở F-09 là các dạng cơ bản của bốn nhóm hay gồm cả các thể nâng cao như て, bị động, sai khiến và điều kiện? → A: B — gồm cả các thể nâng cao; tính từ な và danh từ cũng thuộc phạm vi.
- Q: Với bề mặt mơ hồ như `食べられる` (khả năng/bị động), hiển thị hai cách giải thích hay báo chưa xác định? → A: Hiển thị cả hai cách giải thích để người đọc tự chọn; hệ thống không tự quyết định một nghĩa.
- Q: Phạm vi lần này có bao gồm cách dùng kính ngữ của `れる/られる` không? → A: “Bao gồm cả cách dùng kính ngữ”. Quyết định bổ sung này không hủy lựa chọn hiển thị mọi cách hiểu hợp lệ; không tự mở rộng sang mọi cấu trúc kính ngữ khác.
- Q: Với `食べさせられる`, phạm vi có bao gồm cả cách hiểu sai khiến-khả năng ngoài sai khiến-bị động không? → A: “nên hiển thị cả 2 và giải thích cả 2 cho người dùng biết nhé, nếu chưa rõ ở đâu thì cứ hỏi tôi trước khi làm nhé”. Hiển thị riêng sai khiến-bị động (bị yêu cầu/bắt ăn) và sai khiến-khả năng (có thể cho phép/khiến ai đó ăn), đều với nhãn “có thể”; không tự chọn một nghĩa.

## Phạm vi và nền tảng đã có

Kế thừa [SPEC ban đầu](../feat-tokenizer-deinflection/SPEC.md) và [bằng chứng kiểm thử](../feat-tokenizer-deinflection/evidence/validation-report.md).
Tìm dạng gốc, giải thích Việt, chuỗi thành phần và hiển thị popup đã có cho 9 quy tắc dưới đây;
đây là **baseline phát triển**, không phải trần phạm vi hay corpus đã kiểm duyệt.

| Quy tắc hiện có | Ví dụ | Dạng gốc |
|---|---|---|
| Động từ lịch sự, không quá khứ | 食べます | 食べる |
| Động từ lịch sự, quá khứ | 食べました | 食べる |
| Động từ phủ định | 食べない | 食べる |
| Động từ phủ định, quá khứ | 食べなかった | 食べる |
| Động từ quá khứ thông thường | 食べた | 食べる |
| Động từ lịch sự, phủ định | 食べません | 食べる |
| Tính từ quá khứ | 高かった | 高い |
| Tính từ phủ định | 高くない | 高い |
| Tính từ phủ định, quá khứ | 高くなかった | 高い |

Corpus mới SHALL bao phủ đủ bốn nhóm: động từ, tính từ い, tính từ な và danh từ đi với hệ
`だ/です`. Với động từ, danh mục bắt buộc gồm khẳng định/phủ định × không quá khứ/quá khứ ×
thường/lịch sự; thể て, khả năng, bị động, cách dùng kính ngữ của `れる/られる`, sai khiến, sai khiến-bị động, điều kiện (`ば`/`たら`),
ý chí, mệnh lệnh và mong muốn. Với tính từ い: cùng các trục cơ bản, thể nối `くて`, trạng từ
`く` và điều kiện. Với tính từ な/danh từ: cùng các trục cơ bản qua hệ `だ/です`, các biến thể
phủ định `じゃ/では`, điều kiện; tính từ な thêm dạng bổ nghĩa `な` và trạng từ `に`.
Ca động từ bất quy tắc `する/来る`, `いい/よい`, biến âm và chuỗi sai khiến-bị động phải có
đáp án/ca âm riêng. Danh từ không được mô tả như tự đổi đuôi; giải thích phần trợ động từ/copula.

“Bao phủ hết” được đo bằng 100% các ô trong **danh mục dạng chia có phiên bản được duyệt**,
gồm những nhóm và thể nêu trên. Danh mục ghi rõ biến thể chính tả/ngữ dụng, ranh giới
word-unit và cách xử lý dạng đa nghĩa; không tuyên bố bao phủ mọi cấu trúc có thể tạo ra
trong tiếng Nhật. Nếu có nhiều phân tích hợp lệ và đủ chứng cứ, hiển thị các phương án
tiếng Việt với nhãn “có thể”, để người đọc tự chọn; không tự khẳng định phương án nào đúng.
Nếu không đủ chứng cứ cho một phương án, không bịa thêm phương án đó.
Kính ngữ ở đây là cách dùng tôn kính (尊敬) của `れる/られる`; không đồng nhất với đuôi
lịch sự `ます`. Điều kiện áp dụng và ca không được gán nhầm phải có nguồn/đáp án riêng;
không thêm nghĩa kính ngữ cho mọi từ chỉ vì trùng đuôi. Tham khảo [Japan Foundation](https://www.kyozai.jpf.go.jp/kyozai/material/BMA00085/ja/render.do)
và [Tokyo University of Foreign Studies](https://www.coelang.tufs.ac.jp/mt/ja/gmod/contents/explanation/092.html);
giải thích Việt được biên soạn độc lập, không sao chép nội dung nguồn.
Ngoài phạm vi: tách câu thành từng từ, phân tích hình thái tổng quát, xây Web App, đổi nhà
cung cấp, làm lại toàn bộ popup và xây pipeline dữ liệu toàn nền tảng F-20. Cho phép mở
contract mới tương thích ngược để trả nhiều phương án; contract 1/2 hiện có phải giữ nguyên.
Duyệt SPEC không đồng nghĩa duyệt corpus, dữ liệu production hoặc cho phép triển khai code.

## User Scenarios & Testing

### User Story 1 — Chốt phạm vi hỗ trợ (Priority: P1)

Product Owner và người kiểm duyệt xác định bảng dạng chia đầy đủ cho bốn nhóm đã chọn.
**Why this priority**: 9 quy tắc baseline thiếu tính từ な, danh từ và nhiều thể nâng cao.
**Independent Test**: Kiểm tra danh mục corpus và hồ sơ phê duyệt, không cần sửa chương trình.

**Acceptance Scenarios**:

1. **Given** 9 quy tắc và tri thức có sẵn, **When** lập danh mục bốn nhóm và thể nâng cao, **Then** từng ô bắt buộc có dạng, ví dụ, dạng gốc, quyết định giữ/sửa/bổ sung và nguồn; không còn ô chưa quyết định.
2. **Given** danh mục đề xuất, **When** Product Owner duyệt, **Then** tất cả ô bắt buộc/ngoài phạm vi và biến thể được khóa theo phiên bản; mục chưa duyệt không thành kết quả được cam kết.

### User Story 2 — Kiểm duyệt giải thích toàn phạm vi (Priority: P1)

Người đọc nhận đúng dạng gốc, ý nghĩa ngữ pháp và giải thích tiếng Việt từng thành phần.
**Why this priority**: Chương trình chạy đúng test chưa chứng minh nội dung ngôn ngữ đúng.
**Independent Test**: Người kiểm duyệt đối chiếu từng quy tắc với nguồn và đáp án dự kiến.

**Acceptance Scenarios**:

1. **Given** một quy tắc baseline hoặc ô mới, **When** kiểm duyệt, **Then** có kết luận về dạng gốc, ý nghĩa, chuỗi thành phần, giải thích Việt và nguồn tham khảo.
2. **Given** lỗi nội dung hoặc thiếu nguồn/quyền sử dụng, **When** đánh giá, **Then** mục bị chặn nghiệm thu đến khi sửa và duyệt lại; không tự gắn nhãn production.

### User Story 3 — Bổ sung đúng phần thiếu (Priority: P2)

Người đọc được giải thích các trường hợp bắt buộc trong corpus đã chốt nhưng chưa hỗ trợ.
**Why this priority**: Bổ sung mọi khoảng thiếu trong danh mục đã duyệt, giữ lại 9 quy tắc đúng.
**Independent Test**: Chạy từng trường hợp thiếu đã duyệt và đối chiếu kết quả người đọc thấy.

**Acceptance Scenarios**:

1. **Given** trường hợp bắt buộc còn thiếu và từ gốc có trong từ điển, **When** phân tích, **Then** trả đúng dạng gốc, giải thích Việt và các thành phần bao phủ chính xác từ được chọn.
2. **Given** không còn khoảng thiếu trong corpus đã duyệt, **When** nghiệm thu, **Then** bước bổ sung kết thúc bằng bằng chứng đối chiếu, không bắt buộc thêm quy tắc mới.
3. **Given** `食べられる` có nghĩa khả năng, bị động và kính ngữ được corpus duyệt, **When** phân tích bằng contract mới, **Then** người đọc thấy ba phương án tiếng Việt có nhãn “có thể”, cùng bề mặt nhưng nghĩa riêng, và tự chọn; contract 1/2 không bị đổi hành vi.
4. **Given** `食べさせられる` có cách hiểu sai khiến-bị động và sai khiến-khả năng được corpus duyệt, **When** phân tích, **Then** hiển thị cả hai cách hiểu và giải thích từng thành phần tương ứng, không chỉ giữ cách hiểu “bị bắt ăn”.

### Edge Cases

- Không hỗ trợ/thiếu nội dung Việt: báo chưa khả dụng, không đoán. Nhiều phân tích đủ căn cứ: hiển thị từng phương án có nhãn rõ; thiếu căn cứ cho phương án nào thì không bịa phương án đó.
- Cùng bề mặt có thể là khả năng/bị động/kính ngữ hoặc mệnh lệnh/mục từ khác: không gán một nghĩa duy nhất khi thiếu ngữ cảnh; thứ tự phương án không hàm ý xác suất/ưu tiên.
- Danh từ + `だ/です` và tính từ な dùng phần nối tương tự nhưng phải giữ đúng loại từ/dạng gốc; không biến danh từ thành “động từ chia”.
- Nhận ra cách chia nhưng không có mục từ gốc: không tạo mục từ hay giải thích giả.
- Từ đã khớp chính xác, câu nhiều từ hoặc có dấu câu: giữ hành vi hiện có, không lấy động từ đầu tiên làm dạng gốc cả câu.
- Ký tự ngoài BMP, thành phần bị hở/chồng lấn: không trả vị trí hoặc chuỗi giải thích sai.
- Dịch vụ lỗi, hết thời gian, người dùng đổi vùng chọn/hủy yêu cầu: giữ trạng thái trung thực, không hiển thị kết quả cũ.

## Requirements

### Functional Requirements

- **FR-001 — EARS[Event]**: Khi chốt corpus, nhóm phụ trách SHALL lập danh mục có mã ô dạng chia/quy tắc, bốn nhóm từ, các trục cơ bản và thể nâng cao nêu ở phần phạm vi, biến thể, ví dụ/đáp án, nguồn và quyết định giữ/sửa/loại/bổ sung; đối chiếu cả 9 quy tắc baseline.
- **FR-002 — EARS[State]**: Trước khi bổ sung, Product Owner SHALL duyệt danh sách bắt buộc/ngoài phạm vi, phiên bản và ngày duyệt; thay đổi phạm vi phải được duyệt lại.
- **FR-003 — EARS[Ubiquitous]**: Mỗi quy tắc được chọn SHALL có ít nhất 2 ca đúng và 1 ca không được nhận diện nhầm; mỗi ô dạng chia bắt buộc có ít nhất 1 ca đúng và ca phân biệt khi dễ nhầm; toàn corpus có động từ, tính từ い/な, danh từ, bất quy tắc và các thể nâng cao đã duyệt.
- **FR-004 — EARS[Event]**: Khi kiểm duyệt tất cả quy tắc/ô bắt buộc, gồm 9 quy tắc baseline, người dùng được chỉ định SHALL ghi người/ngày duyệt, kết luận, vấn đề và cách xử lý; đáp án dự kiến phải được duyệt độc lập với kết quả chương trình.
- **FR-005 — EARS[Unwanted]**: Khi nội dung sai, thiếu nguồn hoặc điều kiện sử dụng chưa rõ, quy tắc SHALL bị chặn nghiệm thu; sửa đổi tạo phiên bản mới, giữ lịch sử và ca hồi quy của lỗi.
- **FR-006 — EARS[Event]**: Khi có khoảng thiếu đã duyệt, hệ thống SHALL bổ sung quy tắc/nội dung Việt tương ứng dưới dạng tri thức có phiên bản; không gắn cứng nội dung ngôn ngữ vào logic chương trình.
- **FR-007 — EARS[Event]**: Với ca hỗ trợ có mục từ gốc, người đọc SHALL thấy dạng quan sát, dạng gốc, ý nghĩa ngữ pháp, thành phần có thứ tự và giải thích Việt; các thành phần của mỗi phương án phải tái tạo đúng toàn bộ từ quan sát, không hở/chồng lấn. Khi nhiều cách phân tích đều có chứng cứ, SHALL hiển thị tất cả như các phương án “có thể”, để người đọc tự chọn, không tự xếp hạng một nghĩa là đúng.
- **FR-008 — EARS[Unwanted]**: Với ca không hỗ trợ/thiếu nội dung/dịch vụ không khả dụng, hệ thống SHALL không bịa kết quả; dạng khả năng/bị động trùng bề mặt hoặc token chỉ có mục từ phái sinh SHALL có ca phân biệt, mỗi phương án chỉ được hiển thị khi có chứng cứ và mục từ gốc tương ứng. Không tạo liên kết từ điển giả. Contract 1/2 hiện tại và exact-first SHALL giữ nguyên; contract mới cho nhiều phương án phải có version riêng, kiểm tra identity/UTF-16 và chống phản hồi trễ.
- **FR-009 — EARS[State]**: Khi nghiệm thu, corpus SHALL có bản cố định, nguồn hoặc quyết định biên tập, quyền sử dụng/attribution, dấu kiểm toàn vẹn và hồ sơ duyệt; mọi mục bắt buộc phải được triển khai và kiểm chứng, không được âm thầm loại ca lỗi.
- **FR-010 — EARS[Ubiquitous]**: Tra cứu SHALL không yêu cầu đăng nhập; không lưu nội dung người dùng chọn vào log/lịch sử mặc định. Bản phát triển không tự trở thành dữ liệu production; phát hành phải qua quy trình phê duyệt dữ liệu hiện hành.

### Key Entities

- **Corpus MVP**: Danh mục phạm vi và các ca kiểm chứng có đầu vào, kết quả mong đợi, nguồn, phiên bản và trạng thái duyệt.
- **Quy tắc chia từ**: Định danh/phiên bản, điều kiện áp dụng, ý nghĩa và nội dung Việt từng thành phần.
- **Hồ sơ kiểm duyệt**: Người/ngày duyệt, kết luận, vấn đề, quyết định biên tập và bằng chứng xử lý.
- **Bản dữ liệu nghiệm thu**: Danh mục quy tắc cố định, nguồn/quyền sử dụng, dấu kiểm toàn vẹn và kết quả kiểm chứng.

## Success Criteria

### Measurable Outcomes

- **SC-001**: 100% ô dạng chia bắt buộc của bốn nhóm và thể nâng cao có quyết định phạm vi, ví dụ/nguồn; 9 quy tắc baseline đều được đối chiếu; danh mục được Product Owner duyệt trước bổ sung.
- **SC-002**: 100% ô/quy tắc bắt buộc, gồm 9/9 baseline, có hồ sơ kiểm duyệt; mọi lỗi phát hiện được xử lý hoặc loại có lý do và duyệt lại trước nghiệm thu.
- **SC-003**: 100% ca đúng/sai của corpus đã duyệt đạt đáp án độc lập; mỗi quy tắc đủ mức ca tối thiểu FR-003; số mục bắt buộc còn thiếu bằng 0.
- **SC-004**: 100% ca đa nghĩa có đủ chứng cứ hiển thị đúng mọi phương án và nhãn không chắc chắn; 100% ca thiếu chứng cứ/nội dung, không hỗ trợ, lỗi và đổi vùng chọn không sinh giải thích giả hoặc kết quả cũ; contract 1/2 hồi quy vẫn đạt.
- **SC-005**: 100% ô/quy tắc nghiệm thu có nội dung Việt, nguồn/quyền sử dụng rõ và bản dữ liệu cố định; không còn khoảng thiếu trong danh mục đã duyệt hay vấn đề ngôn ngữ/provenance chưa xử lý.

| Công việc | Yêu cầu | Bằng chứng nghiệm thu |
|---|---|---|
| Chốt corpus | FR-001–003 | Danh mục + phê duyệt phạm vi; SC-001/003 |
| Kiểm duyệt tất cả ô/quy tắc | FR-004–005 | Hồ sơ đánh giá/sửa lỗi; SC-002/005 |
| Bổ sung và nghiệm thu | FR-006–010 | Corpus không còn khoảng thiếu, kiểm chứng và bản dữ liệu; SC-003–005 |

## Assumptions

- Corpus là đầu ra của bước 1; danh mục phải liệt kê rõ các ô dạng chia/biến thể trước khi chốt số quy tắc triển khai. Quyết định B không tự động phê duyệt gói 9 quy tắc cũ hoặc các đáp án mới.
- Product Owner duyệt phạm vi và chỉ định người kiểm duyệt có chuyên môn; không suy ra phê duyệt từ việc test đạt hoặc từ nội dung do AI tạo.
- Kế thừa tra từ và trình bày hiện có; kiểm chứng từng ca phụ thuộc mục từ gốc và dịch vụ phân tích sẵn sàng. Xây Web App và cơ chế phát hành toàn nền tảng thuộc feature riêng.
- Tuân thủ Constitution §3–10: tri thức có phiên bản, nguồn/quyền sử dụng, identity và vị trí chính xác, độc lập nhà cung cấp, tối thiểu dữ liệu, không log text, chống phản hồi trễ; không đề xuất ngoại lệ.
- Truy vết: CONJ-001/002, VOC-002/007, DATA-001/003/005, I18N-003, PRIV-002/006, ASYNC-002; kế hoạch sau phải chuyển các nghĩa vụ này thành công việc và kiểm chứng cụ thể.
