-- ============================================================
-- PHAN HE 2 - File 09A: Hop nhat nhan OLS vao NHANVIEN
-- Chay sau 01-08 va sau khi policy BV_LABEL_POLICY da duoc tao
-- ============================================================
-- Muc tieu:
--   1. Luu cap bac/co so/khoa OLS ngay tren NHANVIEN.
--   2. Tu dong gan nhan OLS cho Oracle user cua nhan vien.
--   3. Cho DPV/BS/KTV xem THONGBAO bang chinh user nhan vien.
-- ============================================================

CONNECT BVADMIN/"BVAdmin@2025";

-- Them cot theo cach idempotent de co the chay lai khi demo.
DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM USER_TAB_COLUMNS
    WHERE TABLE_NAME = 'NHANVIEN' AND COLUMN_NAME = 'CAPBAC';

    IF v_count = 0 THEN
        EXECUTE IMMEDIATE q'[
            ALTER TABLE NHANVIEN ADD (
                CAPBAC    VARCHAR2(10) CHECK (CAPBAC IN ('NV','LDK','BGD')),
                COSO      VARCHAR2(10) CHECK (COSO IN ('HCM','HPN','HNI')),
                KHOA_NHAN VARCHAR2(10) CHECK (KHOA_NHAN IN ('TH','TK','TM','ALL'))
            )
        ]';
    END IF;
END;
/

-- Gan metadata mau cho 7 nhan vien san co.
UPDATE NHANVIEN SET CAPBAC='NV',  COSO='HCM', KHOA_NHAN='ALL' WHERE MANV='NV001';
UPDATE NHANVIEN SET CAPBAC='NV',  COSO='HNI', KHOA_NHAN='ALL' WHERE MANV='NV002';
UPDATE NHANVIEN SET CAPBAC='LDK', COSO='HCM', KHOA_NHAN='TM'  WHERE MANV='NV003';
UPDATE NHANVIEN SET CAPBAC='LDK', COSO='HNI', KHOA_NHAN='TK'  WHERE MANV='NV004';
UPDATE NHANVIEN SET CAPBAC='LDK', COSO='HNI', KHOA_NHAN='TH'  WHERE MANV='NV005';
UPDATE NHANVIEN SET CAPBAC='NV',  COSO='HCM', KHOA_NHAN='TM'  WHERE MANV='NV006';
UPDATE NHANVIEN SET CAPBAC='NV',  COSO='HNI', KHOA_NHAN='TH'  WHERE MANV='NV007';
COMMIT;

CREATE OR REPLACE VIEW NV_NHANVIEN_View AS
SELECT
    MANV, HOTEN, PHAI, NGAYSINH, CMND,
    QUEQUAN, SODT, VAITRO, CHUYENKHOA,
    CAPBAC, COSO, KHOA_NHAN
FROM NHANVIEN
WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');

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
    OR NVL(:NEW.CAPBAC,    '#') != NVL(:OLD.CAPBAC,    '#')
    OR NVL(:NEW.COSO,      '#') != NVL(:OLD.COSO,      '#')
    OR NVL(:NEW.KHOA_NHAN, '#') != NVL(:OLD.KHOA_NHAN, '#')
    THEN
        RAISE_APPLICATION_ERROR(-20003,
            'Khong duoc phep thay doi thong tin dinh danh, vai tro hoac nhan OLS cua nhan vien.');
    END IF;

    UPDATE NHANVIEN
    SET    QUEQUAN = :NEW.QUEQUAN,
           SODT    = :NEW.SODT
    WHERE  ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
END trg_nv_update_self;
/

-- Procedure nay phai duoc chay boi tai khoan co quyen goi SA_USER_ADMIN.
CONNECT SYSTEM/oracle;
GRANT SELECT ON BVADMIN.NHANVIEN TO LBACSYS;

CONNECT LBACSYS/lbacsys;

CREATE OR REPLACE PROCEDURE sp_apply_ols_label_for_nv(
    p_manv IN VARCHAR2
) AS
    v_oracle_user VARCHAR2(100);
    v_capbac      VARCHAR2(10);
    v_coso        VARCHAR2(10);
    v_khoa        VARCHAR2(10);
    v_label       VARCHAR2(100);
BEGIN
    SELECT ORACLE_USER, CAPBAC, COSO, KHOA_NHAN
    INTO   v_oracle_user, v_capbac, v_coso, v_khoa
    FROM   BVADMIN.NHANVIEN
    WHERE  MANV = p_manv;

    IF v_oracle_user IS NULL OR v_capbac IS NULL THEN
        RETURN;
    END IF;

    v_label := v_capbac;
    IF v_coso IS NOT NULL THEN
        v_label := v_label || ':' || v_coso;
    END IF;
    IF v_khoa IS NOT NULL AND v_khoa <> 'ALL' THEN
        v_label := v_label || ':' || v_khoa;
    END IF;

    SA_USER_ADMIN.SET_USER_LABELS(
        policy_name    => 'BV_LABEL_POLICY',
        user_name      => UPPER(v_oracle_user),
        max_read_label => v_label
    );
END;
/

BEGIN
    FOR r IN (
        SELECT MANV
        FROM BVADMIN.NHANVIEN
        WHERE ORACLE_USER IS NOT NULL
          AND CAPBAC IS NOT NULL
    ) LOOP
        sp_apply_ols_label_for_nv(r.MANV);
    END LOOP;
END;
/

CONNECT SYSTEM/oracle;
GRANT SELECT ON BVADMIN.THONGBAO TO DPV_Role;
GRANT SELECT ON BVADMIN.THONGBAO TO BS_Role;
GRANT SELECT ON BVADMIN.THONGBAO TO KTV_Role;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO DPV_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO DPV_Role;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO BS_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO BS_Role;
GRANT SELECT ON BVADMIN.NV_NHANVIEN_View TO KTV_Role;
GRANT UPDATE ON BVADMIN.NV_NHANVIEN_View TO KTV_Role;

CONNECT BVADMIN/"BVAdmin@2025";
SELECT MANV, ORACLE_USER, CAPBAC, COSO, KHOA_NHAN
FROM NHANVIEN
ORDER BY MANV;
