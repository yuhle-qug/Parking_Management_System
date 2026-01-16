# Hoàn thiện thanh toán online (chặn tiền mặt)

## Mục tiêu
- Chỉ cho phép phương thức online/QR; từ chối "Cash".
- Lưu giao dịch với trạng thái Pending/Success/Failed/Cancelled, có retry và timeout.
- Callback/hủy từ gateway cập nhật trạng thái và mở cổng đúng gate.

## Các bước triển khai
1) **Model/Entity**: mở rộng `Payment` (PaymentId, Status, Attempts, ProviderLog, TransactionCode, Method limited) nếu cần field mới; kiểm tra [backend/Parking.Core/Entities/CoreEntities.cs].
2) **Interface**: cập nhật `IPaymentService.ProcessPaymentAsync` để bắt buộc `method` thuộc tập online; loại bỏ/throw khi `method` là Cash; thêm tham số metadata nếu cần.
3) **Controller**: chỉnh [backend/Parking.API/Controllers/PaymentController.cs] để validate method != Cash, tạo giao dịch Pending, gọi service; trả về TransactionCode, QrContent, Status; thêm lỗi timeout/gateway.
4) **Gateway adapter**: đảm bảo `IPaymentGateway.RequestPaymentAsync` hỗ trợ timeout/retry từ service; log ProviderMessage/Error; mock gateway có thể trả Accepted=false để test nhánh Failed.
5) **State machine**: trong `PaymentService`, thiết lập `Payment.Status` = PendingExternal, đổi sang Completed/Failed/Cancelled ở các hàm confirm/cancel; lưu Attempts, ProviderLog; chỉ `OpenGateAsync` khi Completed và session Closed.
6) **Callback/hủy**: controller callback/cancel ghi nhận trạng thái, không mở cổng khi Failed/Cancelled; trả thông báo rõ ràng.
7) **UI/frontend** (nếu có): ẩn/disable tùy chọn Cash, hiển thị mã QR + trạng thái đang chờ; handle retry.
8) **Test manual/auto**: tạo session pending payment, thử phương thức Cash -> 400; mock gateway fail -> Status=Failed; success -> Completed + cổng mở.
