# Kiểm thử & dữ liệu: test cases, seed sạch, script backup

## Mục tiêu
- Bổ sung test (unit/integration) cho fee calculation, membership expiry/renew, lost ticket flow, auth.
- Làm sạch seed/json, thêm script backup/restore.

## Các bước triển khai
1) **Tests**: tạo dự án test (xUnit/NUnit). Test:
   - `ParkingFeePolicy` với peak/overnight/dailyMax.
   - Lost-ticket fee + incident log + payment status.
   - Membership register/extend/cancel payment status transitions.
   - Auth: hash verify, JWT authorize protected endpoints.
2) **Test data builders**: thêm factory/helper để tạo session/ticket mẫu tránh lặp.
3) **Seed cleanup**: xem `DataStore/*.json`; bỏ dữ liệu mẫu dư, giữ tối thiểu; đồng bộ schema mới (cardId, payment fields, history).
4) **Backup scripts**: PowerShell `SeedData.ps1` hoặc script mới `BackupData.ps1` để nén thư mục DataStore vào `_backup/` với timestamp; script restore tương ứng.
5) **CI/local**: thêm hướng dẫn chạy test (`dotnet test`), và cách reset dữ liệu (copy từ seed clean hoặc backup). Có thể thêm task vào `CleanRebuild.bat`.
6) **Documentation**: cập nhật README/BUILD guide để người khác biết cách chạy test, backup, reset data.
