# Test tra từ/F09 bằng database demo

## Chuẩn bị

- Docker Desktop **Linux Engine** đang chạy; images PostgreSQL/tokenizer đã có.
- .NET SDK 10, dependencies đã restore, **PowerShell 7** (`pwsh`); không cần cài thêm package.
- `.env` local như [hướng dẫn backend](local-backend-testing.md); DB gốc phải khác `jp_dict_demo`.
- Extension đã build với backend origin `http://127.0.0.1:5000`.

Đây là **17 mục từ tổng hợp để thử hệ thống**, không phải JMdict hay từ điển thật.
DB demo nằm trong PostgreSQL Docker hiện có; không tạo container mới, không seed DB gốc.

## Chạy và tra thử

1. Nếu backend cũ đang chạy ở cổng 5000, **Ctrl+C** trong cửa sổ đó trước.
   Script không tự dừng process đang chạy.
2. Mở PowerShell, chạy:

```powershell
cd E:\jp-dict-extension
pwsh -NoProfile -File .\scripts\start-demo.ps1
```

Script tạo `jp_dict_demo` nếu chưa có, chạy EF migrations hiện có, nạp 17 fixture
rồi chạy backend với `candidate-20261110.1` và Sidecar. Giữ cửa sổ này mở;
chờ backend lắng nghe tại `http://127.0.0.1:5000`.
Launcher dùng Sidecar `http://127.0.0.1:50051`, khớp cổng IPv4 Compose;
tránh `localhost` ưu tiên IPv6 trên Windows. Không đổi timeout 500ms.

3. Chrome → `chrome://extensions` → **Reload** Extension đang nạp từ
   `E:\jp-dict-extension\extension\dist\chromium`; tải lại tab web thường.
   Nếu chưa có bản build mới, chạy `npm run build` trong thư mục `extension`
   với dependencies đã cài; không cần `npm ci` cho lần thử này.
4. Bôi đen từng từ trong **nội dung trang**, không phải ô nhập liệu:

| Chọn | Kết quả cần thấy |
|---|---|
| `食べる` | Nghĩa tiếng Việt: ăn |
| `食べました` | Gốc `食べる`, giải thích lịch sự/quá khứ |
| `食べられる` | Cả ba cách hiểu: khả năng, bị động, kính ngữ |
| `静かでした` | Gốc `静か`, giải thích tính từ な và lịch sự/quá khứ |
| `学生です` | Gốc `学生`, giải thích danh từ và thể lịch sự |
| `裁判官` | Không có mục từ trong demo; không dùng làm ca kiểm tra có kết quả |

F06 vẫn ưu tiên mục từ exact; dạng đã chia có exact match không nhất thiết có
giải thích F09. Demo không tự tách cả câu thành các từ; hãy chọn riêng từ/dạng chia.

## Quay về DB gốc

**Ctrl+C** để dừng demo, rồi chạy:

```powershell
.\scripts\start-local.ps1
```

Demo không sửa `.env`; connection và các biến process tạm thời được khôi phục
khi script kết thúc. `start-local.ps1` dùng lại cấu hình gốc như trước (vẫn giữ
connection override nếu bạn đã tự đặt). DB gốc hiện chưa có mục từ thật;
quay về DB gốc có thể trả kết quả rỗng. Container và DB demo được giữ lại.

## Dữ liệu và cơ chế an toàn

17 mục từ gốc: `食べる`, `書く`, `読む`, `行く`, `する`, `来る`, `高い`,
`安い`, `よい` (có cả `いい`, `良い`), `静か`, `便利`, `きれい`, `学生`,
`先生`, `𠮷`, `書ける`, `できる`. Hai mục cuối phục vụ kiểm tra exact-first;
`𠮷` phục vụ kiểm tra Unicode. Chúng không đại diện cho độ đầy đủ của từ điển.

Seed chỉ đọc `dictionary_fixtures` trong corpus F09 đã khóa hash, **không nạp
`cases` hoặc đáp án `expected`**. Nghĩa/POS/forms/readings được lưu vào PostgreSQL;
giải thích F09 vẫn do engine và dataset thực thi, không tra bảng đáp án test.
Source manifest/records đánh dấu nguồn tổng hợp, không phải bản phát hành production.

- Chỉ seed qua lệnh opt-in vào DB chính xác `jp_dict_demo` trên loopback;
  không seed khi startup thường hoặc qua HTTP.
- DB mới hoặc DB có marker demo và graph fixture hợp lệ mới được chấp nhận.
  Nạp lại giữ nguyên IDs/số mục từ; dữ liệu lạ, trộn nguồn hay fixture đã bị sửa
  sẽ bị từ chối, **không reset/xóa/ghi đè**.
- Đây không phải công cụ audit toàn bộ schema/ngữ pháp hay importer từ điển thật.

## Kiểm tra và xử lý lỗi

Chỉ kiểm tra cấu hình/hash, không chạy dịch vụ hay ghi DB:

```powershell
pwsh -NoProfile -File .\scripts\start-demo.ps1 -ValidateOnly
```

Chỉ chuẩn bị/nạp DB, không chạy backend:

```powershell
pwsh -NoProfile -File .\scripts\prepare-demo.ps1
```

- Không có Linux Engine: mở Docker Desktop, chờ Engine running rồi chạy lại.
- Cổng 5000 bị chiếm: dừng backend cũ bằng Ctrl+C; không kill process tùy tiện.
- Seed bị từ chối: không sửa corpus, marker hoặc reset DB để vượt kiểm tra;
  gửi lỗi đã bỏ secrets để đối chiếu. Không gửi `.env`/connection string.
- Popup rỗng với từ ngoài danh sách: bình thường; demo chỉ có 17 mục từ.
- `Healthy` không chứng minh mọi tra cứu hoạt động; thử các ca ở bảng trên.
- F09 thiếu giải thích ở lần chọn đầu: cấu hình cũ dùng `localhost` có thể thử
  IPv6 trong khi Compose chỉ mở IPv4. Đã tái hiện lỗi địa chỉ bằng đối chứng
  `localhost → 127.0.0.1 → localhost`; launcher đã đổi sang IPv4. Ctrl+C backend
  cũ rồi chạy lại script cập nhật; không reset DB, sửa `.env`, restart Sidecar
  hoặc tăng deadline. Nếu vẫn lặp lại, gửi ảnh/log đã bỏ secrets để kiểm tra;
  `unavailable` cũng có thể do dependency khác, không mặc định mọi lỗi cùng nguyên nhân.
- Kiểm thử popup tự động dùng package trên **Brave** vì Chrome hiện bỏ qua
  cờ nạp extension tự động; không thay thế việc bạn tự thử Chrome theo bước 3–4.

## Tham khảo

- [Plan demo](../plans/261014-local-demo-dictionary/plan.md)
- [Script chạy demo](../scripts/start-demo.ps1), [script chuẩn bị](../scripts/prepare-demo.ps1)
- [Hướng dẫn backend/DB gốc](local-backend-testing.md)
- [Bằng chứng kiểm thử demo](../plans/261014-local-demo-dictionary/reports/validation-report.md)
- [Plan sửa địa chỉ Sidecar local](../plans/261015-f09-local-sidecar-stability/plan.md)
