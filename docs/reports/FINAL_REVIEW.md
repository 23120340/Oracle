# Final Review

Ngay review: 2026-05-24

## Ket qua da lam

- Bo sung `PhanHe2/09_OLS_NhanVien_Unified.sql` de hop nhat nhan OLS vao `NHANVIEN`.
- Bo sung `PhanHe2/09_Recovery_Demo.sql` cho kich ban Flashback recovery co checkpoint SCN.
- Bo sung `scripts/setup.ps1` de chay tuan tu script SQL 01-09.
- Sap xep tai lieu vao `docs/assignment`, `docs/guides`, `docs/reports`, `docs/planning`.
- Cap nhat README theo migration 08-09, Oracle Net encryption, font Montserrat va tai khoan OLS.
- Cap nhat app de tab profile hien thi `CAPBAC/COSO/KHOA_NHAN` va tab thong bao co nhan OLS hien tai.
- Khoi phuc `ApplicationIcon` va tao `HospitalApp/hospital.ico`.
- `dotnet build` va `dotnet publish -c Release -r win-x64 --self-contained false -o dist/win-x64` thanh cong voi 0 warning, 0 error.

## Review rui ro con lai

- Chua the xac thuc runtime tren Oracle that trong moi truong nay. Can chay SQL*Plus voi database co OLS, LBACSYS va Flashback da bat.
- `REPORT_DRAFT.md` la khung bao cao; can dien MSSV/ho ten that va chen screenshot truoc khi convert sang docx.
- `09_OLS_NhanVien_Unified.sql` tao procedure trong schema `LBACSYS`; may Oracle co the can cap quyen/ten password LBACSYS khac voi `lbacsys`.
- `09_Recovery_Demo.sql` dung `FLASHBACK TABLE`, nen can undo retention du va quyen Flashback/row movement.
- Mot so file hien co dang mojibake do encoding lich su; build C# van xu ly duoc chuoi, nhung nen mo bang UTF-8 trong editor de tranh ghi de sai encoding.

## Checklist can chay tren may co Oracle

```powershell
.\scripts\setup.ps1 -HostName localhost -Port 1521 -Sid XEPDB1 -SysPass oracle -BvAdminPass BVAdmin@2025 -LbacsysPass lbacsys
```

Sau do test:

- Dang nhap `DPV_NV001`, `BS_NV003`, `KTV_NV006`, `BN_BN001`.
- Dang nhap `u1_giamdoc` va `u4_nvtk_hcm` de so sanh OLS.
- Chay `@PhanHe2/09_Recovery_Demo.sql` va chup ket qua.
- Query `DBA_AUDIT_TRAIL` va `DBA_FGA_AUDIT_TRAIL`.

## Lenh build

```powershell
cd HospitalApp
dotnet build
dotnet publish -c Release -r win-x64 --self-contained false -o ..\dist\win-x64
```
