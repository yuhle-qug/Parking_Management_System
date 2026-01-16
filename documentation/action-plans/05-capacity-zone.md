# Sức chứa & zone (bãi đầy theo gate, trừ slot đúng thời điểm)

## Mục tiêu
- API báo bãi đầy theo gate/zone.
- Slot chỉ trừ khi xe thực sự ra (sau thanh toán/mở cổng), không khi PendingPayment.
- Map gate→zone cho check-out/mở cổng đúng.

## Các bước triển khai
1) **Repository**: thêm hàm `CountActiveByGateAsync` (dựa trên sessions + zone mapping) hoặc precompute từ zones/gateIds.
2) **API**: tạo endpoint `GET /Zones/status?gateId=` trả capacity/active/available; trả thông báo "Bãi đầy" khi available=0.
3) **Session lifecycle**: trong `ParkingService.CheckOutAsync`, đừng giảm capacity khi set PendingPayment; chỉ sau thanh toán thành công (`PaymentService.ConfirmExternalPaymentAsync`) mới Close session và giải phóng slot.
4) **Gate mapping**: lưu `ExitGateId` trong session/payment; `OpenGateAsync` dùng gate ra (ưu tiên request.ExitGateId nếu có, fallback ticket gate). Đảm bảo zone selection khi check-in dựa gate; khi check-out, validate gate belongs to that zone if cần.
5) **Data**: cập nhật `sessions.json` schema nếu thêm ExitGateId.
6) **Frontend**: ở Dashboard hiển thị trạng thái bãi theo gate/zone; chặn check-in khi API trả đầy.
7) **Test**: mô phỏng đủ chỗ -> API trả full; checkout pending không giảm count; thanh toán xong giảm count; gate sai zone -> báo lỗi.
