# Gia hạn & huỷ vé tháng (ghi lịch sử)

## Mục tiêu
- API gia hạn vé tháng với số tháng và tính phí theo policy.
- API hủy vé tháng có quy tắc (không xóa thẳng), trạng thái Cancelled, có log.
- Lưu lịch sử gia hạn/hủy (auditable).

## Các bước triển khai
1) **Entities/DTO**: thêm `MembershipHistory` (TicketId, Action=Extend/Cancel, Months, Amount, PerformedBy, Time, Note). Lưu trữ tại repo (JSON file mới `membership_history.json`).
2) **Service**: mở rộng `IMembershipService` với `ExtendMonthlyTicketWithPaymentAsync(ticketId, months, performedBy)` và `CancelMonthlyTicketAsync(ticketId, reason, performedBy)`, ghi history mỗi hành động.
3) **Fee calc**: tái sử dụng policy/discount hiện có; khi extend -> set PaymentStatus=PendingExternal, Status=PendingPayment, reset TransactionCode/Qr; gọi PaymentGateway để tạo QR nếu cần.
4) **Controller**: thêm endpoints `POST /Membership/tickets/{id}/extend` và `POST /Membership/tickets/{id}/cancel`; bỏ `DELETE tickets/{id}` hoặc trả 405; đảm bảo validations (months>0, không extend vé đã Cancelled/Expired quá xa).
5) **History repository**: tạo repo JSON đọc/ghi lịch sử; expose `GET /Membership/history?ticketId=` để tra soát.
6) **Frontend**: trên màn Membership thêm nút Gia hạn (chọn số tháng, hiển thị phí, QR), nút Hủy (lý do). Hiển thị bảng lịch sử.
7) **Test**: gia hạn 3 tháng -> fee đúng, ticket PaymentStatus=PendingExternal, history ghi; hủy -> Status=Cancelled, không xóa record; không cho gia hạn vé cancelled.
