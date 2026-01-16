# Phân tích 3 Use Case Chính của Admin

Tài liệu này đối chiếu mã nguồn hệ thống với 3 chức năng quản trị chính để xác định các lớp (classes), vai trò và mô hình thiết kế được sử dụng.

## 1. Quản lý Vé Tháng (Membership Management)

**Chức năng & Vai trò:**
Cho phép nhân viên và quản trị viên thực hiện các nghiệp vụ liên quan đến khách hàng dài hạn: đăng ký vé tháng mới, gia hạn vé sắp hết hạn, hủy vé, và duyệt các yêu cầu hủy vé từ nhân viên.

**Số lượng class chính:** ~8 class
**Chi tiết các lớp:**

| Loại (Layer) | Tên Class | Vai trò & Chức năng |
| :--- | :--- | :--- |
| **Controller** | `MembershipController` | Tiếp nhận request từ Frontend (Membership.jsx). Điều hướng xử lý Đăng ký (`Register`), Gia hạn (`ExtendTicket`), Hủy (`CancelTicket`), và Duyệt hủy (`ApproveCancelTicket`). |
| **Service** | `MembershipService` | Chứa logic nghiệp vụ phức tạp: Kiểm tra logic ngày hết hạn, chuyển đổi trạng thái vé (PendingPayment -> Active), validate dữ liệu khách hàng. |
| **Repository** | `MonthlyTicketRepository` | Tương tác trực tiếp với Database để CRUD bảng `MonthlyTickets`. |
| **Repository** | `MembershipPolicyRepository`| Quản lý các gói cước vé tháng (Price/Month, VehicleType) để Service tham chiếu tính tiền. |
| **Entity** | `MonthlyTicket` | **Aggregate Root**. Chứa dữ liệu vé (StartDate, EndDate, Status) và các phương thức `Activate()`, `SetPaymentFailed()` (Domain Logic). |
| **Entity** | `Customer` | Thông tin khách hàng (Name, Phone, IdentityNumber). |
| **Entity** | `Vehicle` | Thông tin xe (LicensePlate, Type). Sử dụng Factory Pattern (`IVehicleFactory` - ngầm định hoặc switch case trong Controller/Service) để tạo instance. |

---

## 2. Báo cáo & Thống kê (Reporting System)

**Chức năng & Vai trò:**
Cung cấp cái nhìn tổng quan về hiệu quả hoạt động: doanh thu theo thời gian, mật độ xe vào ra (Traffic), và theo dõi các sự cố vận hành (Mất vé, lỗi hệ thống). Hệ thống áp dụng **Strategy Pattern** và **Factory Pattern** ở đây để dễ dàng mở rộng thêm các loại báo cáo mới.

**Số lượng class chính:** ~8 class
**Chi tiết các lớp:**

| Loại (Layer) | Tên Class | Vai trò & Chức năng |
| :--- | :--- | :--- |
| **Controller** | `ReportController` | Endpoint chính (`RequestReport`). Nhận tham số `type` (Revenue/Traffic) và ủy quyền cho Factory lấy chiến lược xử lý phù hợp. |
| **Controller** | `IncidentController` | Endpoint phụ trợ, xử lý việc ghi nhận và truy vấn danh sách sự cố (`Incident`). |
| **Factory** | `ReportFactory` | Quyết định khởi tạo `IReportStrategy` nào dựa trên tham số input string (VD: "REVENUE" -> `RevenueReportStrategy`). |
| **Interface** | `IReportStrategy` | Định nghĩa giao diện chung `GenerateReportAsync(from, to)` cho tất cả các loại báo cáo. |
| **Strategy** | `RevenueReportStrategy` | Logic tính toán tổng doanh thu từ các lượt xe (`ParkingSession`) và vé tháng (`MembershipHistory`). |
| **Strategy** | `TrafficReportStrategy` | Logic tính toán lưu lượng xe vào/ra theo giờ (`HourlyTraffic`). |
| **Repository** | `ParkingSessionRepository` | Cung cấp dữ liệu thô về các lượt gửi xe để tính toán. |
| **Entity** | `Incident` | Đại diện cho một sự cố (Mất vé, Hỏng cổng rào, v.v.). |

---

## 3. Quản trị Hệ thống (System Administration)

**Chức năng & Vai trò:**
Cấu hình nền tảng vận hành: Quản lý tài khoản nhân viên (Users), Phân quyền (Role), và Thiết lập bảng giá cước (Price Policies). Đây là chức năng dành riêng cho Admin.

**Số lượng class chính:** ~7-8 class
**Chi tiết các lớp:**

| Loại (Layer) | Tên Class | Vai trò & Chức năng |
| :--- | :--- | :--- |
| **Controller** | `UserAccountController` | Quản lý Users: Login (`Login`), Tạo tài khoản (`CreateUser`), Khóa/Mở khóa (`UpdateStatus`). |
| **Controller** | `PricePolicyController` | CRUD các chính sách giá (`PricePolicy`) cho vé lượt (theo giờ, qua đêm). |
| **Service** | `JwtService` | Sinh JWT Token để xác thực người dùng sau khi login thành công. |
| **Service** | `BcryptPasswordHasher` | Mã hóa mật khẩu người dùng (Hashing) để bảo mật, không lưu plain-text. |
| **Repository** | `UserRepository` | Truy vấn và lưu trữ thông tin tài khoản (`UserAccount`). |
| **Repository** | `PricePolicyRepository` | Truy vấn và lưu trữ bảng giá. |
| **Entity** | `UserAccount` | Entity cơ sở cho tài khoản. Có các class con `AdminAccount`, `AttendantAccount` (dùng cho phân quyền logic). |
| **Entity** | `PricePolicy` | Chứa cấu hình giá vé: `RatePerHour`, `OvernightSurcharge`, `VehicleType`. |

---

### Tổng kết kiến trúc
Hệ thống tuân thủ **Layered Architecture** (Controller -> Service -> Repository -> Database) kết hợp với các **Design Patterns** hiện đại ở các module phức tạp:
*   **Module Membership**: Tập trung vào Domain Logic trong Entity và Service.
*   **Module Report**: Sử dụng **Strategy & Factory** để tuân thủ Open/Closed Principle (OCP - Dễ mở rộng báo cáo mới).
*   **Module Admin**: Tập trung vào Security (JWT, Hashing) và CRUD cấu hình.
