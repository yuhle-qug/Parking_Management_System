# Đối chiếu tài liệu thiết kế vs hệ thống hiện tại

## Tổng quan
- Tài liệu tham chiếu: p1 mô tả hệ thống, p2 sơ đồ usecase, p3 phân tích usecase.
- Mã kiểm tra: backend API + services + repositories, frontend Vite dashboard.
- Nhận xét nhanh: hệ thống đã có khung check-in/out, vé tháng, báo cáo, user; còn thiếu nhiều luồng/điều kiện trong đặc tả (mất vé, thanh toán online bắt buộc, cấu hình bảng giá, đối chiếu thẻ-biển số, gia hạn vé tháng, phân quyền chặt, kiểm soát đầy bãi, nhật ký thao tác).

## Mức độ phủ chức năng theo use case
- **UC01 Tiếp nhận xe vào**: Có API/UI cơ bản ([backend/Parking.API/Controllers/CheckInController.cs](backend/Parking.API/Controllers/CheckInController.cs), [backend/Parking.Services/Services/ParkingService.cs](backend/Parking.Services/Services/ParkingService.cs), [frontend/src/pages/Dashboard.jsx](frontend/src/pages/Dashboard.jsx)); thiếu: quẹt thẻ từ, kiểm tra thẻ đang có phiên, log thao tác, phân luồng khi bãi đầy theo gate, đọc biển số tự động, liên kết EntryUI/GateDevice như thiết kế.
- **UC02 Cho xe ra & tính phí**: Có API tính phí và trạng thái PendingPayment ([backend/Parking.API/Controllers/CheckOutController.cs](backend/Parking.API/Controllers/CheckOutController.cs), [backend/Parking.Services/Services/ParkingService.cs](backend/Parking.Services/Services/ParkingService.cs)); thiếu: đối chiếu thẻ-biển số, xử lý mất vé, mapping cổng ra, log, chính sách qua đêm/giờ cao điểm, mở cổng sau thanh toán theo gate ra.
- **UC03 Thanh toán**: Có PaymentService gọi `IPaymentGateway` ([backend/Parking.Services/Services/PaymentService.cs](backend/Parking.Services/Services/PaymentService.cs)) và mock gateway ([backend/Parking.Infrastructure/External/MockPaymentGatewayAdapter.cs](backend/Parking.Infrastructure/External/MockPaymentGatewayAdapter.cs)); PaymentController gửi method "Cash/QR" ([backend/Parking.API/Controllers/PaymentController.cs](backend/Parking.API/Controllers/PaymentController.cs)) trái ràng buộc “KHÔNG hỗ trợ tiền mặt”; thiếu: trạng thái giao dịch, mã giao dịch, retry, timeout, thông báo lỗi, hủy thanh toán.
- **UC04 Đăng ký vé tháng**: Có API/UI đăng ký ([backend/Parking.API/Controllers/MembershipController.cs](backend/Parking.API/Controllers/MembershipController.cs), [backend/Parking.Services/Services/MembershipService.cs](backend/Parking.Services/Services/MembershipService.cs), [frontend/src/pages/Membership.jsx](frontend/src/pages/Membership.jsx)); thiếu: chọn gói/plan thực sự (planId đang cố định), thanh toán qua gateway trước khi kích hoạt, ràng buộc 1-1 vé-vehicle được kiểm tra nhưng không có log, không nhập giấy tờ tùy thân như usecase gợi ý.
- **UC05 Gia hạn vé tháng**: Logic service có ([backend/Parking.Services/Services/MembershipService.cs](backend/Parking.Services/Services/MembershipService.cs)) nhưng không có API/UI; không tính phí gia hạn, không gọi PaymentGateway, không ghi lịch sử gia hạn.
- **UC06 Mất vé**: Chưa triển khai; không có endpoint, UI hay PricePolicy mất vé; IncidentController chỉ ghi nhận sự cố chung ([backend/Parking.API/Controllers/IncidentController.cs](backend/Parking.API/Controllers/IncidentController.cs)) nhưng không gắn phí/luồng cho xe ra.
- **Quản lý tài khoản người dùng**: Có đăng nhập/tạo/xóa/đổi trạng thái ([backend/Parking.API/Controllers/UserAccountController.cs](backend/Parking.API/Controllers/UserAccountController.cs), [backend/Parking.Infrastructure/Repositories/UserRepository.cs](backend/Parking.Infrastructure/Repositories/UserRepository.cs), [frontend/src/pages/Admin.jsx](frontend/src/pages/Admin.jsx)); thiếu: mã hóa mật khẩu, phân quyền chi tiết theo chức năng, session/token, audit log, cấm đổi/xóa admin chỉ một phần.
- **Quản lý bảng giá**: Chưa có màn hình/API cấu hình; PricePolicy đang hard-code & seed ([backend/Parking.Core/Entities/PricePolicy.cs](backend/Parking.Core/Entities/PricePolicy.cs), [backend/Parking.Infrastructure/Repositories/ParkingZoneRepository.cs](backend/Parking.Infrastructure/Repositories/ParkingZoneRepository.cs)); membership_policies seed cố định ([backend/Parking.Infrastructure/Repositories/MembershipPolicyRepository.cs](backend/Parking.Infrastructure/Repositories/MembershipPolicyRepository.cs)).
- **Báo cáo & thống kê**: Có báo cáo doanh thu/traffic và bảng active sessions ([backend/Parking.API/Controllers/ReportController.cs](backend/Parking.API/Controllers/ReportController.cs), [frontend/src/pages/Report.jsx](frontend/src/pages/Report.jsx)); dữ liệu biểu đồ phần frontend vẫn mock, chưa có báo cáo chi tiết theo usecase (vé tháng, doanh thu theo khung giờ/gate).
- **Nhận diện biển số & thiết bị**: Có client nhận diện và endpoint bật/tắt ([backend/Parking.API/Controllers/PlateRecognitionController.cs](backend/Parking.API/Controllers/PlateRecognitionController.cs), [backend/Parking.Infrastructure/External/LicensePlateRecognitionClient.cs](backend/Parking.Infrastructure/External/LicensePlateRecognitionClient.cs)); UI chưa tích hợp, GateDevice mock chỉ mở cổng, không đọc thẻ/biển số thực ([backend/Parking.Infrastructure/External/MockGateDevice.cs](backend/Parking.Infrastructure/External/MockGateDevice.cs)).
- **Kiểm soát bãi đầy / zone**: Repository chọn zone theo loại xe/điện và capacity ([backend/Parking.Infrastructure/Repositories/ParkingZoneRepository.cs](backend/Parking.Infrastructure/Repositories/ParkingZoneRepository.cs)); không có API báo "Bãi đầy" theo gate như đặc tả và không trừ slot khi xe ra (zone.ActiveSessions không được giảm khi checkout).

## Chênh lệch chính so với tài liệu
- **Thẻ từ vs nhập biển số thủ công**: Usecase yêu cầu quẹt thẻ + đọc biển số; hiện chỉ nhập plate thủ công ở UI, không lưu/đối chiếu cardId.
- **Luồng mất vé & xác minh**: Thiếu hoàn toàn; không có phí phạt, không tìm kiếm phiên theo thông tin mô tả, không có xác minh biển số/camera.
- **Thanh toán online bắt buộc**: Controller ghi method "Cash/QR" và gateway mock luôn thành công; thiếu trạng thái thất bại, retry, timeout, webhook từ cổng ngoài.
- **Bảng giá linh hoạt**: Thiếu cấu hình khung giờ, qua đêm, phí tối đa, phí mất vé; PricePolicy chỉ tính giờ tròn * 10k * hệ số loại xe.
- **Vé tháng**: Đăng ký không thu phí, không chọn gói/chu kỳ, không lưu lịch sử, chưa có gia hạn/hủy đúng luồng; background chỉ tự động chuyển Expired.
- **Đối chiếu biển số khi ra**: Không so khớp plate lúc vào/ra; không log thao tác, không chặn khi biển số khác.
- **Quản trị & bảo mật**: Mật khẩu lưu plain JSON, không token/session, không phân quyền chi tiết cho từng API, chưa có audit log.
- **Tích hợp thiết bị**: GateDevice mock, không tách EntryUI/ExitUI, không mở barie theo gate ra, không trạng thái thiết bị.
- **Báo cáo**: Frontend dùng dữ liệu giả cho biểu đồ; chưa có báo cáo mất vé, vé tháng, lỗi thiết bị.

## Đề xuất backlog ưu tiên (bám sát thiết kế)
1) **Hoàn thiện luồng thanh toán online**: thay PaymentController để tạo giao dịch + gọi gateway, lưu trạng thái Pending/Success/Failed, cấm method cash, thêm retry/timeout.
2) **Mất vé**: thêm LostTicketFeePolicy, endpoint xử lý mất vé (nhập plate/time loại xe -> tìm session, tính phí phạt + phí gửi, log Incident, thanh toán, mở cổng).
3) **Đối chiếu thẻ-biển số**: lưu cardId tại check-in, khi check-out kiểm tra card + biển; nếu lệch → chuyển luồng mất vé.
4) **Gia hạn/hủy vé tháng có thanh toán**: API renew with months + phí, ghi lịch sử; cấm Delete thẳng; frontend thêm flow gia hạn.
5) **Bảng giá cấu hình được**: CRUD PricePolicy + MembershipPolicy, hỗ trợ khung giờ/qua đêm/max daily; UI cho Admin.
6) **Quản lý sức chứa & zone**: trừ slot khi xe ra; trả về thông báo bãi đầy theo gate; mapping gate→zone.
7) **Bảo mật**: băm mật khẩu, JWT/Session, phân quyền theo role, ẩn API nhạy cảm; audit log cho check-in/out/payment.
8) **Tích hợp LPR & thiết bị**: UI upload/stream ảnh, tự điền biển số; GateDevice interface thêm readCard/readPlate, mở barie theo cổng ra.
9) **Báo cáo đúng số liệu**: bỏ mock, thêm báo cáo lượt theo khung giờ, vé tháng, mất vé, doanh thu theo phương thức.
10) **Kiểm thử & dữ liệu**: bổ sung test cho fee calculation, monthly expiry, lost ticket; làm sạch seed/json, thêm script backup.


## Sau khi Update con nhunữ muc sau: 
Thanh toán online: vẫn cho phép method mặc định "Cash/QR" và không chặn tiền mặt; gateway vẫn mock, chưa có trạng thái thất bại/timeout thực từ cổng ngoài; không lưu giao dịch riêng ngoài session. Xem PaymentController.cs và PaymentService.cs.
Mất vé: đã có LostTicketFeePolicy và endpoint lost-ticket, có ghi Incident; tuy nhiên chưa thấy luồng thanh toán/hủy riêng cho mất vé (vẫn dùng chung Payment), chưa xác minh theo mô tả chi tiết. Xem CheckOutController.cs và ParkingService.cs.
Đối chiếu thẻ–biển số: chưa lưu cardId trong Ticket/ParkingSession, IGateDevice không đọc thẻ; check-out chỉ so khớp plate nếu người dùng nhập thêm. Xem CoreEntities.cs và ParkingService.cs.
Gia hạn/hủy vé tháng có thanh toán: chưa có API renew/cancel; Delete ticket vẫn mở thẳng; không ghi lịch sử/gọi payment khi gia hạn. Xem MembershipController.cs và MembershipService.cs.
Bảng giá cấu hình: PricePolicy đang hard-code, zones seed cố định, không có CRUD hay storage động cho PricePolicy/MembershipPolicy. Xem PricePolicy.cs và ParkingZoneRepository.cs.
ức chứa & zone: chưaS có API “bãi đầy theo gate”; capacity được giải phóng ngay khi status chuyển PendingPayment (trước khi thực sự ra cổng); chưa map gate→zone cho check-out/mở cổng. Xem ParkingService.cs.
Bảo mật: mật khẩu lưu plain và so sánh trực tiếp, không JWT/session, chưa phân quyền chi tiết, không audit log. Xem UserAccountController.cs.
LPR & thiết bị: IGateDevice chỉ có OpenGate; MockGateDevice có ReadPlateAsync nhưng interface không khai báo, không có readCard; UI chưa tích hợp upload/stream. Xem IServices.cs, MockGateDevice.cs, PlateRecognitionController.cs.
Báo cáo: frontend vẫn dùng dữ liệu mock cho chart và hourly traffic khi lỗi; chưa có báo cáo mất vé/vé tháng/phương thức thanh toán. Xem Report.jsx.
Kiểm thử & dữ liệu: chưa thấy test cases mới; seed/json vẫn hard-code; chưa có script backup/clean được thêm.