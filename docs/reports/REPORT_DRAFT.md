# Bao cao do an ATBM HTTT - HospitalApp

> Dien ten nhom, MSSV va anh chup man hinh that truoc khi convert sang `.docx`.

## 1. Thong tin nhom

| MSSV | Ho ten | Phan he 1 | Phan he 2 | Tong |
| --- | --- | ---: | ---: | ---: |
| 21120xxx | Thanh vien A | 40% | 35% | 37% |
| 21120yyy | Thanh vien B | 35% | 35% | 35% |
| 21120zzz | Thanh vien C | 25% | 30% | 28% |

## 2. Kien truc tong quan

HospitalApp la ung dung WinForms .NET 8 ket noi Oracle qua `Oracle.ManagedDataAccess.Core`. Ung dung khong tu quyet dinh quyen truy cap du lieu y te; RBAC, VPD, OLS, audit va trigger duoc thuc thi o Oracle Database Engine.

Luong chinh: WinForms -> `OracleHelper` -> Oracle DB -> RBAC/VPD/OLS/Audit.

## 3. Phan he 1 - Quan tri Oracle

Giao dien DBA ho tro tao/xoa/khoa user, tao/xoa role, grant/revoke system privilege, object privilege, column privilege va role. Cac input dinh danh Oracle di qua `OracleHelper.SafeIdentifier` de giam rui ro injection trong DDL.

Can chen screenshot cac tab trong `AdminDashboard`.

## 4. Phan he 2 - Quan ly du lieu y te

### 4.1 Schema

Bang chinh gom `NHANVIEN`, `BENHNHAN`, `HSBA`, `HSBA_DV`, `DONTHUOC`, `THONGBAO`. Hai bang `NHANVIEN` va `BENHNHAN` co cot `ORACLE_USER` de anh xa mot nguoi dung ung dung voi mot Oracle account.

### 4.2 TC#1 - Account mapping

Script `02_TC1_accounts.sql` tao account cho nhan vien/benh nhan va cap role tuong ung. Migration `08_App_Migrations.sql` bo sung `sp_create_benhnhan_full` de DPV tao benh nhan moi kem Oracle account.

### 4.3 YC1 - RBAC va VPD

KTV va benh nhan dung view + trigger de chi thay va chi sua cot hop le. DPV/BS dung VPD: DPV xem duoc toan bo du lieu dieu phoi; BS chi thay ho so minh phu trach.

### 4.4 YC2 - OLS

Policy `BV_LABEL_POLICY` co level `NV < LDK < BGD`, compartment `HCM/HPN/HNI`, group `TH/TK/TM`. Script `05_YC2_OLS_ThongBao.sql` tao user demo `u1-u8`; script `09_OLS_NhanVien_Unified.sql` gan nhan truc tiep cho nhan vien trong `NHANVIEN`.

### 4.5 YC3 - Audit

`06_YC3_Audit.sql` cau hinh Standard Audit va FGA. Cac trigger log nghiep vu ghi gia tri cu/moi cho BS/KTV. Khi demo can chup `DBA_AUDIT_TRAIL`, `DBA_FGA_AUDIT_TRAIL` va cac bang log.

### 4.6 YC4 - Backup/Recovery

`07_YC4_Backup_Recovery.sql` mo ta RMAN, Data Pump, Flashback. `09_Recovery_Demo.sql` la kich ban co the chay de tao checkpoint SCN, gia lap xoa nham `HSBA_DV`, doc audit/FGA va Flashback table ve SCN.

## 5. Bao mat lop ung dung

- `OracleErrorMapper`: khong day raw `ORA-xxxxx` len UI.
- `InputValidator`: validate CCCD, phone, ma dinh danh, password strength.
- `SessionManager`: idle timeout va logout ro rang.
- `AppAuditLogger`: rolling log cho thao tac app.
- `ConfirmDeleteDialog`: xac nhan thao tac xoa.
- Login form: lockout 5 lan sai trong 60 giay.

## 6. UI/UX

Ung dung dung theme tap trung trong `Theme/UiTheme.cs`, font Montserrat neu co file `.ttf`, toast, search box, shortcut F5/Ctrl+S/Ctrl+L, tab self-service cho nhan vien va tab thong bao OLS.

## 7. Huong dan build

```powershell
cd HospitalApp
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained false -o ..\dist\win-x64
```

## 8. Ket luan

Do an the hien defense in depth: database enforcement bang RBAC/VPD/OLS/Audit, app hardening de giam loi thao tac va giam ro ri thong tin tren giao dien.
