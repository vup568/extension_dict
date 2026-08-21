# FEATURE SPEC: Dictionary Lookup API
# Version: 0.1.0 (DRAFT) | Owner: @lead-dev | Date: 2026-08-17
# Inherits: .sdd/constraints/global.md, .sdd/constraints/business.md, .sdd/constraints/safety.md
# Target Stack: .NET 10 (C#) Backend, SQL Server (MSSQL), SQLite (In-Memory Tests)

--------------------------------------------------------------------------------

## 1. Context & Goal
### Business Problem:
Người học tiếng Nhật khi đọc văn bản thực tế phải tra cứu qua lại giữa nhiều công cụ rời rạc (từ điển, bộ phân tích từ loại, bộ biến đổi từ) gây đứt gãy luồng tập trung [REQ 48]. Phiên bản V1 giải quyết bằng cách nhét toàn bộ dữ liệu từ điển (>100MB) xuống client (IndexedDB), khiến Extension khởi động vô cùng chậm chạp và ngốn dung lượng bộ nhớ lớn [MIGRATION 7.1, 7.3].

### Feature Goal:
Xây dựng một API tra cứu từ điển tập trung (Server-Authoritative) trên Backend .NET 10 [MIGRATION 6.3, 15]. API này sẽ thực hiện tra cứu từ vựng tiếng Nhật (JMdict), trả về kết quả tiếng Việt ưu tiên (kèm âm đọc Hiragana/Katakana) nhanh chóng dưới 200ms để phục vụ tính năng popup thời gian thực của Extension và Web App [REQ P-03, P-04, VOC-003, VOC-004, PERF-002].

### Success Metrics:
*   Độ trễ phản hồi Backend (P95 Latency) cho tác vụ tra cứu từ đơn lẻ < 200ms [REQ PERF-002].
*   Độ bao phủ của dữ liệu đạt 100% các từ thuộc tập dữ liệu canonical JMdict đã nạp [REQ DATA-003].

--------------------------------------------------------------------------------

## 2. Actors & Roles

| Actor | Mô tả | Quyền hạn trong Feature |
| :--- | :--- | :--- |
| **Guest** (Unauthenticated) | Người dùng vãng lai sử dụng Extension hoặc Website để đọc báo/văn bản [REQ AUTH-001]. | Được phép thực hiện tra cứu từ điển không giới hạn qua API (Anonymous Lookup) [MIGRATION 9.10]. |
| **System** (Automated) | Sidecar Tokenizer Service (Go/Python) hoặc các tiến trình nền [MIGRATION 17, 8.2]. | Thực hiện phân tích morphology và gửi dữ liệu token chuẩn hóa sang Backend .NET để tra cứu [MIGRATION 6.5]. |

*Lưu ý:* Tính năng tra cứu từ điển thuộc lớp **Anonymous Reading Features** [REQ AUTH-001], tuyệt đối **SHALL NOT** yêu cầu đăng nhập tài khoản để đảm bảo trải nghiệm đọc mượt mà nhất [3.1, REQ AUTH-001].

--------------------------------------------------------------------------------

## 3. Functional Requirements (EARS Notation)

### 3.1. Exact Vocabulary Lookup (Tra cứu từ chuẩn hóa)

*   **EARS[Event]:** WHEN Guest gửi yêu cầu tra cứu với một từ tiếng Nhật dạng chuẩn hóa (Dictionary Form) qua endpoint `GET /api/v2/dictionary/lookup?q={query}`, 
    **THE system SHALL** thực hiện tìm kiếm chính xác trong bảng từ điển và trả về danh sách các mục từ vựng (JMdict Entries) khớp với từ khóa [REQ VOC-001].

*   **EARS[Ubiquitous]:** THE system SHALL trả về đầy đủ các thông tin ngữ nghĩa ngôn ngữ học của từ khóa được cung cấp bởi nguồn JMdict canonical, bao gồm [MIGRATION 5.1]:
    1.  Mã định danh duy nhất của nguồn gốc từ điển (`UpstreamEntryId` - ví dụ: JMdict Sequence ID) [REQ 73].
    2.  Dạng viết (Kanji/Orthography nếu có - `WrittenForms`) [MIGRATION 5.1].
    3.  Cách đọc (Kana/Furigana - `Readings`) [MIGRATION 5.1, REQ VOC-003].
    4.  Các nét nghĩa dịch (Senses) ưu tiên hiển thị bản dịch tiếng Việt (`VietnameseSenses`), nếu không có bản dịch tiếng Việt thì fallback sang bản dịch tiếng Anh (`EnglishSenses`) [REQ P-02, VOC-004].
    5.  Phần loại từ (Parts of Speech - ví dụ: n, vs, v5-v) và các giới hạn về dạng viết hoặc cách đọc đi kèm (`LinguisticRestrictions`) [MIGRATION 5.1, REQ 73].

*   **EARS[Ubiquitous]:** THE system SHALL bảo toàn nguyên vẹn mối quan hệ ràng buộc ngữ nghĩa (Match Provenance) trong kết quả trả về, tuyệt đối **SHALL NOT** trộn lẫn hoặc làm biến dạng nghĩa của các dạng viết và cách đọc bị giới hạn bởi nguồn dữ liệu gốc [REQ 73, 74].

### 3.2. Deinflection & Morphological Resolution (Xử lý biến đổi từ)

*   **EARS[Event]:** WHEN Guest gửi yêu cầu tra cứu với một từ tiếng Nhật đang ở dạng chia/biến đổi (ví dụ: 食べました, 高くなかった) [MIGRATION 9.4],
    **THE system SHALL** gọi qua lớp Adapter để yêu cầu Sidecar Tokenizer phân tích morphology để lấy dạng gốc (Base Form / Dictionary Form: 食べる, 高い) [MIGRATION 6.5, 17], sau đó tiến hành tra cứu từ điển theo dạng gốc và đồng thời giải thích rõ quy trình biến đổi từ (Conjugation Explanation) bằng tiếng Việt [REQ CONJ-001, CONJ-002].

--------------------------------------------------------------------------------

## 4. Non-Functional Requirements

### 4.1. Hiệu năng & Độ trễ (Performance Targets)
*   **SLO-001:** P95 response time cho endpoint tra cứu từ đơn lẻ phải **< 200ms** trong điều kiện kết nối mạng LAN/Intranet bình thường giữa backend và database [REQ PERF-002].
*   **SLO-002:** API phải chịu được tải tối thiểu **500 Requests Per Second (RPS)** với tỷ lệ lỗi (Error Rate) < 0.1%.

### 4.2. Khả năng mở rộng & Bản quyền (Extensibility & IP)
*   **EXT-001 (Linguistic Isolation):** Lớp domain nghiệp vụ của .NET Backend tuyệt đối **SHALL NOT** được phép tham chiếu hoặc sử dụng trực tiếp các nhãn từ loại thô (Raw Tokenizer Labels) của các thư viện bên thứ ba [MIGRATION 6.5, 17]. Mọi giao tiếp phải thông qua cấu trúc dữ liệu trung gian chuẩn hóa [MIGRATION 6.5].
*   **EXT-002 (Internationalization-Ready):** Cấu trúc dữ liệu đầu ra của API phải phân tách rạch ròi giữa mã ngôn ngữ (Language Tags: `vi`, `en`) để sẵn sàng hỗ trợ đa ngôn ngữ trong tương lai mà không phải đập đi xây lại database entities [REQ P-02, I18N-004].

--------------------------------------------------------------------------------

## 5. Data Model (SQL Server Schema - Conceptual)

Để đảm bảo hiệu năng đọc cực lớn (Read-Heavy), database SQL Server sẽ lưu trữ dữ liệu từ điển canonical thông qua các bảng được tối ưu hóa chỉ mục (Indexes) bằng công cụ SSMS:

```sql
-- 1. Bảng lưu mục từ vựng gốc (JMdict Entry)
CREATE TABLE jmdict_entries (
    id INT IDENTITY(1,1) PRIMARY KEY,
    sequence_id INT NOT NULL UNIQUE, -- ID canonical từ upstream JMdict
    created_at DATETIME2 DEFAULT GETUTCDATE()
);
CREATE NONCLUSTERED INDEX idx_jmdict_seq ON jmdict_entries(sequence_id);

-- 2. Bảng lưu các dạng viết (Orthography/Kanji)
CREATE TABLE jmdict_written_forms (
    id INT IDENTITY(1,1) PRIMARY KEY,
    entry_id INT NOT NULL FOREIGN KEY REFERENCES jmdict_entries(id),
    value NVARCHAR(100) NOT NULL, -- Ví dụ: 食べる, 漢字
    is_primary BIT NOT NULL DEFAULT 0,
    CONSTRAINT fk_written_entry FOREIGN KEY (entry_id) REFERENCES jmdict_entries(id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX idx_written_val ON jmdict_written_forms(value);

-- 3. Bảng lưu các cách đọc (Readings/Kana)
CREATE TABLE jmdict_readings (
    id INT IDENTITY(1,1) PRIMARY KEY,
    entry_id INT NOT NULL,
    value NVARCHAR(100) NOT NULL, -- Ví dụ: たべる, かんじ
    is_primary BIT NOT NULL DEFAULT 0,
    CONSTRAINT fk_reading_entry FOREIGN KEY (entry_id) REFERENCES jmdict_entries(id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX idx_reading_val ON jmdict_readings(value);

-- 4. Bảng lưu các nét nghĩa (Senses) phân tách theo ngôn ngữ
CREATE TABLE jmdict_senses (
    id INT IDENTITY(1,1) PRIMARY KEY,
    entry_id INT NOT NULL,
    language_code VARCHAR(10) NOT NULL DEFAULT 'vi', -- 'vi' hoặc 'en' [REQ P-02]
    gloss NVARCHAR(MAX) NOT NULL, -- Nghĩa dịch phân tách bằng dấu chấm phẩy
    pos_tags VARCHAR(255) NULL, -- Các nhãn từ loại (dạng chuỗi)
    CONSTRAINT fk_sense_entry FOREIGN KEY (entry_id) REFERENCES jmdict_entries(id) ON DELETE CASCADE
);
CREATE NONCLUSTERED INDEX idx_sense_lang ON jmdict_senses(entry_id, language_code);
```

--------------------------------------------------------------------------------

## 6. Error Handling (Unwanted Patterns)

*   **EARS[Unwanted]:** WHERE từ khóa cần tra cứu `q` rỗng hoặc có độ dài vượt quá 255 ký tự, 
    **THE system SHALL** trả về mã lỗi **HTTP 400 Bad Request** kèm cấu trúc lỗi JSON máy có thể đọc [MIGRATION 6.12, 22]:
    ```json
    {
        "error_code": "INVALID_QUERY_PARAMETER",
        "message": "Từ khóa tra cứu không được trống và không được vượt quá 255 ký tự.",
        "request_id": "0HMV893..."
    }
    ```

*   **EARS[Unwanted]:** WHERE từ khóa `q` không tìm thấy bất kỳ kết quả khớp nào trong cơ sở dữ liệu, 
    **THE system SHALL** trả về mã trạng thái **HTTP 200 OK** với một danh sách rỗng `[]` để tránh làm vỡ giao diện popup và giảm thiểu logic xử lý biệt lệ không cần thiết ở Frontend client.

*   **EARS[Unwanted]:** WHERE kết nối đến SQL Server bị mất hoặc truy vấn bị quá thời gian (Timeout > 5 giây) [REQ PERF-004], 
    **THE system SHALL** trả về mã lỗi **HTTP 503 Service Unavailable** [MIGRATION 9.9], tự động ghi log lỗi chi tiết kèm `request_id` lên hệ thống giám sát và hiển thị thông điệp an toàn cho người dùng:
    ```json
    {
        "error_code": "DATABASE_TEMPORARILY_UNAVAILABLE",
        "message": "Hệ thống tra cứu đang bận. Vui lòng thử lại sau giây lát.",
        "request_id": "0HMV942..."
    }
    ```

--------------------------------------------------------------------------------

## 7. Acceptance Criteria (Điều kiện Nghiệm thu)

*   **AC-001 (Exact Match):** Gọi `GET /api/v2/dictionary/lookup?q=日本語` trả về HTTP 200, danh sách chứa ít nhất một mục từ có `sequence_id` khớp với upstream, hiển thị chữ Kanji "日本語", cách đọc "にほんご", và nghĩa tiếng Việt "Tiếng Nhật".
*   **AC-002 (Language Priority):** Tra cứu từ `猫` trả về bản dịch nghĩa tiếng Việt (`vi`) trước tiên. Nếu từ điển chỉ có nghĩa tiếng Anh, hệ thống phải tự động fallback sang trả về nghĩa tiếng Anh (`en`).
*   **AC-003 (Empty State):** Tra cứu một từ vô nghĩa (ví dụ: `asdfghjkl`) trả về danh sách trống `[]` và HTTP 200.
*   **AC-004 (Query Validation):** Gửi request rỗng (`q=`) hoặc query quá dài (>255 ký tự) lập tức bị chặn ở tầng HTTP routing và trả về HTTP 400 cùng JSON error chuẩn hóa.
*   **AC-005 (Structured Error):** Mọi response có HTTP Status >= 400 bắt buộc phải tuân thủ đúng cấu trúc JSON `{ error_code, message, request_id }` [MIGRATION 6.12]. Tuyệt đối không được để lộ stack trace hoặc cấu trúc thư mục của server ra ngoài [3.2].

--------------------------------------------------------------------------------

## 8. Out of Scope (Nằm ngoài phạm vi MVP)

Để đảm bảo tiến độ dự án, các tính năng sau đây nghiêm cấm tuyệt đối **SHALL NOT** được phép triển khai trong Sprint này [REQ 70]:
*   Tính năng gợi ý từ thông minh khi gõ (AutoComplete Suggestions) hoặc tìm kiếm mờ (Fuzzy/FTS Search nâng cao).
*   Chức năng lưu từ vựng vào thư mục cá nhân (My Vocabulary) — vì tính năng này đòi hỏi hạ tầng Authentication phức tạp, sẽ làm ở các sprint sau [MIGRATION 18].
*   Cơ chế đồng bộ hóa dữ liệu từ điển ngoại tuyến (Offline Sync/Cache) xuống Extension Client [REQ 70].

--------------------------------------------------------------------------------

## 9. Notes & Open Questions (Thảo luận làm rõ - Human Verification)

Để hoàn tất quy trình SDD Pha 1 và chính thức "khóa" bản đặc tả này lên phiên bản v1.0.0, bạn vui lòng cho tôi xin ý kiến phản hồi về **3 câu hỏi lớn** sau đây:

1.  **Về Cấu trúc Dữ liệu nghĩa dịch (Senses):** Trong database canonical JMdict, một từ có thể có rất nhiều nét nghĩa và mỗi nghĩa lại tương ứng với các từ loại (nouns, verbs) khác nhau. Đối với MVP, chúng ta có nên **gộp chung (collapse)** tất cả các nét nghĩa dịch thành một chuỗi duy nhất để Frontend dễ hiển thị, hay **giữ nguyên phân cấu trúc dạng mảng (Array of Senses)** để sau này làm tính năng lưu từ vựng (My Vocabulary) có thể chọn lưu cụ thể nét nghĩa mong muốn? *(Khuyến nghị: Giữ cấu trúc dạng mảng để bảo vệ tính toàn vẹn dữ liệu [REQ 73]).*
2.  **Về API Versioning:** API Endpoint trong spec này tôi đang để tạm là `/api/v2/dictionary/lookup`. Bạn có muốn tuân thủ đúng chuẩn API-First của Hiến pháp là cấu hình định dạng API theo tiền tố `/api/v2/...` hay đổi sang một quy ước định tuyến khác?
3.  **Về Thư viện Tokenizer Sidecar:** Ở bước tra cứu từ biến đổi (Deinflection - Mục 3.2), .NET Backend sẽ gọi sang một Sidecar API để bóc tách từ loại. Bạn có dự định dựng Sidecar này bằng ngôn ngữ nào (Go hay Python) và sử dụng tokenizer nào (MeCab, Sudachi hay IPADIC) cho môi trường Dev cục bộ trước? *(Khuyến nghị: Go + Sudachi/MeCab vì cực nhẹ và khởi chạy container tức thời dưới 1 giây [MIGRATION 8.2]).*
