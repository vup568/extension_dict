## 📋 Pre-Commit & Pull Request Checklist (Agent-Generated Code)

> **Mô tả PR / Summary:**
> 
> 

---

### 🔍 15-Item Checklist (Tích chọn `[x]` hoặc đánh dấu `N/A` nếu không áp dụng)

#### 💻 1. CODE (Chất lượng mã nguồn)
- [ ] **01. Local Build & Syntax (Bắt buộc):** Code chạy được locally — không có compile/syntax errors (`dotnet build` pass).
- [ ] **02. Debug Cleanup:** Không có `console.log`, `debugger`, `print(` còn sót (hoặc N/A).
- [ ] **03. No TODOs:** Không còn `TODO` / `FIXME` dở dang trong code merge (hoặc N/A).
- [ ] **04. Naming Conventions:** Tuân thủ quy ước đặt tên trong `AGENTS.md` (PascalCase C#, camelCase TS).
- [ ] **05. Strict Typing:** Không sử dụng type `any` trong TypeScript (nếu có TS / N/A).

#### 🧪 2. TEST (Kiểm thử & Độ tin cậy)
- [ ] **06. Zero Regression (Bắt buộc):** Tất cả existing tests vẫn pass (`dotnet test`).
- [ ] **07. Business Logic Coverage:** Viết unit tests cho business logic / service mới (hoặc N/A).
- [ ] **08. Coverage Baseline:** Test coverage không giảm so với baseline (hoặc N/A).

#### 🛡️ 3. SEC (Bảo mật & Safe Data)
- [ ] **09. Secrets Scan (Bắt buộc):** Không có API keys, connection strings, passwords hardcode trong code.
- [ ] **10. Input Validation:** Đã implement schema validation (FluentValidation / Zod) cho endpoints mới (hoặc N/A).
- [ ] **11. Injection Protection:** SQL queries dùng ORM / Parameterized queries, không dùng raw string concatenation.

#### 🎯 4. SPEC (Đúng đặc tả & Không dư thừa)
- [ ] **12. Spec Criteria:** Code implement đúng 100% acceptance criteria trong `SPEC.md`.
- [ ] **13. No Feature Creep:** Agent không tự ý thêm các tính năng nằm ngoài scope được định nghĩa.

#### 📚 5. DOC (Tài liệu API)
- [ ] **14. API Documentation:** Cập nhật Swagger / OpenAPI spec cho endpoint mới/sửa (hoặc N/A).

#### ⚡ 6. PERF (Hiệu năng DB & Cấu trúc)
- [ ] **15. No N+1 Queries:** Đã kiểm tra và optimize DB queries (dùng `.Include()` / eager loading khi cần / N/A).
