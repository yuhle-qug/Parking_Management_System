# Bảo mật: băm mật khẩu, JWT/session, phân quyền, audit log

## Mục tiêu
- Mật khẩu lưu dưới dạng hash (salted), không plain.
- Xác thực bằng JWT (hoặc session cookie) với thời hạn, refresh nếu cần.
- Phân quyền theo role cho từng API (Admin/Attendant/User) + ẩn API nhạy cảm.
- Audit log cho check-in/out/payment/membership actions.

## Các bước triển khai
1) **Password hashing**: dùng `PasswordHasher` (ASP.NET Core Identity style) hoặc BCrypt. Migrate file `users.json` để lưu Hash + Salt; cập nhật `UserRepository` để verify hash, không so sánh plain.
2) **Auth middleware**: thêm JWT issuance trong `Login` (UserAccountController) trả access token; configure `AddAuthentication().AddJwtBearer(...)`; thêm `[Authorize(Roles="...")]` cho controllers/actions. Bỏ trả thông tin user nếu không auth.
3) **Roles/permissions**: định nghĩa enum/const roles; map hành động (CRUD user, pricing, reports) cho Admin; check-in/out/payment cho Attendant; report view có thể giới hạn. Thêm policy-based authorization.
4) **Token storage**: ở frontend lưu token, gắn Authorization header. Thêm logout/refresh nếu cần.
5) **Audit log**: tạo entity `AuditLog` (Action, UserId, Time, Payload/ReferenceId, Success). Repo JSON mới `audit_logs.json`; middleware hoặc service ghi log khi gọi check-in/out/payment/membership/user change.
6) **Secure endpoints**: ẩn hoặc require Admin cho user management, pricing CRUD, membership history. Không cho xóa admin mặc định; limit brute force (lockout counter) nếu kịp.
7) **Test**: login -> token; call protected API without token -> 401; role mismatch -> 403; password hash verify; audit log ghi lại check-in và payment.
