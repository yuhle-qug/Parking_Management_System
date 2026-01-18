# Chương trình kiểm thử web (E2E) – Parking Management System

## 1. Mục tiêu
- Xác nhận các luồng nghiệp vụ chính của hệ thống web hoạt động đúng theo tài liệu sequence diagram.
- Tăng mức độ bao phủ kiểm thử trên các màn hình: Đăng nhập, Check-in, Check-out, Vé tháng, Báo cáo.
- Tạo bộ kiểm thử tự động có thể đưa vào báo cáo nghiệm thu.

## 2. Phạm vi kiểm thử
### In-scope
- Đăng nhập & phân quyền giao diện.
- Check-in xe (vé lượt, vé tháng).
- Check-out xe (vé lượt, vé tháng, mất vé) + thanh toán.
- Đăng ký vé tháng, gia hạn, hủy vé.
- Báo cáo doanh thu & lưu lượng, hiển thị biểu đồ.

### Out-of-scope
- Hiệu năng, bảo mật sâu (pentest).
- Tích hợp OCR thực tế (dịch vụ bên ngoài).
- Kiểm thử API nội bộ (đã có backend unit tests).

## 3. Tham chiếu tài liệu sequence diagram
- Check-in: [documentation/sequence_diag_final/check_in.txt](documentation/sequence_diag_final/check_in.txt)
- Check-out: [documentation/sequence_diag_final/check_out.txt](documentation/sequence_diag_final/check_out.txt)
- Đăng ký vé tháng: [documentation/sequence_diag_final/dki_ve_thang.txt](documentation/sequence_diag_final/dki_ve_thang.txt)
- Gia hạn vé: [documentation/sequence_diag_final/gia_han_ve.txt](documentation/sequence_diag_final/gia_han_ve.txt)
- Hủy vé: [documentation/sequence_diag_final/huy_ve.txt](documentation/sequence_diag_final/huy_ve.txt)
- Báo cáo: [documentation/sequence_diag_final/bao_cao_thong_ke.txt](documentation/sequence_diag_final/bao_cao_thong_ke.txt)

## 4. Môi trường kiểm thử
- Frontend: React + Vite, chạy tại http://localhost:5173
- Backend: ASP.NET Core, chạy tại http://localhost:5166
- CSDL: JSON DataStore (mặc định)
- Trình duyệt: Chromium (Playwright)

## 5. Dữ liệu kiểm thử
- Tài khoản mặc định: admin / 123
- Biển số test: sinh ngẫu nhiên theo prefix (vd: 30MEM-1234)
- Vé tháng: tạo mới trong test để tránh trùng dữ liệu

## 6. Danh sách test case chính
### 6.1. Đăng nhập
| ID | Mô tả | Tiền điều kiện | Bước chính | Kết quả mong đợi |
|---|---|---|---|---|
| AUTH-01 | Đăng nhập thành công | Có tài khoản admin | Nhập đúng user/pass → Đăng nhập | Điều hướng tới Dashboard |
| AUTH-02 | Đăng nhập thất bại | Không | Nhập sai mật khẩu | Hiển thị thông báo lỗi |

### 6.2. Check-in
| ID | Mô tả | Tiền điều kiện | Bước chính | Kết quả mong đợi |
|---|---|---|---|---|
| CI-01 | Check-in vé lượt | Đã đăng nhập | Nhập biển số → Xác nhận | Log thành công, tạo vé |
| CI-02 | Check-in vé tháng | Có vé tháng hợp lệ | Chọn vé tháng → nhập cardId | Log thành công, không in vé |

### 6.3. Check-out
| ID | Mô tả | Tiền điều kiện | Bước chính | Kết quả mong đợi |
|---|---|---|---|---|
| CO-01 | Check-out vé lượt | Có session | Nhập biển số + mã vé | Hiển thị phí, thanh toán thành công |
| CO-02 | Check-out mất vé | Có xe đang gửi | Chọn Mất Vé → nhập biển số | Hiển thị phí mất vé, thanh toán thành công |

### 6.4. Vé tháng
| ID | Mô tả | Tiền điều kiện | Bước chính | Kết quả mong đợi |
|---|---|---|---|---|
| MEM-01 | Đăng ký vé tháng | Đăng nhập | Nhập form → xác nhận | Vé xuất hiện tab Active |
| MEM-02 | Gia hạn vé tháng | Có vé tháng | Chọn “Gia hạn” → xác nhận | Lịch sử có bản ghi gia hạn |
| MEM-03 | Hủy vé tháng (admin) | Có vé tháng | Chọn “Hủy vé” → xác nhận | Vé chuyển sang tab “Đã hủy” |

### 6.5. Báo cáo
| ID | Mô tả | Tiền điều kiện | Bước chính | Kết quả mong đợi |
|---|---|---|---|---|
| REP-01 | Báo cáo doanh thu | Admin | Chọn ngày → Làm mới | Biểu đồ hiển thị |

## 7. Mapping test tự động (Playwright)
| Test Case | File kiểm thử |
|---|---|
| AUTH-01 | [frontend/tests/e2e/specs/checkin.spec.ts](frontend/tests/e2e/specs/checkin.spec.ts) |
| AUTH-02 | [frontend/tests/e2e/specs/login_negative.spec.ts](frontend/tests/e2e/specs/login_negative.spec.ts) |
| CI-01 | [frontend/tests/e2e/specs/checkin.spec.ts](frontend/tests/e2e/specs/checkin.spec.ts) |
| CI-02 | [frontend/tests/e2e/specs/checkin_monthly.spec.ts](frontend/tests/e2e/specs/checkin_monthly.spec.ts) |
| CO-02 | [frontend/tests/e2e/specs/checkout_lost_ticket.spec.ts](frontend/tests/e2e/specs/checkout_lost_ticket.spec.ts) |
| MEM-01 | [frontend/tests/e2e/specs/membership.spec.ts](frontend/tests/e2e/specs/membership.spec.ts) |
| MEM-02, MEM-03 | [frontend/tests/e2e/specs/membership_renew_cancel.spec.ts](frontend/tests/e2e/specs/membership_renew_cancel.spec.ts) |
| REP-01 | [frontend/tests/e2e/specs/reports.spec.ts](frontend/tests/e2e/specs/reports.spec.ts) |

## 8. Hướng dẫn chạy test
1. Cài dependencies: `npm install`
2. Chạy backend: `dotnet run --project backend/Parking.API`
3. Chạy E2E:
   ```bash
   npm run test:e2e
   ```

## 9. Ghi chú
- Các test sử dụng dữ liệu ngẫu nhiên để tránh xung đột.
- Với flow “Hủy vé tháng”, hệ thống dùng dialog confirm/prompt/alert; Playwright đã xử lý trong test.
- Báo cáo kiểm thử HTML được tạo trong [frontend/playwright-report](frontend/playwright-report).
