-- ============================================================
-- verify_all.sql — KIỂM TRA NHANH (smoke test) Phân hệ 2 sau khi cài
-- ============================================================
-- Mục đích: thành viên chạy 1 lệnh để xác nhận YC1–YC4 đã thiết lập đúng.
-- Chỉ ĐỌC (SELECT), KHÔNG sửa dữ liệu.
--
-- CÁCH CHẠY (PowerShell):
--   $env:NLS_LANG = ".AL32UTF8"
--   sqlplus /nolog "@PhanHe2/verify_all.sql"
-- ============================================================
-- >>> ĐIỀN MẬT KHẨU SYS CỦA MÁY BẠN VÀO ĐÂY (chỉ sửa 1 dòng) <<<
DEFINE SYS_PWD = "oracle"
DEFINE DB      = "//localhost:1521/XEPDB1"

SET DEFINE ON
SET LINESIZE 140
SET PAGESIZE 200
WHENEVER SQLERROR CONTINUE

PROMPT
PROMPT ================== KIEM TRA PHAN HE 2 (YC1-YC4) ==================

CONNECT sys/"&SYS_PWD"@&DB AS SYSDBA

PROMPT
PROMPT [Tai khoan] mong doi SO_TK = 20 (BVADMIN + HOSPITAL_DBA + 7 NV + 3 BN + u1..u8)
SELECT COUNT(*) AS so_tk FROM dba_users WHERE username IN
 ('BVADMIN','HOSPITAL_DBA','DPV_NV001','DPV_NV002','BS_NV003','BS_NV004','BS_NV005',
  'KTV_NV006','KTV_NV007','BN_BN001','BN_BN002','BN_BN003',
  'U1_GIAMDOC','U2_LDTM_HCM','U3_LDTK_HNI','U4_NVTK_HCM','U5_NVTM_HCM','U6_LDP_TM_HCM','U7_LDP_ALL','U8_NVTH_HNI');

PROMPT
PROMPT [YC1 - VPD] mong doi 4 policy: POL_HSBA_DPV_BS / POL_HSBA_DV_DPV_BS / POL_BENHNHAN_DPV_BS / POL_DONTHUOC_BS
COL object_name FORMAT A12
COL policy_name FORMAT A24
SELECT object_name, policy_name FROM dba_policies
WHERE object_owner='BVADMIN' AND policy_name LIKE 'POL\_%' ESCAPE '\' ORDER BY object_name;

PROMPT
PROMPT [YC2 - OLS] mong doi TONG=7 va CO_NHAN=7 (moi dong THONGBAO co nhan OLS)
SELECT COUNT(*) AS tong, COUNT(OLS_LABEL) AS co_nhan FROM BVADMIN.THONGBAO;

PROMPT
PROMPT [YC3 - Audit] mong doi UNIFIED_POL=6 va FGA_POL=4
SELECT COUNT(*) AS unified_pol FROM audit_unified_enabled_policies WHERE policy_name LIKE 'POL\_%' ESCAPE '\';
SELECT COUNT(*) AS fga_pol FROM dba_audit_policies WHERE object_schema='BVADMIN';

PROMPT
PROMPT [Crypto/TDE] cot ma hoa (chi co neu da chay file 13 - khong bat buoc)
COL table_name FORMAT A12
COL column_name FORMAT A14
SELECT table_name, column_name, salt FROM dba_encrypted_columns ORDER BY table_name, column_name;

PROMPT
PROMPT ---------- KIEM TRA QUYEN DOC THUC TE (dang nhap tung user) ----------

PROMPT [YC2 - OLS] u1_giamdoc mong doi thay 7 thong bao
CONNECT u1_giamdoc/"U1@2025"@&DB
SELECT COUNT(*) AS u1_thay FROM BVADMIN.THONGBAO;

PROMPT [YC2 - OLS] u8_nvth_hni mong doi thay dung TB001 + TB006
CONNECT u8_nvth_hni/"U8@2025"@&DB
SELECT MATB FROM BVADMIN.THONGBAO ORDER BY MATB;

PROMPT [YC1 - VPD] DPV_NV001 (xem TAT CA HSBA)
CONNECT DPV_NV001/"BV@2025!"@&DB
SELECT COUNT(*) AS dpv_thay_hsba FROM BVADMIN.HSBA;

PROMPT [YC1 - VPD] BS_NV003 (CHI thay HSBA cua minh - phai NHO HON DPV)
CONNECT BS_NV003/"BV@2025!"@&DB
SELECT COUNT(*) AS bs_thay_hsba FROM BVADMIN.HSBA;

PROMPT
PROMPT ================== HET KIEM TRA ==================
PROMPT  Dat ky vong: TK=20 | VPD=4 policy | OLS 7/7 | Audit 6 unified + 4 FGA
PROMPT             | u1 thay 7 | u8 thay TB001+TB006 | BS thay < DPV
EXIT
