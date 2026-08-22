## 📋 Pre-Commit & Pull Request Checklist (Agent-Generated Code)

> **Mô tả PR / Summary:**
> 
> 

---

### 🔍 15-Item Checklist (Tất cả items phải pass trước khi Merge)

#### 💻 1. CODE (Chất lượng mã nguồn)
- [ ] **01. Local Build & Syntax:** Code chạy được locally — không có compile/syntax errors (`dotnet build`, `npm run build` pass).
- [ ] **02. Debug Cleanup:** Không có `console.log`, `debugger`, `print(`, hoặc debug statements còn sót lại.
- [ ] **03. No TODOs:** Không còn `TODO` / `FIXME` / placeholder code do Agent để lại trong mã merge.
- [ ] **04. Naming Conventions:** Tuân thủ quy ước đặt tên trong `AGENTS.md` (PascalCase cho C#, camelCase cho TS/JS).
- [ ] **05. Strict Typing:** Không sử dụng type `any` trong TypeScript (`@typescript-eslint/no-explicit-any`).

#### 🧪 2. TEST (Kiểm thử & Độ tin cậy)
- [ ] **06. Zero Regression:** Tất cả existing tests vẫn pass (`dotnet test`, `npm test`).
- [ ] **07. Business Logic Coverage:** Viết unit tests cho tất cả business logic / service mới (≥1 test per logic branch).
- [ ] **08. Coverage Baseline:** Test coverage không giảm so with baseline hiện tại.

#### 🛡️ 3. SEC (Bảo mật & Safe Data)
- [ ] **09. Secrets Scan:** Không có API keys, connection strings, passwords hardcode trong code (sử dụng env variables).
- [ ] **10. Input Validation:** Đã implement schema validation (FluentValidation / Zod / Joi) cho tất cả endpoints mới.
- [ ] **11. Injection Protection:** SQL queries dùng ORM / Parameterized queries, không nối chuỗi raw SQL.

#### 🎯 4. SPEC (Đúng đặc tả & Không dư thừa)
- [ ] **12. Spec Criteria:** Code implement đúng 100% acceptance criteria trong `SPEC.md`.
- [ ] **13. No Feature Creep:** Agent không tự ý thêm các tính năng nằm ngoài scope được định nghĩa.

#### 📚 5. DOC (Tài liệu API)
- [ ] **14. API Documentation:** Cập nhật Swagger / OpenAPI spec cho bất kỳ endpoint nào được thêm mới hoặc chỉnh sửa.

#### ⚡ 6. PERF (Hiệu năng DB & Cấu trúc)
- [ ] **15. No N+1 Queries:** Đã kiểm tra và optimize DB queries (dùng `Include` / `eager loading` / batching khi cần).
