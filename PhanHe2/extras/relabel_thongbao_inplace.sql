-- ============================================================
-- CÁCH 1 — Vá tại chỗ: gán lại nhãn OLS cho 7 dòng THONGBAO đã tồn tại
-- ============================================================
-- Dùng khi DB đã dựng (01→13) nhưng u1–u8 không thấy thông báo vì
-- cột OLS_LABEL = NULL (file 05 cũ thiếu LABEL_DEFAULT).
--
-- KHÁC fix_ols_thongbao.sql: file này KHÔNG xóa/chèn lại — chỉ UPDATE nhãn
-- nên giữ nguyên nội dung tiếng Việt hiện có. BVADMIN có quyền FULL nên ghi
-- thẳng nhãn vào cột được.
--
-- CÁCH CHẠY (BẮT BUỘC CONNECT thật, KHÔNG qua scripts/setup.ps1):
--   PowerShell:  $env:NLS_LANG = ".AL32UTF8"
--                sqlplus /nolog "@PhanHe2/extras/relabel_thongbao_inplace.sql"
--   >>> Sửa mật khẩu / service (XEPDB1) bên dưới cho khớp DB của bạn nếu khác.
-- ============================================================
SET DEFINE OFF
WHENEVER SQLERROR EXIT SQL.SQLCODE

CONNECT BVADMIN/"BVAdmin@2025"@//localhost:1521/XEPDB1

UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','NV')             WHERE MATB='TB001';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','BGD')            WHERE MATB='TB002';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','LDK')            WHERE MATB='TB003';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','LDK::TH')        WHERE MATB='TB004';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HCM:TH')      WHERE MATB='TB005';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HNI:TH')      WHERE MATB='TB006';
UPDATE THONGBAO SET OLS_LABEL=CHAR_TO_LABEL('BV_LABEL_POLICY','LDK:HPN:TH,TK')  WHERE MATB='TB007';
COMMIT;

PROMPT
PROMPT ===== BVADMIN (FULL): phai thay du 7 dong, OLS_LABEL khac NULL =====
COLUMN OLS_LABEL FORMAT A14
SELECT MATB, OLS_LABEL, SUBSTR(NOIDUNG,1,40) AS NOIDUNG FROM THONGBAO ORDER BY MATB;

PROMPT
PROMPT ===== u1_giamdoc (BGD): phai thay du 7 =====
CONNECT u1_giamdoc/"U1@2025"@//localhost:1521/XEPDB1
SELECT MATB FROM BVADMIN.THONGBAO ORDER BY MATB;

PROMPT
PROMPT ===== u8_nvth_hni (NV,HNI,TH): phai thay TB001 + TB006 =====
CONNECT u8_nvth_hni/"U8@2025"@//localhost:1521/XEPDB1
SELECT MATB FROM BVADMIN.THONGBAO ORDER BY MATB;

PROMPT
PROMPT ===== u4_nvtk_hcm (NV,HCM,TK): chi thay TB001 =====
CONNECT u4_nvtk_hcm/"U4@2025"@//localhost:1521/XEPDB1
SELECT MATB FROM BVADMIN.THONGBAO ORDER BY MATB;

PROMPT
PROMPT >>> Xong.
EXIT
