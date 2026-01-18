# Báo cáo kết quả Unit Test

Tài liệu này chi tiết hóa các trường hợp kiểm thử (test cases) đã được triển khai và kết quả chạy kiểm thử tự động cho hệ thống quản lý bãi xe.

**Ngày chạy**: 17/01/2026
**Trạng thái**: ✅ PASSED (21/21 tests)

---

## 1. Tổng quan Coverage

Hệ thống kiểm thử bao phủ các tầng quan trọng của backend theo kiến trúc Clean Architecture:

*   **Services Layer**: Kiểm thử logic nghiệp vụ cốt lõi ([ParkingService](file:///d:/ParkingManagementSystem/backend/Parking.Services/Services/ParkingService.cs#11-327), [MembershipService](file:///d:/ParkingManagementSystem/backend/Parking.Services/Services/MembershipService.cs#11-283), `Reporting`).
*   **Controller Layer**: Kiểm thử tích hợp API và validation ([CheckInController](file:///d:/ParkingManagementSystem/backend/Parking.API/Controllers/CheckInController.cs#9-70), [MembershipController](file:///d:/ParkingManagementSystem/backend/Parking.API/Controllers/MembershipController.cs#18-25)).

---

## 2. Chi tiết Unit Test

### 2.1. ParkingService ([ParkingServiceTests.cs](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs))
Logic check-in và check-out xe lượt.

| Test Case | Mô tả | Kết quả |
| :--- | :--- | :--- |
| [CheckInAsync_StandardVehicle_Success](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#49-78) | Kiểm thử check-in xe thành công, tạo session và vé, mở cổng. | ✅ Pass |
| [CheckInAsync_VehicleAlreadyIn_ThrowsInvalidOperationException](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#79-92) | Đảm bảo không cho phép một xe check-in 2 lần liên tiếp. | ✅ Pass |
| [CheckInAsync_ZoneFull_ThrowsInvalidOperationException](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#93-115) | Đảm bảo từ chối check-in khi bãi xe đã đầy. | ✅ Pass |
| [CheckOutAsync_StandardVehicle_ReturnsPendingPayment](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#116-153) | Xe lượt ra bãi phải thanh toán phí (PendingPayment). | ✅ Pass |
| [CheckOutAsync_MonthlyTicket_AutoCompletes](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#154-184) | Xe vé tháng ra bãi được đi ngay (Completed), không cần thanh toán. | ✅ Pass |
| [ConfirmPaymentAsync_Success_OpensGate](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ParkingServiceTests.cs#185-204) | Xác nhận thanh toán thành công sẽ mở cổng ra. | ✅ Pass |

### 2.2. MembershipService ([MembershipServiceTests.cs](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/MembershipServiceTests.cs))
Quản lý vé tháng.

| Test Case | Mô tả | Kết quả |
| :--- | :--- | :--- |
| [RegisterMonthlyTicketAsync_NewCustomer_Success](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/MembershipServiceTests.cs#39-68) | Đăng ký vé tháng mới cho khách hàng mới. | ✅ Pass |
| [RegisterMonthlyTicketAsync_ActiveTicketExists_ThrowsException](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/MembershipServiceTests.cs#69-84) | Ngăn chặn đăng ký trùng vé tháng cho cùng 1 xe. | ✅ Pass |
| [ExtendMonthlyTicketAsync_Success](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/MembershipServiceTests.cs#85-116) | Gia hạn vé tháng thành công, cập nhật ngày hết hạn. | ✅ Pass |
| [CancelMonthlyTicketAsync_AsAdmin_CancelsImmediately](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/MembershipServiceTests.cs#117-134) | Admin hủy vé tháng có hiệu lực ngay lập tức. | ✅ Pass |

### 2.3. Reporting ([ReportTests.cs](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ReportTests.cs))
Hệ thống báo cáo sử dụng Factory và Strategy Pattern.

| Test Case | Mô tả | Kết quả |
| :--- | :--- | :--- |
| [GetStrategy_Revenue_ReturnsRevenueReportStrategy](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ReportTests.cs#28-41) | Factory trả về đúng chiến lược báo cáo doanh thu. | ✅ Pass |
| [GetStrategy_Traffic_ReturnsTrafficReportStrategy](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ReportTests.cs#42-55) | Factory trả về đúng chiến lược báo cáo lưu lượng. | ✅ Pass |
| [GetStrategy_Invalid_ThrowsArgumentException](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ReportTests.cs#56-62) | Báo lỗi khi yêu cầu loại báo cáo không tồn tại. | ✅ Pass |
| [RevenueReportStrategy_GenerateReportAsync_CalculatesCorrectly](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Services/ReportTests.cs#63-112) | Tính toán tổng doanh thu chính xác từ các giao dịch thành công. | ✅ Pass |

### 2.4. Controllers

#### CheckInController ([ControllerTests.cs](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/ControllerTests.cs))
| Test Case | Mô tả | Kết quả |
| :--- | :--- | :--- |
| [CheckIn_ValidRequest_ReturnsOk](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/ControllerTests.cs#25-58) | API trả về 200 OK và thông tin vé khi request hợp lệ. | ✅ Pass |
| [CheckIn_ServiceThrows_ReturnsBadRequest](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/ControllerTests.cs#59-79) | API trả về 400 BadRequest khi Service báo lỗi (ví dụ: bãi đầy). | ✅ Pass |

#### MembershipController ([MembershipControllerTests.cs](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/MembershipControllerTests.cs))
| Test Case | Mô tả | Kết quả |
| :--- | :--- | :--- |
| [Register_ValidParams_ReturnsOk](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/MembershipControllerTests.cs#44-96) | API đăng ký vé tháng trả về 200 OK và trạng thái PendingPayment. | ✅ Pass |
| [Register_ServiceThrows_ReturnsBadRequest](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/MembershipControllerTests.cs#102-116) | API trả về 400 BadRequest khi có lỗi nghiệp vụ. | ✅ Pass |
| [ConfirmPayment_Success_ActivatesTicket](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/MembershipControllerTests.cs#117-141) | Callback thanh toán thành công kích hoạt vé (Active). | ✅ Pass |
| [ConfirmPayment_Failed_UpdatesStatus](file:///d:/ParkingManagementSystem/backend/Parking.Tests/Controllers/MembershipControllerTests.cs#142-165) | Callback thanh toán thất bại cập nhật trạng thái lỗi. | ✅ Pass |

---

## 3. Kết luận
Bộ kiểm thử tự động đã bao phủ các luồng nghiệp vụ quan trọng nhất. Code tuân thủ các nguyên lý OOP và Dependency Injection, cho phép Mocking dễ dàng và kiểm thử cô lập.

**Lệnh chạy kiểm thử lại**:
```bash
cd backend
dotnet test
```
