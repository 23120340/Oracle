-- ============================================================
-- PHÂN HỆ 2 - File 11: NV_LOOKUP_View + grants cho DPV/BS/KTV
-- ============================================================
-- Vấn đề: DPV cần lookup BS để gán HSBA, KTV để gán dịch vụ;
-- BS cần lookup KTV để chỉ định; nhưng các role không có SELECT trên
-- NHANVIEN (table chứa CMND nhạy cảm).
--
-- Giải pháp: tạo NV_LOOKUP_View expose chỉ (MANV, HOTEN, VAITRO, CHUYENKHOA).
-- Grant SELECT cho 3 role.
-- ============================================================
-- Chạy: SET NLS_LANG=.AL32UTF8 trước khi sqlplus
-- ============================================================

CONNECT BVADMIN/BVAdmin@2025;

CREATE OR REPLACE VIEW NV_LOOKUP_View AS
SELECT MANV, HOTEN, VAITRO, CHUYENKHOA
FROM   NHANVIEN;

CONNECT SYSTEM/oracle;

GRANT SELECT ON BVADMIN.NV_LOOKUP_View TO DPV_Role;
GRANT SELECT ON BVADMIN.NV_LOOKUP_View TO BS_Role;
GRANT SELECT ON BVADMIN.NV_LOOKUP_View TO KTV_Role;

-- Kiểm tra
CONNECT BVADMIN/BVAdmin@2025;
SELECT MANV, HOTEN, VAITRO FROM NV_LOOKUP_View ORDER BY VAITRO, HOTEN;
