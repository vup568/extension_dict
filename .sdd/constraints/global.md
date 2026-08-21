# .sdd/constraints/global.md — Ràng buộc Kỹ thuật Toàn cục (Global Constraints)
# Phiên bản: 1.1.0 | Cập nhật: 2026-08-21 | Trách nhiệm: Tech Lead
# Áp dụng cho: Mọi AI Agent, Mọi Developer, Hệ thống CI/CD, Môi trường Thực thi

## 1. TECHNOLOGY STACK (Hệ công nghệ Cố định)
Hệ thống **JP Reading Platform V2** chạy trên các nền tảng kỹ thuật sau. Tuyệt đối KHÔNG tự ý cài đặt thêm hoặc thay đổi phiên bản lớn mà không thông qua quy trình phê duyệt RFC [MIGRATION 19].

### 1.1 Backend (Dịch vụ Trung tâm)
*   **Runtime & Ngôn ngữ:** .NET 10 (C# 14) [MIGRATION 13.4, 19].
*   **Web Framework:** ASP.NET Core Minimal APIs (Không sử dụng Controllers truyền thống cho các REST API mới để tối giản overhead và tối ưu latency) [MIGRATION 13.4].
*   **Database ORM/Driver:** Entity Framework Core 10 (EF Core) [MIGRATION 13.4].
*   **Database Thực thi (Dev/Prod):** PostgreSQL 16 [ADR-002, ARCH-005].
*   **Database Kiểm thử tích hợp (Integration Test):** PostgreSQL 16 qua Docker Testcontainers [ADR-002, ARCH-006].
*   **Caching & Session Storage:** Redis (go-redis tương đương được chuyển dịch sang StackExchange.Redis trong .NET) [MIGRATION 7.2].
*   **API Documentation:** Microsoft.AspNetCore.OpenApi (Sinh tài liệu OpenAPI/Swagger tích hợp mặc định) [MIGRATION 18 (Step 10), REQ BROWSER-001].

### 1.2 Frontend (Clients)
*   **Runtime & Ngôn ngữ:** React 19 + TypeScript (Strict Mode) [MIGRATION 6.11, REQ P-04].
*   **Build Tooling & Bundle:** Vite (Tối ưu kích thước bundle để nhúng gọn vào Extension và Web App) [MIGRATION 6.11, REQ P-04].
*   **Styling:** Tailwind CSS v4 (Sử dụng scoped prefix để tránh lỗi vỡ layout khi nhúng popup Extension vào trang web của host) [MIGRATION 8.4].
*   **API Client:** Axios hoặc Fetch native kết hợp React Query (để tận dụng cache & auto-retry) [MIGRATION 9.9].

### 1.3 NLP & Linguistic Boundary (Xử lý Ngôn ngữ học)
*   **Linguistic Processing Integration:** Provider-Independent Adapter Interface trong .NET Backend [MIGRATION 17, 8.2].
*   **Sidecar Morphology Tokenizer:** Sidecar microservice siêu nhẹ viết bằng Python hoặc Go (sử dụng Sudachi hoặc MeCab), giao tiếp nội bộ với .NET Backend thông qua gRPC hoặc HTTP cục bộ [MIGRATION 8.2, 17].

---

## 2. APPROVED EXTERNAL PACKAGES (Danh sách Thư viện được duyệt)
AI Agent chỉ được phép sử dụng các thư viện nằm trong danh sách được duyệt dưới đây. Mọi thư viện mới phát sinh bắt buộc phải có giải trình và được Tech Lead ký duyệt [MIGRATION 17, 19].

### 2.1 Backend (.NET NuGet Packages)
*   `Npgsql.EntityFrameworkCore.PostgreSQL` — Provider EF Core cho PostgreSQL, bao gồm EF Core Migrations [ADR-002, ARCH-007].
*   `Testcontainers.PostgreSql` — Khởi tạo PostgreSQL 16 thật cho database-dependent integration tests [ADR-002, ARCH-006].
*   `StackExchange.Redis` — Thư viện caching kết nối Redis [MIGRATION 7.2].
*   `FluentValidation.AspNetCore` — Thư viện validate runtime dữ liệu ở ranh giới hệ thống [MIGRATION 6.12, REQ 60].
*   `System.Text.Json` — Trình phân giải JSON native của .NET (Không dùng Newtonsoft.Json do tối ưu hiệu năng).
*   `xunit` & `FluentAssertions` — Khung kiểm thử Unit và Integration Test backend.

### 2.2 Frontend (npm Packages)
*   `react` & `react-dom` (v19) — Thư viện UI Core [MIGRATION 8.3].
*   `typescript` — Compile-time type checker (Bắt buộc bật strict mode) [MIGRATION 4.3].
*   `zod` — Schema validation ở Client system boundaries [MIGRATION 6.12, REQ 60].
*   `lucide-react` — Icon pack gọn nhẹ cho giao diện.

---

## 3. BANNED PACKAGES (Thư viện nghiêm cấm sử dụng)
*   ❌ `Newtonsoft.Json` (Json.NET) — Bị cấm vì tốc độ phân giải chậm và tốn tài nguyên hơn `System.Text.Json` gốc của .NET 10.
*   ❌ `Dapper` (Raw SQL) — Nghiêm cấm dùng Dapper để viết query thủ công trừ các tác vụ phân tích báo cáo lớn có phê duyệt riêng. Luôn ưu tiên EF Core LINQ để AI dễ phân tích schema [MIGRATION 13.5].
*   ❌ Thư viện Google Translate trực tiếp ở Frontend client — Bị cấm để bảo vệ an toàn host permissions và bảo mật dữ liệu [MIGRATION 6.13, 7.5, 7.7].
*   ❌ `any` type trong TypeScript — Cấm sử dụng bừa bãi kiểu dữ liệu `any`. Bắt buộc dùng kiểu dữ liệu tĩnh cụ thể hoặc sử dụng `unknown` kết hợp Type Guard [MIGRATION 4.3].

---

## 4. NAMING CONVENTIONS & STYLE GUIDES (Quy chuẩn đặt tên và viết code)
Sự nhất quán trong cấu trúc code giúp AI hiểu dự án nhanh hơn và duy trì chất lượng hệ thống lâu dài [MIGRATION 13.4].

### 4.1 Backend (C#/.NET 10)
*   **Quy tắc chung:** Tuân thủ chuẩn coding của Microsoft.
*   **Namespaces, Classes, Methods, Properties:** Bắt buộc đặt tên theo dạng `PascalCase` (ví dụ: `Namespace JP.Reading.Domain`, class `DictionaryEntry`, method `LookupVocabularyAsync()`).
*   **Parameters, Local Variables:** Đặt tên theo dạng `camelCase` (ví dụ: `japaneseText`, `entryId`).
*   **Interfaces:** Bắt buộc bắt đầu bằng tiền tố `I` (ví dụ: `IDictionaryRepository`, `IMorphologyService`).
*   **Async/Await:** Mọi phương thức bất đồng bộ bắt buộc phải có hậu tố `Async` trong tên phương thức (ví dụ: `SaveWordAsync()`) và phải nhận một `CancellationToken` làm đối số cuối cùng để tránh rò rỉ tài nguyên mạng [MIGRATION 9.9].

### 4.2 Frontend (React/TypeScript)
*   **Components & Classes:** Đặt tên file và component theo dạng `PascalCase` (ví dụ: `PopupShell.tsx`, `VocabularyCard.tsx`).
*   **Hooks:** Đặt tên theo dạng `camelCase` bắt đầu bằng `use` (ví dụ: `useSelectedText.ts`, `useAsyncQuery.ts`).
*   **Helper Functions, Variables:** Đặt tên theo dạng `camelCase` (ví dụ: `formatGrammarPattern()`, `staleResponseFlag`).
*   **API Routes & Folders:** Đặt tên theo dạng `kebab-case` (ví dụ: thư mục `feat-dict-lookup/`, route `/api/v2/dictionary-lookup`).

---

## 5. ENVIRONMENT COHERENCE (Môi trường & Đồng bộ)
*   **Cấu hình biến môi trường:** Connection string PostgreSQL, Redis, URL Sidecar Tokenizer và API key dịch thuật ngoài phải lấy qua environment variables hoặc secret store, được đọc bằng `IConfiguration`. Không commit secrets hay connection string thật vào source control [ADR-002, SEC-002].
*   **Đồng bộ DB Schema:** `tests/integration/` phải khởi tạo PostgreSQL 16 bằng Testcontainers, apply cùng EF Core Migrations với runtime và kiểm tra behavior phụ thuộc provider. Provider-specific PostgreSQL SQL/mapping được phép khi cần cho schema đã duyệt [ADR-002, ARCH-006, ARCH-007].
