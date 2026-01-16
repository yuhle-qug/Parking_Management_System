# Đối chiếu thẻ-biển số & mất vé (PDF biên bản)

## Mục tiêu
- Lưu `cardId` trên `Ticket` và `ParkingSession` khi check-in.
- Check-out bắt buộc khớp cả plate + cardId; lệch thì chuyển luồng mất vé.
- Luồng mất vé cho phép tải/in mẫu biên bản PDF: `D:\ParkingManagementSystem\backend\Mau_bien_ban_mat_ve.pdf`.

## Các bước triển khai
1) **Model**: thêm `CardId` vào `Ticket` và `ParkingSession` ([backend/Parking.Core/Entities/CoreEntities.cs]) và các JSON storage liên quan (từ repo). Cập nhật seed/ticket creation trong `ParkingService.CheckInAsync` để nhận `cardId` từ request.
2) **API Check-in**: sửa `CheckInController` + request model để nhận `CardId`; lưu vào ticket/session.
3) **API Check-out**: trong `ParkingService.CheckOutAsync`, nhận thêm `cardId` (và plate nhập lại) từ controller; so khớp `session.Ticket.CardId` và `session.Vehicle.LicensePlate`. Nếu sai -> throw và hướng dẫn dùng lost-ticket endpoint.
4) **Lost-ticket endpoint**: mở rộng `CheckOutController.LostTicket` nhận flag `PrintReport`; nếu mất vé, cung cấp link/download file PDF mẫu (đường dẫn trên) để bảo vệ xử lý; log Incident chứa cardId nếu có.
5) **Gate logic**: khi LostTicket -> vẫn tính phí + LostTicketFeePolicy; thanh toán xong mới mở cổng.
6) **Front/UI**: thêm input CardId ở check-in và check-out; bổ sung nút "In biên bản mất vé" để tải PDF.
7) **Data migration**: thêm field `cardId` mặc định null cho các file JSON cũ (`tickets.json`, `sessions.json`).
8) **Test**: check-in với cardId, checkout đúng -> pass; checkout sai cardId -> bị chặn; lost-ticket -> trả về link PDF + phí phạt.
