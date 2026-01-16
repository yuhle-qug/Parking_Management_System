# Phân tích 3 Chức năng Quản trị (Admin Tab)

Tài liệu này phân tích chi tiết các class tham gia vào 3 chức năng cụ thể trong tab Quản trị (Admin) của frontend, dùng để trả lời câu hỏi về cấu trúc clean code/OOP của hệ thống.

## 1. Quản lý Tài khoản (User Management)
*Nghiệp vụ: Đăng nhập, Tạo tài khoản mới, Xem danh sách, Khóa/Mở khóa tài khoản.*

*   **Số lượng class tham gia trực tiếp:** 8 (chưa tính DTOs)
*   **Chi tiết:**

| Tên Class | Vai trò & Chức năng |
| :--- | :--- |
| **`UserAccountController`** | **Controller**. Tiếp nhận API Login, Create, GetAll, UpdateStatus. Điều phối giữa logic bảo mật và dữ liệu. |
| **`UserAccount`** | **Entity**. Lớp cha chứa thông tin cơ bản: Username, PasswordHash, Role, Status. |
| **`AdminAccount`** | **Entity** (kế thừa). Biểu diễn user Role ADMIN. |
| **`AttendantAccount`** | **Entity** (kế thừa). Biểu diễn user Role ATTENDANT. |
| **`IUserRepository`** | **Repository Interface**. Định nghĩa các phương thức thao tác DB: `AddAsync`, `FindByUsernameAsync`, `GetAllAsync`. |
| **`UserRepository`** | **Repository Implementation**. Thực thi truy vấn DB thực tế (EF Core/In-memory). |
| **`JwtService`** | **Service**. Sinh token xác thực (JWT) khi login thành công. |
| **`BcryptPasswordHasher`** | **Service**. Mã hóa (Hash) và kiểm tra mật khẩu, đảm bảo không lưu password thô. |

---

## 2. Quản lý Bảng giá vé gửi (Ticket Price Policy)
*Nghiệp vụ: Cấu hình giá vé lượt (theo giờ, qua đêm, ngày lễ) cho từng loại xe.*

*   **Số lượng class tham gia trực tiếp:** 3
*   **Chi tiết:**

| Tên Class | Vai trò & Chức năng |
| :--- | :--- |
| **`PricePolicyController`** | **Controller**. Tiếp nhận API CRUD (`GetAll`, `Create`, `Update`, `Delete`). Thực hiện validaton cơ bản (giá > 0). |
| **`PricePolicy`** | **Entity**. Chứa cấu trúc bảng giá phức tạp: `RatePerHour`, `OvernightSurcharge`, `PeakRanges`. |
| **`IPricePolicyRepository`** | **Repository**. Abstraction cho việc lưu trữ Policy. Controller gọi trực tiếp Repo này (Simple CRUD, không qua Service). |

*(Lưu ý: Có thêm `IParkingZoneRepository` được inject vào Controller chỉ để kiểm tra ràng buộc khóa ngoại trước khi xóa).*

---

## 3. Quản lý Bảng giá vé tháng (Membership Price Policy)
*Nghiệp vụ: Cấu hình các gói vé tháng (Giá tiền, Loại xe áp dụng).*

*   **Số lượng class tham gia trực tiếp:** 3
*   **Chi tiết:**

| Tên Class | Vai trò & Chức năng |
| :--- | :--- |
| **`MembershipController`** | **Controller**. (Phần endpoint `/policies`). Tiếp nhận API CRUD gói vé tháng. Gọi trực tiếp Repository để xử lý ghi (Write), gọi Service để đọc (Read). |
| **`MembershipPolicy`** | **Entity**. Chứa thông tin gói: `PolicyId`, `Name`, `MonthlyPrice`, `VehicleType`. |
| **`IMembershipPolicyRepository`** | **Repository**. Quản lý việc lưu trữ các gói vé tháng. Được Controller gọi trực tiếp cho các thao tác `Add`, `Update`, `Delete`. |

*(Lưu ý: `MembershipService` có tham gia vào method `GetAllPolicies` nhưng với chức năng CRUD Create/Update/Delete thì Controller gọi thẳng Repository để tối ưu hóa vì logic đơn giản).*
