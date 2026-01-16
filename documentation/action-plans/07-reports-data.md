# Báo cáo: bỏ mock, thêm số liệu mất vé/vé tháng/phương thức thanh toán

## Mục tiêu
- Frontend Report không dùng dữ liệu mock; hiển thị đúng từ API.
- Thêm báo cáo mất vé, vé tháng, doanh thu theo phương thức thanh toán, lưu lượng theo khung giờ/gate.

## Các bước triển khai
1) **Backend**: mở rộng `ReportController` để trả:
   - Doanh thu theo ngày/gate/paymentMethod.
   - Lượt mất vé, tổng phí phạt.
   - Vé tháng: số đăng ký mới, gia hạn, hủy, doanh thu membership.
   - Lưu lượng theo giờ (entries/exits) từ sessions.
2) **Services/Repos**: thêm truy vấn từ `sessions.json`, `incidents.json`, `monthly_tickets.json`, `payments` trong session. Có thể thêm trường `PaymentMethod` và `IsLostTicket` để thống kê.
3) **Frontend** ([frontend/src/pages/Report.jsx]): bỏ mock fallback; handle lỗi bằng thông báo UI; vẽ chart từ dữ liệu thực (Bar, Line, Pie) cho paymentMethod breakdown, lost-ticket counts, membership revenue.
4) **API contracts**: chuẩn hóa response (labels + values) để frontend không phải mock; thêm endpoint `/Report/lost-tickets`, `/Report/membership`, `/Report/payment-methods`.
5) **Test**: seed vài sessions với lost-ticket/payment methods khác nhau; verify chart hiển thị đúng; tắt mock code.
