-- ============================================================
-- PHÂN HỆ 2 - File 08: Migrations cho lớp Application
-- Bổ sung sau khi đã chạy 01-07
-- ============================================================
-- Nội dung:
--   A. SEQ_HSBA / SEQ_BENHNHAN  - sinh mã an toàn (không collision)
--   B. NV_NHANVIEN_View         - self-service cho nhân viên (TC#5)
--   C. APP_LOGIN_LOG            - log đăng nhập tại app
--   D. sp_create_benhnhan_full  - wrapper tạo BN + Oracle account
--   E. Cấp quyền EXECUTE        - cho các role để app dùng được
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

-- ============================================================
-- A. SEQUENCES sinh mã
-- ============================================================
CREATE SEQUENCE SEQ_HSBA      START WITH 1000 INCREMENT BY 1 NOCACHE NOCYCLE;
CREATE SEQUENCE SEQ_BENHNHAN  START WITH 1000 INCREMENT BY 1 NOCACHE NOCYCLE;

-- Hàm sinh MAHSBA tiếp theo: 'HS' || padded number, đảm bảo duy nhất
CREATE OR REPLACE FUNCTION fn_next_mahsba RETURN VARCHAR2 AS
BEGIN
    RETURN 'HS' || LPAD(SEQ_HSBA.NEXTVAL, 6, '0');
END fn_next_mahsba;
/

CREATE OR REPLACE FUNCTION fn_next_mabn RETURN VARCHAR2 AS
BEGIN
    RETURN 'BN' || LPAD(SEQ_BENHNHAN.NEXTVAL, 6, '0');
END fn_next_mabn;
/

GRANT EXECUTE ON fn_next_mahsba TO DPV_Role;
GRANT EXECUTE ON fn_next_mabn   TO DPV_Role;

-- ============================================================
-- B. NV_NHANVIEN_View - Self-service cho nhân viên (TC#5)
-- Trên bảng NHANVIEN: chỉ thấy dòng của mình, chặn UPDATE các cột định danh
-- ============================================================
CREATE OR REPLACE VIEW NV_NHANVIEN_View AS
SELECT
    MANV, HOTEN, PHAI, NGAYSINH, CMND,
    QUEQUAN, SODT, VAITRO, CHUYENKHOA
FROM NHANVIEN
WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');

-- INSTEAD OF trigger: chặn UPDATE các trường định danh & cố định
CREATE OR REPLACE TRIGGER trg_nv_update_self
INSTEAD OF UPDATE ON NV_NHANVIEN_View
FOR EACH ROW
BEGIN
    IF :NEW.MANV       != :OLD.MANV
    OR :NEW.HOTEN      != :OLD.HOTEN
    OR :NEW.PHAI       != :OLD.PHAI
    OR :NEW.NGAYSINH   != :OLD.NGAYSINH
    OR :NEW.CMND       != :OLD.CMND
    OR :NEW.VAITRO     != :OLD.VAITRO
    OR :NEW.CHUYENKHOA != :OLD.CHUYENKHOA
    THEN
        RAISE_APPLICATION_ERROR(-20003,
            N'Không được phép thay đổi MÃ NV, HỌ TÊN, PHÁI, NGÀY SINH, CMND, VAI TRÒ, CHUYÊN KHOA.');
    END IF;

    UPDATE NHANVIEN
    SET    QUEQUAN = :NEW.QUEQUAN,
           SODT    = :NEW.SODT
    WHERE  ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
END trg_nv_update_self;
/

-- Cấp quyền cho 3 role nhân sự
CONNECT SYSTEM/oracle;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO DPV_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO DPV_Role;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO BS_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO BS_Role;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO KTV_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO KTV_Role;

-- ============================================================
-- C. APP_LOGIN_LOG - log đăng nhập tại app layer (bổ sung cho DB audit)
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

CREATE TABLE APP_LOGIN_LOG (
    LOG_ID      NUMBER         GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ATTEMPT_AT  TIMESTAMP      DEFAULT SYSTIMESTAMP,
    USERNAME    VARCHAR2(100),
    SUCCESS     CHAR(1)        CHECK (SUCCESS IN ('Y','N')),
    OS_USER     VARCHAR2(100),
    HOST_NAME   VARCHAR2(200),
    FAIL_REASON VARCHAR2(500)
);

-- Procedure ghi log đăng nhập (gọi từ app sau khi connect thành công/thất bại)
CREATE OR REPLACE PROCEDURE sp_log_login(
    p_username    IN VARCHAR2,
    p_success     IN CHAR,
    p_os_user     IN VARCHAR2 DEFAULT NULL,
    p_host_name   IN VARCHAR2 DEFAULT NULL,
    p_fail_reason IN VARCHAR2 DEFAULT NULL
) AS
    PRAGMA AUTONOMOUS_TRANSACTION;
BEGIN
    INSERT INTO APP_LOGIN_LOG(USERNAME, SUCCESS, OS_USER, HOST_NAME, FAIL_REASON)
    VALUES (UPPER(p_username), p_success, p_os_user, p_host_name, p_fail_reason);
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        ROLLBACK;
END sp_log_login;
/

GRANT EXECUTE ON sp_log_login TO PUBLIC;

-- ============================================================
-- D. sp_create_benhnhan_full - wrapper: INSERT BN + tạo Oracle account
-- DPV gọi procedure này thay vì tự INSERT (TC#1: BN phải có Oracle account)
-- ============================================================
CONNECT SYSTEM/oracle;
GRANT CREATE USER TO BVADMIN;

CONNECT BVADMIN/"BVAdmin@2025";

CREATE OR REPLACE PROCEDURE sp_create_benhnhan_full(
    p_mabn       IN VARCHAR2,
    p_tenbn      IN NVARCHAR2,
    p_phai       IN CHAR,
    p_ngaysinh   IN DATE,
    p_cccd       IN VARCHAR2,
    p_sonha      IN NVARCHAR2 DEFAULT NULL,
    p_tenduong   IN NVARCHAR2 DEFAULT NULL,
    p_quanhuyen  IN NVARCHAR2 DEFAULT NULL,
    p_tinhtp     IN NVARCHAR2 DEFAULT NULL,
    p_password   IN VARCHAR2 DEFAULT 'BV@2025!'
) AS
    v_username VARCHAR2(100);
BEGIN
    -- 1. Insert vào BENHNHAN
    INSERT INTO BENHNHAN(MABN, TENBN, PHAI, NGAYSINH, CCCD,
                         SONHA, TENDUONG, QUANHUYEN, TINHTP)
    VALUES (p_mabn, p_tenbn, p_phai, p_ngaysinh, p_cccd,
            p_sonha, p_tenduong, p_quanhuyen, p_tinhtp);

    -- 2. Tạo Oracle account
    v_username := 'BN_' || p_mabn;
    EXECUTE IMMEDIATE 'CREATE USER ' || v_username ||
                      ' IDENTIFIED BY "' || p_password || '"' ||
                      ' DEFAULT TABLESPACE USERS QUOTA 0 ON USERS';
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_username;
    EXECUTE IMMEDIATE 'GRANT BenhNhan_Role TO ' || v_username;

    -- 3. Cập nhật ORACLE_USER liên kết (TC#1)
    UPDATE BENHNHAN
    SET    ORACLE_USER = v_username
    WHERE  MABN = p_mabn;

    COMMIT;
END sp_create_benhnhan_full;
/

GRANT EXECUTE ON sp_create_benhnhan_full TO DPV_Role;

-- ============================================================
-- E. Cấp EXECUTE cho các function context cho DPV (đã grant PUBLIC trong 02)
-- ============================================================

-- Kiểm tra: liệt kê tất cả objects mới
CONNECT BVADMIN/"BVAdmin@2025";
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS
FROM   USER_OBJECTS
WHERE  OBJECT_NAME IN (
    'SEQ_HSBA','SEQ_BENHNHAN',
    'FN_NEXT_MAHSBA','FN_NEXT_MABN',
    'NV_NHANVIEN_VIEW','TRG_NV_UPDATE_SELF',
    'APP_LOGIN_LOG','SP_LOG_LOGIN',
    'SP_CREATE_BENHNHAN_FULL'
);
