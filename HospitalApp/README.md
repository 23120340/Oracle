# HospitalApp – Đồ án CSC12001

Ứng dụng WinForm C# (.NET 8) tích hợp **Phân hệ 1** và **Phân hệ 2**.

## Cấu trúc project

```
HospitalApp/
├── HospitalApp.csproj          ← SDK-style, .NET 8 Windows
├── Program.cs
├── Database/
│   └── OracleHelper.cs         ← Wrapper Oracle connection/query
└── Forms/
    ├── LoginForm.cs             ← Đăng nhập, nhận diện role
    ├── Admin/
    │   └── AdminDashboard.cs   ← Phân hệ 1: Quản trị DB
    └── Hospital/
        ├── BSForm.cs           ← Phân hệ 2: Bác sĩ/Y sĩ
        ├── DPVForm.cs          ← Phân hệ 2: Điều phối viên
        ├── KTVForm.cs          ← Phân hệ 2: Kỹ thuật viên
        └── BNForm.cs           ← Phân hệ 2: Bệnh nhân
```

## Yêu cầu

- .NET 8 SDK (Windows)
- Oracle Database 19c+ (đã chạy các script SQL trong `Oracle/PhanHe2/`)
- NuGet: `Oracle.ManagedDataAccess.Core 23.4.0`

## Cài đặt và chạy

```bash
cd d:\repos\HospitalApp
dotnet restore
dotnet run
```

## Luồng đăng nhập

```
LoginForm
 ├── DBA/SYSTEM       → AdminDashboard  (Phân hệ 1)
 ├── DPV_NV001        → DPVForm         (Điều phối viên)
 ├── BS_NV003         → BSForm          (Bác sĩ)
 ├── KTV_NV006        → KTVForm         (Kỹ thuật viên)
 └── BN_BN001         → BNForm          (Bệnh nhân)
```

## Bảo mật tự động

| Role  | Cơ chế             | Hiệu quả                                       |
|-------|--------------------|------------------------------------------------|
| DBA   | –                  | Full access (Phân hệ 1)                        |
| DPV   | VPD + Column Grant | Xem tất cả, UPDATE giới hạn cột               |
| BS    | VPD (Row filter)   | Chỉ thấy HSBA/BN/DONTHUOC của mình           |
| KTV   | RBAC View          | Chỉ thấy HSBA_DV được giao (MAKTV = mình)    |
| BN    | RBAC View          | Chỉ thấy 1 dòng của chính mình               |

Các hạn chế này được Oracle DB thực thi tự động — ứng dụng không cần kiểm tra thêm.
