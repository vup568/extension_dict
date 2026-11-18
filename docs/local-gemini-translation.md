# Dịch câu Nhật–Anh với Gemini

Backend sử dụng `gemini-3.5-flash-lite`; extension chỉ gửi văn bản khi nhấn nút dịch. Theo Google, nội dung gửi qua gói miễn phí có thể được dùng để cải thiện sản phẩm. API key chỉ cấu hình trong tiến trình backend, không đưa vào extension.

1. Mở Docker Desktop và PowerShell 7, sau đó chạy các dòng sau tại máy của bạn:

   ```powershell
   Set-Location 'E:\jp-dict-extension'
   $geminiKey = Read-Host 'Nhập Gemini API key' -AsSecureString
   $env:Translation__Gemini__ApiKey = [System.Net.NetworkCredential]::new('', $geminiKey).Password
   & "$env:USERPROFILE\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe" -File .\scripts\start-real-dictionary.ps1
   ```

2. Đường dẫn PowerShell 7 ở trên là bản hiện có trong môi trường Codex trên máy này; nếu bản này được cập nhật hoặc di chuyển, cần cài PowerShell 7 hoặc dùng đường dẫn `pwsh.exe` khác. Dán API key tại prompt ẩn; không chia sẻ key qua chat. Giữ cửa sổ PowerShell mở. Bước kiểm tra toàn vẹn từ điển có thể mất vài phút.
3. Vào `chrome://extensions`, nhấn Reload ở extension và tải lại tab đọc. Bôi đen một câu và nhấn **Dịch sang tiếng Anh**.

Khi backend dừng, đóng cửa sổ PowerShell để xóa biến môi trường theo phiên. Không có khóa thì chức năng tra từ vẫn hoạt động, còn dịch báo chưa khả dụng. Nếu có lỗi, xem hạn mức model, khả năng truy cập gói miễn phí, kết nối mạng và [hướng dẫn Google AI Studio](https://ai.google.dev/gemini-api/docs/api-key); không gửi khóa hay nội dung riêng tư khi báo lỗi.
