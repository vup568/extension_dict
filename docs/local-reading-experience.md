# Thử trải nghiệm đọc trên máy

## Khởi động backend và extension

1. Mở Docker Desktop. Nếu đã nhập từ điển Nhật–Anh vào cơ sở dữ liệu local riêng, làm theo [hướng dẫn chạy từ điển Nhật–Anh](./local-japanese-english-dictionary.md). Dữ liệu mẫu cũng dùng được với [backend demo](./local-demo-testing.md). Khởi động backend từ mã mới để extension gọi được cả tra bằng con trỏ và phát âm.
2. Từ thư mục dự án, build extension bằng dependencies đã có.

   ```powershell
   npm.cmd --prefix extension run build
   ```

3. Trong trang quản lý extension của Chrome, Edge hoặc Brave, nạp thư mục `extension/dist/chromium` hoặc nhấn **Reload** nếu đã nạp. Tải lại tab HTTP(S) có nội dung tiếng Nhật rồi bôi đen một từ.

## Tra cứu và điều khiển popup

- Bôi đen câu như `猫を食べました。` rồi chọn từng vị trí từ trong mục **Từ vựng**. Popup hiển thị mục từ và cách đọc tìm được, cùng giải thích thể chia khi có dữ liệu. Mở **Kanji** hoặc **Ngữ pháp** để xem các mục tương ứng.
- Để bật quét, mở **Cài đặt** trong popup và chọn **Tra từ bằng con trỏ**. Sau đó giữ **Shift** và dừng chuột trên chữ Nhật khoảng 300 ms. Extension gửi tối đa 64 đơn vị UTF-16 quanh con trỏ đến backend tra cứu. Quét mặc định tắt và bỏ qua chú âm ruby, ô nhập liệu và vùng có thể chỉnh sửa. Nhả Shift giữ kết quả đã hiện.
- Bấm **Ghim** để giữ kết quả khi rê chuột qua từ khác. Bôi đen vùng mới vẫn thay kết quả đã ghim. Bấm **Bỏ ghim** để quét tiếp, hoặc dùng **Đóng** hay **Esc** để đóng popup.
- Kéo nút ↘ ở góc dưới bên phải để đổi kích thước. Với bàn phím, mở **Cài đặt** rồi dùng các nút tăng hoặc giảm chiều rộng và chiều cao. Extension lưu kích thước và tùy chọn quét.
- Để dịch, bôi đen câu rồi mở **Dịch câu** và bấm **Dịch sang tiếng Anh**. Mở mục này chỉ hiện thông tin về dữ liệu gửi tới Google. [Cấu hình dịch Gemini](./local-gemini-translation.md)

## Bật giọng đọc AI Gemini

Trong cửa sổ PowerShell 7 dành cho backend, đặt model, giọng và API key trước khi chạy script khởi động. Model hỗ trợ trong cấu hình là `gemini-3.8-flash-lite-tts` và `gemini-3.8-flash-tts`. Giọng có thể chọn là `Kore` hoặc `Aoede`. Tài khoản Google phải có quyền gọi model đã chọn.

```powershell
$env:Speech__Gemini__Model = 'gemini-3.8-flash-lite-tts'
$env:Speech__Gemini__Voice = 'Kore'
$readingSpeechKey = Read-Host 'Nhập Gemini API key cho phát âm' -AsSecureString
$env:Speech__Gemini__ApiKey = [System.Net.NetworkCredential]::new('', $readingSpeechKey).Password
pwsh -NoProfile -File .\scripts\start-real-dictionary.ps1
```

Backend dùng `Translation__Gemini__ApiKey` nếu `Speech__Gemini__ApiKey` để trống. Giữ API key trong tiến trình backend. Đóng cửa sổ PowerShell sau khi dừng backend để xóa các biến của phiên.

Nếu thiếu cache NuGet nhưng đã có `src/interface/bin/Debug/net10.0/WebApi.dll` build từ feature 005, bạn có thể dùng bản đó để nghe mẫu. Đặt model, giọng và key như trên, rồi chạy đoạn sau từ thư mục dự án sau khi Docker Desktop, PostgreSQL và Sudachi đã chạy. Backend xác minh dữ liệu từ điển đã nhập trước khi mở cổng.

```powershell
. .\scripts\local-backend-config.ps1
$readingEnvFile = (Resolve-Path -LiteralPath .env).ProviderPath
$readingDb = Read-LocalPostgresEnvironment -Path $readingEnvFile
$readingDb['POSTGRES_DB'] = 'jp_dict_real_jaen_20261030_v3'
$env:ConnectionStrings__DefaultConnection = New-LocalBackendConnectionString -Values $readingDb
$env:DictionaryImport__LocalTarget = $readingDb['POSTGRES_DB']
$env:DictionaryImport__LocalRunId = 'jaen_20261030_v3'
$env:DictionaryImport__LocalSourceLock = (Resolve-Path .\data\dictionary-import\audit-20261015\source-lock.ja-en-v3.json).Path
$env:DictionaryImport__LocalSourceLockSha256 = (Get-FileHash $env:DictionaryImport__LocalSourceLock -Algorithm SHA256).Hash.ToLowerInvariant()
$env:ConjugationDatasetVersion = 'candidate-20261013.1'
$env:GrpcTokenizerAddress = 'http://127.0.0.1:50051'
Push-Location .\src\interface
try { dotnet .\bin\Debug\net10.0\WebApi.dll --urls http://127.0.0.1:5000 }
finally { Pop-Location }
```

- Trong một mục từ, mở **Phát âm**, chọn cách đọc nếu có nhiều lựa chọn, rồi bấm **Nghe cách đọc**.
- Với câu đã bôi đen, mở **Đọc vùng bôi đen** rồi bấm **Nghe vùng bôi đen**. Khi tra bằng con trỏ, chỉ có tùy chọn nghe cách đọc của mục từ.
- Google nhận cách đọc đã chọn hoặc nội dung bôi đen khi bạn bấm **Nghe**. Google có thể dùng dữ liệu gửi qua gói miễn phí để cải thiện sản phẩm. Extension giải mã WAV bằng Web Audio trong bộ nhớ. **Nghe lại** phát lại bản hiện tại. Đổi kết quả hoặc đóng popup sẽ giải phóng âm thanh.
- Nếu giọng đọc báo chưa khả dụng, kiểm tra model, giọng và API key trong cửa sổ đã khởi động backend. Khi chạm hạn mức, thử lại sau. Nếu đoạn đọc quá dài, chọn đoạn ngắn hơn.

Nếu backend đang chạy nhưng popup báo giọng đọc chưa khả dụng ngay cả với từ đơn, chạy lệnh sau trong cửa sổ PowerShell riêng. Lệnh gửi một lần từ mẫu `ねこ` đến Gemini với model và giọng trong hướng dẫn, rồi hiện mã HTTP và chi tiết lỗi đã che khóa. Nhập khóa vào ô kín khi được nhắc.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File 'E:\jp-dict-extension\extension\tests\demo\check-gemini-speech.ps1'
```

Nếu thấy `Audio delivery mode is not supported`, bản backend đang gửi tham số `response_format.delivery` mà model từ chối. Bản sửa giữ định dạng WAV và dùng cách trả âm thanh mặc định của Gemini. Khởi động lại backend bằng bản DLL được build sau bản sửa.

### Nghe và xác nhận mẫu

Giữ backend đã cấu hình mở. Trong cửa sổ PowerShell khác, từ thư mục dự án, chạy Vite có sẵn trong extension để mở trang mẫu cục bộ.

```powershell
Set-Location .\extension
.\node_modules\.bin\vite.cmd --host 127.0.0.1 --port 4174 --strictPort
```

Mở `http://127.0.0.1:4174/tests/demo/reading-listening-samples.html` trong trình duyệt đã nạp extension. Nghe từng mẫu và ghi `1–4: đạt` hoặc mô tả chữ đọc sai, thừa, thiếu ở mẫu tương ứng. Hai mục `橋` và `箸` cùng có cách đọc `はし`. Hãy kiểm mục từ đang chọn dựa trên nghĩa tiếng Anh. Ghi lại model và giọng đã chọn cùng lỗi hạn mức nếu gặp. Giữ API key trong phiên backend.

## Trạng thái kiểm chứng

Popup và quét Shift đã được kiểm thử trên Edge với PostgreSQL và Sudachi thật. Các bước còn mở gồm ma trận trình duyệt, nghe giọng Nhật từ tài khoản Gemini thật và duyệt độc lập corpus thể chia mới. [Checklist triển khai](../plans/261104-yomitan-reading-experience/task.md)
