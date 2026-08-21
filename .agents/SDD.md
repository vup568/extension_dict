# SDD.md — JP Reading Platform V2 Project Memory
# Đọc file AGENTS.md trước để hiểu full project context và các quy tắc ứng xử của Agent.

## 1. TL;DR (Đọc nhanh — 60 giây)
> **JP Reading Platform V2** là hệ thống trợ lý học tập và đọc tiếng Nhật thời gian thực dành cho người Việt Nam (Vietnamese-first).
> - **Triết lý sản phẩm:** "Reading First" — ưu tiên trải nghiệm đọc trực tiếp trên trang, không yêu cầu người dùng tải các gói dữ liệu từ điển/phân tích dung lượng lớn xuống client.
> - **Kiến trúc hệ thống:** Greenfield, Server-Authoritative. Toàn bộ logic lõi (tra từ, phân tích ngữ pháp, biến đổi từ) được tập trung tại Backend.
> - **Tech Stack chính thức:**
>   - **Backend:** .NET 10 (C#) — Tổ chức theo Clean Architecture.
>   - **Frontend:** React 19 + TypeScript + Vite (Xây dựng SPA tối giản cho Extension Popup và Web App).
>   - **Database:** PostgreSQL 16 quản lý schema bằng EF Core Migrations; Integration Tests sử dụng Docker Testcontainers với PostgreSQL thật để bảo toàn hành vi provider.
>   - **NLP Integration:** .NET Backend gọi sang Sidecar Tokenizer Service siêu nhẹ (viết bằng Go/Python sử dụng MeCab/Sudachi) thông qua ranh giới Adapter cô lập.
> - **Clients:** Browser Extension đa nền tảng (Chrome, Edge, Brave, Firefox) + Web App dùng chung hệ thống Backend API.

---

## 2. MANUAL MEMORY (Lưu trữ Kiến thức Hệ thống)

### 2.1 Architecture Decisions (Quyết định Kiến trúc — ADR)

*   **ADR-001: Backend .NET 10 & Frontend React 19 [Ref: .sdd/rfcs/ADR-001-backend-tech-stack.md]**
    *   *Quyết định:* Sử dụng **.NET 10 (C#)** để xây dựng Backend dịch vụ tập trung và **React 19 + TypeScript + Vite** để xây dựng ứng dụng Frontend (Extension Popup & Web App).
    *   *Lý do:* .NET 10 cung cấp hệ thống định kiểu nghiêm ngặt (strict typing) và cơ chế Dependency Injection mạnh mẽ, giảm thiểu tối đa lỗi ảo tưởng (hallucination) của AI khi viết code Clean Architecture. React + Vite cho phép đóng gói SPA cực nhẹ, đáp ứng thời gian render popup tức thời (<200ms) và tối ưu hóa Shadow DOM để cách ly style an toàn trên host pages.
    
*   **ADR-002: PostgreSQL 16 + EF Core + PostgreSQL Testcontainers [Ref: AGENTS.md, Section 2]**
    *   *Quyết định hiện hành:* Sử dụng **PostgreSQL 16** làm RDBMS chính và quản lý schema bằng **EF Core Migrations**. Integration Tests chạy với PostgreSQL thật qua **Docker Testcontainers**.
    *   *Lý do:* Dùng cùng database engine cho production và integration test giúp phát hiện khác biệt về kiểu dữ liệu, collation, indexing, transaction và SQL semantics mà SQLite in-memory không thể đại diện đáng tin cậy.
    *   *Migration note:* `.sdd/rfcs/adr-002-database-choice.md` ngày 2026-08-17 còn ghi SQL Server/SQLite và phải được supersede bằng một ADR được phê duyệt riêng trước khi implementation dựa vào ADR đó.

*   **ADR-003: Server-Authoritative Analysis & Dictionary Lookup**
    *   *Quyết định:* Chuyển toàn bộ cơ chế tra cứu từ điển (JMdict, Kanjidic2), phân tích từ loại (Morphology/Tokenizer), phân tích biến đổi từ (Conjugation) và phát hiện ngữ pháp từ Client lên Backend.
    *   *Lý do:* Giải phóng Client khỏi việc tải các bộ dữ liệu từ điển và gói tokenizer cồng kềnh (>100MB+), giúp giao diện popup Extension render tức thời (<200ms) và đồng bộ hóa 100% kết quả phân tích giữa Web và Extension.

*   **ADR-004: Stable Domain Identifiers cho Learning Resources**
    *   *Quyết định:* Mọi tài nguyên học tập được lưu (Vocabulary/Grammar) của người dùng bắt buộc phải tham chiếu đến Định danh canonical duy nhất của Backend (e.g., JMdict Entry ID, Grammar Rule ID) thay vì lưu chuỗi text hiển thị thô.
    *   *Lý do:* Đảm bảo khi dữ liệu từ điển/ngữ pháp nền tảng được cập nhật hoặc chỉnh sửa nghĩa dịch bởi ban biên tập, thư viện học tập cá nhân của người dùng không bị lỗi liên kết hoặc mất dữ liệu.

*   **ADR-005: Tokenizer & External Provider Independence**
    *   *Quyết định:* Xây dựng lớp **Adapter** ở tầng Infra của .NET Backend để kết nối với Sidecar Tokenizer Service (Go/Python) hoặc các nhà cung cấp dịch thuật bên ngoài (Google Translate/DeepL API).
    *   *Lý do:* Cách ly hoàn toàn Domain Logic khỏi sự thay đổi của các thư viện NLP hoặc nhà cung cấp dịch thuật bên thứ ba, cho phép hoán đổi nhà cung cấp mà không cần sửa đổi mã nguồn cốt lõi.

*   **ADR-006: Ruby-Safe Selection & Stale Async Protection**
    *   *Quyết định:* 
        1. Cơ chế bóc tách văn bản trên Extension DOM phải tự động bỏ qua thẻ Ruby (`<rt>`, `<rp>`) để giữ nguyên văn bản gốc tiếng Nhật.
        2. Client phải thiết lập cơ chế hủy/bỏ qua các phản hồi bất đồng bộ (async response) lỗi thời nếu người dùng đã thực hiện vùng chọn mới (sử dụng `AbortController` của React).
    *   *Lý do:* Ngăn chặn furigana làm nhiễu dữ liệu đầu vào của Tokenizer, đồng thời triệt tiêu race condition gây ghi đè dữ liệu sai lệch trên giao diện popup.

---

### 2.2 Lessons Learned (Những bài học từ Legacy V1 & Incident lịch sử)

*   **LESSON-001: Tránh rò rỉ nhãn phân tích Tokenizer (Tokenizer Leakage)**
    *   *Vấn đề:* Phiên bản V1 cho phép các nhãn từ loại thô của thư viện tokenizer rò rỉ khắp nơi trong code giao diện.
    *   *Giải pháp V2:* Tạo cấu trúc dữ liệu Token chuẩn hóa trung gian (Normalized Token DTO) ở ranh giới tầng Interface để cách ly hoàn toàn.
    
*   **LESSON-002: Nói KHÔNG với việc nhúng cứng (Hardcode) tri thức ngôn ngữ**
    *   *Vấn đề:* Các bộ quy tắc biến đổi từ (Conjugation) và 102 mẫu ngữ pháp N5–N4 bị viết chết thành các file code constants trong V1 TypeScript, gây cực kỳ khó khăn khi bảo trì và mở rộng.
    *   *Giải pháp V2:* Chuyển dịch toàn bộ Grammar và Conjugation thành dữ liệu có cấu trúc, được lưu trữ trong PostgreSQL và được phiên bản hóa (versioned data) độc lập với Engine thực thi.

*   **LESSON-003: Loại bỏ IndexedDB làm kho lưu trữ canonical dữ liệu từ điển**
    *   *Vấn đề:* V1 cố gắng đồng bộ dữ liệu từ điển JMdict vào IndexedDB của trình duyệt khiến Extension khởi động rất chậm và ngốn tài nguyên RAM của thiết bị.
    *   *Giải pháp V2:* Gỡ bỏ hoàn toàn kiến trúc IndexedDB này trên Client. IndexedDB trong V2 chỉ được dùng cho mục đích lưu preferences hoặc session-caching cực kỳ nhỏ.

*   **LESSON-004: Loại bỏ phân quyền dư thừa trên Client (Extension Host Permissions)**
    *   *Vấn đề:* V1 Extension trực tiếp gọi API Google Translate trên client, đòi hỏi cấp quyền host-permission rộng gây lo ngại về bảo mật và khó duyệt lên các Web Store.
    *   *Giải pháp V2:* Đưa dịch thuật về Backend Orchestration. Extension chỉ kết nối duy nhất đến Backend API được chỉ định.

---

### 2.3 Current Sprint Notes

*   **Sprint hiện tại:** Sprint 1 — Greenfield Foundation & Scaffolding (.NET 10 + React 19)
*   **Mục tiêu chính:** 
    1. Thiết lập cấu trúc thư mục Hybrid Project chuẩn mực với thư mục `.sdd/` và `.agents/`.
    2. Định nghĩa các API Contracts dùng chung (Shared DTOs) dùng chung giữa React Frontend và .NET Backend thông qua Swagger/OpenAPI.
    3. Triển khai đường ống dữ liệu (ETL pipeline) để import dữ liệu từ điển JMdict thô vào PostgreSQL một cách deterministic.
*   **Các Spec đang hoạt động:** `.sdd/specs/feat-dict-lookup/SPEC.md` (Draft v0.1).
*   **Điểm nghẽn hiện tại (Blockers):** Đang phân tích rủi ro bản quyền và kiểm thử chất lượng dữ liệu của tập FVDP/OVDP trước khi nạp chính thức vào Production.

---

## 3. PATTERNS TO FOLLOW (Quy chuẩn Phát triển)

### 3.1 Kiến trúc mã nguồn (Clean Architecture Layers trong .NET 10)
Mã nguồn Backend trong thư mục `src/` bắt buộc phải tuân thủ nghiêm ngặt ranh giới 4 tầng để AI thực thi tự động (ADD) không làm hỏng tính cô lập của hệ thống:

1.  **Domain Layer (`src/Domain/`):**
    *   Chứa Entities (e.g., `Word`, `Kanji`, `GrammarRule`), Value Objects và các Interfaces định nghĩa Repository/Service.
    *   *Quy tắc cứng:* **Tuyệt đối KHÔNG** được phép import bất kỳ thư viện bên ngoài, ORM (EF Core), hay framework nào tại đây.
2.  **Usecase Layer (`src/Application/`):**
    *   Chứa logic nghiệp vụ thuần túy (e.g., `LookupWordQuery`, `AnalyzeSentenceCommand`, `SaveVocabularyCommand`).
    *   Chỉ tương tác với Domain Layer và các Interfaces trừu tượng.
3.  **Interface Layer (`src/Infrastructure.Controllers/` hoặc `WebAPI/`):**
    *   Chứa Controllers, Minimal API HTTP Routers, Request/Response DTOs.
    *   Chịu trách nhiệm validate runtime dữ liệu ở ranh giới hệ thống (Sử dụng FluentValidation).
4.  **Infra Layer (`src/Infrastructure/`):**
    *   Chứa EF Core DbContext, PostgreSQL Migrations, Redis Caching, và các Http Clients kết nối với Sidecar Tokenizer Service hoặc API dịch thuật bên ngoài.

### 3.2 Quy chuẩn viết Code và Đặt tên (Naming & Code Styles)
*   **Quy ước đặt tên file & thư mục:** 
    *   Backend (.NET 10): Tên file, Class và Interface tuân theo `PascalCase` (e.g., `DictionaryRepository.cs`, `IDictionaryRepository.cs`). Tên thư mục tương ứng với Namespace.
    *   Frontend (TypeScript/React): `PascalCase` cho components (`PopupShell.tsx`), `camelCase` cho helpers/hooks/variables (`useAsyncQuery.ts`).
*   **Cơ chế Xử lý Lỗi (Error Handling):** 
    *   **Cấm nuốt lỗi (No silent failures/swallowed exceptions).**
    *   Mọi API lỗi trả xuống Client bắt buộc phải sử dụng **Structured Machine-Readable Error Responses** chứa `{error_code, message, request_id}` thay vì trả về text thuần túy.
*   **Truy vết đặc tả (EARS Traceability):**
    *   Mỗi dòng logic nghiệp vụ được viết trong usecase bắt buộc phải có comment tham chiếu ngược lại Spec bằng thẻ tag chuẩn:
    ```csharp
    // EARS[Event]: WHEN user selects text containing Japanese characters
    ```

### 3.3 Quy chuẩn Kiểm thử (Testing Standard)
*   **Unit Tests (`tests/unit/`):** Phải cô lập 100%, sử dụng Mock (Moq hoặc NSubstitute) cho các Interfaces. Target coverage tối thiểu của usecase là **85%**.
*   **Integration Tests (`tests/integration/`):** Chạy thực tế với **PostgreSQL 16 qua Docker Testcontainers** để kiểm tra tính toàn vẹn dữ liệu và hành vi query trên đúng production provider.
*   **E2E Tests (`tests/e2e/`):** Kiểm thử luồng tích hợp trình duyệt (kiểm thử ruby-safe selection, stale async protection sử dụng Playwright).

---

## 4. AUTO MEMORY (Tự động ghi nhớ)

### 2026-08-18 — Conceptual Data Model

*   Canonical linguistic resources dùng typed global ID, ổn định qua revision; `DictionarySense` là entity độc lập, language-neutral, có localized gloss và applicability đối với written form/reading.
*   Nguồn từ điển được giữ dưới dạng `SourceRecord`; mọi liên kết/merge nhiều nguồn vào canonical resource phải là quyết định editorial được review, không tự động suy luận chỉ từ written form hoặc reading trùng khớp.
*   `KnowledgeRelease` là snapshot immutable; chỉ release đã publish mới trở thành current. Analysis/translation interactions là ephemeral, không lưu raw selected text hay history mặc định.

### 2026-08-18 — Đồng bộ baseline tài liệu

*   `AGENTS.md` là nguồn có thẩm quyền hiện hành cho tech stack: .NET 10, React 19 và PostgreSQL 16.
*   Project Memory đã loại bỏ SQL Server/SQLite khỏi baseline hiện hành. ADR-002 và các feature spec cũ còn tham chiếu SQL Server/SQLite được ghi nhận là artifact cần supersede hoặc cập nhật qua một nhiệm vụ tài liệu riêng.
*   Product Requirements được chuẩn hóa theo hướng mỗi hành vi có một requirement ID canonical; các phần scope, journey, Definition of Done và roadmap chỉ tham chiếu ID để tránh nhiều nguồn sự thật.

### 2026-08-21 — API Contract Documentation Baseline

*   Theo chỉ đạo của project owner, contract API MVP mới dùng prefix `/api/` và không version trong URL. Yêu cầu versioned contracts (`API-002`) vẫn được giữ bằng metadata/header contract; cơ chế compatibility chính xác được ghi là proposed cho tới khi feature SPEC phê duyệt.
*   Endpoint Dictionary Lookup hiện có nằm ngoài scope của baseline này và không bị sửa đổi. `docs/api/mvp-api-contract.md` phân tách requirements đã chốt với HTTP/API shapes đề xuất, để tài liệu không bị hiểu nhầm là runtime contract đã triển khai.
