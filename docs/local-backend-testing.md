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
`GrpcTokenizerAddress=http://localhost:50051` và
`ConjugationDatasetVersion=candidate-20261013.1`.
Không in connection/mật khẩu; các biến process được khôi phục khi script kết thúc.

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
- Popup vẫn báo lỗi: gửi ảnh và log lỗi đã bỏ secrets; không gửi `.env`.

## Tham khảo

- [README](../README.md)
- [Script local](../scripts/start-local.ps1)
- [Báo cáo F09](../.sdd/specs/feat-conjugation-explanation/evidence/validation-report.md)
