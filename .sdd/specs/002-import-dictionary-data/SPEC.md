# Feature Specification: Import đầy đủ dữ liệu từ điển V2

**Feature Branch**: `feat/tokenizer-deinflection` (hiện tại; không tạo/chuyển branch trong bước tài liệu)

**Created**: 2026-10-15

**Status**: Implementation authorized on 2026-10-15; P0–P2 mechanisms under verification. Source/license, semantic mapping and activation approvals remain pending; no real database import.

**Input**: “import đủ từ vựng, kanji và nghĩa tiếng Việt như V1”; user duyệt Shadow Plan bằng Proceed.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tra từ thật thay vì bộ demo (Priority: P1)

Người đọc tra từ/cách đọc trong toàn bộ nguồn từ vựng đã duyệt, xem nghĩa Việt khi có nghĩa phù hợp được kiểm duyệt, nếu thiếu thì xem nghĩa Anh từ nguồn.

**Why this priority**: Backend hiện chỉ có bộ demo 17 mục, chưa đáp ứng tra cứu thực tế.

**Independent Test**: Dùng tập từ ngoài demo, đối chiếu từng kết quả với bản nguồn cố định và đáp án kiểm duyệt độc lập.

**Acceptance Scenarios**:

1. **Given** bộ từ vựng đầy đủ đã nạp/kiểm tra, **When** tra `裁判官`, `学生` hoặc cách đọc tương ứng có trong nguồn, **Then** trả đúng entry, cách đọc, từ loại và nghĩa áp dụng, không dùng fixture ID.
2. **Given** nghĩa Việt đã duyệt cho đúng sense, **When** tra entry đó, **Then** ưu tiên nghĩa Việt; thiếu nghĩa Việt thì dùng nghĩa Anh, không tự tạo bản dịch.
3. **Given** từ có nhiều form/reading/sense hoặc reading chỉ dùng kana, **When** tra một dạng cụ thể, **Then** không gán nghĩa/cách đọc bị nguồn loại trừ cho dạng đó.

### User Story 2 - Xem kanji và giải thích dạng chia (Priority: P1)

Người đọc xem thông tin từng kanji và tiếp tục dùng F06/F09 với từ điển thật.

**Why this priority**: Hoàn tất phạm vi V1 đã chốt mà không làm hỏng khả năng phân tích đã nghiệm thu.

**Independent Test**: Tra kanji nguồn có hỗ trợ, cùng `食べました`, `食べられる`, tính từ な và danh từ; đối chiếu canonical entry/POS và giải thích đã duyệt.

**Acceptance Scenarios**:

1. **Given** ký tự có trong nguồn kanji, **When** xem chi tiết, **Then** có cách đọc/meaning/số nét/bộ thủ khi nguồn cung cấp; Hán Việt không bị gọi nhầm là nghĩa Việt; giá trị ước lượng được đánh dấu.
2. **Given** nguồn thật có entry/POS phù hợp và Sidecar sẵn sàng, **When** tra `食べました`, **Then** tìm được `食べる` và giải thích tiếng Việt theo F09.
3. **Given** dạng mơ hồ `食べられる`, **When** F09 xác nhận các cách hiểu được hỗ trợ, **Then** giữ các giải thích khả năng/bị động/kính ngữ phù hợp; không tự chọn một nghĩa duy nhất.

### User Story 3 - Nạp và đổi bộ dữ liệu an toàn (Priority: P1)

Người vận hành nạp lại cùng nguồn, xem số lượng/nguồn gốc/lỗi và chỉ đưa bộ dữ liệu đã kiểm tra vào dùng khi chủ dự án duyệt.

**Why this priority**: Không làm hỏng DB hiện tại hoặc trình bày dữ liệu chưa duyệt như dữ liệu chuẩn.

**Independent Test**: Mô phỏng gián đoạn/chạy lại/nguồn lỗi; đối soát trước-sau DB gốc và demo, bộ dữ liệu đang dùng không thay đổi.

**Acceptance Scenarios**:

1. **Given** bộ nguồn cố định đã được phép sử dụng, **When** nạp lần đầu rồi chạy lại/tiếp tục sau gián đoạn, **Then** không nhân đôi, không đổi canonical identity và không công bố dữ liệu dở dang.
2. **Given** thiếu bằng chứng nguồn/license, checksum sai, mapping mơ hồ hoặc target không được phép, **When** yêu cầu nạp/kích hoạt, **Then** bị chặn với báo cáo rõ lý do, không sửa DB đang dùng.
3. **Given** bộ dữ liệu mới đạt kiểm tra và user duyệt, **When** chuyển sang thử bộ mới hoặc quay lại bộ cũ, **Then** không reset/xóa dữ liệu cũ, không đổi cấu hình bí mật đã lưu.

### Edge Cases

- Kana-only, reading không dùng kanji, restricted form/reading/sense, POS kế thừa, đồng tự/đồng âm nhưng khác nghĩa.
- Unicode supplementary, dấu tiếng Việt, dấu chấm/gạch nối trong kunyomi; không chuẩn hóa làm mất identity.
- Entry Việt không có reading hoặc không xác định được sense tương đương; nội dung lỗi/HTML/English-pivot, nhiều nghĩa, overrides V1.
- Nguồn mới đổi thứ tự sense, trùng ID, file bị cắt, phiên bản sai, hết đĩa, hủy hoặc hai tiến trình cùng nạp.
- Thiếu trường kanji, nhiều stroke/radical variants; JLPT cũ không được giả làm mức N hiện hành chính thức.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: MUST nhập toàn bộ entry của bản nguồn từ vựng đầy đủ đã duyệt, không giới hạn common-only/demo. Giữ upstream ID, forms, readings, senses, POS và restrictions có ảnh hưởng nghĩa.
- **FR-002**: MUST nhập toàn bộ ký tự của bản nguồn kanji đã duyệt; giữ các trường có sẵn, Hán Việt, nghĩa Anh và nghĩa Việt hợp lệ kèm nguồn; thiếu trường không được điền dữ liệu bịa.
- **FR-003**: MUST kiểm kê toàn bộ nguồn nghĩa Việt và tri thức overrides V1; mỗi record có kết quả xử lý/mapping hoặc lý do cách ly/loại bỏ. Không cắt nội dung theo giới hạn đóng gói V1.
- **FR-004**: MUST chỉ gắn nghĩa Việt vào canonical entry/sense/form/reading sau quyết định tương đương ngữ nghĩa được duyệt; trùng chữ/cách đọc chỉ tạo đề xuất, không tự chứng minh tương đương.
- **FR-005**: MUST giữ phân biệt nguồn gốc, facts, giá trị suy ra và editorial overrides; không coi Hán Việt là bản dịch nghĩa của kanji.
- **FR-006**: MUST ghi nguồn/publisher, exact version, checksum, license/attribution, transformation identity, counts, rejects và bằng chứng validation cho mỗi bộ nguồn.
- **FR-007**: MUST chặn sử dụng canonical khi nguồn/license/redistribution/quality chưa được làm rõ và duyệt, đặc biệt FVDP/OVDP theo OD-009; xác nhận phạm vi không đóng OD-009.
- **FR-008**: MUST tái tạo cùng facts/canonical IDs từ cùng nguồn và quyết định duyệt; lặp lại/tiếp tục không tạo trùng hoặc âm thầm trộn phiên bản khác.
- **FR-009**: MUST nạp vào khu vực dữ liệu riêng chưa phục vụ người dùng; chỉ chuyển thử sau validation và duyệt rõ ràng. Không reset/import vào DB gốc/demo trong workflow này.
- **FR-010**: MUST có báo cáo đối soát tổng nguồn, accepted/quarantined/rejected, các mapping Việt chưa giải quyết, dung lượng/thời gian thực đo và trạng thái hoàn tất từng nhóm dữ liệu.
- **FR-011**: MUST giữ F06 exact-first, fallback khi Sidecar unavailable và F09 POS/restriction checks/ambiguity; importer không thay engine bằng đáp án corpus.
- **FR-012**: MUST bảo toàn API/extension compatibility, Unicode, privacy và stale-response protection; không đưa toàn bộ từ điển vào extension hoặc yêu cầu đăng nhập để tra từ.
- **FR-013**: MUST kiểm tra dữ liệu thật độc lập với importer, có negative cases cho restriction, no-kanji reading, sai semantic mapping và pending/rejected nghĩa Việt.
- **FR-014**: MUST có thông tin nguồn/giấy phép truy cập được từ giao diện và hướng dẫn cập nhật; không suy ra license dữ liệu từ license mã nguồn/mirror.

### Key Entities *(include if feature involves data)*

- **Bộ nguồn cố định**: nguồn, phiên bản, checksum, license, attribution và quyết định được phép dùng.
- **Mục từ/kanji chuẩn**: canonical identity, facts và các ràng buộc áp dụng, không lấy chuỗi dịch làm ID.
- **Quyết định liên kết/override**: record nguồn, đích entry/sense/form/reading, lý do, reviewer và revision.
- **Lần nhập/báo cáo đối soát**: inputs, tiến độ, thống kê, lỗi, checksum facts và điều kiện được thử sử dụng.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% entry từ vựng và ký tự kanji hợp lệ trong nguồn đã duyệt được kiểm đếm/nạp; không lỗi identity/restriction chưa giải quyết trước khi đánh dấu bộ dữ liệu hoàn tất.
- **SC-002**: 100% record nghĩa Việt và overrides V1 được đối soát; mọi trường hợp không phục vụ có lý do và danh sách chủ dự án duyệt. Không gọi phạm vi hoàn tất nếu thiếu cả nhóm nghĩa Việt hoặc còn bỏ sót chưa thống kê.
- **SC-003**: 100% ca kiểm tra độc lập được chủ dự án duyệt trả đúng identity, reading và nghĩa áp dụng; gồm từ ngoài demo, restriction, kanji và F09.
- **SC-004**: Hai lần tái tạo từ cùng inputs cho cùng canonical IDs/facts checksum; test chạy lại và resume không có bản ghi trùng.
- **SC-005**: DB gốc và demo giữ nguyên nội dung sau kiểm tra thành công/thất bại/quay lại cấu hình cũ; không có secret hoặc selected text trong logs/evidence mặc định.
- **SC-006**: Mỗi nhóm nguồn có provenance/attribution, số lượng và dung lượng thực đo; tất cả gate chưa đạt được báo rõ, không suy từ HTTP 200 thành nghiệm thu dữ liệu đầy đủ.

## Assumptions

- “Như V1” là đủ ba nhóm dữ liệu và tri thức đúng đã có, không sao chép lỗi hoặc kiến trúc IndexedDB cũ; không hứa mọi entry đều có nghĩa Việt nếu nguồn không có hoặc mapping chưa đủ bằng chứng.
- Nguồn chính dự kiến: JMdict Japanese/English đầy đủ, KANJIDIC2; nguồn nghĩa Việt V1 là ứng viên cần review riêng. Số lượng theo pinned artifacts, không lấy con số lịch sử làm số hiện tại.
- User là người duyệt corpus/mapping và nghiệm thu; approval nguồn được lưu riêng, không giả lập chữ ký hay ngày duyệt.
- Phụ thuộc F06/F07/F09 và Docker PostgreSQL/Sudachi hiện có. Đây là import thử local, chưa phải production KnowledgeRelease/F20; không mở rộng sang hosting, sentence segmentation, dịch máy hoặc học từ.

### Constitutional Impact

- Áp dụng Constitution §§2–5, 7–10: server-authoritative, identity/restrictions, reviewed/versioned knowledge, provenance/licensing, privacy và human gates. Không đề xuất ngoại lệ.
- Truy vết: REQUIREMENT `ID-001–006`, `VOC-001–008`, `KAN-001–002`, `CONJ-001–002`, `DATA-001–005`, `PRIV-*`, `SEC-002`; MIGRATION_DECISION §§5.1–5.2, 5.5, 6.2, 7.2.
- Xem [research](research.md) cho bằng chứng V1/V2 và nguồn; production publication vẫn cần gate F20 riêng.
