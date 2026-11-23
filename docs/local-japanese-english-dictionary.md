# Chạy từ điển Nhật–Anh trên máy

Chỉ dùng hướng dẫn này sau khi lần import `jaen_20261030_v3` đã hoàn tất. Database riêng là `jp_dict_real_jaen_20261030_v3`; cấu hình gốc và `app_db` không được thay đổi. Từ vựng dùng JMdict Nhật–Anh, thông tin kanji dùng KANJIDIC2; không có nghĩa Nhật–Việt trong lần nhập này.

## Khởi động

1. Mở Docker Desktop, kiểm tra PostgreSQL và tokenizer của dự án có thể chạy.
2. Từ PowerShell 7 tại thư mục dự án, chạy:

   ```powershell
   pwsh -File .\scripts\start-real-dictionary.ps1
   ```

   Giữ cửa sổ này mở. Backend chỉ mở cổng sau khi đối chiếu source lock, dữ liệu đã nhập, phiên bản quy tắc chia thể và ngữ pháp mẫu. Kiểm tra toàn bộ dữ liệu có thể mất nhiều phút. Khi bảng ngữ pháp trong database riêng đang trống, startup tạo và kiểm tra 5 mẫu seed hiện có của dự án.
3. Nếu chưa build extension, chạy `npm --prefix extension run build` với các dependencies đã có. Mở `chrome://extensions`, bật chế độ nhà phát triển, chọn **Load unpacked** và chọn `E:\jp-dict-extension\extension\dist\chromium`. Tải lại tab đang đọc, sau đó bôi đen từ tiếng Nhật.
4. Để dừng backend, nhấn Ctrl+C ở cửa sổ PowerShell. Có thể chạy `scripts/start-local.ps1` như trước để dùng database gốc. Lệnh chạy dữ liệu thật chỉ thiết lập biến môi trường cho tiến trình và khôi phục chúng khi thoát; không sửa `.env`.

Tra thử `食べる`, `食べました`, `橋` và `箸`. Nếu cổng 5000 đã được một backend khác sử dụng, cần dừng backend đó trước. Extension được cấu hình kết nối `127.0.0.1:5000`; PostgreSQL và tokenizer phải chạy ở máy này.

Sau khi cập nhật mã, chạy lại `npm --prefix extension run build`, nhấn **Reload** ở extension trong `chrome://extensions` và tải lại tab đang đọc. Có thể bôi đen cả câu `猫を食べました。`: popup hiển thị `猫` và `食べました` với mục từ gốc `食べる`. Câu `私は日本語を勉強しています。` được tra theo từng cụm, trong đó `勉強しています` liên kết đến mục JMdict `勉強`.

## Nâng cấp database sau khi mã có migration mới

Nếu sau khi cập nhật mã, backend dừng ngay với `Database initialization failed` và không mở cổng, database riêng có thể thiếu migration mới (ví dụ `20261118075656_AddYcoConjugationCandidate` thêm bộ quy tắc `candidate-20261110.1`). Khởi động không bao giờ tự sửa database riêng. Hãy dừng mọi backend đang dùng database này, sao lưu nếu cần, rồi chạy lệnh nâng cấp một lần từ thư mục dự án (Windows PowerShell 5.1 hoặc PowerShell 7):

```powershell
. .\scripts\local-backend-config.ps1
$values = Read-LocalPostgresEnvironment -Path (Resolve-Path -LiteralPath .env).ProviderPath
$values['POSTGRES_DB'] = 'jp_dict_real_jaen_20261030_v3'
$env:ConnectionStrings__DefaultConnection = New-LocalBackendConnectionString -Values $values
dotnet run --project .\src\interface\WebApi.csproj --no-restore -- --dictionary-import upgrade --source-lock .\data\dictionary-import\audit-20261015\source-lock.ja-en-v3.json --run-id jaen_20261030_v3 --confirm-target jp_dict_real_jaen_20261030_v3
Remove-Item Env:\ConnectionStrings__DefaultConnection
```

Lệnh kiểm tra source lock, quyền sở hữu, checkpoint và facts hash trước khi ghi. Nó chỉ áp các migration còn thiếu khi chúng chỉ thêm bộ quy tắc chia thể, rồi kiểm tra lại và báo `facts_hash` không đổi. Migration đổi bảng hoặc lịch sử migration có lỗ hổng sẽ bị từ chối mà không ghi gì. Không thể gỡ ngược một bộ quy tắc đã thêm, nhưng có thể chọn lại bộ cũ bằng `-DatasetVersion`. Sau khi nâng cấp, khởi động như mục trên.

Kết quả phụ thuộc vào cách Sudachi tách câu, dữ liệu JMdict và các quy tắc chia thể hiện có. Không phải mọi cụm hay thể chia đều được nhận diện; nếu việc tra bị giới hạn, popup sẽ báo rằng kết quả chỉ là một phần.

## Nguồn và giới hạn

- JMdict Japanese/English và KANJIDIC2 được cung cấp bởi Electronic Dictionary Research and Development Group theo [điều kiện sử dụng EDRDG](https://www.edrdg.org/edrdg/licence.html). Mã SKIP thuộc Jack Halpern / Kanji Dictionary Publishing Society: [quyền và ghi nhận](https://www.kanji.org/dictionaries/skip_permission.htm).
- Lần chạy này dành cho thử nghiệm local; trước khi phát hành cần hoàn thiện ghi nguồn, điều kiện giấy phép và chính sách cập nhật cho người dùng.
- Một sense của mục JMdict `1339160` có hai điều kiện nguồn mâu thuẫn. Dữ liệu và điều kiện gốc được giữ nguyên, nhưng sense đó không hiển thị qua cặp chữ–cách đọc bất khả thi. Không sửa hoặc suy đoán nghĩa.
- Kiểm tra toàn vẹn phản ánh trạng thái tại thời điểm khởi động, không khóa database chỉ đọc trong suốt phiên. Không chạy import hay sửa database đồng thời với backend.
- Database candidate hiện dành cho thử tra cứu local: cơ chế kiểm tra sẽ từ chối khởi động lại nếu database chứa dữ liệu người dùng hoặc dữ liệu học. Không dùng database này làm kho lưu dữ liệu học cá nhân.
- Bộ ngữ pháp hiện có 5 mẫu, chưa phải toàn bộ ngữ pháp tiếng Nhật; việc tra thể chia chưa được bảo đảm phủ mọi trường hợp.
