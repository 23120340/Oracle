-- ============================================================
-- PHÂN HỆ 2 - File 13: Grant SELECT cho audit log tables
-- ============================================================
-- Cho phép DPV/BS/KTV xem nhật ký audit:
--   • APP_LOGIN_LOG      — đăng nhập ứng dụng (file 08)
--   • LOG_BS_HSBA        — thay đổi HSBA (file 04)
--   • LOG_BS_DONTHUOC    — thay đổi đơn thuốc (file 04)
--   • LOG_KTV_KETQUA     — cập nhật kết quả xét nghiệm (file 03)
-- ============================================================
-- Chạy với BVADMIN (owner của log tables) — không cần SYSTEM:
--   sqlplus BVADMIN/<BVADMIN_pass>@//localhost:1521/XEPDB1
--   SQL> @13_Audit_Grants.sql
-- ============================================================

GRANT SELECT ON APP_LOGIN_LOG    TO DPV_Role;
GRANT SELECT ON LOG_BS_HSBA      TO DPV_Role;
GRANT SELECT ON LOG_BS_DONTHUOC  TO DPV_Role;
GRANT SELECT ON LOG_KTV_KETQUA   TO DPV_Role;

GRANT SELECT ON LOG_BS_HSBA      TO BS_Role;
GRANT SELECT ON LOG_BS_DONTHUOC  TO BS_Role;
GRANT SELECT ON LOG_KTV_KETQUA   TO BS_Role;

GRANT SELECT ON LOG_KTV_KETQUA   TO KTV_Role;

-- Verify
SELECT 'APP_LOGIN_LOG' AS TBL, COUNT(*) FROM APP_LOGIN_LOG
UNION ALL SELECT 'LOG_BS_HSBA',     COUNT(*) FROM LOG_BS_HSBA
UNION ALL SELECT 'LOG_BS_DONTHUOC', COUNT(*) FROM LOG_BS_DONTHUOC
UNION ALL SELECT 'LOG_KTV_KETQUA',  COUNT(*) FROM LOG_KTV_KETQUA;
