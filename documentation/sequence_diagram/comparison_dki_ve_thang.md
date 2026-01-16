# So sánh Sơ đồ Tuần tự (dki_ve_thang.txt) và Hệ thống Hiện tại

Tài liệu này so sánh quy trình đăng ký vé tháng được mô tả trong sơ đồ tuần tự (`dki_ve_thang.txt`) với việc triển khai thực tế trong mã nguồn (`MembershipController.cs`, `MembershipService.cs`, `Membership.jsx`).

## 1. Tổng quan
*   **Sơ đồ (Diagram):** Mô tả quy trình tuần tự, chặt chẽ: Kiểm tra -> Tính phí -> Thanh toán thành công -> Tạo vé.
*   **Mã nguồn (Code):** Triển khai theo hướng thực tế (Asynchronous/State-based): Kiểm tra -> Tạo vé (Pending) -> Yêu cầu thanh toán -> Cập nhật trạng thái sau.

## 2. Chi tiết So sánh

### 2.1. Luồng Đăng ký và Tạo vé (Major Mismatch)
*   **Trong Sơ đồ:**
    1.  `MembershipUI` gọi `registerMonthlyTicket`.
    2.  Controller kiểm tra thông tin.
    3.  Hiển thị phí.
    4.  Thực hiện thanh toán (`processPaymentForMembership`).
    5.  **Chỉ sau khi thanh toán thành công**, mới gọi `MonthlyTicket.createMonthlyTicket()`.
*   **Trong Code (`MembershipService.cs`):**
    1.  API `Register` được gọi.
    2.  Kiểm tra khách hàng, xe.
    3.  Thực hiện `_ticketRepo.AddAsync(newTicket)` ngay lập tức với trạng thái `PendingPayment`.
    4.  Sau đó mới gọi `_paymentGateway.RequestPaymentAsync`.
    5.  Trả về thông tin vé kèm mã QR thanh toán.
    *   **Nhận xét:** Code tạo vé *trước* khi thanh toán là hợp lý trong thực tế để track transaction ID và trạng thái thanh toán (pending), tránh mất dấu nếu thanh toán lỗi. Sơ đồ mô tả quy trình lý tưởng "trả tiền rồi mới có hàng".

### 2.2. Luồng Thanh toán
*   **Trong Sơ đồ:**
    *   Tách biệt bước `register` (tính phí) và `processPaymentForMembership` (gọi cổng thanh toán).
    *   `PaymentController` chịu trách nhiệm gọi `ExternalPayment`.
*   **Trong Code:**
    *   Gộp chung: API `Register` trong `MembershipController` tự gọi `_paymentGateway.RequestPaymentAsync`.
    *   Không có bước gọi API riêng từ Frontend để bắt đầu thanh toán sau khi hiện phí (Frontend gọi 1 API `register` duy nhất sau khi user confirm trên modal).
    *   Việc xác nhận thanh toán được xử lý qua `ConfirmPayment` (thủ công) hoặc `PaymentCallback` (tự động webhook).

### 2.3. Kiến trúc và Các lớp (Entities vs Services)
*   **Trong Sơ đồ:**
    *   `MembershipController` gọi trực tiếp các Entity (`Customers`, `Vehicles`, `MonthlyTicket`).
*   **Trong Code:**
    *   Sử dụng mô hình **Controller - Service - Repository**.
    *   `MembershipController` gọi `IMembershipService`.
    *   `MembershipService` gọi `ICustomerRepository`, `IMonthlyTicketRepository`.
    *   Đây là điểm khác biệt về kiến trúc (Code tốt hơn, tách biệt concerns hơn sơ đồ đơn giản).

### 2.4. Chi tiết các hàm
| Bước trong Sơ đồ | Hàm tương ứng trong Code | Trạng thái | Ghi chú |
| :--- | :--- | :--- | :--- |
| `findOrCreateCustomers()` | `MembershipService.RegisterMonthlyTicketAsync` (logic check & add) | **Khớp** | Code tự động kiểm tra và thêm Customer nếu chưa tồn tại. |
| `createOrUpdate()` (Vehicles) | Logic trong `Services` | **Khớp** | Code map thông tin xe và lưu kèm vé (dù không tách riêng Entity Vehicle update rõ ràng nhưng flow logic đúng). |
| `hasActiveTicket()` | `_ticketRepo.FindActiveByPlateAsync` | **Khớp** | Code throw exception nếu xe đã có vé active. |
| `createMembershipFee()` | `CalculateFeeAsync` | **Khớp** | Logic tính tiền dựa trên Policy. |
| `processPaymentForMembership()` | `_paymentGateway.RequestPaymentAsync` | **Lệch** | Code gọi hàm này *bên trong* `Register`, không tách ra API riêng. |
| `createMonthlyTicket()` (Cuối cùng) | `_ticketRepo.AddAsync` (Đầu tiên) | **Lệch** | Code tạo vé trạng thái Pending ngay từ đầu. |

## 3. Kết luận
Hệ thống hiện tại đã triển khai đầy đủ các nghiệp vụ được mô tả, tuy nhiên **thứ tự thực hiện** và **kiến trúc chi tiết** có sự khác biệt so với sơ đồ UML:
1.  **Thứ tự tạo vé:** Code tạo vé sớm hơn (Pending) để quản lý trạng thái thanh toán tốt hơn.
2.  **Kiến trúc:** Code sử dụng Service Pattern thay vì Controller gọi trực tiếp Entity.
3.  **Tích hợp thanh toán:** Code tích hợp chặt chẽ việc yêu cầu thanh toán vào luồng đăng ký thay vì tách rời.

**Đề xuất cập nhật Sơ đồ:** Nên cập nhật sơ đồ tuần tự để phản ánh đúng thực tế logic "Tạo Pending Ticket -> Request Payment -> Callback/Confirm -> Active Ticket".
