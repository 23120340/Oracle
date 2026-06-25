# SQL UTF-8 Setup — BẮT BUỘC đọc trước khi chạy script

> Mọi file `.sql` trong thư mục này đều **UTF-8 với BOM**.
> Vietnamese strings dùng `N'...'` literal (NVARCHAR2).
> Nếu chạy mà thấy chữ Việt thành `???` hoặc kí tự lạ → đọc file này.

---

## Vấn đề thường gặp

Khi SQL*Plus / SQL Developer / SQLcl đọc file `.sql` chứa tiếng Việt mà **NLS_LANG client** không khớp UTF-8, Oracle decode sai → INSERT vào DB bị lệch byte → đọc ra `???`.

## Cách 1 — Set `NLS_LANG` trước khi chạy (khuyên dùng)

### Windows CMD
```cmd
set NLS_LANG=.AL32UTF8
sqlplus SYSTEM/oracle@//localhost:1521/XEPDB1
SQL> @D:\repos\Oracle\PhanHe2\01_schema_data.sql
```

### Windows PowerShell
```powershell
$env:NLS_LANG = ".AL32UTF8"
sqlplus SYSTEM/oracle@//localhost:1521/XEPDB1
```

### Persistent (Windows User env)
```powershell
[Environment]::SetEnvironmentVariable("NLS_LANG", "AMERICAN_AMERICA.AL32UTF8", "User")
# Restart terminal sau khi set
```

## Cách 2 — SQL Developer

`Tools` → `Preferences` → `Environment` → `Encoding` → **UTF-8**
Restart SQL Developer.

## Cách 3 — SQLcl (Oracle SQLcl modern)

SQLcl mặc định đọc UTF-8 với BOM → không cần config thêm. Khuyên dùng SQLcl thay SQL*Plus cũ.

## Verify sau khi chạy

```sql
ALTER SESSION SET NLS_LENGTH_SEMANTICS = CHAR;

SELECT MABN, TENBN, TIENSUBENH FROM BVADMIN.BENHNHAN WHERE ROWNUM = 1;
```

Nếu thấy đúng `"Mai Thi Hoa"` + `"Tiểu đường type 2"` → encoding OK.
Nếu thấy `???` hoặc kí tự lạ → NLS_LANG client sai, drop schema và chạy lại.

## Verify database character set

```sql
SELECT VALUE FROM NLS_DATABASE_PARAMETERS WHERE PARAMETER = 'NLS_CHARACTERSET';
-- Phải là AL32UTF8 (hoặc UTF8)

SELECT VALUE FROM NLS_DATABASE_PARAMETERS WHERE PARAMETER = 'NLS_NCHAR_CHARACTERSET';
-- Phải là AL16UTF16 hoặc UTF8
```

Nếu DB charset KHÔNG phải AL32UTF8 (ví dụ WE8MSWIN1252), bạn cần tạo lại DB với AL32UTF8 character set. Oracle XE 21c mặc định AL32UTF8 → OK.

## Thứ tự chạy script

```sql
@01_schema_data.sql              -- schema + dữ liệu mẫu
@02_TC1_accounts.sql             -- Oracle accounts cho NV/BN (TC#1)
@03_YC1_C2_RBAC_KTV_BN.sql       -- RBAC + view + INSTEAD OF trigger
@04_YC1_C3_VPD_DPV_BS.sql        -- VPD policy + audit trigger
@05_YC2_OLS_ThongBao.sql         -- OLS labels (cần LBACSYS)
@06_YC3_Audit.sql                -- Standard + FGA audit
@07_YC4_Backup_Recovery.sql      -- RMAN, DataPump, Flashback config
@08_App_Migrations.sql           -- SEQ_HSBA, NV_NHANVIEN_View, app log
@09_OLS_NhanVien_Unified.sql     -- (tuỳ chọn) hợp nhất OLS với NHANVIEN
@extras/recovery_demo.sql        -- (tuỳ chọn) demo flashback recovery
@10_XE_App_Demo_Fix.sql          -- (nếu cần) fix cho Oracle XE
```
