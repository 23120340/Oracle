# Đồ án CSC12001 – An toàn và Bảo mật Dữ liệu trong HTTT

> **Môn học:** CSC12001 – An toàn và Bảo mật Dữ liệu trong Hệ thống Thông tin
> **Năm học:** 2025 – 2026
> **Trường:** Trường Đại học Khoa học Tự nhiên – Khoa Công nghệ Thông tin
> **Giảng viên:** TS. Phạm Thị Bạch Huệ · ThS. Lương Vĩ Minh · ThS. Tiết Gia Hồng

Đồ án gồm **2 phân hệ trong cùng một ứng dụng WinForms**:
- **Phân hệ 1** – Ứng dụng quản trị CSDL Oracle (cho DBA).
- **Phân hệ 2** – Ứng dụng quản lý dữ liệu y tế (RBAC, VPD, OLS, Audit, Backup/Recovery).

---

## 1. Cấu trúc repo

```
Oracle/
├── HospitalApp/                 ← Source WinForms C# (.NET 8) — Phân hệ 1 + 2
│   ├── HospitalApp.csproj
│   ├── Program.cs
│   ├── Database/OracleHelper.cs
│   ├── Forms/                   ← LoginForm, Admin/AdminDashboard, Hospital/{DPV,BS,KTV,BN,OLSViewer}Form
│   ├── Controls/                ← Sidebar, Card, StatusBar, Toast, SearchBox, MyProfilePanel, …
│   ├── Security/                ← OracleErrorMapper, InputValidator, SessionManager, AppAuditLogger
│   ├── Theme/                   ← UiTheme (Montserrat), IconRegistry, Animator
│   └── Resources/Fonts/         ← Montserrat-Regular.ttf, Montserrat-Bold.ttf (đã nhúng)
│
├── PhanHe2/                     ← Script Oracle SQL (Phân hệ 2)
│   ├── 00_UTF8_SETUP.md         ← ⚠ ĐỌC TRƯỚC: cấu hình UTF-8 cho tiếng Việt
│   ├── 01_schema_data.sql       ← Tạo BVADMIN + bảng + dữ liệu mẫu
│   ├── 02_TC1_accounts.sql      ← Tạo Oracle account cho NV/BN (TC#1)
│   ├── 03_YC1_C2_RBAC_KTV_BN.sql← RBAC: view + INSTEAD OF trigger cho KTV & Bệnh nhân
│   ├── 04_YC1_C3_VPD_DPV_BS.sql ← VPD: policy + trigger ghi vết cho ĐPV & Bác sĩ
│   ├── 05_YC2_OLS_ThongBao.sql  ← OLS: nhãn 3 thành phần cho THONGBAO (cần LBACSYS)
│   ├── 06_YC3_Audit.sql         ← Standard Audit (5 ngữ cảnh) + FGA (4 tình huống)
│   ├── 07_YC4_Backup_Recovery.sql← RMAN / Data Pump / Flashback
│   ├── 08_App_Migrations.sql    ← Sequence, APP_LOGIN_LOG, proc tạo BN
│   ├── 09_OLS_NhanVien_Unified.sql ← Cột CAPBAC/COSO/KHOA_NHAN + gán nhãn OLS nhân viên + NV_NHANVIEN_View
│   ├── 09_Recovery_Demo.sql     ← Demo phục hồi Flashback (chạy khi vấn đáp)
│   ├── 10_XE_App_Demo_Fix.sql   ← Sửa account mapping cho Oracle XE
│   ├── 11_NV_Lookup_Grants.sql  ← NV_LOOKUP_View (DPV/BS tra cứu nhân viên)
│   ├── 12_Fix_UTF8_Data.sql     ← Sửa dữ liệu tiếng Việt nếu bị lệch encoding
│   ├── 13_Audit_Grants.sql      ← Grant SELECT các bảng log
│   ├── 15_TDE_Encryption.sql    ← (Mở rộng) Mã hóa cột nhạy cảm at-rest bằng TDE
│   ├── setup_all.sql            ← Tổng hợp view + grant (chạy SAU CÙNG, bằng BVADMIN)
│   ├── setup_admin_user.sql     ← Tạo HOSPITAL_DBA (tài khoản vào AdminDashboard)
│   ├── fix_fga_ora28138.sql     ← Hotfix ORA-28138 (FGA predicate đơn) — chạy bằng BVADMIN, không cần -Reset
│   ├── fix_ols_thongbao.sql     ← Hotfix gán nhãn OLS cho THONGBAO (u1–u8 không thấy thông báo)
│   ├── fix_benhnhan_account.sql ← Hotfix: DPV tạo BN mới + tự tạo tài khoản đăng nhập (auto-MABN)
│   ├── run_migrations.ps1       ← Runner chạy 01→13 + setup_all
│   └── REVIEW_LOI_PHANHE2.md    ← Báo cáo rà soát lỗi + trạng thái sửa (checklist)
│
├── scripts/setup.ps1            ← Runner nhanh (01→10 bằng SYS)
├── docs/
│   ├── assignment/              ← Đề bài gốc (PDF)
│   ├── guides/                  ← DEMO_SCRIPT, SETUP_ENCRYPTION, TALKING_POINTS
│   ├── reports/                 ← REPORT_DRAFT, FINAL_REVIEW
│   └── planning/                ← PLAN_REMAINING
└── dist/                        ← Bản publish (win-x64, win-x64-fixed)
```

---

## 2. Yêu cầu môi trường

| Thành phần | Yêu cầu |
|------------|---------|
| Oracle Database | **Oracle XE 21c** (khuyên dùng) hoặc 19c+ |
| Oracle Label Security (OLS) | **Bắt buộc cho Yêu cầu 2** — phải được cài (xem §4) |
| Character set DB | **AL32UTF8** (mặc định của XE 21c) — cần cho tiếng Việt |
| .NET SDK | **.NET 8 SDK (Windows)** |
| NuGet | `Oracle.ManagedDataAccess.Core 23.4.0` (tự restore) |
| Công cụ chạy SQL | **SQL\*Plus** hoặc **SQLcl** (khuyên dùng vì hỗ trợ UTF-8 BOM sẵn) |

> ⚠️ **Mật khẩu trong script là giả định** — phải khớp DB của bạn hoặc sửa lại trước khi chạy:
> `SYS/<your_sys_pwd>`, `SYSTEM/oracle`, `LBACSYS/lbacsys`, `BVADMIN/"BVAdmin@2025"`.
> Các file 06/07/10 dùng biến thay thế `&&sys_pwd` → SQL\*Plus sẽ **hỏi mật khẩu SYS một lần**.

---

## 3. ⚠️ Bước BẮT BUỘC trước khi chạy: bật UTF-8 (tiếng Việt)

Nếu **không** đặt `NLS_LANG` đúng trước khi chạy script chứa tiếng Việt, dữ liệu sẽ bị lưu sai byte → hiển thị `???` hoặc lỗi dấu. Đặt **trước khi mở** SQL\*Plus:

```powershell
# PowerShell  ← PHẢI dùng cú pháp này trong PowerShell
$env:NLS_LANG = ".AL32UTF8"
```
```cmd
:: CMD (cmd.exe) — KHÔNG dùng trong PowerShell
set NLS_LANG=.AL32UTF8
```

> ⚠️ **Bẫy thường gặp:** trong **PowerShell**, `set NLS_LANG=...` **KHÔNG** đặt biến môi trường (`set` là alias của `Set-Variable`) → sqlplus đọc file UTF-8 sai charset → tiếng Việt thành rác kiểu `ThÃ´ng bÃ¡o`. Bắt buộc dùng `$env:NLS_LANG = ".AL32UTF8"` rồi mới chạy `sqlplus` trong **cùng cửa sổ**.

Kiểm tra character set của DB (phải là `AL32UTF8`):

```sql
SELECT VALUE FROM NLS_DATABASE_PARAMETERS WHERE PARAMETER = 'NLS_CHARACTERSET';
```

Chi tiết: [PhanHe2/00_UTF8_SETUP.md](PhanHe2/00_UTF8_SETUP.md).

---

## 4. Cài đặt Oracle Label Security (chỉ cho Yêu cầu 2)

OLS cần được cài và tài khoản `LBACSYS` được mở khoá. Trên XE/CDB-PDB:

```sql
-- Kết nối SYS AS SYSDBA, tới đúng PDB (vd XEPDB1)
ALTER SESSION SET CONTAINER = XEPDB1;          -- nếu là PDB
-- Cài OLS nếu chưa có:
@?/rdbms/admin/catols.sql
```

> ⚠️ **LBACSYS là COMMON USER** (dùng chung cả CDB). Đổi mật khẩu/mở khoá nó khi đang ở trong PDB
> sẽ báo `ORA-65066: must apply to all containers`. Phải làm từ **CDB root** + `CONTAINER=ALL`:
> ```powershell
> sqlplus sys/"<mat_khau_SYS>"@localhost:1521/XE as sysdba   -- service XE = root, KHÔNG phải XEPDB1
> ```
> ```sql
> ALTER USER LBACSYS IDENTIFIED BY "Lbac@2025" CONTAINER=ALL;
> ```
> (Nếu `@localhost:1521/XE` báo `ORA-12514`, dùng `sqlplus / as sysdba` để vào root.)

Kiểm tra OLS đã bật:

```sql
SELECT VALUE FROM V$OPTION WHERE PARAMETER = 'Oracle Label Security';  -- TRUE
```

> Nếu không cài OLS, các file 01–04, 06–13 vẫn chạy được; chỉ riêng **Yêu cầu 2 (file 05)** sẽ lỗi.

---

## 5. Khởi tạo CSDL — các bước chạy đầy đủ

### ✅ Cách 1 (KHUYÊN DÙNG) — một lệnh `scripts/setup.ps1`

Runner này nối **một lần** vào XEPDB1 bằng SYS, tự xử lý đúng container (CDB/PDB), tự đặt
`NLS_LANG=.AL32UTF8` và ghi file tạm **UTF-8** (tiếng Việt không hỏng dấu). Nó chạy **toàn bộ**:
`01 → 10`, rồi `11/13/setup_all` (trong schema BVADMIN), `setup_admin_user`, và demo phục hồi.

```powershell
cd D:\repos\Oracle
# Lần đầu hoặc chạy lại: thêm -Reset để DROP sạch user/role demo cũ trước (tránh ORA-01920/00955)
.\scripts\setup.ps1 -HostName localhost -Port 1521 -Sid XEPDB1 -SysPass "<mat_khau_SYS>" -Reset
```

Tham số: `-AppOnly` (bỏ audit/backup), `-SkipRecoveryDemo` (bỏ demo phục hồi),
`-BvAdminPass`, `-LbacsysPass`.

> ⚠️ **Phải dùng `-Reset` khi chạy lại** — nếu DB đã có `BVADMIN`/role từ lần trước, chạy lại
> không `-Reset` sẽ báo `ORA-01920`/`ORA-00955`/`ORA-00947`.
> Yêu cầu 2 (OLS, file 05) cần đã cài Oracle Label Security trong XEPDB1 — xem §4 (nếu chưa cài,
> runner vẫn chạy tiếp các phần khác, chỉ OLS không thiết lập được).

### Cách 2 — thủ công bằng SQL\*Plus (khi cần kiểm soát từng bước)

> Trên Oracle **XE (CDB/PDB)**: các lệnh `CONNECT user/pass` **không kèm service** bên trong file
> 01–10 sẽ nhảy về `CDB$ROOT` (sai container) → **nên dùng Cách 1** cho nhóm file này.
> Các file 11/12/13/setup_all/setup_admin_user **không có CONNECT** nên chạy thủ công tốt:

```powershell
$env:NLS_LANG = ".AL32UTF8"
```

**Pha B — kết nối BVADMIN** (view tra cứu + grant):
```powershell
sqlplus 'BVADMIN/"BVAdmin@2025"@//localhost:1521/XEPDB1'
```
```sql
@PhanHe2/11_NV_Lookup_Grants.sql   -- NV_LOOKUP_View cho DPV/BS
@PhanHe2/13_Audit_Grants.sql       -- grant SELECT bảng log
@PhanHe2/setup_all.sql             -- tổng hợp view + grant cuối cùng
@PhanHe2/12_Fix_UTF8_Data.sql      -- (chỉ khi) dữ liệu Việt mẫu bị lệch dấu
EXIT
```

**Pha C — kết nối SYS** (tài khoản DBA cho AdminDashboard):
```powershell
sqlplus 'SYS/<mat_khau_SYS>@//localhost:1521/XEPDB1 AS SYSDBA'
```
```sql
@PhanHe2/setup_admin_user.sql      -- tạo HOSPITAL_DBA / Hospital@DBA2025
EXIT
```

> `PhanHe2/run_migrations.ps1` là một runner khác (01→13 + setup_all) nhưng **phụ thuộc mật khẩu
> CONNECT cố định trong file** — kém tin cậy hơn `setup.ps1`; chỉ dùng nếu bạn đã sửa mật khẩu cho khớp.

---

## 6. Build & chạy ứng dụng

```powershell
cd HospitalApp
dotnet restore
dotnet run            # hoặc: dotnet build -c Release
```

Bản publish sẵn có trong `dist/win-x64-fixed/HospitalApp.exe`.

### Đăng nhập

Màn hình đăng nhập → bấm **Tùy chọn nâng cao** để chỉnh Host/Port/Service:

```text
Host: localhost      Port: 1521      Service: XEPDB1
```

App tự nhận diện vai trò (qua `BVADMIN.NHANVIEN/BENHNHAN`, nhãn OLS, hoặc tiền tố tên) và mở đúng giao diện:

| Tài khoản | Mật khẩu | Vai trò | Giao diện |
|-----------|----------|---------|-----------|
| `HOSPITAL_DBA` | `Hospital@DBA2025` | DBA | AdminDashboard (Phân hệ 1) |
| `SYSTEM` | *(mật khẩu DB)* | DBA | AdminDashboard (Phân hệ 1) |
| `DPV_NV001` | `BV@2025!` | Điều phối viên | DPVForm |
| `BS_NV003` | `BV@2025!` | Bác sĩ / Y sĩ | BSForm |
| `KTV_NV006` | `BV@2025!` | Kỹ thuật viên | KTVForm |
| `BN_BN001` | `BV@2025!` | Bệnh nhân | BNForm |
| `u1_giamdoc` … `u8_nvth_hni` | `U1@2025` … `U8@2025` | OLS demo | OLSViewerForm |

> ℹ️ **Đăng nhập bằng `SYSTEM`:** app kết nối SYSTEM như user thường (KHÔNG cần `AS SYSDBA`).
> Nếu không vào được thì gần như chắc chắn là **sai mật khẩu hoặc sai Service**:
> - Mật khẩu `oracle` trong các script chỉ là **giả định** — phải nhập đúng mật khẩu SYSTEM bạn đặt khi cài Oracle XE.
> - Trong **Tùy chọn nâng cao**, Service phải trỏ đúng PDB chứa schema `BVADMIN` (mặc định `XEPDB1`). Nếu SYSTEM của bạn nằm ở service khác (vd `XE`), sửa lại cho khớp.
> - Sai 5 lần liên tiếp sẽ bị app khoá tạm 60 giây — đợi hết khoá rồi thử lại.
>
> ✅ **Khuyến nghị:** dùng tài khoản DBA chuyên dụng `HOSPITAL_DBA / Hospital@DBA2025` (tạo bởi `PhanHe2/setup_admin_user.sql`) thay cho SYSTEM — mật khẩu cố định, không phụ thuộc môi trường cài đặt.

---

## 7. Phân hệ 1 – Ứng dụng Quản trị CSDL Oracle

Giao diện **AdminDashboard** cho DBA:

| Tính năng | Mô tả |
|-----------|-------|
| Quản lý User | Tạo, xoá, khoá/mở khoá tài khoản Oracle |
| Quản lý Role | Tạo, xoá role |
| Cấp quyền | Grant quyền hệ thống / đối tượng (table/view/procedure/function) / cấp role; phân quyền tới **mức cột** cho SELECT/UPDATE; tuỳ chọn **WITH GRANT OPTION** |
| Thu hồi quyền | Revoke quyền hệ thống / đối tượng / cột / role |
| Xem quyền | Liệt kê system/object/column/role privilege của user hoặc role |
| Nhật ký audit | Xem `DBA_AUDIT_TRAIL` (7 ngày gần nhất) |

---

## 8. Phân hệ 2 – Ứng dụng Quản lý Dữ liệu Y tế

### Lược đồ CSDL (schema owner = `BVADMIN`)

| Bảng | Mô tả |
|------|-------|
| `BENHNHAN` | Bệnh nhân — có cột `ORACLE_USER` ánh xạ tài khoản (TC#1) |
| `NHANVIEN` | Nhân viên (DPV/BS/KTV) — có `ORACLE_USER`, `CAPBAC/COSO/KHOA_NHAN` (nhãn OLS) |
| `HSBA` | Hồ sơ bệnh án |
| `HSBA_DV` | Dịch vụ hỗ trợ chẩn đoán |
| `DONTHUOC` | Đơn thuốc |
| `THONGBAO` | Thông báo nội bộ (áp dụng OLS) |

> Các cột văn bản dài (CHANDOAN/DIEUTRI/KETLUAN/KETQUA/TIENSUBENH/TIENSUBENHGD) dùng **`NVARCHAR2(2000)`**
> (Unicode) để so sánh `:OLD/:NEW` trong trigger ghi vết và tránh lỗi `ORA-00932`.

### Yêu cầu 1 — Cấp quyền truy cập

**TC#1:** DBA tạo Oracle account cho mọi nhân viên/bệnh nhân; lưu tên tài khoản vào cột `ORACLE_USER`
→ nhận diện người dùng chỉ cần **1 bảng**: `WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER')`.

> 🔒 **MÃBN bất biến:** `MABN` là khoá chính + cơ sở tên tài khoản `BN_<MABN>` (TC#1) + bị `HSBA` tham
> chiếu (FK). Khi DPV tạo BN mới, mã **tự sinh** (`SEQ_BENHNHAN`) và **không cho sửa** — chặn ở app (ô khoá)
> lẫn DB (trigger `trg_benhnhan_mabn_immutable` raise `ORA-20010` nếu `UPDATE` đổi MABN). DPV vẫn "sửa dữ
> liệu BENHNHAN" (TC#2) ở các trường khác. TC#5 cũng quy định bệnh nhân không được sửa mã. → Phù hợp đề bài.

**Câu 2 — RBAC** (Kỹ thuật viên, Bệnh nhân):

| Role | Cơ chế | Quyền |
|------|--------|-------|
| `KTV_Role` | View `KTV_HSBA_DV_View` + INSTEAD OF trigger | Chỉ xem `HSBA_DV` mình thực hiện (`MAKTV`=mình); UPDATE duy nhất `KETQUA` (có ghi vết) |
| `BenhNhan_Role` | View `BN_BENHNHAN_View`/`BN_HSBA_View` + trigger | Chỉ xem dòng của mình; sửa địa chỉ + tiền sử bệnh; **không** sửa MABN/TENBN/PHAI/NGAYSINH/CCCD |

**Câu 3 — VPD** (Điều phối viên, Bác sĩ):

| Role | Bảng | Predicate VPD |
|------|------|---------------|
| `DPV_Role` | `HSBA`, `HSBA_DV`, `BENHNHAN` | rỗng (xem tất cả) |
| `BS_Role` | `HSBA` | `MABS = fn_get_manv()` |
| `BS_Role` | `HSBA_DV`, `DONTHUOC` | `MAHSBA IN (SELECT MAHSBA FROM HSBA WHERE MABS = …)` |
| `BS_Role` | `BENHNHAN` | `MABN IN (SELECT MABN FROM HSBA WHERE MABS = …)` |

> Các hàm policy VPD cũng có **nhánh cho KTV và Bệnh nhân** (để RBAC view ở Câu 2 hoạt động dưới VPD),
> và **miễn lọc cho `BVADMIN`** (phục vụ bảo trì/sửa dữ liệu). Mọi UPDATE
> `CHANDOAN`/`DIEUTRI`/`KETLUAN` và `TENTHUOC`/`LIEUDUNG` đều được trigger ghi vết.

### Yêu cầu 2 — Oracle Label Security (THONGBAO)

Policy `BV_LABEL_POLICY`, nhãn **3 thành phần**:

| Thành phần | Giá trị | Ngữ nghĩa |
|-----------|---------|-----------|
| **Level** | `NV(10)` < `LDK(20)` < `BGD(30)` | Cấp bậc |
| **Compartment** | `HCM`, `HPN`, `HNI` | Cơ sở (AND – phải đúng cơ sở) |
| **Group** | `TH`, `TK`, `TM` | Khoa (OR – cần ≥1 khoa) |

Nhãn dữ liệu mẫu t1–t7: `NV`, `BGD`, `LDK`, `LDK::TH`, `NV:HCM:TH`, `NV:HNI:TH`, `LDK:HPN:TH,TK`.
User u1–u8 được gán `max_read_label` tương ứng (xem file 05). User u1–u8 được tạo bằng SYSTEM, gán nhãn bằng LBACSYS.

> ⚠️ **Nếu đăng nhập u1–u8 mà KHÔNG thấy thông báo nào** (kể cả `u1_giamdoc` lẽ ra thấy đủ 7): các dòng
> `THONGBAO` chưa được gán nhãn (cột `OLS_LABEL` = NULL). Hai nguyên nhân:
> 1. Gán nhãn dòng phải chạy bằng **phiên BVADMIN thật** (có quyền FULL), KHÔNG chạy qua `setup.ps1`
>    (vốn chạy mọi thứ dưới SYS).
> 2. Policy chỉ bật `READ_CONTROL` nên `SA_SESSION.SET_ROW_LABEL` **không** tự gán nhãn khi INSERT —
>    phải gán thẳng `OLS_LABEL = CHAR_TO_LABEL('BV_LABEL_POLICY', '<nhãn>')` trong câu INSERT.
>
> **Cách sửa nhanh** — chạy [PhanHe2/fix_ols_thongbao.sql](PhanHe2/fix_ols_thongbao.sql) bằng CONNECT thật:
> ```powershell
> $env:NLS_LANG = ".AL32UTF8"   # PowerShell — KHÔNG dùng "set" (xem §3)
> sqlplus /nolog "@d:\repos\Oracle\PhanHe2\fix_ols_thongbao.sql"
> ```
> Yêu cầu trước đó: đã đặt mật khẩu LBACSYS (§4) và BVADMIN có quyền FULL (script tự cấp).
> Kết quả đúng: cột `OLS_LABEL` in ra **có số** → đăng nhập `u1_giamdoc/U1@2025` thấy đủ 7 thông báo.

### Yêu cầu 3 — Kiểm toán

- **Standard Audit:** 5 ngữ cảnh theo user/đối tượng cụ thể, cả thành công lẫn thất bại.
- **Fine-Grained Audit (FGA):** 4 tình huống (cập nhật ĐƠNTHUỐC sau khi tạo; BS cập nhật HSBA hợp lệ; cập nhật bất hợp pháp; thao tác bất hợp pháp trên HSBA_DV).
  - ⚠️ `audit_condition` của FGA **phải là một predicate đơn** — không được chứa `AND`/`OR`/`IN` (vi phạm → `ORA-28138` khi DML). Các tình huống "bất hợp pháp" vì thế bọc logic nhiều toán tử vào hàm `fn_is_illegal_hsba` / `fn_is_illegal_hsba_dv` (trả `'Y'/'N'`) rồi so sánh đơn.
- **Trigger log:** `LOG_BS_HSBA`, `LOG_BS_DONTHUOC`, `LOG_KTV_KETQUA` (lưu giá trị cũ/mới).
- ⚠️ Trên Oracle 21c chạy **Unified Auditing**, đọc nhật ký từ `UNIFIED_AUDIT_TRAIL` (xem PHẦN 1B trong file 06) thay vì `DBA_AUDIT_TRAIL`.

### Yêu cầu 4 — Sao lưu & Phục hồi

| Phương pháp | Loại | Ghi chú |
|-------------|------|---------|
| RMAN Full / Incremental | Physical | Job Scheduler gọi `.bat` (mặc định **chưa bật** — bật sau khi cấu hình đường dẫn) |
| Data Pump (expdp) | Logical | Job Scheduler gọi `.bat` |
| Flashback Table / Query | Point-in-time | Dùng undo + ROW MOVEMENT — demo ở `09_Recovery_Demo.sql` |

### (Mở rộng) Mã hóa — Cryptography

Bổ sung tầng **mã hóa** cho access-control (để có cả *access control + cryptography*):

- **Mã hóa đường truyền** — Oracle Native Network Encryption (AES256 + SHA) qua `sqlnet.ora`. App `Oracle.ManagedDataAccess` tự thương lượng, không sửa code.
- **Mã hóa dữ liệu at-rest** — TDE (Transparent Data Encryption, AES) cho cột nhạy cảm: `BENHNHAN.CCCD`, `NHANVIEN.CMND` (NO SALT → giữ UNIQUE) + `BENHNHAN.DIUNGTHUOC`. Trong suốt với app; chạy `15_TDE_Encryption.sql`. *(`TIENSUBENH/TIENSUBENHGD` không mã hóa được do giới hạn kích thước NVARCHAR2 — `ORA-28331`.)*

→ Chi tiết từng bước + kiểm chứng: **[docs/guides/SETUP_ENCRYPTION.md](docs/guides/SETUP_ENCRYPTION.md)**.

> ⚠️ TDE: **giữ kỹ mật khẩu keystore + sao lưu thư mục wallet** — mất là không giải mã được dữ liệu.

---

## 9. Bảo mật & UTF-8 ở tầng ứng dụng

- **Tiếng Việt (UTF-8) đầu-cuối:** đọc dữ liệu qua `TO_NCHAR(...)` (giữ Unicode, không phụ thuộc DB charset);
  ghi dữ liệu bind tham số kiểu **`NVarchar2`** (`OracleHelper.Param`); mọi file `.cs` lưu **UTF-8 có BOM**.
- **Font Montserrat** được **nhúng** (`Resources/Fonts/*.ttf`, đủ glyph tiếng Việt); fallback Segoe UI nếu thiếu.
- `Security/OracleErrorMapper.cs` – map lỗi Oracle sang thông báo thân thiện (không lộ schema).
- `Security/InputValidator.cs` – validate CCCD/SĐT/mã định danh, mask CCCD.
- `Security/SessionManager.cs` – idle timeout tự logout.
- `Security/AppAuditLogger.cs` – log phía app.
- `Forms/LoginForm.cs` – khoá brute-force 5 lần sai / 60 giây.
- Oracle Net Encryption (TLS/checksum): xem [docs/guides/SETUP_ENCRYPTION.md](docs/guides/SETUP_ENCRYPTION.md).

Tất cả kiểm soát truy cập do **Oracle DB Engine** thực thi (RBAC + VPD + OLS) — app kết nối bằng đúng tài khoản đăng nhập, không tự xử lý logic phân quyền.

---

## 10. Khắc phục sự cố thường gặp

| Triệu chứng | Nguyên nhân & cách xử lý |
|-------------|--------------------------|
| `ORA-01756 quoted string not properly terminated`, chữ Việt thành rác (`ßnh`, `Θ`, `╨`…) khi chạy `setup.ps1` | `setup.ps1` cũ ghi file tạm bằng ANSI làm hỏng UTF-8 → **đã sửa** (ghi UTF-8 + tự đặt `NLS_LANG`); dùng bản `setup.ps1` mới |
| `ORA-01920`/`ORA-00955`/`ORA-00947` khi chạy lại | DB còn `BVADMIN`/bảng/role từ lần trước → chạy `setup.ps1` với **`-Reset`** (drop sạch rồi tạo lại) |
| `PLS-00103 Encountered end-of-file` ở `EXEC ...` (file 02) | Comment `--` cùng dòng `EXEC` nuốt mất `END;` → **đã sửa** (bỏ comment cuối dòng); dùng bản file 02 mới |
| `ORA-00933 SQL command not properly ended` ở câu GRANT/ALTER | Có comment `--` ngay sau `;` trên cùng dòng → SQL\*Plus không nhận `;` là dấu kết thúc → **đã sửa** (đưa comment lên dòng riêng) |
| `Enter value for ...:` (prompt đứng im) | Ký tự `&` trong comment/chuỗi bị hiểu là biến thay thế → **đã sửa** (`setup.ps1` tự `SET DEFINE OFF`; bỏ `&` trong comment). Đang kẹt: bấm **Ctrl+C** để thoát |
| Tiếng Việt thành `???` / lỗi dấu trong DB | Chưa đặt `NLS_LANG=.AL32UTF8` trước khi chạy 01 → chạy lại sau khi set, hoặc chạy `12_Fix_UTF8_Data.sql` bằng BVADMIN |
| Tiếng Việt thành `ThÃ´ng bÃ¡o` khi chạy file `.sql` bằng `sqlplus` trong **PowerShell** | Đã gõ `set NLS_LANG=...` (cú pháp CMD, vô tác dụng trong PowerShell) → sqlplus đọc UTF-8 sai. Dùng `$env:NLS_LANG = ".AL32UTF8"` rồi chạy lại trong cùng cửa sổ (xem §3) |
| Đăng nhập u1–u8 OLS **không thấy thông báo nào** | `THONGBAO.OLS_LABEL` đang NULL (nhãn chưa gán) → chạy `PhanHe2/fix_ols_thongbao.sql` bằng CONNECT thật (xem §8 – Yêu cầu 2) |
| DPV tạo bệnh nhân mới **nhưng tài khoản đăng nhập không tạo được** (báo `ORA-01031`) | BVADMIN thiếu quyền cấp `CREATE SESSION` cho tài khoản BN mới → chạy `PhanHe2/fix_benhnhan_account.sql` (cấp `CREATE SESSION ... WITH ADMIN OPTION` + viết lại `sp_create_benhnhan_full` an toàn, MABN tự sinh) |
| `ORA-65066: must apply to all containers` khi `ALTER USER LBACSYS` | LBACSYS là common user → đổi từ **CDB root** (`@.../XE`) + `CONTAINER=ALL` (xem §4) |
| `ORA-12660` khi app kết nối sau khi bật NNE | Server đặt `ENCRYPTION_SERVER=REQUIRED` nhưng client không thỏa → tạm hạ `REQUESTED` (xem SETUP_ENCRYPTION.md §1.3) |
| `ORA-28365: wallet is not open` sau khi bật TDE | Keystore chưa mở (thường do restart DB mà chưa tạo auto-login) → tạo **auto-login keystore** hoặc mở tay `ADMINISTER KEY MANAGEMENT SET KEYSTORE OPEN ...` (SETUP_ENCRYPTION.md §2.4) |
| `ORA-12154: could not resolve connect identifier` khi gõ tay trong PowerShell | Mật khẩu có ký tự `@` + PowerShell nuốt dấu `"` → sqlplus hiểu nhầm. Test bằng `sqlplus /nolog` rồi `CONNECT user/"pass@..."@host` bên trong; hoặc đổi mật khẩu không có `@` |
| Tiếng Việt lỗi dấu trên giao diện | Dùng bản app mới (đã đổi `TO_CHAR`→`TO_NCHAR` + bind `NVarchar2`); rebuild lại |
| File 05 lỗi `SA_*`/`LBACSYS` | OLS chưa được cài/mở khoá → xem §4 |
| `ORA-01017 invalid username/password` khi chạy script | Mật khẩu CONNECT trong file chưa khớp DB → sửa lại (SYS/SYSTEM/LBACSYS/BVADMIN) — xem §2 |
| **`ORA-28138`** khi BS *thêm dịch vụ chẩn đoán* hoặc KTV *lưu kết quả* | `audit_condition` của FGA chứa `OR`/`NOT IN` (Oracle chỉ cho 1 predicate đơn) → **đã sửa** (file 06 bọc logic vào hàm `fn_is_illegal_hsba*` trả `'Y'/'N'`). Áp dụng nhanh không cần `-Reset`: chạy `PhanHe2/fix_fga_ora28138.sql` bằng `BVADMIN`, hoặc `setup.ps1 -Reset` |
| Đăng nhập app bằng `SYSTEM` báo `ORA-01017` | Mật khẩu `oracle` chỉ là **giả định** — nhập đúng mật khẩu SYSTEM thật của bạn; hoặc dùng `HOSPITAL_DBA/Hospital@DBA2025` (xem §6) |
| Đăng nhập app báo `ORA-28000`/`ORA-28001` | Tài khoản bị **khoá** / mật khẩu **hết hạn** ở DB → `ALTER USER <user> ACCOUNT UNLOCK;` (và đặt lại mật khẩu nếu cần) bằng SYS/SYSTEM |
| Đăng nhập app báo "Tài khoản tạm khoá Ns" | App tự khoá 60 giây sau **5 lần sai** liên tiếp → đợi hết khoá rồi nhập đúng mật khẩu |
| DPV/BS không thấy danh sách bác sĩ/KTV | Chưa chạy `11_NV_Lookup_Grants.sql` (Pha B) |
| Đăng nhập DBA không vào được AdminDashboard | Chưa chạy `setup_admin_user.sql` (Pha C) hoặc dùng `HOSPITAL_DBA/Hospital@DBA2025` |
| Audit trả 0 dòng | DB ở chế độ Unified Auditing → đọc `UNIFIED_AUDIT_TRAIL` (file 06 PHẦN 1B) |

---

## 11. Tài liệu kèm theo

- [PhanHe2/REVIEW_LOI_PHANHE2.md](PhanHe2/REVIEW_LOI_PHANHE2.md) – rà soát lỗi chi tiết + trạng thái đã sửa (checklist B/H/M/L).
- [docs/reports/REPORT_DRAFT.md](docs/reports/REPORT_DRAFT.md) – khung báo cáo để điền MSSV/ảnh, convert `.docx`.
- [docs/guides/DEMO_SCRIPT.md](docs/guides/DEMO_SCRIPT.md) – kịch bản demo từng vai trò.
- [docs/guides/TALKING_POINTS.md](docs/guides/TALKING_POINTS.md) – câu hỏi vấn đáp thường gặp.
- [docs/guides/SETUP_ENCRYPTION.md](docs/guides/SETUP_ENCRYPTION.md) – cấu hình Oracle Net Encryption.
- [docs/reports/FINAL_REVIEW.md](docs/reports/FINAL_REVIEW.md) – review tổng thể.
