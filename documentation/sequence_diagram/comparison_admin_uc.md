# So sánh Mã nguồn & Tài liệu: 2 Use Case Quản trị

Tài liệu này so sánh việc triển khai thực tế (Codebase) của "Quản lý Tài khoản" và "Quản lý Bảng giá" với tài liệu thiết kế hiện có (`p1`, `p2`, `p3`), từ đó đề xuất cập nhật.

## 1. UC Quản lý Tài khoản Người dùng (User Management)

### Hiện trạng
*   **Codebase (`UserAccountController.cs`):**
    *   Có đầy đủ các chức năng: Login (`/login`), Tạo user mới (`/create`), Xem danh sách (`/`), Khóa/Mở khóa (`/{userId}/status`).
    *   Có cơ chế bảo mật: `BcryptPasswordHasher` (Hashing), `JwtService` (Token), `AuditService` (Log).
    *   Phân quyền: Có 2 role chính là `ADMIN` và `ATTENDANT`.
*   **Tài liệu (`p2`, `p3`):**
    *   **p2 (Sơ đồ Use Case):** Có liệt kê "Quản trị viên (Admin): quản lý ... tài khoản". Tuy nhiên, chưa có đặc tả chi tiết (bảng mô tả) cho UC này như các UC operational (Check-in/out).
    *   **p3 (Phân tích Use Case):** Chưa có mục phân tích chi tiết (Class diagram/Sequence) cho "Quản lý tài khoản".

### Đánh giá độ khớp: ⚠️ THIẾU
Tài liệu hiện tại mới chỉ *nhắc đến* Actor Admin có quyền này, nhưng chưa mô tả chi tiết luồng xử lý như trong Code.

### Đề xuất sửa đổi Tài liệu
1.  **Thêm Đặc tả Use Case (vào `p2`):**
    *   Tên: **Quản lý tài khoản người dùng**.
    *   Actor: **Admin**.
    *   Chức năng: Tạo tài khoản cho nhân viên, khóa tài khoản khi nghỉ việc, reset mật khẩu (nếu có).
    *   Luồng chính: Admin đăng nhập -> Vào trang quản trị -> Nhập thông tin (Username, Role) -> Hệ thống tạo & Hash pass -> Lưu DB -> Ghi Log.
2.  **Thêm Phân tích Use Case (vào `p3`):**
    *   Bổ sung Sequence Diagram "Tạo tài khoản mới" (dựa trên `user_management_seq.txt` đã tạo).
    *   Liệt kê Class: `UserAccountController`, `UserRepository`, `PasswordHasher`, `AuditService` (đây là điểm mới so với các UC cũ -> thể hiện tính bảo mật).

---

## 2. UC Quản lý Bảng giá (Price Policy Management)

### Hiện trạng
*   **Codebase (`PricePolicyController.cs`):**
    *   CRUD đầy đủ: `GetAll`, `Create`, `Update`, `Delete`.
    *   Entity `PricePolicy` rất linh động: hỗ trợ Multi-Peak Ranges (giờ cao điểm linh hoạt), Overnight Surcharge, Vehicle Type.
    *   Logic xóa: Kiểm tra ràng buộc (`ZoneRepo`) trước khi xóa để tránh lỗi dữ liệu.
*   **Tài liệu (`p2`, `p3`):**
    *   **p2:** Có nhắc đến Admin "quản lý cấu hình bảng giá".
    *   **p3:** Trong phần phân tích `CheckOutController`, có nhắc đến class `PricePolicy` với method `calculateFee`. Tuy nhiên, chưa có mục riêng để phân tích việc **Admin Thêm/Sửa bảng giá** diễn ra như thế nào.

### Đánh giá độ khớp: ⚠️ THIẾU
Tương tự như User Management, phần này mới chỉ dừng ở mức "nhắc tên" chứ chưa được phân tích kỹ thuật chi tiết về cách Admin cấu hình nó. Code thực tế đã hỗ trợ cấu hình phức tạp (Peak Ranges) mà tài liệu có thể chưa phản ánh hết.

### Đề xuất sửa đổi Tài liệu
1.  **Thêm Đặc tả Use Case (vào `p2`):**
    *   Tên: **Cấu hình bảng giá**.
    *   Actor: **Admin**.
    *   Chức năng: Thiết lập giá vé cho từng loại xe, khung giờ cao điểm, phụ thu qua đêm.
2.  **Thêm Phân tích Use Case (vào `p3`):**
    *   Bổ sung Sequence Diagram "Tạo/Sửa bảng giá" (dựa trên `price_policy_seq.txt` đã tạo).
    *   Cập nhật mô tả Class `PricePolicy`: Nhấn mạnh khả năng cấu hình động (`PeakRanges`, `LostTicketFee`) thay vì chỉ là một class tĩnh tính toán đơn giản.

---

## Kết luận chung
Hệ thống code (Admin Dashboard) đã phát triển đầy đủ các tính năng quản trị mạnh mẽ (Security, Config linh hoạt), trong khi tài liệu hiện tại (`p1`, `p2`, `p3`) đang tập trung nặng vào quy trình vận hành (Operational - Check-in/out).

**Hành động cần thiết:** Cần bổ sung 2 mục lớn vào tài liệu p2 và p3 để phản ánh đúng công sức đã bỏ ra cho phần Backend Admin.
