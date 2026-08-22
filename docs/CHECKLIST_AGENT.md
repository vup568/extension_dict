# ✅ Checklist: Trước Khi Commit Code Do Agent Tạo

Tệp tài liệu hướng dẫn và bảng tra cứu tiêu chuẩn 15 điểm (15-item checklist) tổ chức theo 6 categories dành cho lập trình viên và AI Agent trước khi commit code hoặc tạo Pull Request (PR).

---

## 📊 Bảng Tiêu Chí Chi Tiết

| # | Cat | Tiêu chí | Ghi chú / Lý do & Cách kiểm tra |
|---|---|---|---|
| **01** | **CODE** | Code chạy được locally — không có compile/syntax errors | `dotnet build`, `npm run build`, `go build` phải pass 100%. |
| **02** | **CODE** | Không có `console.log`, `print`, hoặc debug statements còn sót | Grep check: `console.log\|debugger\|print(` |
| **03** | **CODE** | Không có `TODO` / `FIXME` comments trong code được merge | Agent thường để lại comment TODO chưa hoàn thành. |
| **04** | **CODE** | Code tuân theo naming conventions trong `AGENTS.md` | `PascalCase` cho C#, `camelCase` cho JS/TS, `snake_case` khi cần. |
| **05** | **CODE** | Không có `any` type trong TypeScript (nếu dùng TS) | Linter rule: `@typescript-eslint/no-explicit-any`. |
| **06** | **TEST** | Tất cả existing tests vẫn pass sau thay đổi | Run `dotnet test` / `npm test` — đảm bảo zero regression. |
| **07** | **TEST** | Unit tests được viết cho business logic mới | Mỗi function service/usecase cần ≥1 unit test. |
| **08** | **TEST** | Test coverage không giảm so với baseline | Kiểm tra với `nyc report`, `coverage.py`, hoặc Coverlet (.NET). |
| **09** | **SEC** | Không có secrets, API keys, passwords trong code | Scan secrets (`git-secrets` / manual review). Chỉ dùng env variables. |
| **10** | **SEC** | Input validation được implement cho tất cả endpoint mới | FluentValidation / Zod / Joi schema phải có đầy đủ. |
| **11** | **SEC** | SQL injection không thể xảy ra | Dùng ORM / Parameterized queries. Review tất cả raw DB queries. |
| **12** | **SPEC** | Code implement đúng acceptance criteria trong Spec | Đọc lại `SPEC.md` và check từng criteria. |
| **13** | **SPEC** | Không có feature creep — agent không thêm gì ngoài scope | So sánh git diff với phạm vi yêu cầu ban đầu. |
| **14** | **DOC** | API documentation (Swagger/OpenAPI) được cập nhật | Cập nhật Swagger annotations / OpenAPI spec khi thêm/sửa endpoint. |
| **15** | **PERF** | Không có N+1 queries — ORM queries được optimize | Sử dụng `.Include()` / eager loading hoặc join queries hợp lý. |

---

## 🛠️ Hướng dẫn tích hợp vào Workflow

### 1. Tích hợp tự động vào GitHub PR Template
Tệp `.github/pull_request_template.md` đã được tạo trong repository. Mỗi khi bạn tạo một Pull Request trên GitHub, danh sách checklist này sẽ tự động xuất hiện trong khung mô tả PR.

### 2. Các lệnh kiểm tra nhanh (Quick CLI Commands)
Bạn có thể tự chạy các lệnh kiểm tra sau trước khi commit:

- **Check Build Backend:**
  ```bash
  dotnet build --no-incremental
  ```
- **Check Build Frontend (nếu có):**
  ```bash
  npm run build
  ```
- **Check Tests:**
  ```bash
  dotnet test
  ```
- **Grep Debug statements & TODOs:**
  ```bash
  git grep -n -E "console\.log|debugger|print\(|TODO|FIXME"
  ```
