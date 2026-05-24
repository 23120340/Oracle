-- ============================================================
-- PHÂN HỆ 2 - File 02: Thiết lập tài khoản (TC#1)
-- ============================================================
-- TC#1 yêu cầu:
--   1. DBA tạo Oracle account cho tất cả NHANVIEN + BENHNHAN
--   2. Kết nối tên tài khoản với dòng dữ liệu trong 1 bảng duy nhất
--      (không join nhiều bảng) → lưu ORACLE_USER trong bảng gốc
--   3. Ép thỏa các chính sách bảo mật liên quan người dùng này
-- Giải pháp: lưu tên tài khoản Oracle trực tiếp vào cột ORACLE_USER
--            → SELECT * FROM NHANVIEN WHERE ORACLE_USER = SYS_CONTEXT(...)
--              chỉ cần 1 bảng, không cần join bảng tra cứu riêng
-- ============================================================
-- Chạy với quyền DBA (SYSTEM)
-- ============================================================

CONNECT SYSTEM/oracle;

-- Cấp quyền quản lý user cho BVADMIN (để BVADMIN có thể chạy script)
GRANT CREATE USER    TO BVADMIN;
GRANT DROP USER      TO BVADMIN;
GRANT ALTER USER     TO BVADMIN;
GRANT GRANT ANY ROLE TO BVADMIN;

-- ============================================================
-- Procedure tạo tài khoản cho NHÂN VIÊN
-- Nhận MANV, tự sinh username và password, cập nhật ORACLE_USER
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

CREATE OR REPLACE PROCEDURE BVADMIN.sp_create_nhanvien_account(
    p_manv   IN BVADMIN.NHANVIEN.MANV%TYPE,
    p_passwd IN VARCHAR2 DEFAULT 'BV@2025!'
) AUTHID DEFINER AS
    v_username VARCHAR2(100);
    v_hoten    NVARCHAR2(100);
    v_vaitro   VARCHAR2(20);
    v_sql      VARCHAR2(500);
BEGIN
    -- Lấy thông tin nhân viên
    SELECT HOTEN, VAITRO INTO v_hoten, v_vaitro
    FROM BVADMIN.NHANVIEN WHERE MANV = p_manv;

    -- Tạo username: role_manv, ví dụ: BS_NV003
    v_username := v_vaitro || '_' || p_manv;

    -- Tạo Oracle user
    v_sql := 'CREATE USER ' || v_username ||
             ' IDENTIFIED BY "' || p_passwd || '"' ||
             ' DEFAULT TABLESPACE USERS' ||
             ' QUOTA 10M ON USERS';
    EXECUTE IMMEDIATE v_sql;

    -- Cấp quyền kết nối cơ bản
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_username;

    -- Cập nhật ORACLE_USER trong NHANVIEN (liên kết 1 bảng - TC#1)
    UPDATE BVADMIN.NHANVIEN
    SET    ORACLE_USER = v_username
    WHERE  MANV = p_manv;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Created: ' || v_username || ' for MANV=' || p_manv);
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Error for ' || p_manv || ': ' || SQLERRM);
END;
/

-- ============================================================
-- Procedure tạo tài khoản cho BỆNH NHÂN
-- ============================================================
CREATE OR REPLACE PROCEDURE BVADMIN.sp_create_benhnhan_account(
    p_mabn   IN BVADMIN.BENHNHAN.MABN%TYPE,
    p_passwd IN VARCHAR2 DEFAULT 'BV@2025!'
) AUTHID DEFINER AS
    v_username VARCHAR2(100);
    v_sql      VARCHAR2(500);
BEGIN
    v_username := 'BN_' || p_mabn;

    v_sql := 'CREATE USER ' || v_username ||
             ' IDENTIFIED BY "' || p_passwd || '"' ||
             ' DEFAULT TABLESPACE USERS' ||
             ' QUOTA 0 ON USERS';
    EXECUTE IMMEDIATE v_sql;

    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_username;

    -- Cập nhật ORACLE_USER trong BENHNHAN (liên kết 1 bảng - TC#1)
    UPDATE BVADMIN.BENHNHAN
    SET    ORACLE_USER = v_username
    WHERE  MABN = p_mabn;

    COMMIT;
    DBMS_OUTPUT.PUT_LINE('Created: ' || v_username || ' for MABN=' || p_mabn);
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
        DBMS_OUTPUT.PUT_LINE('Error for ' || p_mabn || ': ' || SQLERRM);
END;
/

-- ============================================================
-- Tạo tài khoản cho các nhân viên mẫu
-- ============================================================
SET SERVEROUTPUT ON;

EXEC BVADMIN.sp_create_nhanvien_account('NV001');  -- DPV_NV001
EXEC BVADMIN.sp_create_nhanvien_account('NV002');  -- DPV_NV002
EXEC BVADMIN.sp_create_nhanvien_account('NV003');  -- BS_NV003
EXEC BVADMIN.sp_create_nhanvien_account('NV004');  -- BS_NV004
EXEC BVADMIN.sp_create_nhanvien_account('NV005');  -- BS_NV005
EXEC BVADMIN.sp_create_nhanvien_account('NV006');  -- KTV_NV006
EXEC BVADMIN.sp_create_nhanvien_account('NV007');  -- KTV_NV007

-- Tạo tài khoản cho bệnh nhân mẫu
EXEC BVADMIN.sp_create_benhnhan_account('BN001');  -- BN_BN001
EXEC BVADMIN.sp_create_benhnhan_account('BN002');  -- BN_BN002
EXEC BVADMIN.sp_create_benhnhan_account('BN003');  -- BN_BN003

-- ============================================================
-- Kiểm chứng TC#1: kết nối 1 tài khoản với 1 dòng dữ liệu
-- chỉ cần 1 bảng, không join bảng tra cứu bổ sung
-- ============================================================

-- Nhân viên xem thông tin chính mình (1 bảng):
-- SELECT * FROM BVADMIN.NHANVIEN
-- WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');

-- Bệnh nhân xem thông tin chính mình (1 bảng):
-- SELECT * FROM BVADMIN.BENHNHAN
-- WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');

-- Kiểm tra tài khoản đã tạo và liên kết
SELECT MANV, HOTEN, VAITRO, ORACLE_USER FROM BVADMIN.NHANVIEN ORDER BY VAITRO, MANV;
SELECT MABN, TENBN, ORACLE_USER FROM BVADMIN.BENHNHAN;

-- ============================================================
-- Helper functions - dùng chung cho RBAC/VPD (chạy bởi BVADMIN)
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

-- Lấy MANV của user đang đăng nhập
CREATE OR REPLACE FUNCTION BVADMIN.fn_get_manv RETURN VARCHAR2 AS
    v_manv BVADMIN.NHANVIEN.MANV%TYPE;
BEGIN
    SELECT MANV INTO v_manv
    FROM   BVADMIN.NHANVIEN
    WHERE  ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
    RETURN v_manv;
EXCEPTION
    WHEN NO_DATA_FOUND THEN RETURN NULL;
END fn_get_manv;
/

-- Lấy VAITRO của user đang đăng nhập
CREATE OR REPLACE FUNCTION BVADMIN.fn_get_vaitro RETURN VARCHAR2 AS
    v_vaitro BVADMIN.NHANVIEN.VAITRO%TYPE;
BEGIN
    SELECT VAITRO INTO v_vaitro
    FROM   BVADMIN.NHANVIEN
    WHERE  ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
    RETURN v_vaitro;
EXCEPTION
    WHEN NO_DATA_FOUND THEN RETURN NULL;
END fn_get_vaitro;
/

-- Lấy MABN của bệnh nhân đang đăng nhập
CREATE OR REPLACE FUNCTION BVADMIN.fn_get_mabn RETURN VARCHAR2 AS
    v_mabn BVADMIN.BENHNHAN.MABN%TYPE;
BEGIN
    SELECT MABN INTO v_mabn
    FROM   BVADMIN.BENHNHAN
    WHERE  ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
    RETURN v_mabn;
EXCEPTION
    WHEN NO_DATA_FOUND THEN RETURN NULL;
END fn_get_mabn;
/

GRANT EXECUTE ON BVADMIN.fn_get_manv   TO PUBLIC;
GRANT EXECUTE ON BVADMIN.fn_get_vaitro TO PUBLIC;
GRANT EXECUTE ON BVADMIN.fn_get_mabn   TO PUBLIC;
