# JP Reading Platform V2 — Feature Map & Use Case Inventory

> **Analysis Date**: 2026-08-21  
> **Methodology**: Evidence-based extraction from source-of-truth documents only  
> **Status**: Official Reference Document (`docs/FEATURE_MAP.md`)

---

## 1. Source of Truth Reviewed

| Document | Path | Role | Version/Status |
|---|---|---|---|
| **REQUIREMENT.md** | `REQUIREMENT.md` | Product behavior & scope authority | v2.1.0-draft, Pending Re-approval |
| **AGENTS.md** | `AGENTS.md` | Approved engineering baseline & execution protocol | Active (v2.0.0) |
| **SDD.md** | `SDD.md` | Project Memory — ADR, Lessons Learned | Active |
| **MIGRATION_DECISION.md** | `MIGRATION_DECISION.md` | Legacy asset disposition authority | v1.0.0 Approved (Updated §19) |
| **Constitution** | `.sdd/constitution.md` | Project-wide invariants & governance | v1.0.0 Ratified 2026-08-13 |
| **DATABASE.md** | `DATABASE.md` | Database design central doc | Active |
| **Schema Overview** | `docs/database/schema-overview.md` | Table roles & relationships | Active |
| **feat-platform-foundation SPEC** | `.sdd/specs/feat-platform-foundation/SPEC.md` | Platform boundary specification | Draft |
| **feat-dict-lookup SPEC** | `.sdd/specs/feat-dict-lookup/SPEC.md` | Dictionary lookup API specification | Draft v0.1.0 (Updated to PostgreSQL 16) |
| **feat-auth SPEC** | `.sdd/specs/feat-auth/SPEC.md` | Auth specification | **Empty** — Pending specification |

---

## 2. Project Goals & Actors

### 2.1 Problem Being Solved
**Evidence**: `REQUIREMENT.md` §1.2  
Người học tiếng Nhật khi đọc văn bản thực tế phải chuyển qua nhiều công cụ (tra từ, tra kanji, tìm dạng gốc, nhận diện ngữ pháp, dịch câu, lưu nội dung) gây đứt gãy luồng tập trung.

### 2.2 Product Identity
**Evidence**: `REQUIREMENT.md` §1.1  
> JP Reading Platform là **Japanese Reading Assistant + Learning Platform**, không chỉ là một dictionary extension.

### 2.3 Product Goals

| Goal ID | Goal | Expected Outcome | Evidence |
|---|---|---|---|
| G-01 | Reading First | Giảm thao tác và chuyển context khi đọc | REQUIREMENT.md §1.3 |
| G-02 | Linguistic Correctness | Kết quả giữ đúng identity, restriction và occurrence | REQUIREMENT.md §1.3 |
| G-03 | Shared Semantics | Extension và Web dùng cùng knowledge layer | REQUIREMENT.md §1.3 |
| G-04 | Optional Learning | Tra cứu ẩn danh; đăng nhập chỉ khi lưu dữ liệu cá nhân | REQUIREMENT.md §1.3 |
| G-05 | Vietnamese First | Người Việt nhận trải nghiệm chính bằng tiếng Việt | REQUIREMENT.md §1.3 |
| G-06 | Safe Evolution | Provider, client và content có thể thay đổi mà không phá identity | REQUIREMENT.md §1.3 |

### 2.4 Actors

| Actor | Type | Evidence |
|---|---|---|
| Anonymous Reader | Primary user | REQUIREMENT.md §3 |
| Authenticated Learner | Primary user | REQUIREMENT.md §3 |
| Product Owner | Governance | REQUIREMENT.md §3 |
| Browser Host Page | External system | REQUIREMENT.md §3 |
| Browser Extension | Client system | REQUIREMENT.md §3 |
| Web Application | Client system | REQUIREMENT.md §3 |
| Backend | Authoritative system | REQUIREMENT.md §3 |
| Translation Provider | External provider | REQUIREMENT.md §3 |
| Tokenizer Sidecar | External runtime | REQUIREMENT.md §3 |
| Authentication Provider | External provider | REQUIREMENT.md §3 |
| Data Operator / Linguistic Editor | Operational | REQUIREMENT.md §3 |
| System Operator | Operational | REQUIREMENT.md §3 |
| Anki / Quizlet | External destination | REQUIREMENT.md §3 |

---

## 3. Comprehensive Feature Map (F-01 to F-22)

### F-01: Japanese Text Detection
* **Goal**: Xác định chính xác văn bản tiếng Nhật từ selection, tránh false positives.
* **Actor**: Browser Extension.
* **Trigger**: User tạo selection trên trang web.
* **Main flow**: Extension kiểm tra selection bằng Unicode-aware detector → accept Japanese text hợp lệ (gồm supplementary CJK) → reject Hangul, emoji, unrelated scripts.
* **Constraints**: Must be Unicode-correct; supplementary ideographs `𠮟` must be accepted; Hangul/emoji/musical symbols must be rejected; no broad code-point ranges.
* **MVP Status**: **Must** (JPN-001).

### F-02: Ruby-Safe DOM Extraction
* **Goal**: Trích xuất base text tiếng Nhật từ DOM mà không bị furigana annotation (`rt`/`rp`) làm nhiễu.
* **Actor**: Browser Extension.
* **Trigger**: Sau khi Japanese text detection (F-01) thành công.
* **Main flow**: Extension trích text từ DOM → loại bỏ nội dung thẻ `rt` và `rp` → bảo toàn nguyên vẹn base text.
* **Constraints**: `rt` và `rp` content **SHALL NOT** xuất hiện trong linguistic input gửi đi phân tích.
* **MVP Status**: **Must** (DOM-001).

### F-03: Auto Popup on Selection
* **Goal**: Tự động hiển thị popup khi chọn text tiếng Nhật, không cần click icon/hotkey.
* **Actor**: Anonymous Reader via Extension.
* **Trigger**: Selection ổn định chứa Japanese text hợp lệ.
* **Main flow**: Selection hợp lệ → debounce (250–400ms) → render popup shell + loading state → gửi analysis request → hiển thị kết quả.
* **Constraints**: Shadow DOM isolation khỏi host CSS; debounce 250–400ms; popup shell xuất hiện trước khi nhận response.
* **MVP Status**: **Must** (EXT-001, EXT-002, EXT-003, BROWSER-003), **Should** (EXT-004 debounce).

### F-04: Stale Response Protection & Cancellation
* **Goal**: Đảm bảo response bất đồng bộ cũ không ghi đè state của interaction mới.
* **Actor**: Extension, Web Application.
* **Trigger**: User tạo selection/action mới khi request cũ đang xử lý trên đường truyền.
* **Main flow**: Gửi request kèm correlation/request identity → khi interaction mới bắt đầu, cancel request cũ qua `AbortController` → response cũ (nếu đến muộn) bị từ chối cập nhật state.
* **Constraints**: Logic đúng đắn không được phụ thuộc duy nhất vào cancellation (ASYNC-002).
* **MVP Status**: **Must** (ASYNC-001, ASYNC-002), **Should** (EXT-005 cancellation).

### F-05: Unified Analysis Request
* **Goal**: Gửi 1 request hợp nhất cho selection thay vì nhiều request riêng lẻ.
* **Actor**: Extension, Backend.
* **Trigger**: Selection ổn định hợp lệ.
* **Main flow**: Extension gửi 1 envelope → Backend xử lý hợp nhất → trả vựng, kanji, morphology, conjugation, grammar trong cùng 1 response.
* **Constraints**: Phải hỗ trợ partial result nếu 1 phần provider gặp sự cố (NET-006).
* **MVP Status**: **Must** (EXT-006).

### F-06: Vocabulary Lookup (Exact + Deinflection)
* **Goal**: Tra cứu từ vựng chính xác theo form/reading chuẩn hóa, resolve dạng biến đổi về dạng gốc.
* **Actor**: Anonymous Reader, Backend.
* **Trigger**: Analysis request chứa Japanese word/phrase.
* **Main flow**: Normalize input → nếu là dạng chia thì resolve base form qua Tokenizer Sidecar → tra cứu DB → trả kết quả kèm reading, nghĩa dịch (Vietnamese ưu tiên), POS, match provenance.
* **Constraints**: Vietnamese meaning được ưu tiên (VOC-004); English fallback (VOC-005); bảo toàn match provenance (VOC-007); không gán sai nghĩa cho form/reading hạn chế (VOC-008).
* **MVP Status**: **Must** (VOC-001–004, VOC-007–008), **Should** (VOC-005, VOC-006).

### F-07: Kanji Information
* **Goal**: Hiển thị canonical information cho từng ký tự kanji xuất hiện trong analyzed text.
* **Actor**: Anonymous Reader, Backend.
* **Trigger**: Analyzed text chứa ký tự kanji.
* **Main flow**: Tách kanji unique → lookup `kanji_records` → trả onyomi, kunyomi, nghĩa tiếng Việt, Hán Việt, stroke count, grade, và cờ provenance cho JLPT level.
* **Constraints**: Phân biệt rõ authoritative source value (grade, stroke) vs derived/approximate value (JLPT level) (KAN-002).
* **MVP Status**: **Must** (KAN-001, KAN-002).

### F-08: Grammar Detection & Occurrence
* **Goal**: Phát hiện grammar patterns trong input, lưu giữ từng occurrence identity và offset span.
* **Actor**: Anonymous Reader, Backend.
* **Trigger**: Analysis request.
* **Main flow**: Input text → grammar engine phân tích theo rule patterns (N5–N4) → trả về từng occurrence kèm canonical grammar ID và unambiguous span.
* **Constraints**: Dùng sentence context khi safe (GRM-001); N5–N4 coverage (GRM-003); repeated occurrences không bị collapse (GRM-008); meaningful overlaps phải biểu diễn được.
* **MVP Status**: **Must** (GRM-002, GRM-003, GRM-005, GRM-007, GRM-008).

### F-09: Conjugation Explanation
* **Goal**: Giải thích các bước biến đổi ngữ pháp từ dạng observed về dạng gốc (base form).
* **Actor**: Anonymous Reader, Backend.
* **Trigger**: Word unit nhận diện được là dạng chia biến đổi.
* **Main flow**: Phân tích quy tắc biến đổi (ví dụ: 食べました → 食べる + ます + た) → hiển thị chuỗi giải thích ngữ pháp bằng tiếng Việt.
* **Constraints**: Ưu tiên giải thích bằng tiếng Việt (CONJ-002).
* **MVP Status**: **Should** (CONJ-001), **Must** (CONJ-002).

### F-10: On-Demand Translation
* **Goal**: Dịch đoạn văn JP → VI / EN theo hành động yêu cầu chủ động (explicit) của người dùng.
* **Actor**: Anonymous Reader, Backend, Translation Provider.
* **Trigger**: Người dùng click nút "Dịch" trong popup/web.
* **Main flow**: User click → Client gửi translation request → Backend điều phối qua provider boundary → trả kết quả dịch kèm nhãn ngôn ngữ và privacy disclosure.
* **Constraints**: Chỉ thực hiện sau explicit user action (TRN-004); provider boundary độc lập (TRN-002); không để credential ở client (TRN-003); hiển thị thông báo quyền riêng tư (PRIV-004).
* **MVP Status**: **Must** (TRN-001–TRN-004).

### F-11: Web Application — Japanese Search/Analyze
* **Goal**: Cung cấp giao diện tra cứu/phân tích văn bản tiếng Nhật trực tiếp trên trang Web.
* **Actor**: Anonymous Reader, Web Application.
* **Trigger**: Người dùng nhập văn bản vào thanh tìm kiếm trên Web App.
* **Main flow**: Input validation → gửi API request tới chung Backend với Extension → hiển thị kết quả từ vựng, kanji, ngữ pháp, biến đổi từ và dịch thuật.
* **Constraints**: Dùng chung Backend capabilities với Extension (WEB-002); báo lỗi chuẩn hóa nếu input không hợp lệ (WEB-001).
* **MVP Status**: **Must** (WEB-001–WEB-003).

### F-12: Web Application — Grammar Library
* **Goal**: Duyệt thư viện ngữ pháp theo cấp độ JLPT (N5, N4), tìm kiếm pattern, xem chi tiết.
* **Actor**: Anonymous Reader, Web Application.
* **Trigger**: Người dùng truy cập mục "Thư viện Ngữ pháp".
* **Main flow**: Lựa chọn JLPT N5/N4 → tìm kiếm mẫu ngữ pháp → xem chi tiết (cấu trúc, ý nghĩa tiếng Việt, ví dụ mẫu, lưu ý).
* **Constraints**: Phải hiển thị đúng các trường dữ liệu hiện có; không làm mất chi tiết tiếng Việt nếu thiếu bản dịch tiếng Anh (WEB-005).
* **MVP Status**: **Must** (WEB-004), **Should** (WEB-005).

### F-13: Authentication (Anonymous Reading + Authenticated Learning)
* **Goal**: Tra cứu ẩn danh không cần account; chỉ bắt buộc đăng nhập khi lưu dữ liệu cá nhân; dùng chung identity giữa Extension và Web.
* **Actor**: Anonymous Reader, Authenticated Learner.
* **Trigger**: Tự động (ẩn danh); hoặc khi nhấn Save / Quản lý tài khoản (xác thực).
* **Main flow**: Đọc/tra cứu hoàn toàn không yêu cầu login → Khi nhấn Save: trả phản hồi yêu cầu auth → Điều hướng đăng nhập (Email/Password hoặc Google OAuth) → Tiếp tục thao tác save.
* **Constraints**: Tra cứu/đọc/dịch **SHALL NOT** yêu cầu login (AUTH-001); lưu dữ liệu yêu cầu auth (AUTH-002); đồng bộ identity giữa Extension và Web (AUTH-003); kiểm tra quyền sở hữu resource (AUTH-004); băm mật khẩu bằng BCrypt/Argon2id (SEC-005).
* **MVP Status**: **Must** (AUTH-001–AUTH-004).

### F-14: Save Vocabulary
* **Goal**: Lưu từ vựng vào danh sách học cá nhân (My Vocabulary).
* **Actor**: Authenticated Learner.
* **Trigger**: Người dùng nhấn icon/nút Save Vocabulary từ kết quả tra cứu.
* **Main flow**: Nhấn Save → Kiểm tra Auth (nếu ẩn danh → yêu cầu login) → Ghi nhận `learning_references` với canonical `dictionary_id` + `user_id` → Hiển thị trạng thái đã lưu.
* **Constraints**: Dùng canonical dictionary ID, không dùng text làm identity (LEARN-002); đảm bảo tính chống trùng lặp/idempotence (LEARN-007); áp dụng soft-delete (LEARN-008).
* **MVP Status**: **Must** (LEARN-001, LEARN-002, LEARN-007, LEARN-008).

### F-15: Save Grammar
* **Goal**: Lưu điểm ngữ pháp vào danh sách học cá nhân (My Grammar).
* **Actor**: Authenticated Learner.
* **Trigger**: Người dùng nhấn Save Grammar từ kết quả nhận diện ngữ pháp.
* **Main flow**: Nhấn Save → Kiểm tra Auth → Ghi nhận `learning_references` với canonical `grammar_id` + `user_id` → Hiển thị trạng thái đã lưu.
* **Constraints**: Sử dụng canonical grammar ID (ID-003); chống trùng lặp (LEARN-007); soft-delete (LEARN-008).
* **MVP Status**: **Must** (LEARN-003, LEARN-007, LEARN-008).

### F-16: My Vocabulary (View & Delete)
* **Goal**: Quản lý và xem danh sách từ vựng đã lưu trên Web App / Extension.
* **Actor**: Authenticated Learner.
* **Trigger**: Người dùng truy cập mục "Từ vựng của tôi".
* **Main flow**: Tải danh sách từ vựng active (`deleted_at IS NULL`) của user hiện tại → Người dùng có thể xem lại hoặc thực hiện xóa khỏi danh sách.
* **Constraints**: Chỉ hiển thị dữ liệu của user đang đăng nhập (LEARN-004, AUTH-004); thao tác xóa thực hiện theo cơ chế Soft Delete (LEARN-008).
* **MVP Status**: **Must** (LEARN-004, LEARN-006, LEARN-008).

### F-17: My Grammar (View & Delete)
* **Goal**: Quản lý và xem danh sách ngữ pháp đã lưu trên Web App / Extension.
* **Actor**: Authenticated Learner.
* **Trigger**: Người dùng truy cập mục "Ngữ pháp của tôi".
* **Main flow**: Tải danh sách ngữ pháp active của user hiện tại → Người dùng xem chi tiết hoặc thực hiện xóa.
* **Constraints**: Phân quyền tuyệt đối theo `user_id` (LEARN-005, AUTH-004); xóa bằng Soft Delete (LEARN-008).
* **MVP Status**: **Must** (LEARN-005, LEARN-006, LEARN-008).

### F-18: Quick Review
* **Goal**: Ôn tập nhanh các từ vựng và ngữ pháp đã lưu dưới dạng Flashcard/Lật đáp án.
* **Actor**: Authenticated Learner, Web Application.
* **Trigger**: Người dùng chọn chế độ "Ôn tập nhanh" (Quick Review).
* **Main flow**: Lấy danh sách mục đã lưu active → Hiển thị mặt trước (Từ/Mẫu ngữ pháp) → Người dùng click lật mặt sau để xem nghĩa và ví dụ → Chuyển sang mục tiếp theo.
* **Constraints**: Không chứa dữ liệu đã bị soft-delete (REV-001); ẩn đáp án trước khi người dùng chủ động xem (REV-002); **KHÔNG** phụ thuộc vào thuật toán SRS phức tạp trong MVP (REV-004).
* **MVP Status**: **Must** (REV-001, REV-002, REV-004), **May** (REV-003 tùy chọn nút Đã biết/Chưa biết).

### F-19: Export to Anki / Quizlet
* **Goal**: Xuất dữ liệu từ vựng/ngữ pháp đã lưu ra định dạng CSV/TSV chuẩn để import vào Anki/Quizlet.
* **Actor**: Authenticated Learner.
* **Trigger**: Người dùng nhấn "Xuất dữ liệu Anki/Quizlet" trên Web App.
* **Main flow**: Chọn danh sách cần xuất → Hệ thống tạo file UTF-8 CSV/TSV với các cột được format chuẩn (Front, Back, Reading, Example) → Trả về file tải xuống.
* **Constraints**: Định dạng UTF-8 mã hóa chuẩn xác chữ tiếng Nhật và tiếng Việt (EXP-004); không tự động sync 2 chiều với Anki (EXP-005); hoạt động độc lập không cần liên kết tài khoản Anki (EXP-001).
* **MVP Status**: **Must** (EXP-001–EXP-005).

### F-20: Knowledge Operations & Data Pipeline
* **Goal**: Quy trình nạp, kiểm thử, quản lý phiên bản và publish dữ liệu ngôn ngữ (JMdict, KANJIDIC2, Grammar) đảm bảo tính tái lập và nguồn gốc (provenance).
* **Actor**: Data Operator, System.
* **Trigger**: Khi chuẩn bị phát hành bản cập nhật dữ liệu từ điển/ngữ pháp mới.
* **Main flow**: Thu thập dataset gốc kèm metadata provenance → Chạy pipeline ETL tái lập → Validate schema & constraint → Lưu trữ `source_manifests` & `source_records` → Tạo `knowledge_releases` snapshot immutable → Publish bản mới (cập nhật `is_current = TRUE`).
* **Constraints**: Phải ghi nhận checksum, license, version (DATA-001); không phụ thuộc dataset thương mại đóng (DATA-002); pipeline phải reproducible (DATA-003); bản publish là immutable (DATA-005).
* **MVP Status**: **Must** (DATA-001–DATA-005).

### F-21: Shared API Contracts & Structured Errors
* **Goal**: Chuẩn hóa giao tiếp giữa Extension, Web App và Backend qua REST/Minimal APIs với định dạng phản hồi và lỗi thống nhất.
* **Actor**: Browser Extension, Web Application, Backend.
* **Trigger**: Mọi request giao tiếp client-server.
* **Main flow**: Client gửi request theo versioned API contract → Backend validate runtime → Trả về thành công hoặc lỗi chuẩn dạng structured JSON `{ error_code, message, request_id }`.
* **Constraints**: API contract provider-neutral (API-001); versioning rõ ràng `/api/v2/...` (API-002); lỗi HTTP >= 400 bắt buộc theo đúng định dạng structured error (API-004); không lộ stack trace internal (SEC-003).
* **MVP Status**: **Must** (API-001–API-004).

### F-22: Reliability, Operations & Security (Rate Limiting, Caching, Protection)
* **Goal**: Đảm bảo an toàn hệ thống, bảo vệ quyền riêng tư người dùng, xử lý timeout và chịu tải cao.
* **Actor**: Backend, System Operator.
* **Trigger**: Mọi truy vấn hệ thống.
* **Main flow**: Kiểm tra Rate Limit → Kiểm tra Cache → Xử lý logic → Ghi log telemetry (không chứa text bôi chọn của user) → Trả phản hồi an toàn mã hóa SSL/TLS.
* **Constraints**: Rate limit trả mã HTTP 429 kèm error code (RATE-001, RATE-002); sử dụng định danh ẩn danh không xâm phạm quyền riêng tư (RATE-003); **KHÔNG** lưu raw selected text của user vào log hệ thống mặc định (PRIV-002, PRIV-006); truyền tải qua HTTPS/SSL (SEC-001); không hardcode secret keys (SEC-002).
* **MVP Status**: **Must** (PRIV, SEC, RATE, PERF).

---

## 4. Use Case Inventory (UC-01 to UC-09)

### UC-01: Read and Analyze Japanese Text (Extension)
* **Actor**: Anonymous Reader
* **Preconditions**: Extension đã được cài đặt trên trình duyệt; Backend khả dụng.
* **Trigger**: User bôi chọn (select) một đoạn văn bản tiếng Nhật trên trang web bất kỳ.
* **Main flow**:
  1. User chọn text trên trang web.
  2. Extension phát hiện văn bản là tiếng Nhật hợp lệ (`JPN-001`).
  3. Extension trích xuất base text, loại bỏ các thẻ furigana `rt`/`rp` (`DOM-001`).
  4. Extension áp dụng debounce 250–400ms (`EXT-004`).
  5. Extension hiển thị khung Popup Shell kèm trạng thái Loading (`EXT-003`).
  6. Extension gửi Unified Analysis Request sang Backend (`EXT-006`).
  7. Backend phân tích và trả về tập dữ liệu gồm: Từ vựng, Kanji, Ngữ pháp, Dạng biến đổi.
  8. Popup hiển thị đầy đủ thông tin phân tích cho người dùng.
* **Alternative flows**:
  * *Text không phải tiếng Nhật*: Không bật popup (`EXT-002`).
  * *Backend Timeout / Mất mạng*: Popup hiển thị thông báo lỗi thân thiện kèm nút "Thử lại" (`NET-003`, `NET-005`).
  * *User chọn text mới liên tục*: Request cũ bị abort, popup cập nhật kết quả của request mới nhất (`ASYNC-001`).
* **Postconditions**: Người dùng xem được phân tích ngôn ngữ học mà không bị gián đoạn luồng đọc.

### UC-02: Translate Selected Text On-Demand
* **Actor**: Anonymous Reader
* **Preconditions**: Popup phân tích (hoặc kết quả Web) đang mở.
* **Trigger**: Người dùng chủ động nhấn vào mục / nút "Dịch".
* **Main flow**:
  1. Người dùng nhấn nút Dịch.
  2. Client gửi yêu cầu dịch thuật kèm ID phiên tương tác hiện tại.
  3. Backend gọi tới Translation Provider (qua Adapter layer) để lấy bản dịch.
  4. Khung dịch hiển thị đoạn dịch tiếng Việt (hoặc tiếng Anh) kèm thông báo bảo mật quyền riêng tư (`PRIV-004`).
* **Postconditions**: Đoạn dịch được hiển thị đúng ngữ cảnh; không tự động dịch khi chưa có thao tác click từ user.

### UC-03: Save Vocabulary to Personal Library
* **Actor**: Authenticated Learner (hoặc Anonymous Reader → được gợi ý Đăng nhập)
* **Preconditions**: Kết quả tra cứu từ vựng đang hiển thị.
* **Trigger**: Người dùng nhấn biểu tượng Save (Lưu từ vựng).
* **Main flow**:
  1. User nhấn Lưu từ vựng.
  2. Hệ thống kiểm tra trạng thái đăng nhập:
     * *Nếu chưa đăng nhập*: Hiển thị hộp thoại/thông báo yêu cầu đăng nhập. User tiến hành đăng nhập thành công.
  3. Hệ thống gửi yêu cầu lưu từ vựng kèm `user_id` và canonical `dictionary_id`.
  4. Backend kiểm tra chống trùng lặp (`LEARN-007`) và tạo bản ghi `learning_references`.
  5. Nút Save chuyển sang trạng thái "Đã lưu". Từ vựng xuất hiện trong danh sách "Từ vựng của tôi".
* **Postconditions**: Bản ghi học tập được lưu trữ an toàn và phân quyền riêng tư cho người dùng.

### UC-04: Save Grammar Rule to Personal Library
* **Actor**: Authenticated Learner
* **Preconditions**: Kết quả nhận diện ngữ pháp đang hiển thị.
* **Trigger**: Người dùng nhấn nút Save tại mục mẫu ngữ pháp.
* **Main flow**: Tương tự UC-03 nhưng liên kết với canonical `grammar_id`.
* **Postconditions**: Điểm ngữ pháp xuất hiện trong danh sách "Ngữ pháp của tôi".

### UC-05: Web Application Search & Sentence Analysis
* **Actor**: Anonymous Reader
* **Preconditions**: Trang Web Application đã được tải.
* **Trigger**: Người dùng dán/nhập đoạn văn tiếng Nhật vào ô tìm kiếm và nhấn Enter / Phân tích.
* **Main flow**:
  1. Web App kiểm tra dữ liệu đầu vào.
  2. Phân tích gửi tới chung Backend API như Extension.
  3. Trang hiển thị kết quả phân tích theo các tab/block: Từ vựng, Kanji, Mẫu ngữ pháp N5–N4, Biến đổi từ, và Khung dịch.
* **Postconditions**: Người dùng có giao diện xem chi tiết chuyên sâu trên màn hình lớn.

### UC-06: Browse & Search Grammar Library (Web)
* **Actor**: Anonymous Reader
* **Preconditions**: Truy cập trang Web Application.
* **Trigger**: Người dùng chuyển sang tab "Thư viện Ngữ pháp".
* **Main flow**:
  1. Người dùng lọc theo cấp độ JLPT N5 hoặc N4.
  2. Nhập từ khóa tìm kiếm mẫu ngữ pháp (ví dụ: `～てはいけない`).
  3. Chọn một cấu trúc để mở trang chi tiết: xem giải thích tiếng Việt, cách chia, ví dụ minh họa.
* **Postconditions**: Người dùng tra cứu và học lý thuyết ngữ pháp hệ thống.

### UC-07: Quick Review Saved Items
* **Actor**: Authenticated Learner
* **Preconditions**: Người dùng đã đăng nhập và có ít nhất 1 từ vựng/ngữ pháp đã lưu.
* **Trigger**: Người dùng nhấn chọn "Ôn tập nhanh" trên Web App.
* **Main flow**:
  1. Hệ thống ngẫu nhiên/sắp xếp danh sách từ vựng/ngữ pháp active của user.
  2. Hiển thị thẻ Flashcard (mặt trước: Chữ Kanji/Kana).
  3. Người dùng suy nghĩ và nhấn "Xem đáp án" để lật mặt sau (Nghĩa tiếng Việt, ví dụ).
  4. Người dùng nhấn "Tiếp theo" để chuyển thẻ.
* **Postconditions**: Người dùng củng cố lại kiến thức đã lưu một cách nhanh chóng.

### UC-08: Export Learning Data to Anki / Quizlet
* **Actor**: Authenticated Learner
* **Preconditions**: Người dùng có danh sách từ vựng/ngữ pháp trong kho cá nhân.
* **Trigger**: Người dùng nhấn nút "Xuất Anki (CSV)" trên trang quản lý.
* **Main flow**:
  1. Người dùng chọn phạm vi xuất (Tất cả từ vựng, hoặc nhóm chọn lọc).
  2. Hệ thống xuất file `.csv` chuẩn UTF-8 chứa các trường: `Word, Reading, Meaning, Example`.
  3. File được tải về máy tính người dùng.
  4. Người dùng mở Anki/Quizlet và Import file CSV vào bộ thẻ cá nhân.
* **Postconditions**: Dữ liệu học tập được trích xuất an toàn ra file tiêu chuẩn.

### UC-09: Process and Publish Knowledge Release (Data Ops)
* **Actor**: Data Operator / System Operator
* **Preconditions**: Coi như có bản cập nhật JMdict / KANJIDIC2 mới từ nguồn upstream.
* **Trigger**: Chạy tiến trình nạp dữ liệu định kỳ hoặc thủ công.
* **Main flow**:
  1. Thu thập file XML/JSON nguồn gốc.
  2. Khởi tạo `source_manifests` ghi nhận checksum, nguồn, license.
  3. Chạy pipeline chuyển đổi dữ liệu chuẩn hóa, ghi log các record bị từ chối (nếu có).
  4. Đưa dữ liệu vào snapshot `knowledge_releases`.
  5. Tiến hành kiểm thử tự động về tính toàn vẹn dữ liệu.
  6. Đánh dấu cờ `is_current = TRUE` để đưa phiên bản dữ liệu mới vào vận hành.
* **Postconditions**: Hệ thống Backend tra cứu dữ liệu mới mà không làm vỡ các ID canonical đã lưu của người dùng.

---

## 5. Feature Dependencies & User Journeys

### 5.1 Dependency Graph

```
F-01 (Japanese Detection)
  └→ F-02 (Ruby-Safe DOM Extraction)
       └→ F-03 (Auto Popup)
            ├→ F-04 (Stale Response Protection)
            └→ F-05 (Unified Analysis)
                 ├→ F-06 (Vocabulary Lookup) ←── Tokenizer Sidecar
                 ├→ F-07 (Kanji Info)
                 ├→ F-08 (Grammar Detection) ←── Tokenizer Sidecar + Grammar Data
                 ├→ F-09 (Conjugation) ←── Tokenizer Sidecar
                 └→ F-10 (Translation) ←── Translation Provider

F-11 (Web Search/Analyze) ──→ Dùng chung Backend với F-05

F-12 (Grammar Library) ──→ Dữ liệu Ngữ pháp (F-08/F-20)

F-13 (Authentication) ──→ Auth Provider (OD-002)
  ├→ F-14 (Save Vocabulary) ──→ F-06
  ├→ F-15 (Save Grammar) ──→ F-08
  ├→ F-16 (My Vocabulary) ──→ F-14
  ├→ F-17 (My Grammar) ──→ F-15
  ├→ F-18 (Quick Review) ──→ F-16, F-17
  └→ F-19 (Export) ──→ F-16, F-17

F-20 (Knowledge Operations) ──→ Tiền đề dữ liệu cho F-06, F-07, F-08
F-21 (Shared API Contracts) ──→ Tiền đề hạ tầng giao tiếp client-backend
F-22 (Reliability/Operations) ──→ Hạ tầng bảo mật & an toàn hệ thống
```

### 5.2 Core User Journeys

| Journey | Thứ tự các Features | Tương ứng |
|---|---|---|
| **Extension Reading Flow** | F-01 → F-02 → F-03 → F-04 → F-05 → (F-06 / F-07 / F-08 / F-09) → F-10 | UC-01, UC-02 |
| **Save Vocabulary Flow** | F-06 → F-13 (Auth check) → F-14 → F-16 | UC-03 |
| **Save Grammar Flow** | F-08 → F-13 (Auth check) → F-15 → F-17 | UC-04 |
| **Web Learning & Export** | F-11 → F-14/F-15 → F-16/F-17 → F-18 → F-19 | UC-05, UC-07, UC-08 |
| **Knowledge Publish Flow** | F-20 (Vận hành độc lập ở Backend) | UC-09 |

---

## 6. MVP Feature Scope Summary

Tất cả 22 tính năng trên đều nằm trong phạm vi **MVP (Minimum Viable Product)** đã được phê duyệt chính thức theo `REQUIREMENT.md` §7.1. 

| Nhóm chức năng | Danh sách Features trong MVP |
|---|---|
| **Client Extension** | F-01 (Detection), F-02 (Ruby-Safe), F-03 (Auto Popup), F-04 (Stale Protection), F-05 (Unified Analysis) |
| **Linguistic Backend** | F-06 (Vocabulary), F-07 (Kanji), F-08 (Grammar N5-N4), F-09 (Conjugation) |
| **Services & Ops** | F-10 (Translation), F-20 (Knowledge Ops), F-21 (Shared API), F-22 (Operations & Security) |
| **Web Application** | F-11 (Web Search), F-12 (Grammar Library) |
| **Auth & Learning** | F-13 (Auth), F-14 (Save Vocab), F-15 (Save Grammar), F-16 (My Vocab), F-17 (My Grammar), F-18 (Quick Review), F-19 (Anki Export) |

---

## 7. Open Questions / Missing Requirements

Dưới đây là danh sách 20 điểm mâu thuẫn / câu hỏi mở đã được trích xuất để chờ làm rõ (Product Owner & Lead Dev clarification):

| # | Mã OD / Điểm mở | Nội dung chi tiết | Tác động & Phụ thuộc |
|---|---|---|---|
| 1 | **OD-001** | Chọn Translation Provider cụ thể & phương án fallback | Quyết định chi phí & độ trễ của F-10 |
| 2 | **OD-002** | Chọn Auth Provider & luồng session recovery (hiện file `feat-auth/SPEC.md` đang **trống**) | **Blocker triển khai F-13, F-14, F-15** |
| 3 | **OD-003** | Quy ước chung cho Offset Span (UTF-16 code units hay Unicode code points?) | Ảnh hưởng tính chính xác highlight của F-08 |
| 4 | **OD-004** | Ngưỡng Rate-Limit chính thức & vòng đời định danh ẩn danh | An toàn hệ thống cho F-22 |
| 5 | **OD-005** | Profiling Benchmark & Latency SLO chính thức (P95 ≤ 800ms) | Tiêu chuẩn nghiệm thu Backend |
| 6 | **OD-006** | Chọn công nghệ Caching (Redis/Memory), TTL & quy tắc cache invalidation | Tối ưu hiệu năng Backend cho F-22 |
| 7 | **OD-007** | Định dạng chi tiết các cột khi Export ra Anki / Quizlet | **Blocker hoàn thiện F-19** |
| 8 | **OD-008** | Cấu trúc Matcher Metadata & chiến lược cắt câu an toàn (Safe Sentence Boundary) | Độ chính xác nhận diện ngữ pháp (F-08) |
| 9 | **OD-009** | Đánh giá bản quyền & chất lượng tập dữ liệu nghĩa dịch Việt (FVDP/OVDP) | Ảnh hưởng chất lượng nghĩa dịch (F-06) |
| 10 | **OD-010** | Ngôn ngữ & Thư viện cho Tokenizer Sidecar (Go/Python, Sudachi/MeCab) | **Blocker triển khai Deinflection (F-06, F-08, F-09)** |
| 11 | **OD-011** | Chế độ Shadow DOM (Open/Closed) & ứng xử vị trí Popup Extension | Giao diện Popup Extension (F-03) |
| 12 | **OD-012** | Thuật toán hiển thị & lưu trạng thái nút Đã biết/Chưa biết trong Quick Review | Logic ôn tập (F-18) |
| 13 | **OD-013** | Thời hạn lưu trữ thông tin xóa mềm (Soft-delete retention & purge policy) | Quản lý dữ liệu người dùng (F-16, F-17) |
| 14 | **OD-014** | Giới hạn độ dài tối đa của câu/đoạn văn bản bôi chọn | Tránh quá tải request & lãng phí tài nguyên |
| 15 | **Document Conflict** | `feat-dict-lookup/SPEC.md` tham chiếu MSSQL / SQLite | **Đã sửa** thành PostgreSQL 16 ngày 2026-08-21 |
| 16 | **Document Conflict** | `MIGRATION_DECISION.md` §19 coi Tech Stack là "non-decisions" | **Đã sửa** cập nhật các quyết định đã chốt ngày 2026-08-21 |
| 17 | **Missing Spec** | Luồng UX chi tiết khi người dùng Ẩn danh nhấn Save → yêu cầu Login → Redirect quay lại | Cần làm rõ cho F-13, F-14 |
| 18 | **Missing Spec** | Cấu trúc Routing / Navigation tổng thể của Web Application | Cần cho xây dựng Frontend Web App |
| 19 | **Missing Spec** | Thứ tự ưu tiên layout các section hiển thị trong Extension Popup | Cần cho xây dựng Frontend Extension |
| 20 | **Unclear Spec** | Xử lý khi 1 phần trong Unified Analysis bị lỗi (Partial Failure UI) | Cần quy ước hiển thị rõ ràng trên UI |

---

## 8. Consolidated Summary Table

| Feature ID | Tên Chức Năng | Đối tượng chính | Use Cases liên quan | Trạng thái MVP | Nguồn bằng chứng (Requirement ID) |
|---|---|---|---|---|---|
| **F-01** | Japanese Text Detection | Extension | UC-01 | **Must** | JPN-001 |
| **F-02** | Ruby-Safe DOM Extraction | Extension | UC-01 | **Must** | DOM-001 |
| **F-03** | Auto Popup on Selection | Reader, Extension | UC-01 | **Must** | EXT-001–004, BROWSER-003 |
| **F-04** | Stale Response Protection | Extension, Web App | UC-01, UC-02 | **Must** | ASYNC-001–002, EXT-005 |
| **F-05** | Unified Analysis Request | Extension, Backend | UC-01 | **Must** | EXT-006 |
| **F-06** | Vocabulary Lookup | Reader, Backend | UC-01, UC-05 | **Must** | VOC-001–008 |
| **F-07** | Kanji Information | Reader, Backend | UC-01, UC-05 | **Must** | KAN-001–002 |
| **F-08** | Grammar Detection | Reader, Backend | UC-01, UC-05 | **Must** | GRM-001–008 |
| **F-09** | Conjugation Explanation | Reader, Backend | UC-01, UC-05 | **Must / Should** | CONJ-001–002 |
| **F-10** | On-Demand Translation | Reader, Backend, Provider | UC-02 | **Must** | TRN-001–004, PRIV-004 |
| **F-11** | Web Search & Analyze | Reader, Web App | UC-05 | **Must** | WEB-001–003 |
| **F-12** | Web Grammar Library | Reader, Web App | UC-06 | **Must** | WEB-004–005 |
| **F-13** | Authentication | Reader, Learner | UC-03, UC-04 | **Must** | AUTH-001–004, SEC-005 |
| **F-14** | Save Vocabulary | Learner | UC-03 | **Must** | LEARN-001–002, LEARN-007–008 |
| **F-15** | Save Grammar | Learner | UC-04 | **Must** | LEARN-003, LEARN-007–008 |
| **F-16** | My Vocabulary | Learner, Web App | UC-03 | **Must** | LEARN-004, LEARN-006, LEARN-008 |
| **F-17** | My Grammar | Learner, Web App | UC-04 | **Must** | LEARN-005, LEARN-006, LEARN-008 |
| **F-18** | Quick Review | Learner, Web App | UC-07 | **Must** | REV-001–004 |
| **F-19** | Export to Anki / Quizlet | Learner | UC-08 | **Must** | EXP-001–005 |
| **F-20** | Knowledge Operations | Data Operator, Backend | UC-09 | **Must** | DATA-001–005 |
| **F-21** | Shared API Contracts | All Clients & Backend | Tất cả UCs | **Must** | API-001–004 |
| **F-22** | Reliability & Operations | Backend, System Ops | Tất cả UCs | **Must** | NET, RATE, CACHE, PRIV, SEC |
