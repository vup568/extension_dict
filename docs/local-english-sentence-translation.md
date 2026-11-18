# Dịch câu tiếng Nhật sang tiếng Anh trên máy

> Cấu hình runtime hiện đã chuyển sang Gemini. Xem [hướng dẫn Gemini](local-gemini-translation.md); hướng dẫn DeepL dưới đây chỉ là thông tin cũ, không còn phản ánh cấu hình mặc định.

Chức năng dịch dùng DeepL API qua backend. Khi bạn nhấn **Dịch sang tiếng Anh**, nội dung được chọn sẽ được gửi đến DeepL. Tra từ và xem kanji tiếp tục dùng từ điển cục bộ; việc dịch chỉ diễn ra sau khi bạn nhấn nút.

## Chuẩn bị khóa

1. Đăng ký một gói DeepL API phù hợp tại [trang giá DeepL](https://www.deepl.com/pro) và tạo khóa tại [API Keys](https://www.deepl.com/your-account/keys). Xem [thông tin gói API](https://support.deepl.com/hc/en-us/articles/360021200939-DeepL-API-plans) và [hướng dẫn xác thực](https://developers.deepl.com/docs/getting-started/auth) để kiểm tra hạn mức và endpoint của gói bạn chọn.
2. Giữ khóa riêng tư: không gửi qua chat, không đưa vào source code hoặc extension.

## Chạy backend có cấu hình dịch

Mở Docker Desktop. Nếu backend cũ đang dùng cổng 5000, cần dừng nó trước để tiến trình mới nhận được khóa. Có thể xem PID đang chiếm cổng bằng `Get-NetTCPConnection -LocalPort 5000 -State Listen`, sau đó kiểm tra PID bằng `Get-Process -Id <PID>` và chỉ dừng nếu đó đúng là backend dự án.

Trong PowerShell 7 tại thư mục dự án, nhập khóa vào prompt ẩn và khởi động backend từ cửa sổ đó:

```powershell
$secureDeepLKey = Read-Host 'Nhập khóa DeepL API' -AsSecureString
$env:Translation__DeepL__ApiKey = [System.Net.NetworkCredential]::new('', $secureDeepLKey).Password
pwsh -File .\scripts\start-real-dictionary.ps1
```

Khóa chỉ được đặt trong môi trường của cửa sổ PowerShell hiện tại và được truyền cho tiến trình backend; đóng cửa sổ khi không cần dùng. Không ghi khóa vào lệnh hoặc tệp của dự án. Nếu một backend cũ đã chiếm cổng 5000, dừng backend đó trước. Quá trình khởi động có thể mất vài phút để kiểm tra từ điển.

Theo tài liệu DeepL, API Free cũ dùng `api-free.deepl.com` và key có hậu tố `:fx`; API Pro dùng `api.deepl.com`. Backend tự chọn host từ hậu tố key; nếu thông tin tài khoản DeepL của bạn yêu cầu host khác trong hai host này, cấu hình trước khi chạy backend:

```powershell
$env:Translation__DeepL__Plan = 'Free' # Hoặc 'Pro', theo endpoint của gói API
```

Các tên `Free` và `Pro` ở đây là tên tùy chọn kỹ thuật cho hai host, không phải khẳng định về tên gói đang bán. Kiểm tra thông tin endpoint của gói mới trong tài khoản DeepL; tính tương thích của khóa/gói mới cần được xác nhận bằng yêu cầu dịch thật.

## Thử trong extension

Build extension nếu cần, nhấn **Reload** tại `chrome://extensions` và tải lại trang đang đọc. Bôi đen câu tiếng Nhật, xem thông báo trong mục dịch, rồi nhấn **Dịch sang tiếng Anh**. Ví dụ: `私は日本語を勉強しています。`.

Nếu hiện thông báo dịch vụ chưa khả dụng, kiểm tra khóa, endpoint của gói, hạn mức và kết nối mạng. Các lỗi dịch không xóa kết quả tra từ. Bản dịch do máy tạo có thể sai hoặc thiếu ngữ cảnh.
