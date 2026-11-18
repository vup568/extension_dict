# JP Reading Platform V2

Trợ lý đọc tiếng Nhật dành cho người Việt: Extension mỏng dùng chung backend
.NET 10, PostgreSQL 16 và tokenizer sidecar. Dự án phát triển theo SDD; yêu cầu
đã phê duyệt là nguồn xác định phạm vi sản phẩm.

## Trạng thái triển khai — 2026-10-14

- Backend đã có tra từ vựng, kanji, engine ngữ pháp và `POST /api/analysis` với
  kết quả từng phần. F-06 có deinflection toàn từ qua Sudachi/gRPC khi bật dataset;
  F-09 candidate mở rộng có giải thích Việt/segments và nhiều cách hiểu qua contract3;
  corpus .3 có G1/G2 đã duyệt ngày2026-10-13 và G3 ngày2026-10-14 (`accepted_for_scoped_corpus`):
  412 ca/60 ô/115 rule IDs đã triển khai, kiểm thử và nghiệm thu trong phạm vi này.
  Production publication và Firefox runtime vẫn là các gate riêng.
  Morphology tổng quát vẫn `unavailable`.
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

Cần .NET SDK 10/dependencies đã restore, Docker Desktop Linux Engine đang chạy,
images PostgreSQL/tokenizer đã có và `.env` local (không commit) chứa
`POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`.
Nếu backend cũ đang chạy trên cổng 5000, dừng bằng **Ctrl+C** tại cửa sổ đó trước.

Trong PowerShell (Windows PowerShell 5.1 hoặc PowerShell 7):

```powershell
cd E:\jp-dict-extension
.\scripts\start-local.ps1
```

Script đọc `.env`, tạo `ConnectionStrings__DefaultConnection` an toàn, bật/chờ
PostgreSQL và tokenizer bằng `--no-build --wait`, pin candidate F09 rồi chạy backend
tại `http://127.0.0.1:5000`. Nếu biến connection đã đặt, script giữ giá trị đó;
không in credential. Script không build image hay import JMdict.

**Khi bạn chạy lệnh này, backend áp dụng EF migrations hiện có vào DB đã cấu hình.**
Không reset volume; database trống vẫn cần nạp dữ liệu từ điển riêng.
Thiếu cấu hình hoặc migration/kết nối DB thất bại sẽ dừng startup, không mở endpoint
`/health/live` rồi mới báo lỗi ở API. Xem [hướng dẫn test local](docs/local-backend-testing.md)
để kiểm tra cấu hình mà không chạy dịch vụ và xử lý lỗi thường gặp.

OD-010 đã chốt **Python + Sudachi qua gRPC** tại
[SPEC F-08 §1.4](.sdd/specs/feat-grammar-detection/SPEC.md#14-key-decision-tokenizer-od-010--resolved).
Code/Dockerfile nằm trong `sidecars/tokenizer/`; Compose có PostgreSQL và tokenizer
trong profile tùy chọn `nlp`. Dictionary ưu tiên exact match, chỉ gọi Sidecar khi
exact không khớp. Không cấu hình dataset vẫn dùng `NullTokenizerAdapter`.

Để thử dataset cũ thay vì candidate, dùng
`.\scripts\start-local.ps1 -DatasetVersion development-20261013.1`.
EF migration cài dataset phát triển vào `conjugation_datasets` (JSONB immutable).
Không UPDATE/DELETE/TRUNCATE nội dung đã cài; bổ sung version mới và đổi cấu hình
pin để dùng nội dung mới. Dataset chứa 9 quy tắc với giải thích tiếng Việt; đây
không phải corpus có linguistic signoff hay KnowledgeRelease production.

Candidate mở rộng dùng pin `ConjugationDatasetVersion=candidate-20261013.1`:
4 nhóm/60 ô/115 rule IDs từ corpus .3 đã duyệt, dữ liệu là các mẫu có tham số,
không phải bảng tra cố định các câu test. Migration mới chỉ thêm version;
chưa chạy trên DB của bạn. Việc cài/pin local cần quyền riêng, không là phát hành production.
Dataset cũ vẫn dùng/replay được. Xem [báo cáo F09](.sdd/specs/feat-conjugation-explanation/evidence/validation-report.md).
Nghiệm thu G3 được ghi trong [acceptance manifest](.sdd/specs/feat-conjugation-explanation/evidence/acceptance-manifest.json);
báo cáo kiểm thử là snapshot trước duyệt, được giữ nguyên để bảo toàn hash.

Nếu cần ngữ pháp, chạy tokenizer sidecar này và đặt
`GrpcTokenizerAddress` theo địa chỉ local của dịch vụ (mặc định
`http://127.0.0.1:50051`, khớp cổng IPv4 Compose). Trên Windows, `localhost` có thể
ưu tiên IPv6 trong khi Compose chỉ mở IPv4, gây lỗi ở lần phân tích đầu.
Launcher local dùng địa chỉ IPv4 này; cấu hình triển khai riêng vẫn được DI giữ nguyên.
Khi sidecar không sẵn sàng, analysis có thể trả grammar
`failed` cùng kết quả vocabulary/kanji còn khả dụng.

Nếu chạy `dotnet run` thủ công, phải cung cấp
`ConnectionStrings__DefaultConnection` từ cấu hình local/secret store trong cùng
cửa sổ trước; `dotnet run` không tự đọc `.env`. Không sao chép credential vào source
hoặc tài liệu.

### Phục hồi database local cũ — 2026-10-14

Nếu startup tạo lại `dictionary_entries` đã tồn tại vì lịch sử EF migration rỗng,
không reset volume hoặc tự chèn lịch sử. Có công cụ phục hồi **một lần**: sao lưu
binary, restore vào clone, đối chiếu schema/dữ liệu rồi thử migrations chính thức.
Mặc định chỉ sửa clone; `-Apply` mới ghi DB Compose gốc sau khi rehearsal đạt.
Cần **PowerShell 7**, Release build với dependencies đã restore, backend đã dừng
và các client DB đã ngắt kết nối. Startup thông thường vẫn hỗ trợ PowerShell 5.1.

Xem [quy trình phục hồi an toàn](docs/local-backend-testing.md#phục-hồi-database-local-cũ--2026-10-14).
Backup/clone được giữ lại, không xóa dữ liệu hoặc import từ điển. DB local hiện
chưa có mục từ; việc import đang hoãn, nên backend chạy được chưa đồng nghĩa
tra từ/F09 có kết quả trên dữ liệu thật.

### Tra thử với 17 mục từ demo

DB gốc vẫn trống; để thử kết quả tra từ/F09, dùng **PowerShell 7** và DB riêng
`jp_dict_demo` trong PostgreSQL Docker hiện có:

```powershell
cd E:\jp-dict-extension
pwsh -NoProfile -File .\scripts\start-demo.ps1
```

Dừng backend cũ ở cổng 5000 trước, Reload Extension và tải lại tab rồi chọn
`食べる`, `食べました`, `食べられる`, `静かでした` hoặc `学生です`.
Đây là 17 fixture tổng hợp, **không phải từ điển thật**; không nạp đáp án test,
không sửa `.env` hay seed DB gốc. Ctrl+C rồi chạy `start-local.ps1` để quay về
cấu hình gốc. Xem [hướng dẫn demo](docs/local-demo-testing.md) để biết danh sách,
giới hạn và cách xử lý lỗi; không reset volume khi seed bị từ chối.

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
- Deinflection chỉ chạy khi pin `ConjugationDatasetVersion` và Sidecar sẵn sàng;
  dataset cũ có9 quy tắc; candidate mới bao phủ danh mục .3 đã duyệt, không suy luận
  từ token đầu của câu. Unsupported/missing content trả unavailable; nhiều cách hiểu
  đủ chứng cứ được trả riêng, không tự chọn/xếp hạng nghĩa.
  `POST /api/analysis` mặc định contract1 vẫn ẩn conjugation; Extension gửi
  `X-Contract-Version: 3` để hiện các phương án, base/VI/segments UTF-16.
  Contract1/2 giữ empty/one-or-none; morphology vẫn rỗng. G3 scoped đã được duyệt;
  production publication và Firefox parity chưa được xác nhận.
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

Cập nhật 2026-10-13: mã F-06/F-09 bước đầu và Docker profile đã được bổ sung;
kiểm chứng riêng được ghi tại [Tokenizer validation report](.sdd/specs/feat-tokenizer-deinflection/evidence/validation-report.md).
Các số liệu 2026-10-11/12 phía trên là bằng chứng lịch sử, không phải kết quả
suite mới. Implementation trên feature branch chưa đồng nghĩa merge/deploy.

Cập nhật 2026-10-14: F09 được nghiệm thu G1/G2/G3 cho corpus .3; 412/412 ca mapped,
79 backend unit tests, 118 integration tests, 251 client unit tests, 136 browser tests
(Chrome/Edge/Brave và packaged MV3) cùng 12 Python tests đạt. Không chạy lại suite
chỉ để ghi nhận approval; Firefox vẫn chưa kiểm chứng. Chưa migrate DB của bạn,
commit/push/merge hoặc phát hành production.

Cập nhật 2026-10-15: sửa địa chỉ Sidecar local sang IPv4 để khớp Compose;
79 backend unit, 209 integration, 251 client unit, typecheck/build đạt.
Hai backend mới qua 48 request; ba popup test package Brave qua 18 request,
không prewarm/retry hoặc tăng deadline. DB gốc và `.env` không đổi; đây không
phải chứng nhận latency hoặc Chrome tự động. Xem [bằng chứng ổn định local](plans/261015-f09-local-sidecar-stability/reports/validation-report.md).

- [Product Requirements](REQUIREMENT.md), [Design Principles](DESIGN.md).
- [Constitution](.sdd/constitution.md), [Migration Decision](MIGRATION_DECISION.md).
- [F-03/F-04 SPEC](.sdd/specs/feat-selection-popup/SPEC.md),
  [PLAN](.sdd/specs/feat-selection-popup/PLAN.md),
  [TASKS](.sdd/specs/feat-selection-popup/TASKS.md).
- [API Contract](docs/api/mvp-api-contract.md), [Project Memory](.agents/SDD.md).

`dev` là baseline V2. `extension_v1` giữ snapshot legacy; reuse phải theo
Migration Decision và regression corpus đã phê duyệt.
