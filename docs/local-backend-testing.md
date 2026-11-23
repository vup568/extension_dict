# Chạy backend local để test Extension/F09

## Chuẩn bị

- .NET SDK 10 và dependencies đã restore; không cài thêm package bằng script này.
- Docker Desktop Linux Engine đang chạy; images PostgreSQL 16 và tokenizer đã có.
  Nếu thiếu image, cần build/tải riêng sau khi được phê duyệt; script không build.
- Root `.env` local, không commit, có đủ `POSTGRES_DB`, `POSTGRES_USER`,
  `POSTGRES_PASSWORD`, `POSTGRES_PORT`. Không gửi file này hoặc connection string
  vào chat/log/source.
- Extension Chromium đã build/nạp; backend origin mặc định `http://127.0.0.1:5000`.

`.env` chỉ hỗ trợ giá trị literal **một dòng**, không nội suy biến hay multiline.
Với mật khẩu có dấu nháy, `#`, `$` hoặc khoảng trắng, dùng giá trị bọc nháy đơn
theo cú pháp `.env`; không dùng giá trị nội suy/escape nháy kép. Script đọc đúng
bốn key PostgreSQL, không thực thi nội dung file.

## Chạy và test

1. Nếu backend cũ đang chiếm cổng 5000, **Ctrl+C** trong cửa sổ đang chạy nó.
   Script không tự dừng/restart process cũ.
2. Trong Windows PowerShell 5.1 hoặc PowerShell 7:

```powershell
cd E:\jp-dict-extension
.\scripts\start-local.ps1 -ValidateOnly
.\scripts\start-local.ps1
```

`-ValidateOnly` chỉ kiểm tra cấu hình `.env`; không xác thực kết nối DB,
không chạy Docker/backend hay thay đổi database.

Lệnh chạy thật tạo connection bằng `DbConnectionStringBuilder`, giữ giá trị
`ConnectionStrings__DefaultConnection` nếu bạn đã đặt trong process, bật/chờ
`db` và `tokenizer` bằng Compose `--no-build --wait`, rồi chạy API với
`GrpcTokenizerAddress=http://127.0.0.1:50051` và
`ConjugationDatasetVersion=candidate-20261110.1`.
Không in connection/mật khẩu; các biến process được khôi phục khi script kết thúc.
Địa chỉ Sidecar dùng IPv4 để khớp cổng Compose; không dùng `localhost` ở cấu hình
local này vì Windows có thể ưu tiên IPv6 `::1` chưa được publish.

**Backend sẽ tự áp dụng EF migrations hiện có vào database được cấu hình khi bạn
chạy lệnh thật.** Việc này thêm schema/dataset cần thiết, không reset volume hoặc
import JMdict. Nếu dùng connection được đặt riêng, migrations áp dụng vào DB đó,
không nhất thiết là DB Compose. Đây là chạy thử local, không publish production.

3. Chờ backend báo đang lắng nghe trên cổng 5000. Mở
   [health/live](http://127.0.0.1:5000/health/live): `Healthy` chỉ xuất hiện sau khi
   startup migration hoàn tất; không chứng minh dữ liệu từ điển hay tokenizer đủ.
4. Chrome → `chrome://extensions` → Reload Extension; tải lại trang web thường.
   Bôi đen một từ hoàn chỉnh trong nội dung DOM, không phải `input`/`textarea`:

- `食べました` → gốc `食べる`, giải thích lịch sự/quá khứ.
- `食べられる` → các cách hiểu khả năng, bị động, kính ngữ.
- `食べさせられる` → các cách hiểu sai khiến-bị động và khả năng sai khiến.
- `静かでした`, `学生です` → tính từ な/danh từ kèm giải thích tiếng Việt.

Các ca trên cần canonical mục từ gốc/POS tương ứng trong DB. Database mới trống
không tự có những mục từ này. Nếu dạng đã chia có exact dictionary match, F06
ưu tiên exact lookup và không gọi deinflection: thiếu phần giải thích khi đó
không nhất thiết là lỗi F09.

## Thử ngay với DB demo riêng

DB gốc hiện chưa có từ điển. Muốn thử các ca trên với dữ liệu mẫu, dừng backend
cũ rồi chạy **PowerShell 7**:

```powershell
cd E:\jp-dict-extension
pwsh -NoProfile -File .\scripts\start-demo.ps1
```

DB `jp_dict_demo` dùng PostgreSQL Docker hiện có, chứa đúng 17 fixture tổng hợp;
không phải JMdict/import thật, không nạp `cases.expected` và không seed DB gốc.
Reload Extension, tải lại tab rồi tra thử. Ctrl+C và `start-local.ps1` quay về
cấu hình gốc; `.env` không đổi. Xem [hướng dẫn demo](local-demo-testing.md).

## Tùy chọn

- `-SkipDependencies`: không chạy Compose nếu PostgreSQL/tokenizer đã sẵn sàng;
  vẫn đọc `.env` và backend vẫn áp dụng migrations khi chạy.
- `-EnvFile`: chọn file cấu hình local khác; không commit secrets.
- `-DatasetVersion development-20261013.1`: thử dataset cũ 9 quy tắc.
- **Ctrl+C** dừng backend; các container Docker vẫn chạy. Không dùng
  `docker compose down -v` vì có thể xóa dữ liệu PostgreSQL.

## Xử lý lỗi

- Không tìm thấy Docker Linux Engine: mở Docker Desktop và chờ Engine sẵn sàng.
- Script báo image thiếu: cần chuẩn bị image riêng; không tự cài dependencies
  hay build/tải image chỉ để bỏ qua lỗi.
- Startup báo thiếu `ConnectionStrings__DefaultConnection`: dùng script thay
  vì lệnh `dotnet run` đơn lẻ. `.env` không được .NET tự nạp.
- Startup DB thất bại: kiểm tra PostgreSQL và cấu hình local, nhất là biến
  connection cũ đang đặt trong process. Đừng gửi credential để debug.
- Health tốt nhưng popup rỗng/thiếu giải thích: kiểm tra mục từ gốc, pin dataset,
  sidecar và exact-first; health không kiểm tra các điều kiện này.
- Nếu backend cũ dùng Sidecar `localhost:50051`, Ctrl+C rồi chạy lại script đã
  cập nhật để dùng `127.0.0.1:50051`. Không cần reset/migrate lại DB, sửa `.env`,
  restart Sidecar hay tăng timeout 500ms để xử lý lỗi địa chỉ này.
- Popup vẫn báo lỗi: gửi ảnh và log lỗi đã bỏ secrets; không gửi `.env`.

## Phục hồi database local cũ — 2026-10-14

DB local đã được phục hồi và kiểm tra ngày2026-10-14:4 migrations,giữ5 mẫu ngữ pháp,
chưa có từ điển. **Không chạy lại recovery cho DB này**;chỉ chạy `start-local.ps1`.
Các bước bên dưới dành cho một DB legacy khác chưa được phục hồi.

Nếu `CREATE TABLE dictionary_entries` thất bại do bảng đã tồn tại nhưng lịch sử
EF migration rỗng, không xóa bảng/volume hay tự chèn migration IDs. Công cụ dưới
đây chỉ dành cho schema legacy local đã được đối chiếu; không tự chạy lúc startup.

1. Dừng backend bằng **Ctrl+C** và ngắt các client DB khác. Docker PostgreSQL
   phải đang chạy; giữ `.env` local, không gửi credentials. Recovery từ chối
   connection override khác DB Compose và không tự kill các kết nối.
2. Cần **PowerShell 7**, .NET SDK/EF local tool và dependencies đã có. Build
   không restore, rồi rehearsal mặc định:

```powershell
cd E:\jp-dict-extension
dotnet build JPReadingPlatform.sln --no-restore --configuration Release -m:1 -nr:false -p:UseSharedCompilation=false
pwsh -NoProfile -File .\scripts\recover-local-database.ps1
```

Script tạo backup binary trong ignored `data/local-recovery/recovery-20261014-<GUID>`,
restore vào DB clone và tạo DB reference từ migration đầu tiên. Nó đối chiếu
schema, kiểm tra SHA256/count của 17 bảng cũ, baseline trên clone rồi áp dụng
migrations chính thức. Mặc định **không sửa schema/lịch sử DB gốc**; backup và
hai DB rehearsal được giữ lại để kiểm tra, không tự xóa.

3. Chỉ sau khi rehearsal báo `PASS` và bạn đã cho phép ghi DB local gốc:

```powershell
pwsh -NoProfile -File .\scripts\recover-local-database.ps1 -Apply
```

`-Apply` thực hiện lại backup/rehearsal trước, kiểm tra DB gốc không đổi rồi mới
normalize/baseline và chạy pending migrations. Compatibility serial/sequence
và uniqueness legacy mạnh hơn được bảo toàn; không tạo identity mới bằng cách
reset sequence. Các rows cũ phải giữ nguyên qua baseline; migration chính thức
chuyển `knowledge_releases.is_current` sang current pointer, nên kiểm chứng sau
migration loại đúng cột đã chuyển này. Nếu bất kỳ kiểm tra nào thất bại, dừng để
đối chiếu backup/clone; không reset volume hoặc thử chèn lịch sử bằng tay.

4. Sau recovery thành công, chạy lại `.\scripts\start-local.ps1` và kiểm tra
`/health/live`. Startup bình thường vẫn chạy với PowerShell 5.1 như trước.

**Recovery không import JMdict hoặc seed fixture test.** DB local hiện chưa có
mục từ và import đang được hoãn; `Healthy`/HTTP 200 chỉ chứng minh backend chạy,
không chứng minh tra từ hay giải thích F09 có kết quả khi thiếu mục từ gốc.
Backup chứa dữ liệu DB: giữ local, không commit hoặc gửi vào chat.

## Tham khảo

- [README](../README.md)
- [Script local](../scripts/start-local.ps1)
- [Script recovery](../scripts/recover-local-database.ps1)
- [Báo cáo F09](../.sdd/specs/feat-conjugation-explanation/evidence/validation-report.md)
