-- ============================================================
-- HOTFIX: DPV thêm bệnh nhân mới NHƯNG tài khoản đăng nhập không được tạo
-- ============================================================
-- Nguyên nhân: proc sp_create_benhnhan_full (chạy dưới BVADMIN) cố GRANT CREATE SESSION
-- cho tài khoản BN mới, nhưng BVADMIN không có quyền cấp CREATE SESSION → ORA-01031,
-- proc dừng giữa chừng (bệnh nhân đã lưu do DDL tự commit, tài khoản dở dang).
--
-- Fix: (1) cấp CREATE SESSION ... WITH ADMIN OPTION cho BVADMIN;
--      (2) viết lại proc: tự sinh MABN, tạo tài khoản TRƯỚC, dọn rác khi lỗi.
--
-- CHẠY (CONNECT thật, KHÔNG qua setup.ps1). Trong PowerShell:
--   $env:NLS_LANG = ".AL32UTF8"
--   sqlplus /nolog "@d:\repos\Oracle\PhanHe2\extras\fix_benhnhan_account.sql"
-- >>> SỬA mật khẩu SYS cho khớp DB của bạn ở dòng CONNECT bên dưới.
-- ============================================================
SET DEFINE OFF

-- 1) Cấp quyền cho BVADMIN (dùng SYS vì SYSTEM có thể đang khoá)
CONNECT sys/"Phamminhquan611*"@localhost:1521/XEPDB1 AS SYSDBA
GRANT CREATE USER TO BVADMIN;
GRANT CREATE SESSION TO BVADMIN WITH ADMIN OPTION;

-- 2) Tạo lại proc dưới phiên BVADMIN
CONNECT BVADMIN/"BVAdmin@2025"@localhost:1521/XEPDB1

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

    -- 2. Kiểm tra trùng (fail-fast)
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

    -- 3. Tạo tài khoản TRƯỚC (DDL tự commit)
    EXECUTE IMMEDIATE 'CREATE USER ' || v_username ||
                      ' IDENTIFIED BY "' || p_password || '"' ||
                      ' DEFAULT TABLESPACE USERS QUOTA 0 ON USERS';
    EXECUTE IMMEDIATE 'GRANT CREATE SESSION TO ' || v_username;
    EXECUTE IMMEDIATE 'GRANT BenhNhan_Role TO ' || v_username;

    -- 4. Lưu bệnh nhân + liên kết tài khoản
    INSERT INTO BENHNHAN(MABN, TENBN, PHAI, NGAYSINH, CCCD,
                         SONHA, TENDUONG, QUANHUYEN, TINHTP, ORACLE_USER)
    VALUES (p_mabn, p_tenbn, p_phai, p_ngaysinh, p_cccd,
            p_sonha, p_tenduong, p_quanhuyen, p_tinhtp, v_username);
    COMMIT;
EXCEPTION
    WHEN OTHERS THEN
        BEGIN EXECUTE IMMEDIATE 'DROP USER ' || v_username; EXCEPTION WHEN OTHERS THEN NULL; END;
        RAISE;
END sp_create_benhnhan_full;
/

GRANT EXECUTE ON sp_create_benhnhan_full TO DPV_Role;

-- 3) MÃBN BẤT BIẾN: chặn mọi UPDATE đổi MABN (khoá chính + tài khoản BN_<MABN> + FK HSBA)
CREATE OR REPLACE TRIGGER trg_benhnhan_mabn_immutable
BEFORE UPDATE OF MABN ON BENHNHAN
FOR EACH ROW
WHEN (NEW.MABN != OLD.MABN)
BEGIN
    RAISE_APPLICATION_ERROR(-20010,
        'MÃBN là định danh bất biến, không được phép sửa (cũ=' || :OLD.MABN ||
        ', mới=' || :NEW.MABN || ').');
END;
/

-- 4) Kiểm tra proc + trigger đã VALID
SELECT OBJECT_NAME, OBJECT_TYPE, STATUS FROM USER_OBJECTS
WHERE  OBJECT_NAME IN ('SP_CREATE_BENHNHAN_FULL', 'TRG_BENHNHAN_MABN_IMMUTABLE');

PROMPT >>> Done. Vao app: DPV -> Them BN -> Luu -> se hien MABN + tai khoan BN_<MABN>.
