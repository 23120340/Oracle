# HospitalApp – Đồ án CSC12001

Ứng dụng WinForms C# (.NET 8) tích hợp **Phân hệ 1** (quản trị CSDL Oracle) và **Phân hệ 2** (quản lý dữ liệu y tế).

> 📖 Hướng dẫn cài đặt CSDL + chạy đầy đủ: xem **[../README.md](../README.md)** (repo gốc).

## Cấu trúc project

```
HospitalApp/
├── HospitalApp.csproj          ← SDK-style, net8.0-windows
├── Program.cs                  ← Entry point; nạp font Montserrat làm mặc định
├── Database/
│   └── OracleHelper.cs         ← Wrapper kết nối/truy vấn; bind chuỗi NVarchar2 (UTF-8)
├── Forms/
│   ├── LoginForm.cs            ← Đăng nhập, nhận diện vai trò, khoá brute-force
│   ├── Admin/
│   │   └── AdminDashboard.cs   ← Phân hệ 1: User/Role/Grant/Revoke/View privileges/Audit
│   └── Hospital/
│       ├── DPVForm.cs          ← Điều phối viên
│       ├── BSForm.cs           ← Bác sĩ / Y sĩ
│       ├── KTVForm.cs          ← Kỹ thuật viên
│       ├── BNForm.cs           ← Bệnh nhân
│       └── OLSViewerForm.cs    ← Demo OLS (user u1–u8)
├── Controls/                   ← Sidebar, Card, StatusBar, Toast, SearchBox, MyProfilePanel, …
├── Security/                   ← OracleErrorMapper, InputValidator, SessionManager, AppAuditLogger
├── Theme/                      ← UiTheme (Montserrat + palette), IconRegistry, Animator
└── Resources/Fonts/            ← Montserrat-Regular.ttf, Montserrat-Bold.ttf (đã nhúng)
```

## Yêu cầu

- .NET 8 SDK (Windows)
- Oracle Database 19c+ / XE 21c (đã chạy các script trong `../PhanHe2/` — xem [../README.md](../README.md))
- NuGet: `Oracle.ManagedDataAccess.Core 23.4.0` (tự restore)

## Build & chạy

```powershell
cd d:\repos\Oracle\HospitalApp
dotnet restore
dotnet run            # hoặc: dotnet build -c Release
```

Bản publish sẵn có: `..\dist\win-x64-fixed\HospitalApp.exe`.

## Luồng đăng nhập

```
LoginForm  (nhập Host/Port/Service + user/password)
 ├── HOSPITAL_DBA / SYSTEM → AdminDashboard  (Phân hệ 1)
 ├── DPV_NV001             → DPVForm          (Điều phối viên)
 ├── BS_NV003              → BSForm           (Bác sĩ / Y sĩ)
 ├── KTV_NV006             → KTVForm          (Kỹ thuật viên)
 ├── BN_BN001              → BNForm           (Bệnh nhân)
 └── u1_giamdoc … u8       → OLSViewerForm    (demo OLS)
```

## Bảo mật tự động (thực thi tại Oracle DB)

| Role | Cơ chế | Hiệu quả |
|------|--------|----------|
| DBA  | – | Full access (Phân hệ 1) |
| DPV  | VPD + column grant | Xem tất cả, UPDATE giới hạn cột |
| BS   | VPD (row filter) | Chỉ thấy HSBA/BN/ĐƠNTHUỐC của mình; ghi vết khi sửa |
| KTV  | RBAC view + VPD branch | Chỉ thấy HSBA_DV được giao (`MAKTV`=mình); ghi vết KETQUA |
| BN   | RBAC view + VPD branch | Chỉ thấy/sửa thông tin của chính mình |

App kết nối bằng đúng tài khoản đăng nhập → Oracle tự áp RBAC/VPD/OLS, app không xử lý logic phân quyền.

## Tiếng Việt (UTF-8) & font

- Đọc cột Unicode bằng `TO_NCHAR(...)`, ghi bằng tham số `NVarchar2` → tiếng Việt đúng dù DB charset khác AL32UTF8.
- Font **Montserrat** được nhúng từ `Resources/Fonts/*.ttf` (đủ glyph tiếng Việt); thiếu file thì fallback Segoe UI.
- Mọi file `.cs` lưu **UTF-8 có BOM** để biên dịch tiếng Việt đúng trên mọi máy.
