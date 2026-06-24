-- ============================================================
-- PHÂN HỆ 2 - File 11: NV_LOOKUP_View + grants cho DPV/BS/KTV
-- ============================================================
-- Vấn đề: DPV cần lookup BS để gán HSBA, KTV để gán dịch vụ.
-- Giải pháp: view NV_LOOKUP_View expose (MANV, HOTEN, VAITRO, CHUYENKHOA).
-- ============================================================
-- Cách chạy (FIX M8): TẤT CẢ chạy bằng BVADMIN — owner view được phép grant trực tiếp
-- cho role, KHÔNG cần file *_GRANTS.sql riêng (file đó không tồn tại):
--   sqlplus BVADMIN/<BVADMIN_pass>@//localhost:1521/XEPDB1
--   SQL> @11_NV_Lookup_Grants.sql
-- ============================================================
SET DEFINE OFF
-- BLOCK A — Chạy với BVADMIN
-- ============================================================

CREATE OR REPLACE VIEW NV_LOOKUP_View AS
SELECT MANV, HOTEN, VAITRO, CHUYENKHOA
FROM   NHANVIEN;

-- Cho phép DPV/BS select view này (BVADMIN owner của view nên grant trực tiếp được).
-- FIX (M7): KTV không cần tra danh sách nhân viên → không grant cho KTV_Role.
GRANT SELECT ON NV_LOOKUP_View TO DPV_Role;
GRANT SELECT ON NV_LOOKUP_View TO BS_Role;

-- Verify
SELECT MANV, HOTEN, VAITRO FROM NV_LOOKUP_View ORDER BY VAITRO, HOTEN;
