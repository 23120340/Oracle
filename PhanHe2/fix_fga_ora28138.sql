-- ============================================================
-- HOTFIX ORA-28138 — FGA audit_condition KHÔNG được chứa AND/OR/IN
-- ============================================================
-- ORA-28138 = "error in evaluating the fine grained audit policy predicate".
-- 2 policy FGA dùng predicate ghép (OR / NOT IN) nên DML bị từ chối lúc chạy:
--   * BS INSERT INTO HSBA_DV  (thêm dịch vụ chẩn đoán)
--   * KTV UPDATE KETQUA qua view (INSTEAD OF trigger -> UPDATE HSBA_DV)
-- Cách sửa: bọc logic nhiều toán tử vào hàm trả 'Y'/'N', audit_condition chỉ so sánh đơn.
--
-- CHẠY (giữ nguyên dữ liệu, KHÔNG cần -Reset), với tư cách BVADMIN:
--   set NLS_LANG=.AL32UTF8
--   sqlplus BVADMIN/"BVAdmin@2025"@localhost:1521/XEPDB1 @PhanHe2/fix_fga_ora28138.sql
-- (Hoặc chạy lại toàn bộ file 06_YC3_Audit.sql — đã idempotent.)
-- ============================================================
SET DEFINE OFF

-- 1) Hàm bọc logic -> predicate đơn cho FGA
CREATE OR REPLACE FUNCTION fn_is_illegal_hsba RETURN VARCHAR2 AS
    v_vaitro NHANVIEN.VAITRO%TYPE := fn_get_vaitro();
BEGIN
    IF v_vaitro IS NULL OR v_vaitro != 'BS' THEN
        RETURN 'Y';
    ELSE
        RETURN 'N';
    END IF;
END fn_is_illegal_hsba;
/

CREATE OR REPLACE FUNCTION fn_is_illegal_hsba_dv RETURN VARCHAR2 AS
    v_vaitro NHANVIEN.VAITRO%TYPE := fn_get_vaitro();
BEGIN
    IF v_vaitro IS NULL OR (v_vaitro != 'BS' AND v_vaitro != 'DPV') THEN
        RETURN 'Y';
    ELSE
        RETURN 'N';
    END IF;
END fn_is_illegal_hsba_dv;
/

-- 2) Gỡ 2 policy lỗi (nếu có) rồi tạo lại với audit_condition đơn
DECLARE
    PROCEDURE drop_fga(p_obj VARCHAR2, p_pol VARCHAR2) IS
    BEGIN
        DBMS_FGA.DROP_POLICY('BVADMIN', p_obj, p_pol);
    EXCEPTION WHEN OTHERS THEN NULL;   -- policy chưa tồn tại
    END;
BEGIN
    drop_fga('HSBA',    'FGA_HSBA_ILLEGAL_UPDATE');
    drop_fga('HSBA_DV', 'FGA_HSBA_DV_ILLEGAL');
END;
/

BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA',
        policy_name     => 'FGA_HSBA_ILLEGAL_UPDATE',
        audit_condition => 'BVADMIN.fn_is_illegal_hsba() = ''Y''',
        audit_column    => 'CHANDOAN,DIEUTRI,KETLUAN',
        enable          => TRUE,
        statement_types => 'UPDATE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED,
        audit_column_opts => DBMS_FGA.ANY_COLUMNS
    );

    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA_DV',
        policy_name     => 'FGA_HSBA_DV_ILLEGAL',
        audit_condition => 'BVADMIN.fn_is_illegal_hsba_dv() = ''Y''',
        audit_column    => NULL,   -- audit mọi cột
        enable          => TRUE,
        statement_types => 'INSERT,UPDATE,DELETE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
END;
/

PROMPT >>> Hotfix ORA-28138 hoan tat. BS them DV / KTV luu ket qua se hoat dong.
