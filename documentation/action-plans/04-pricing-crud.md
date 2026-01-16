# Bảng giá cấu hình (PricePolicy/MembershipPolicy CRUD)

## Mục tiêu
- Bỏ hard-code PricePolicy, cho phép CRUD lưu trữ JSON/DB.
- Hỗ trợ khung giờ, qua đêm, max daily, phí mất vé cấu hình.
- CRUD MembershipPolicy để chọn gói/chu kỳ vé tháng.

## Các bước triển khai
1) **Storage**: tạo file `price_policies.json` và dùng repo mới `PricePolicyRepository` (BaseJsonRepository). Chuyển seed từ `ParkingZoneRepository` sang repo này; zones chỉ tham chiếu `PricePolicyId`.
2) **Entity**: mở rộng `PricePolicy` để chứa rules (ratePerHour, peakRanges, overnightSurcharge, dailyMax, lostTicketFee, vehicleType, gateIds optional). Đảm bảo serializable.
3) **Repository updates**: sửa `ParkingZoneRepository` để load PricePolicy từ repo theo id; seed zones không nhúng policy object mà reference id.
4) **Services/API**: tạo `PricePolicyController` với CRUD (list/create/update/delete). Thêm validation để không xóa policy đang được zone dùng.
5) **MembershipPolicy**: tương tự, tạo CRUD controller/service/repo để quản lý gói vé tháng (vehicleType, name, monthlyPrice, duration options, discount rules).
6) **Fee calculation**: cập nhật `ParkingFeePolicy.CalculateFee` để đọc theo cấu hình (peak ranges, overnight, daily max) thay vì hằng số; inject repo nếu cần hoặc map trước vào session.
7) **Frontend Admin**: thêm UI quản lý bảng giá & gói membership (form, list, link zones). Cho phép chọn policy cho zone.
8) **Migration/seed**: tạo seed mặc định trong repo nếu file trống; cập nhật `zones.json` để chứa PricePolicyId.
9) **Test**: tạo policy với peak/overnight/dailyMax, gắn zone, check-out để verify tính phí; CRUD endpoints trả JSON đúng.
