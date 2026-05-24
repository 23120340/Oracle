# Đồ án CSC12001 – An toàn và Bảo mật Dữ liệu trong HTTT

> **Môn học:** CSC12001 – An toàn và Bảo mật Dữ liệu trong Hệ thống Thông tin  
> **Năm học:** 2025 – 2026  
> **Trường:** Trường Đại học Khoa học Tự nhiên – Khoa Công nghệ Thông tin  
> **Giảng viên:** TS. Phạm Thị Bạch Huệ · ThS. Lương Vĩ Minh · ThS. Tiết Gia Hồng

---

## Nội dung repo

```
Oracle/
├── HospitalApp/           ← Source WinForms C# (Phân hệ 1 + 2)
├── PhanHe2/               ← Script Oracle SQL (Phân hệ 2)
│   ├── 01_schema_data.sql
│   ├── 02_TC1_accounts.sql
│   ├── 03_YC1_C2_RBAC_KTV_BN.sql
│   ├── 04_YC1_C3_VPD_DPV_BS.sql
│   ├── 05_YC2_OLS_ThongBao.sql
│   ├── 06_YC3_Audit.sql
│   ├── 07_YC4_Backup_Recovery.sql
│   ├── 08_App_Migrations.sql
│   ├── 09_OLS_NhanVien_Unified.sql
│   └── 09_Recovery_Demo.sql
├── scripts/
│   └── setup.ps1          ← Runner SQL*Plus
├── docs/
│   ├── assignment/        ← Đề bài gốc
│   ├── guides/            ← Demo, encryption, vấn đáp
│   ├── reports/           ← Draft báo cáo và review cuối
│   └── planning/          ← Kế hoạch nội bộ
└── dist/
    └── win-x64/           ← Bản publish chạy thử
```

---

## Phân hệ 1 – Ứng dụng Quản trị CSDL Oracle

Ứng dụng **WinForm** dành cho DBA, cho phép quản trị toàn bộ hệ thống Oracle DB:

| Tính năng | Mô tả |
|-----------|-------|
| Quản lý User | Tạo, xóa, khóa/mở khóa tài khoản Oracle |
| Quản lý Role | Tạo, xóa role |
| Cấp quyền | Grant quyền hệ thống, đối tượng (table/view/procedure/function), cấp role cho user; hỗ trợ phân quyền đến mức cột cho SELECT/UPDATE; tùy chọn WITH GRANT OPTION |
| Thu hồi quyền | Revoke quyền hệ thống, đối tượng, role |
| Xem quyền | Hiển thị toàn bộ system/object/column/role privilege của user hoặc role |

---

## Phân hệ 2 – Ứng dụng Quản lý Dữ liệu Y tế

Hệ thống quản lý bệnh viện với cơ sở dữ liệu Oracle, áp dụng đầy đủ các cơ chế bảo mật.

### Schema cơ sở dữ liệu

| Bảng | Mô tả |
|------|-------|
| `BENHNHAN` | Thông tin bệnh nhân (có cột `ORACLE_USER` để ánh xạ tài khoản) |
| `NHANVIEN` | Nhân viên bệnh viện – DPV, BS, KTV (có cột `ORACLE_USER`) |
| `HSBA` | Hồ sơ bệnh án |
| `HSBA_DV` | Dịch vụ hỗ trợ chẩn đoán |
| `DONTHUOC` | Đơn thuốc |
| `THONGBAO` | Thông báo nội bộ (áp dụng OLS) |

### Yêu cầu 1 – Cấp quyền truy cập (RBAC + VPD)

**Câu 1 – TC#1:** DBA tạo Oracle account cho toàn bộ nhân viên và bệnh nhân. Tên tài khoản được lưu trực tiếp vào cột `ORACLE_USER` trong `NHANVIEN` / `BENHNHAN` → nhận diện người dùng chỉ cần truy vấn **1 bảng** (`SELECT * FROM NHANVIEN WHERE ORACLE_USER = SYS_CONTEXT(...)`).

**Câu 2 – RBAC** cho Kỹ thuật viên và Bệnh nhân:

| Role | Cơ chế | Quyền |
|------|--------|-------|
| `KTV_Role` | View + INSTEAD OF Trigger | Chỉ xem `HSBA_DV` do mình thực hiện; UPDATE duy nhất cột `KETQUA` |
| `BenhNhan_Role` | View + INSTEAD OF Trigger | Chỉ xem 1 dòng của mình trong `BENHNHAN`; UPDATE địa chỉ và tiền sử bệnh; không sửa được MABN/TENBN/PHAI/NGAYSINH/CCCD |

**Câu 3 – VPD** cho Điều phối viên và Bác sĩ:

| Role | Bảng | Predicate VPD |
|------|------|---------------|
| `DPV_Role` | `HSBA`, `HSBA_DV`, `BENHNHAN` | `''` (xem tất cả, không filter dòng) |
| `BS_Role` | `HSBA` | `MABS = fn_get_manv()` |
| `BS_Role` | `HSBA_DV`, `DONTHUOC` | `MAHSBA IN (SELECT MAHSBA FROM HSBA WHERE MABS = fn_get_manv())` |
| `BS_Role` | `BENHNHAN` | `MABN IN (SELECT MABN FROM HSBA WHERE MABS = fn_get_manv())` |

Mọi UPDATE `CHANDOAN`/`DIEUTRI`/`KETLUAN` và `TENTHUOC`/`LIEUDUNG` đều được ghi vết bằng trigger.

### Yêu cầu 2 – Oracle Label Security (OLS)

Bảng `THONGBAO` được áp dụng policy `BV_LABEL_POLICY` với **3 thành phần nhãn**:

| Thành phần | Giá trị | Ý nghĩa |
|-----------|---------|---------|
| **Level** | `NV(10)` < `LDK(20)` < `BGD(30)` | Cấp bậc nhân sự |
| **Compartment** | `HCM`, `HPN`, `HNI` | Cơ sở địa điểm (AND) |
| **Group** | `TH`, `TK`, `TM` | Khoa chuyên môn (OR) |

Nhãn dữ liệu mẫu:

| ID | Nhãn | Gửi đến |
|----|------|---------|
| t1 | `NV` | Toàn bộ nhân viên |
| t2 | `BGD` | Ban Giám đốc |
| t3 | `LDK` | Tất cả lãnh đạo khoa |
| t4 | `LDK::TH` | Lãnh đạo Khoa tiêu hóa |
| t5 | `NV:HCM:TH` | NV Khoa tiêu hóa tại HCM |
| t6 | `NV:HNI:TH` | NV Khoa tiêu hóa tại Hà Nội |
| t7 | `LDK:HPN:TH,TK` | Lãnh đạo Khoa TH và TK tại Hải Phòng |

### Yêu cầu 3 – Kiểm toán (Audit)

- **Standard Audit:** 5 ngữ cảnh theo dõi SELECT/UPDATE/INSERT/DELETE trên các bảng nhạy cảm, ghi nhận cả thao tác thành công và thất bại.
- **Fine-Grained Audit (FGA):** 4 policy theo dõi các hành vi đặc thù (cập nhật đơn thuốc sau khi tạo, cập nhật hợp lệ và bất hợp pháp HSBA, thao tác bất hợp pháp trên HSBA_DV).
- **Trigger log:** Bảng `LOG_BS_HSBA`, `LOG_BS_DONTHUOC`, `LOG_KTV_KETQUA` ghi vết chi tiết giá trị cũ/mới.

### Yêu cầu 4 – Sao lưu và Phục hồi

| Phương pháp | Loại | Lịch tự động |
|-------------|------|--------------|
| RMAN Full Backup | Physical | Chủ nhật 1:00 AM |
| RMAN Incremental Level 1 | Physical | Hàng đêm 2:00 AM |
| Data Pump (expdp) | Logical | Hàng tuần |
| Flashback Database | Point-in-time | Retention 24h |

---

## Cài đặt và chạy

### Yêu cầu

- Oracle Database 19c+
- .NET 8 SDK (Windows)
- NuGet: `Oracle.ManagedDataAccess.Core 23.4.0`

### Bước 1 – Khởi tạo CSDL

Chạy tuần tự các script trong `PhanHe2/` với SQL*Plus hoặc SQL Developer:

```sql
-- Kết nối với SYSTEM/oracle
@PhanHe2/01_schema_data.sql
@PhanHe2/02_TC1_accounts.sql
@PhanHe2/03_YC1_C2_RBAC_KTV_BN.sql
@PhanHe2/04_YC1_C3_VPD_DPV_BS.sql
@PhanHe2/05_YC2_OLS_ThongBao.sql    -- Cần LBACSYS (Oracle Label Security)
@PhanHe2/06_YC3_Audit.sql
@PhanHe2/07_YC4_Backup_Recovery.sql
@PhanHe2/08_App_Migrations.sql
@PhanHe2/09_OLS_NhanVien_Unified.sql
@PhanHe2/09_Recovery_Demo.sql        -- Demo, có thể chạy riêng khi vấn đáp
```

Hoặc dùng runner PowerShell:

```powershell
.\scripts\setup.ps1 -HostName localhost -Port 1521 -Sid XEPDB1 -SysPass oracle -SkipRecoveryDemo
```

### Bước 2 – Chạy ứng dụng

```bash
cd HospitalApp
dotnet restore
dotnet run
```

### Bước 3 – Đăng nhập

Nhập thông tin kết nối Oracle XE và tài khoản. Ứng dụng tự nhận diện vai trò và mở giao diện phù hợp:

```text
Host: localhost
Port: 1521
Service: XEPDB1
```

| Tài khoản | Vai trò | Giao diện |
|-----------|---------|-----------|
| `SYSTEM` / DBA | Quản trị viên | AdminDashboard – Phân hệ 1 |
| `DPV_NV001` | Điều phối viên | DPVForm |
| `BS_NV003` | Bác sĩ / Y sĩ | BSForm |
| `KTV_NV006` | Kỹ thuật viên | KTVForm |
| `BN_BN001` | Bệnh nhân | BNForm |
| `u1_giamdoc` | OLS demo | OLSViewerForm |
| `u4_nvtk_hcm` | OLS demo | OLSViewerForm |
| `u8_nvth_hni` | OLS demo | OLSViewerForm |

> Mật khẩu mặc định cho tài khoản mẫu: `BV@2025!`

Mật khẩu OLS demo: `U1@2025`, `U2@2025`, ..., `U8@2025`.

### Font Montserrat

App tự nạp font từ `HospitalApp/Resources/Fonts/*.ttf`. Nếu máy chưa có font, copy các file Montserrat `.ttf` vào thư mục này rồi build lại. Nếu không có file font, app fallback về Segoe UI và vẫn chạy bình thường.

### Oracle Net Encryption

Xem [docs/guides/SETUP_ENCRYPTION.md](docs/guides/SETUP_ENCRYPTION.md). Tóm tắt cấu hình client trong `sqlnet.ora`:

```text
SQLNET.ENCRYPTION_CLIENT = REQUIRED
SQLNET.ENCRYPTION_TYPES_CLIENT = (AES256, AES192, AES128)
SQLNET.CRYPTO_CHECKSUM_CLIENT = REQUIRED
SQLNET.CRYPTO_CHECKSUM_TYPES_CLIENT = (SHA512, SHA384, SHA256)
```

Kiểm tra bằng:

```sql
SELECT NETWORK_SERVICE_BANNER
FROM V$SESSION_CONNECT_INFO
WHERE SID = SYS_CONTEXT('USERENV','SID');
```

### Bảo mật lớp ứng dụng

- `HospitalApp/Security/OracleErrorMapper.cs`: map lỗi Oracle sang thông báo thân thiện, tránh lộ raw schema/error.
- `HospitalApp/Security/InputValidator.cs`: validate CCCD, số điện thoại, mã định danh, password strength, mask CCCD.
- `HospitalApp/Security/SessionManager.cs`: idle timeout và tự logout.
- `HospitalApp/Security/AppAuditLogger.cs`: rolling log phía app.
- `HospitalApp/Controls/ConfirmDeleteDialog.cs`: xác nhận thao tác xoá.
- `HospitalApp/Forms/LoginForm.cs`: brute-force lockout 5 lần sai trong 60 giây.

### Tài liệu nộp bài và vấn đáp

- [docs/reports/REPORT_DRAFT.md](docs/reports/REPORT_DRAFT.md): khung báo cáo Markdown để điền MSSV, ảnh chụp và convert sang `.docx`.
- [docs/guides/DEMO_SCRIPT.md](docs/guides/DEMO_SCRIPT.md): kịch bản demo từng role.
- [docs/guides/TALKING_POINTS.md](docs/guides/TALKING_POINTS.md): câu hỏi vấn đáp thường gặp.
- [docs/reports/FINAL_REVIEW.md](docs/reports/FINAL_REVIEW.md): review cuối sau khi hoàn thiện repo.

---

## Bảo mật ở tầng Database

Tất cả kiểm soát truy cập được thực thi tại **Oracle DB Engine** — ứng dụng không cần xử lý thêm logic bảo mật:

```
Người dùng gửi SQL
       │
       ▼
  Oracle Engine
       ├── RBAC  → kiểm tra user có role/privilege phù hợp không
       ├── VPD   → tự động thêm WHERE clause vào câu query
       └── OLS   → so sánh nhãn data với nhãn user trước khi trả kết quả
       │
       ▼
  Trả về đúng dữ liệu người dùng được phép thấy
```
