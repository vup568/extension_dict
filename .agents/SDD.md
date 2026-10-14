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
>   - **NLP Integration:** Python + Sudachi Sidecar qua gRPC (OD-010 resolved);F09 corpus .3 đã nghiệm thu G1/G2/G3,candidate opt-in. Production publication/Firefox runtime vẫn riêng.
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
*   *Implementation note:* ADR-002, global constraints và feature-spec inheritance đã được đồng bộ trực tiếp ngày 2026-08-21; SQL Server/SQLite không còn là provider được hỗ trợ cho V2 runtime, migrations hoặc integration tests.

*   **ADR-003: Server-Authoritative Analysis & Dictionary Lookup**
    *   *Quyết định:* Chuyển toàn bộ cơ chế tra cứu từ điển (JMdict, Kanjidic2), phân tích từ loại (Morphology/Tokenizer), phân tích biến đổi từ (Conjugation) và phát hiện ngữ pháp từ Client lên Backend.
    *   *Lý do:* Giải phóng Client khỏi việc tải các bộ dữ liệu từ điển và gói tokenizer cồng kềnh (>100MB+), giúp giao diện popup Extension render tức thời (<200ms) và đồng bộ hóa 100% kết quả phân tích giữa Web và Extension.

*   **ADR-004: Stable Domain Identifiers cho Learning Resources**
    *   *Quyết định:* Mọi tài nguyên học tập được lưu (Vocabulary/Grammar) của người dùng bắt buộc phải tham chiếu đến Định danh canonical duy nhất của Backend (e.g., JMdict Entry ID, Grammar Rule ID) thay vì lưu chuỗi text hiển thị thô.
    *   *Lý do:* Đảm bảo khi dữ liệu từ điển/ngữ pháp nền tảng được cập nhật hoặc chỉnh sửa nghĩa dịch bởi ban biên tập, thư viện học tập cá nhân của người dùng không bị lỗi liên kết hoặc mất dữ liệu.

*   **ADR-005: Tokenizer & External Provider Independence**
    *   *Quyết định:* Xây dựng lớp **Adapter** ở tầng Infra của .NET Backend để kết nối với Python + Sudachi Sidecar qua gRPC (OD-010, F-08 SPEC §1.4) hoặc các nhà cung cấp dịch thuật bên ngoài (Google Translate/DeepL API).
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

*   **F-09 clarification/approval (2026-10-13/14):** Kính ngữ/hai nghĩa sai khiến đã chốt;corpus .3 có60 ô/412 ca/115 rules/17fixtures. User duyệt G1/G2 đúng packet ngày13 và G3 bằng “ok tôi duyệt nghiệm thu nhé” ngày14;receipts riêng bind exact hashes. Không suy approval từ Proceed hay AI/test results.

*   **Workstream hiện tại (2026-10-13):** `feat/tokenizer-deinflection` hoàn tất code và local verification F-06 deinflection/initial F-09; chưa push/merge/deploy.
*   **Đã delivery:** F-02 merge/push `dev` tại `e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4`; backend dictionary/kanji/grammar/unified analysis đã có runtime.
*   **Delivery hiện tại:** Local merge b3d187d đã qua toàn bộ post-merge gates. Remote delivery qua [PR #10](https://github.com/vup568/extension_dict/pull/10), do `dev` yêu cầu pull request; trạng thái merged của PR là bằng chứng hoàn tất.
*   **F09 completion 2026-10-14:** G1/G2/G3 approved;42/42 tasks complete for scoped corpus. candidate-20261013.1 (909 parameterized realizations/115IDs),v3 parser/popup,canonical POS/joint sense và INSERT-only migration đã kiểm chứng:79units/118integration/251FEunits/136browser/12Python,412cases mapped/0 gaps;review cycle2 no P1/P2. acceptance-manifest.json bind unchanged validation report/source hashes and actual user G3 receipt;giữ historical snapshots.
*   **Giới hạn còn lại:** Candidate development opt-in,không production KnowledgeRelease/publication. Unset dataset Null;analysis1 empty/v2 one-or-none/v3 all evidenced alternatives;morphology unavailable. Firefox runtime,OD-005/publication pending. Sentence segmentation,Grammar reviewed full N5–N4,Web/auth/translation/learning chưa có. G3 không tự cho phép migrate DB người dùng,Git delivery hoặc cài thêm dependency.

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
*   Project Memory, ADR-002 và global constraints thống nhất PostgreSQL 16, Npgsql EF Core Migrations và PostgreSQL Testcontainers. Không áp dụng dual-provider compatibility cho V2.
*   Product Requirements được chuẩn hóa theo hướng mỗi hành vi có một requirement ID canonical; các phần scope, journey, Definition of Done và roadmap chỉ tham chiếu ID để tránh nhiều nguồn sự thật.

### 2026-08-21 — API Contract Documentation Baseline

*   Theo chỉ đạo của project owner, contract API MVP mới dùng prefix `/api/` và không version trong URL. Yêu cầu versioned contracts (`API-002`) vẫn được giữ bằng metadata/header contract; cơ chế compatibility chính xác được ghi là proposed cho tới khi feature SPEC phê duyệt.
*   Endpoint Dictionary Lookup hiện có nằm ngoài scope của baseline này và không bị sửa đổi. `docs/api/mvp-api-contract.md` phân tách requirements đã chốt với HTTP/API shapes đề xuất, để tài liệu không bị hiểu nhầm là runtime contract đã triển khai.

### 2026-08-21 — UI/UX Design Direction Baseline

*   `DESIGN.md` chốt hướng UI/UX content-first, accessible và low-latency cho Extension/Web, với Accessible & Ethical là reference chính; Flat Design Mobile và Japanese Elegant typography là hai reference phụ. Lựa chọn được xếp hạng theo traceability requirement, không theo aesthetic similarity.
*   Popup Extension ưu tiên shell tức thời, trạng thái async truthful, linguistic-first result hierarchy, anonymous reading và explicit translation disclosure. Tài liệu loại trừ landing/pricing/testimonial patterns, selected-text history mặc định và hiệu ứng trang trí nặng.
*   Các token, palette chi tiết, popup positioning, dark mode và navigation model được ghi rõ là design decision mới hoặc open choice; implementation chỉ được chốt trong feature SPEC sau và phải validate accessibility/cross-browser.

### 2026-08-21 — PRD and Platform Foundation Ratification

*   Product Owner **VuPM** approved `REQUIREMENT.md` v2.1.0 on 2026-08-21. Browser Extension and Web Application remain MVP clients; .NET 10/C# 14, React 19/TypeScript/Vite, PostgreSQL 16, EF Core Migrations, PostgreSQL Testcontainers, and the provider-neutral tokenizer sidecar remain the approved technical baseline.
*   Export to Anki/Quizlet is **Post-MVP** (`EXP-001`–`EXP-004`); automatic bidirectional synchronization remains outside MVP (`EXP-005`). The future Export feature requires a dedicated specification plus `OD-007` before implementation.
*   `OD-001`–`OD-014` are deferred under controlled gates: Delivery Team prepares evidence or a proposal, and VuPM approves the decision at the documented feature or operational gate.
*   Database governance alignment completed on 2026-08-21: ADR-002 was rewritten in place by Product Owner direction; PostgreSQL 16, Npgsql EF Core Migrations and PostgreSQL Testcontainers are the single V2 baseline.

### 2026-08-21 — Buildable .NET Foundation

*   `JPReadingPlatform.sln` now contains `Domain`, `Application`, `Infrastructure`, `WebApi`, `UnitTests`, and `IntegrationTests`, all targeting .NET 10/C# 14. References flow inward: Domain ← Application ← Infrastructure ← WebApi; the test projects do not make Domain depend on EF Core.
*   PostgreSQL persistence uses Npgsql EF Core 10.0.3 with aligned EF Core design tooling 10.0.4. The initial migration is versioned under `src/infra/Persistence/Migrations/`; the EF CLI is pinned as a local tool rather than altering the user global tool.
*   `dotnet build JPReadingPlatform.sln --no-restore`, the isolated unit suite, and the PostgreSQL Testcontainers migration test passed on 2026-08-21. Persistence is verified against the production database provider locally.

### 2026-08-21 — Local PostgreSQL Secret Hygiene

*   Compose no longer contains database credentials. Actual local PostgreSQL configuration is stored only in ignored `.env`; Compose requires every database variable, binds PostgreSQL to localhost, and has a readiness healthcheck.
*   The existing PostgreSQL volume was preserved while the database role password was rotated to the local secret. Compose validation, TCP authentication using the new secret, and the PostgreSQL Testcontainers migration test passed.

### 2026-08-21 — Knowledge Release Structural Immutability

*   Current-status is stored in the singleton `current_knowledge_release` pointer rather than on a release row. Controlled publication can therefore switch current facts without mutating the previous published snapshot.
*   Published release metadata, resource revisions and release-manifest memberships are protected by PostgreSQL triggers, restrictive foreign keys and per-release transaction locks. A transaction-local guard rejects ordinary direct mutations outside the internal publication path; this is not a substitute for the deferred runtime DB-role/operator authorization boundary.
*   The internal publication use case and EF transaction publish a candidate and switch the pointer atomically after verifying Source Manifest membership. PostgreSQL Testcontainers cover immutability, draft/direct-SQL rejection, pointer switching/deletion, transaction rollback/retry, child-write concurrency and upgrade backfill from the original `is_current` column.
*   This is structural immutability for the modeled release boundary, not the DATA-001 validation/approval pipeline. Full DATA-005 snapshot coverage for SourceManifest metadata, SourceRecord and EditorialMapping remains a separate design decision because mappings do not yet carry a ReleaseId.

### 2026-09-19 — Unified Analysis API Verification

*   F-05 Unified Analysis API is verified with Docker Testcontainers: `dotnet build --no-restore` completed with zero warnings/errors; unit tests passed 51/51 and integration tests passed 50/50. The endpoint orchestrates vocabulary, kanji and grammar with partial-result status, while morphology and conjugation remain explicitly unavailable.
*   Concurrent capability execution requires an independent EF Core `AppDbContext` per capability; the infrastructure registration is transient to prevent EF Core's single-context concurrent-operation failure.
*   Sentence context is used only when it contains the selected text once. Grammar tokens are restricted to that selection and their offsets remapped, so every returned grammar span remains relative to the public analysis input.

### 2026-09-20 — F-01 Japanese Text Detection Implementation

*   F-01 now has a framework-free TypeScript detector and local boundary under extension/, backed by a deterministic 25-range policy generated from pinned Unicode 17.0.0 Scripts, PropList, UnicodeData and Blocks sources with verified checksums.
*   The regression corpus preserves supplementary code points, malformed UTF-16 units, literal mojibake/entity cases, long-input behavior and distinct invalid/unexpected errors. The local run passed 116 unit tests, the 85% coverage gate, type check, Vite build and 10 Chrome/Edge browser privacy/parity tests.
*   Brave and Firefox evidence remains explicitly pending because neither executable is installed locally. Detector threshold approval also remains pending OD-005; measurements are recorded without claiming the end-to-end popup budget.

### 2026-09-24 — F-02 Ruby-Safe DOM Extraction Implementation

*   F-02 now exposes a synchronous frame-local `SelectionReader` boundary that accepts exactly one coherent non-collapsed Range and returns complete base text or one of six content-free error codes. Original DOM ancestry excludes only HTML `rt`/`rp`; partial offsets, Unicode code units, explicit `br`, and a fixed CSS-independent block policy are preserved through a pure token accumulator.
*   Runtime processing is ephemeral and read-only: no network, storage, logs, popup, listener, debounce, authentication, Backend call, page-context expansion, or new dependency was added. A direct browser regression verifies that only successful ruby-safe output is handed to F-01.
*   Final local evidence passed 133 Extension unit tests with 96% statement coverage, 48/48 Playwright tests across installed Chrome, Edge and Brave, the .NET Release build, 51 unit tests, and 50 PostgreSQL Testcontainers integration tests. Firefox remains explicitly pending because no compatible executable is installed; performance measurements remain ungraded contribution evidence toward PERF-001.

### 2026-10-11 — F-02 Delivery and F-03/F-04 Selection-to-Analysis

*   F-02 merged/pushed to `origin/dev` at `e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4`. New implementation uses `feat/selection-popup-stale-protection`; final commit/push status remains separate from implementation evidence.
*   React 19 popup uses feature-local open Shadow DOM/nonmodal focus policy and manual native popover top layer with fixed-host fallback. Selected DOM Range is ruby-safe and frame-local; popup renders shell/loading before 300ms request debounce. Escape/close, focus restoration, visualViewport and BFCache are handled at the browser boundary.
*   Generation plus UUID invalidates currentness immediately on every selection change, including the debounce window. AbortController attempts cancellation; identity guards success/error/partial regardless of cancellation. Retry creates a fresh identity; independent 5s deadlines prevent an infinite spinner.
*   Content scripts message background; only background fetches one build-configured origin (`VITE_API_BASE_URL`, default loopback5000), with credentials omitted, redirects rejected and sender/tab/frame scope validation. Chromium service-worker and Firefox background-script manifests are built separately; `dist/index.js` library remains intact.
*   Frontend validates actual mixed-case API DTOs, contract_version and interaction echo; sections preserve canonical IDs, matched form/reading, sense restrictions, language, kanji provenance and each grammar occurrence. Morphology/conjugation stay empty/unavailable; no context, translation, auth, history, canonical client store or linguistic corpus is introduced.
*   Dependencies React/react-dom/type packages19.3.0 and Tailwind/Vite plugin4.3.3 plus lock transitives were explicitly owner-approved. Exact source-map-js1.2.2 override fixes the audit finding; npm audit reports0 vulnerabilities. Existing appsettings DB credential was removed; ConnectionStrings__DefaultConnection is externally required.
*   Verified gates: 223/223 units, 96.74% statements/93.58% branches/98.24% functions/97.85% lines coverage; typecheck, Unicode verification, library/extension builds, .NET Release 0 warnings/errors and 51/51 units pass. Full browser suite 112/112 (Chrome 37, Edge 37, Brave 37, packaged MV3 Brave 1), no skip/flaky/failure; real background POST from an ordinary HTTP host uses minimal payload/no cookies/storage. Independent review has no outstanding P1/P2. Evidence: `.sdd/specs/feat-selection-popup/evidence/validation-report.md`.
*   Remaining limits: Firefox execution pending, PostgreSQL Testcontainers blocked by existing Docker startup failure, approved PERF-001 benchmark pending. Input/textarea selection unsupported, whole-selection vocabulary lookup unsegmented, default NullTokenizerAdapter and grammar seed5 patterns remain as before.
*   Delivery verified: `origin/feat/selection-popup-stale-protection` equals local `86cd930c1e05bbc8c68a590b452a63d2d3f5e8df` after a normal push; implementation `99c2634`, tests `18035fa`, external DB configuration `5a0ff1a`. This feature was not merged into dev; dev remains the authorized F-02 merge `e01ee19`.

### 2026-10-12 — F-06 PostgreSQL Revalidation and Capability Truthfulness

*   Docker Engine and the local PostgreSQL 16 Compose service are healthy. The Release solution build passes with zero warnings/errors; unit tests pass 53/53 and PostgreSQL Testcontainers integration tests pass 51/51. Focused F-06 slices pass 13/13 unit and 6/6 integration tests.
*   `DeinflectionResult` now distinguishes an available provider with no match from an unavailable provider. The default `NullTokenizerAdapter` therefore keeps exact lookup usable while truthfully reporting morphology `unavailable`, matching AC-011 and Unified Analysis behavior.
*   F-06 exact lookup, Vietnamese-first fallback, provenance and blank/length validation are verified. Control-character policy remains a recorded gap. Dictionary deinflection integration remains unimplemented; OD-010 is already resolved in F-08. Performance certification remains pending OD-005. Evidence: `.sdd/specs/feat-dict-lookup/evidence/validation-report.md`.

### 2026-10-12 — OD-010 Decision Alignment Correction

*   OD-010 was resolved as Python + Sudachi over gRPC in F-08 SPEC §1.4 and its 2026-08-24 clarification, committed as 82dc015 on 2026-08-30. This is an existing project decision, not a new approval.
*   `sidecars/tokenizer/{app.py,Dockerfile,requirements.txt,proto/tokenizer.proto}` and the registered `GrpcGrammarTokenizerAdapter` already exist. Dictionary DI still registers `NullTokenizerAdapter`; the remaining work is Dictionary deinflection, vocabulary tokenization/Unified Analysis integration and F-09 explanations.
*   Corrected the PRD, F-06 status/tasks/evidence, Feature Map and architecture references that had incorrectly treated OD-010 as pending or claimed the Sidecar did not exist. Functional PostgreSQL verification does not certify live Sidecar runtime/deployment; Compose currently defines only the database.

### 2026-10-13 — Dictionary Sidecar & Initial Vietnamese Conjugation

*   User-authorized order executed: OD-010 docs commit `c0cb500`, unchanged live Docker smoke, data-driven F-06 adapter, initial F-09 API/popup. Implementation branch `feat/tokenizer-deinflection`; no current feature push/merge/deploy claimed.
*   Sudachi uses sanitized logs, synchronized tokenization and half-open UTF-16 spans (including supplementary characters). Missing initialization is gRPC UNAVAILABLE, never false success[]. Compose adds optional `nlp` profile on localhost50051; existing PostgreSQL/volume preserved.
*   Pure Domain `ConjugationDataset` persists immutable versioned JSONB; mapping/rules/VI segments are data, not engine constants. Pin `ConjugationDatasetVersion=development-20261013.1` explicitly; unset retains Null adapter. UPDATE/DELETE/TRUNCATE rejected; new versions install additively. This development seed is not a production KnowledgeRelease approval.
*   Exact-first lookup remains; complete unique whole-word patterns only, no first-verb/suffix guessing. Adapter has 500ms linked cancellation; caller cancellation and secondary/dataset DB failures propagate. F-09 reuses found-entry F-06 result without a second deinflection call.
*   Intentional contract evolution: absent/header1 keeps empty/unavailable conjugations; header2 enables typed VI/base/UTF-16 segments/dataset identity; unknown version structured400. Extension requests2, validates1/2 and renders safe text; morphology/tokens unchanged.
*   Verification: pinned Docker build, Python12/12, .NET Release0 warnings/errors, units58/58, integration83/83 (live22 + isolated adapter10 + prior51), FE236/236, real browsers121/121 (Chrome40/Edge40/Brave40/packaged1). Review no P1/P2. Modified usecase line coverage: Dictionary98%, Analysis87.30%, matcher93.33%, using existing transitive Microsoft.CodeCoverage without installs. FE TS coverage96.74% statements; popup .tsx measured via browsers separately.
*   Test lesson: xUnit test instances initialize per case, so shared DB/provider seeds belong in class fixture; literal JSON braces in EF ExecuteSqlRaw must be escaped. Failed harness cases were corrected and rerun, not skipped. Full corpus/linguistic signoff, sentence segmentation, Firefox parity, performance and F-06 operational gates remain separate. Evidence: `.sdd/specs/feat-tokenizer-deinflection/evidence/validation-report.md`.

### 2026-10-13 — F-09 Corpus Specification Boundary

*   User approved a documentation-only Shadow Plan for `.sdd/specs/feat-conjugation-explanation/SPEC.md`: define/approve MVP corpus, review the existing nine rules, supplement approved gaps and accept the dataset. Existing implementation is reused; no runtime or dataset edits in this task.
*   Corpus selection and linguistic signoff are future deliverables, not inferred from passing tests or approval to write this SPEC. No mandatory new-rule count or claim to cover all Japanese conjugations; if approved corpus has no gap, supplementation closes with evidence only.
*   Separate quality checklist and active `.specify/feature.json` now point to F-09. Feature PLAN/TASKS remain a subsequent step. This does not approve production publication, redefine F-06/F-20, or commit/merge the current changes.

### 2026-10-13 — F-09 Corpus Plan and Design Artifacts

*   User approved planning-only Proceed. Added `feat-conjugation-explanation/PLAN.md`, research/data-model/contracts/quickstart: ordered corpus scope G1 → independent linguistic review G2 → conditional gap completion/acceptance G3. Runtime/data unchanged; no corpus or linguistic approval implied.
*   Technical finding: ConjugationKnowledge accepts only development status; config pin is not approval, and ConjugationDataset currently lacks KnowledgeRelease membership. Plan retains development candidate for local verification; production activation has an explicit publication/F-20 prerequisite, not a silent ResourceRevision/status/schema expansion.
*   Data changes require a new version/resource/forward migration; never mutate the old embedded seed/migration. Hash source artifact bytes and compare JSONB semantically; avoid self-referencing hashes and candidate/report hash cycles.
*   Design validation covers FR-001–010, local links/placeholders/Spec Kit prerequisites and unchanged source fingerprint. No feature TASKS/runtime tests/migration/deployment performed by planning; next step can generate TASKS without bypassing human corpus/review gates.

### 2026-10-13 — F-09 Detailed Task Generation

*   User requested detailed TASKS from PLAN. Generated42 tasks (setup/foundation4, US1 scope8, US2 linguistic review8, US3 conditional gap completion/acceptance18, handoff4), all pending; no execution permission or human approval inferred from generation.
*   G1 PO scope approval and G2 independent reviewer signoff precede runtime supplementation; G3 scoped acceptance is distinct from production publication/F-20. Conditional work may close with evidenced no-op/N/A, never arbitrary new rule count or fake signoff.
*   Tasks include exact paths, test-before-change, old-version replay/append-only data, v1/v2/spans/privacy/async regressions, hashes/semantic JSONB, dependency order and safe parallel examples. Documentation checks and source fingerprint verify generation-only changes; no code/data/migration/commit/push/merge executed.

### 2026-10-14 — Local Backend Startup Configuration Fix

*   The previous local guide omitted ConnectionStrings__DefaultConnection: Compose reads root .env, direct dotnet run does not. Conditional AppDbContext registration and swallowed migration initialization produced health200/analysis500. DI now rejects missing configuration;database initialization must succeed before HTTP starts,with sanitized errors and unchanged transient context lifetime.
*   scripts/start-local.ps1 reads the four literal PostgreSQL dotenv keys without evaluating/echoing secrets,uses a connection-string builder,retains explicit connection configuration,waits for existing db/tokenizer services,and restores process environment. User execution applies existing EF migrations;agent only validated configuration/mocks/isolated Testcontainers,never restarted/migrated the user's service/DB.
*   Fresh Windows evidence:Release build0 warnings/errors,17 targeted tests,79 unit/135 integration and251 frontend unit tests plus typecheck pass. Accepted F09 snapshots/contracts/schema unchanged. Review PASS_WITH_RISK:Windows-only scope explicitly selected by user;new Windows PowerShell test executable names still block Ubuntu CI and clean Git delivery. No commit/merge/push/dependency install.
*   Workflow artifact hook absent from checkout;documented invocation failed MODULE_NOT_FOUND. Explicit fallback checks verify artifact structure and preserve the known delivery blocker,not claim hook approval. Evidence:plans/261014-local-backend-startup/reports/validation-report.md.

### 2026-10-14 — Authorized Local Git Checkpoint

*   Subsequent user authorization explicitly includes F06/F09 and the Windows startup fix for local commits only; no push/merge. Specifications/corpus and the dependent implementation/tests are recorded together in coherent commits. Earlier no-Git statements remain historical evidence of their original task boundaries.
*   Scoped .gitattributes preserve accepted F09 LF bytes and the baseline migration's recorded CRLF bytes. All12 accepted Git blob bindings match SHA256. Frozen archival whitespace is retained rather than invalidating approval hashes; Ubuntu CI incompatibility and absent workflow hook remain unresolved, with no clean CI/ship claim.
