-- ============================================================
-- SETUP_ALL.SQL — Migration tổng hợp (chạy SAU CÙNG)
-- ============================================================
-- Gộp các view + grant mà các form cần để hoạt động.
-- ⚠️ PHẢI chạy SAU khi đã chạy 01 → 13 (cần sẵn role DPV/BS/KTV và bảng THONGBAO).
--
-- FIX (M10/H7): Bước tạo cột CAPBAC/COSO/KHOA_NHAN và gán nhãn OLS cho nhân viên
--   ĐÃ nằm ở 09_OLS_NhanVien_Unified.sql (gán nhãn do LBACSYS thực hiện đúng quyền).
--   Bỏ khỏi đây để tránh định nghĩa trùng / mâu thuẫn ràng buộc và lỗi quyền OLS.
-- ============================================================
-- CÁCH CHẠY (bằng BVADMIN — owner schema):
--   PowerShell:  $env:NLS_LANG = ".AL32UTF8"
--   sqlplus BVADMIN/<BVADMIN_pass>@//localhost:1521/XEPDB1
--   SQL> @setup_all.sql
-- ============================================================

SET DEFINE OFF
SET ECHO ON

PROMPT ── 1. NV_LOOKUP_View (danh bạ điều phối cho DPV/BS) ─────────

CREATE OR REPLACE VIEW NV_LOOKUP_View AS
SELECT MANV, HOTEN, VAITRO, CHUYENKHOA
FROM   NHANVIEN;

-- FIX (M7): chỉ DPV (gán BS/KTV cho HSBA) và BS (chọn KTV cho dịch vụ) cần view này.
-- KTV KHÔNG cần tra danh sách toàn bộ nhân viên → bỏ grant cho KTV_Role (giảm lộ thông tin).
GRANT SELECT ON NV_LOOKUP_View TO DPV_Role;
GRANT SELECT ON NV_LOOKUP_View TO BS_Role;

PROMPT ── 2. Grant SELECT trên THONGBAO ─────────────────────────

GRANT SELECT ON THONGBAO TO DPV_Role;
GRANT SELECT ON THONGBAO TO BS_Role;
GRANT SELECT ON THONGBAO TO KTV_Role;

-- Lưu ý: việc grant SELECT các bảng nhật ký (APP_LOGIN_LOG, LOG_BS_HSBA, LOG_BS_DONTHUOC,
-- LOG_KTV_KETQUA) được quản lý ở file 13_Audit_Grants.sql (theo nhu cầu của app).

PROMPT ── 3. Verify ───────────────────────────────────────────

SELECT 'NV_LOOKUP_View' AS OBJECT_NAME, COUNT(*) AS SO_DONG FROM NV_LOOKUP_View
UNION ALL SELECT 'THONGBAO', COUNT(*) FROM THONGBAO;

PROMPT ── DONE ───────────────────────────────────────────────────
