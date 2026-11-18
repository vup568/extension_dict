---
date: 2026-10-15
session: f09-local-sidecar-stability
status: completed
---

# Journal: 2026-10-15 — F09 local Sidecar stability

## Context

Demo popup từng thiếu giải thích F09 ở lần chọn `食べました` đầu tiên nhưng chạy lại đạt.
User phê duyệt Shadow Plan sửa địa chỉ Sidecar local, không tăng deadline, đổi quy tắc,
import từ điển, reset DB hay thực hiện Git delivery.

## What Happened

- Trên máy Windows này, `localhost` phân giải IPv6 `::1` trước IPv4; TCP IPv6 không
  kết nối trong bound 2 giây, IPv4 kết nối 2ms. Compose chỉ publish `127.0.0.1:50051`.
- Đối chứng backend mới `localhost → IPv4 → localhost` cho thấy first polite-past
  thất bại 626/612ms với localhost, thành công 142ms với IPv4. Đây là mẫu chẩn đoán,
  không phải chứng nhận latency tổng quát; RPC status lịch sử không được ghi nhận.
- Ba default ở DI, appsettings và launcher đổi sang IPv4. DI vẫn giữ địa chỉ được
  cấu hình tường minh cho deployment; launcher local vẫn khôi phục env khi kết thúc.
- Regression trước sửa: 4 lỗi mong đợi/12 ca đạt; sau sửa: 16/16 đạt.
- Build không warning/error; 79 unit, 209 integration, 251 frontend unit,
  typecheck/build đều đạt. Format kiểm tra đúng các file đổi và 16 ca targeted đạt.
- Hai backend mới không override địa chỉ gRPC, lần lượt kiểm tra DI fallback và
  appsettings: 48 request/24 cặp exact→past đạt; first past 121ms/112ms.
- Chạy launcher demo với backend mới rồi ba profile Brave độc lập: 3/3 popup test,
  18 request đạt, không RPC prewarm/retry; deadline vẫn 500ms. Đây là bằng chứng
  package Brave, không phải Chrome tự động hay chứng nhận không bao giờ có lỗi.
- Inventory/hash 20 bảng DB gốc và hash `.env` không đổi. Agent chỉ dừng backend
  kiểm thử của mình; không reset DB, tải dependency hoặc thực hiện Git delivery.

## Reflection

Chạy lại thành công không chứng minh lỗi đã hết. Đối chứng đảo địa chỉ giúp phân biệt
fault transport khởi động với lỗi dữ liệu chia từ mà không nới deadline 500ms hoặc
thêm retry che lỗi. Exact-first vốn không cần F09, nên không dùng trạng thái đó làm ca lỗi.

## Decisions Made

| Decision | Rationale | Impact |
|---|---|---|
| Default local dùng IPv4 | Khớp đúng port Compose đã publish | Không phụ thuộc IPv6-first localhost |
| Giữ explicit DI override | Container/remote/IPv6 deployment có địa chỉ riêng | Không rewrite cấu hình đã chỉ định |
| Giữ deadline/dataset/API/schema | Lỗi tái hiện nằm ở kết nối local | Không đổi phạm vi ngôn ngữ đã nghiệm thu |

## Next Steps

Gate ổn định local đã đạt; xem [plan](../../plans/261015-f09-local-sidecar-stability/plan.md)
và [báo cáo](../../plans/261015-f09-local-sidecar-stability/reports/validation-report.md).
Tiếp theo lập SPEC/PLAN import từ điển thật riêng, kiểm tra nguồn/license; không tải dữ liệu hay
ghi DB gốc từ quyền sửa địa chỉ này. Không commit/push/merge nếu chưa được cho phép.
