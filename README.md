# JP Reading Platform V2

Trợ lý đọc tiếng Nhật dành cho người Việt: Extension mỏng dùng chung backend
.NET 10, PostgreSQL 16 và tokenizer sidecar. Dự án phát triển theo SDD; yêu cầu
đã phê duyệt là nguồn xác định phạm vi sản phẩm.

## Trạng thái triển khai — 2026-10-12

- Backend đã có tra từ vựng, kanji, engine ngữ pháp và `POST /api/analysis` với
  kết quả từng phần. Morphology và conjugation vẫn trả `unavailable`.
- F-01/F-02 đã triển khai; F-02 được merge và push lên `dev` tại
  `e01ee19d2458403d3dc9d9a18df0ffc4e0426dc4` ngày 2026-10-11.
- F-03/F-04 đã triển khai và kiểm chứng trên Chrome/Edge/Brave tại
  `feat/selection-popup-stale-protection`: React 19/open Shadow DOM popup, debounce request
  300 ms, timeout 5 giây, Retry và bảo vệ interaction hiện hành. Native popover
  top layer cách ly transform/overflow của trang; có fixed-host fallback.
  Kết quả được ghi trong [validation report](.sdd/specs/feat-selection-popup/evidence/validation-report.md).
- Delivery vào `dev` qua [PR #10](https://github.com/vup568/extension_dict/pull/10).
  Cây merge đã kiểm chứng: .NET 53 unit tests, 51 PostgreSQL Testcontainers tests;
  Extension typecheck, 223 unit tests và production build đều đạt.
- Web App, dịch thuật, authentication và thư viện học cá nhân chưa triển khai.

[Feature Map](docs/FEATURE_MAP.md) phân biệt mức ưu tiên MVP với tiến độ thực tế.
Firefox vẫn cần kiểm chứng trên executable tương thích; build manifest không
thay cho bằng chứng chạy trình duyệt.

## Chạy backend local

Cần .NET SDK 10, PostgreSQL 16 đã cấu hình và database/dữ liệu tra cứu hiện có.
Lấy connection string từ cấu hình local không commit hoặc secret store, đưa vào
biến môi trường `ConnectionStrings__DefaultConnection`. Không sao chép credential
vào source hoặc tài liệu. Backend áp dụng EF migrations khi khởi động; lệnh này
không tải hay import corpus từ điển.

Nếu cần ngữ pháp, chạy tokenizer sidecar gRPC tương thích và đặt
`GrpcTokenizerAddress` theo địa chỉ local của dịch vụ (mặc định
`http://localhost:50051`). Khi sidecar không sẵn sàng, analysis có thể trả grammar
`failed` cùng kết quả vocabulary/kanji còn khả dụng.

Với dependencies .NET đã restore:

```powershell
dotnet run --project src/interface/WebApi.csproj --no-restore --urls http://127.0.0.1:5000
```

## Build và nạp Extension

Cần Node.js >= 22.12 và npm. Trong môi trường mới, cài đúng dependency lock đã
được phê duyệt rồi build:

```powershell
Set-Location extension
npm ci
npm run typecheck
npm run build
```

Backend origin mặc định `http://127.0.0.1:5000`. Để đổi địa chỉ, đặt trước build:

```powershell
$env:VITE_API_BASE_URL = 'https://api.example.test'
npm run build
```

Chỉ chấp nhận origin không credential/path/query/fragment; HTTPS bắt buộc ngoài
HTTP loopback local. Build tạo permission cho đúng origin đó. Content script
gửi message tới background; background gọi backend, không cần mở CORS cho mọi
trang host. Không đặt API key/private credential trong client env.

- Chrome/Edge/Brave: mở trang quản lý extensions, bật Developer mode, chọn Load
  unpacked và thư mục `extension/dist/chromium`.
- Firefox: dùng Load Temporary Add-on trong `about:debugging`, chọn
  `extension/dist/firefox/manifest.json`. Package đã build; runtime Firefox đang
  chờ verification.
- Cho phép Extension truy cập các website cần đọc rồi tải lại tab sau khi nạp
  hoặc cập nhật Extension. Trang nội bộ trình duyệt và các trang bị trình duyệt
  hạn chế không cho content script chạy.

Chọn tiếng Nhật trong nội dung DOM để tự mở popup. Escape hoặc “Đóng” thoát;
“Thử lại” tạo interaction mới khi lỗi có thể thử lại. Không gửi toàn bộ trang,
không tạo lịch sử hay lưu raw selected text.

## Giới hạn hiện tại

- Selection phải có một DOM Range; selection bên trong `input`/`textarea` chưa
  được hỗ trợ. DOM/contenteditable dùng chính adapter ruby-safe F-02.
- Input tối đa 1.000 UTF-16 code units sau trim; vùng chọn dài bị báo lỗi, không
  tự cắt. Vocabulary có giới hạn riêng 255 units và chưa tách câu/đoạn thành
  từng từ, nên chọn cả đoạn có thể trả vocabulary rỗng hoặc `failed`.
- Deinflection mặc định dùng `NullTokenizerAdapter`; chưa phục hồi dạng gốc
  thực tế. Morphology/conjugation chưa có dữ liệu để trình bày.
- Seed ngữ pháp hiện chỉ có 5 mẫu, chưa chứng minh đủ corpus N5–N4 reviewed.
- Database trống hoặc dependency thiếu có thể cho kết quả rỗng/lỗi từng phần;
  popup không thay thế quá trình nạp và review dữ liệu.

## Kiểm chứng và tài liệu

```powershell
npm test
npm run test:coverage
npm run test:browser
```

Browser tests dùng các trình duyệt đã cài; không tự tải executable. Kết quả
verification và các hạn chế môi trường được ghi tại feature evidence.

Lần kiểm chứng ngày 2026-10-11: 223/223 unit tests, coverage statement 96.74%,
112/112 browser tests (Chrome 37, Edge 37, Brave 37 và packaged MV3 Brave 1),
typecheck/build/Unicode verification và npm audit 0 vulnerabilities. Backend
Release build không warnings/errors, 51/51 unit tests pass. Firefox runtime chưa
chạy; PostgreSQL integration chưa chạy lại được vì Docker startup lỗi. Các
performance sample local chưa thay cho benchmark PERF-001 được phê duyệt.

- [Product Requirements](REQUIREMENT.md), [Design Principles](DESIGN.md).
- [Constitution](.sdd/constitution.md), [Migration Decision](MIGRATION_DECISION.md).
- [F-03/F-04 SPEC](.sdd/specs/feat-selection-popup/SPEC.md),
  [PLAN](.sdd/specs/feat-selection-popup/PLAN.md),
  [TASKS](.sdd/specs/feat-selection-popup/TASKS.md).
- [API Contract](docs/api/mvp-api-contract.md), [Project Memory](.agents/SDD.md).

`dev` là baseline V2. `extension_v1` giữ snapshot legacy; reuse phải theo
Migration Decision và regression corpus đã phê duyệt.
