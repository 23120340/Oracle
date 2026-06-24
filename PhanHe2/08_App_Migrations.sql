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
-- ============================================================
-- FIX (H2): View NV_NHANVIEN_View + trigger trg_nv_update_self + các GRANT cho
-- DPV_Role/BS_Role/KTV_Role được định nghĩa DUY NHẤT ở 09_OLS_NhanVien_Unified.sql
-- (bản 12 cột, gồm CAPBAC/COSO/KHOA_NHAN). Bỏ định nghĩa trùng tại đây để tránh
-- phụ thuộc thứ tự chạy (08↔09 ghi đè lẫn nhau). Xem file 09.

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
-- FIX: proc chạy definer-rights dưới BVADMIN cần cấp CREATE SESSION (TRỰC TIẾP) cho BN mới.
-- BVADMIN không thể cấp CREATE SESSION nếu không có ADMIN OPTION → cấp ở đây.
-- (GRANT ANY ROLE đã có từ file 02 → cấp BenhNhan_Role được.)
GRANT CREATE SESSION TO BVADMIN WITH ADMIN OPTION;

CONNECT BVADMIN/"BVAdmin@2025";

-- p_mabn IN OUT: truyền NULL → proc TỰ SINH mã (fn_next_mabn) rồi trả về mã đã tạo.
-- Thứ tự AN TOÀN: tạo tài khoản TRƯỚC (DDL tự commit) rồi mới INSERT bệnh nhân; nếu bất kỳ
-- bước nào lỗi → DROP USER dọn tài khoản dở (đảm bảo all-or-nothing, không để tài khoản mồ côi).
CREATE OR REPLACE PROCEDURE sp_create_benhnhan_full(
    p_mabn       IN OUT VARCHAR2,
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
    v_username VARCHAR2(128);
    v_cnt      PLS_INTEGER;
BEGIN
    -- 1. Tự sinh MABN nếu không truyền vào
    IF p_mabn IS NULL OR TRIM(p_mabn) IS NULL THEN
        p_mabn := fn_next_mabn();
    END IF;
    v_username := 'BN_' || p_mabn;

    -- 2. Kiểm tra trùng (fail-fast, CHƯA tạo gì)
    SELECT COUNT(*) INTO v_cnt FROM BENHNHAN WHERE MABN = p_mabn;
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20001, 'Mã bệnh nhân ' || p_mabn || ' đã tồn tại.');
    END IF;
    SELECT COUNT(*) INTO v_cnt FROM BENHNHAN WHERE CCCD = p_cccd;
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20002, 'CCCD ' || p_cccd || ' đã được đăng ký cho bệnh nhân khác.');
    END IF;
    SELECT COUNT(*) INTO v_cnt FROM ALL_USERS WHERE USERNAME = UPPER(v_username);
    IF v_cnt > 0 THEN
        RAISE_APPLICATION_ERROR(-20003, 'Tài khoản ' || v_username || ' đã tồn tại.');
    END IF;

    -- 3. Tạo tài khoản TRƯỚC (DDL tự commit → làm trước thì chưa có dữ liệu BN để mất)
    EXECUTE IMMEDIATE 'CREATE USER ' || v_username ||
                      ' IDENTIFIED BY "' || p_password || '"' ||
                      ' DEFAULT TABLESPACE USERS QUOTA 0 ON USERS';
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_username;
    EXECUTE IMMEDIATE 'GRANT BenhNhan_Role TO ' || v_username;

    -- 4. Lưu bệnh nhân + liên kết tài khoản (ORACLE_USER) trong cùng giao dịch
    INSERT INTO BENHNHAN(MABN, TENBN, PHAI, NGAYSINH, CCCD,
                         SONHA, TENDUONG, QUANHUYEN, TINHTP, ORACLE_USER)
    VALUES (p_mabn, p_tenbn, p_phai, p_ngaysinh, p_cccd,
            p_sonha, p_tenduong, p_quanhuyen, p_tinhtp, v_username);
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        -- Dọn tài khoản tạo dở (nếu đã CREATE USER) → tránh tài khoản mồ côi
        BEGIN EXECUTE IMMEDIATE 'DROP USER ' || v_username; EXCEPTION WHEN OTHERS THEN NULL; END;
        RAISE;
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
