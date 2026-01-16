# So sánh: Sơ đồ Tuần tự Báo cáo vs Hệ thống Hiện tại

Tài liệu này so sánh thiết kế được đề xuất trong `documentation/sequence_diagram/report.txt` với hiện thực hóa thực tế trong backend (`ReportController.cs`) và frontend (`Report.jsx`).

## Tóm tắt
Sơ đồ tuần tự mô tả một thiết kế **Hướng đối tượng** sử dụng các mẫu **Factory** và **Strategy**, nơi logic báo cáo được đóng gói bên trong các đối tượng Report cụ thể.

Hiện thực hóa hiện tại tuân theo mẫu **Transaction Script** (hoặc "Fat Controller"), nơi `ReportController` trực tiếp xử lý việc lấy dữ liệu, lọc và tính toán, còn các lớp Report chỉ đơn thuần là **DTO** (Data Transfer Object) không có hành vi.

## So sánh Chi tiết

| Tính năng | Sơ đồ Tuần tự (Đề xuất) | Hiện thực hóa Hiện tại (Code) | Trạng thái |
| :--- | :--- | :--- | :--- |
| **API Endpoint** | `requestReport("REVENUE", startDate, endDate)` <br> *Endpoint tổng quát* | `GET /api/Report/revenue` <br> `GET /api/Report/traffic` <br> *Các endpoint cụ thể* | ❌ **Không khớp** |
| **Logic Controller** | Sử dụng **Factory Method** để tạo đối tượng chiến lược Report cụ thể (ví dụ: `RevenueReport`). Ủy quyền logic cho đối tượng này. | **Hiện thực hóa trực tiếp**. Controller khởi tạo các Repository (`_sessionRepo`), lấy dữ liệu và thực hiện tính toán ngay tại chỗ. | ❌ **Không khớp** |
| **Domain Entity** | `RevenueReport` là **Rich Domain Model** có hành vi:<br>`- create()`<br>`- generateData()`<br>`- calculateTotal()` | `RevenueReport` là **DTO** chỉ có các thuộc tính:<br>`- TotalRevenue`<br>`- TotalTransactions`<br>`- RevenueByPaymentMethod` | ❌ **Không khớp** |
| **Truy cập Dữ liệu** | `RevenueReport` gọi entity/service `ParkingSession` để lấy dữ liệu. | `ReportController` gọi `IParkingSessionRepository` để lấy dữ liệu. | ❌ **Không khớp** |
| **Logic Nghiệp vụ** | Được đóng gói bên trong `RevenueReport.generateData()`. | Bị lộ ra bên trong các action method của `ReportController`. | ❌ **Không khớp** |
| **Input** | `startDate`, `endDate` | `from`, `to` (Được ánh xạ sang `DateTime?`) | ✅ **Khớp** |
| **Output** | Trả về `reportData` (DTO) | Trả về `RevenueReport` (DTO) | ✅ **Khớp** |

## Các phát hiện chính

1.  **Thiếu Design Patterns**: Code không sử dụng các pattern Factory hay Strategy như mô tả trong sơ đồ (`note right of Ctrl`). Controller bị phụ thuộc chặt chẽ vào các loại báo cáo cụ thể.
2.  **Vi phạm SRP**: `ReportController` xử lý cả vòng đời request HTTP VÀ logic nghiệp vụ (tính toán doanh thu, lưu lượng), vi phạm Nguyên lý Trách nhiệm Đơn lẻ (Single Responsibility Principle). Sơ đồ đã ủy quyền việc này cho các Report entity một cách chính xác.
3.  **Cách dùng ở Frontend**: Frontend gọi nhiều API endpoint song song (`Promise.all`) để lắp ghép dashboard, trong khi sơ đồ ngụ ý một luồng tạo báo cáo duy nhất cho mỗi loại.

## Đề xuất Refactor (để khớp với Sơ đồ)

Để code khớp với sơ đồ:
1.  **Tạo Interface Report Strategy**: Định nghĩa `IReportStrategy` với phương thức `Generate(DateTime start, DateTime end)`.
2.  **Hiện thực hóa các Strategy cụ thể**: Di chuyển logic từ Controller sang `RevenueReportStrategy`, `TrafficReportStrategy`, v.v.
3.  **Hiện thực hóa Factory**: Tạo `ReportFactory` để khởi tạo strategy chính xác dựa trên chuỗi phân loại (type string).
4.  **Refactor Controller**: Thay đổi `ReportController` để sử dụng Factory và ủy quyền việc tạo báo cáo cho strategy.
