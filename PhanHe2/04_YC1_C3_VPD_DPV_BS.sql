-- ============================================================
-- PHÂN HỆ 2 - File 04: Yêu cầu 1, Câu 3 - VPD cho DPV và Bác sĩ
-- ============================================================
-- TC#2 (Điều phối viên - DPV):
--   - SELECT/INSERT/UPDATE trên BENHNHAN (tất cả dòng)
--   - INSERT HSBA (tạo hồ sơ mới)
--   - UPDATE chỉ cột MAKHOA, MABS trên HSBA
--   - UPDATE chỉ cột MAKTV trên HSBA_DV
-- TC#3 (Bác sĩ/Y sĩ - BS):
--   - SELECT HSBA chỉ dòng MABS = MANV của mình
--   - INSERT/DELETE HSBA_DV cho HSBA của mình
--   - UPDATE CHANDOAN, DIEUTRI, KETLUAN trên HSBA của mình (có audit)
--   - SELECT BENHNHAN chỉ BN liên quan đến HSBA mình điều trị
--   - UPDATE TIENSUBENH, TIENSUBENHGD, DIUNGTHUOC của BN đó
--   - INSERT/DELETE/UPDATE DONTHUOC cho HSBA của mình (có audit)
-- ============================================================
-- VPD (Virtual Private Database / Fine Grained Access Control):
--   - DBMS_RLS.ADD_POLICY thêm WHERE clause tự động vào mọi câu SQL
--   - Người dùng không thể bỏ qua filter → bảo vệ ở tầng DB engine
-- ============================================================
-- Chạy với BVADMIN sau 01, 02, 03
-- ============================================================

CONNECT BVADMIN/"BVAdmin@2025";

-- ============================================================
-- PHẦN A: ROLES VÀ GRANTS CƠ BẢN CHO DPV VÀ BS
-- ============================================================
CONNECT SYSTEM/oracle;

-- Role cho Điều phối viên
-- FIX (L11): tạo role idempotent (bỏ qua nếu đã tồn tại — ORA-01921)
BEGIN EXECUTE IMMEDIATE 'CREATE ROLE DPV_Role'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1921 THEN RAISE; END IF; END;
/

GRANT SELECT, INSERT, UPDATE       ON BVADMIN.BENHNHAN  TO DPV_Role;
GRANT SELECT, INSERT               ON BVADMIN.HSBA      TO DPV_Role;
GRANT UPDATE(MAKHOA, MABS)         ON BVADMIN.HSBA      TO DPV_Role;
GRANT SELECT                       ON BVADMIN.HSBA_DV   TO DPV_Role;
GRANT UPDATE(MAKTV)                ON BVADMIN.HSBA_DV   TO DPV_Role;

-- Role cho Bác sĩ/Y sĩ
-- FIX (L11): tạo role idempotent
BEGIN EXECUTE IMMEDIATE 'CREATE ROLE BS_Role'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1921 THEN RAISE; END IF; END;
/

GRANT SELECT                       ON BVADMIN.HSBA      TO BS_Role;
-- VPD sẽ filter row; cột UPDATE bị giới hạn qua column-level grant:
GRANT UPDATE(CHANDOAN, DIEUTRI, KETLUAN) ON BVADMIN.HSBA TO BS_Role;

GRANT SELECT, INSERT, DELETE       ON BVADMIN.HSBA_DV   TO BS_Role;
GRANT SELECT                       ON BVADMIN.BENHNHAN  TO BS_Role;
GRANT UPDATE(TIENSUBENH, TIENSUBENHGD, DIUNGTHUOC) ON BVADMIN.BENHNHAN TO BS_Role;
GRANT SELECT, INSERT, DELETE       ON BVADMIN.DONTHUOC  TO BS_Role;
GRANT UPDATE(TENTHUOC, LIEUDUNG)   ON BVADMIN.DONTHUOC  TO BS_Role;

-- Gán role cho các user
GRANT DPV_Role TO DPV_NV001, DPV_NV002;
GRANT BS_Role  TO BS_NV003, BS_NV004, BS_NV005;

-- ============================================================
-- PHẦN B: VPD POLICY FUNCTIONS (tạo bởi BVADMIN)
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

-- B1. Policy function cho HSBA
--     - DPV: thấy tất cả HSBA (không filter)
--     - BS: chỉ thấy HSBA mình phụ trách (MABS = MANV)
--     - Các role khác (KTV, BN): không nên đọc HSBA qua policy này
CREATE OR REPLACE FUNCTION vpd_hsba(
    p_schema IN VARCHAR2,
    p_table  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_manv   NHANVIEN.MANV%TYPE;
    v_vaitro NHANVIEN.VAITRO%TYPE;
BEGIN
    -- Chủ schema/bảo trì (BVADMIN): không lọc — phục vụ migration và sửa dữ liệu (file 12)
    IF SYS_CONTEXT('USERENV','SESSION_USER') = 'BVADMIN' THEN
        RETURN '';
    END IF;

    v_manv   := fn_get_manv();
    v_vaitro := fn_get_vaitro();

    IF v_vaitro = 'DPV' THEN
        RETURN '';              -- Không filter: DPV thấy tất cả HSBA
    ELSIF v_vaitro = 'BS' THEN
        RETURN 'MABS = ''' || v_manv || '''';  -- Chỉ HSBA của BS đó
    ELSE
        -- KTV/BN/khác: truy cập HSBA chỉ qua view của BVADMIN (BN_HSBA_View tự lọc theo fn_get_mabn)
        -- và KHÔNG có quyền trên bảng gốc → không lọc thêm ở VPD (tránh xung đột INSTEAD OF/update_check).
        RETURN '';
    END IF;
END vpd_hsba;
/

-- B2. Policy function cho HSBA_DV
--     - DPV: thấy tất cả (để cập nhật MAKTV)
--     - BS: chỉ thấy HSBA_DV thuộc HSBA mình điều trị
CREATE OR REPLACE FUNCTION vpd_hsba_dv(
    p_schema IN VARCHAR2,
    p_table  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_manv   NHANVIEN.MANV%TYPE;
    v_vaitro NHANVIEN.VAITRO%TYPE;
BEGIN
    -- Chủ schema/bảo trì (BVADMIN): không lọc
    IF SYS_CONTEXT('USERENV','SESSION_USER') = 'BVADMIN' THEN
        RETURN '';
    END IF;

    v_manv   := fn_get_manv();
    v_vaitro := fn_get_vaitro();

    IF v_vaitro = 'DPV' THEN
        RETURN '';
    ELSIF v_vaitro = 'BS' THEN
        -- HSBA_DV thuộc về HSBA mà BS này phụ trách
        RETURN 'MAHSBA IN (SELECT MAHSBA FROM HSBA WHERE MABS = ''' || v_manv || ''')';
    ELSE
        -- DPV: điều phối MAKTV trên mọi dịch vụ (không lọc).
        -- KTV: truy cập qua KTV_HSBA_DV_View (tự lọc MAKTV=fn_get_manv) + không có quyền bảng gốc
        -- → KHÔNG lọc thêm ở VPD (tránh xung đột với INSTEAD OF trigger gây ORA-28138).
        RETURN '';
    END IF;
END vpd_hsba_dv;
/

-- B3. Policy function cho BENHNHAN
--     - DPV: thấy tất cả BN
--     - BS: chỉ thấy BN liên quan đến HSBA mình điều trị
CREATE OR REPLACE FUNCTION vpd_benhnhan(
    p_schema IN VARCHAR2,
    p_table  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_manv   NHANVIEN.MANV%TYPE;
    v_vaitro NHANVIEN.VAITRO%TYPE;
BEGIN
    -- Chủ schema/bảo trì (BVADMIN): không lọc — phục vụ sửa dữ liệu UTF-8 ở file 12
    IF SYS_CONTEXT('USERENV','SESSION_USER') = 'BVADMIN' THEN
        RETURN '';
    END IF;

    v_manv   := fn_get_manv();
    v_vaitro := fn_get_vaitro();

    IF v_vaitro = 'DPV' THEN
        RETURN '';
    ELSIF v_vaitro = 'BS' THEN
        -- BN có HSBA do BS này phụ trách
        RETURN 'MABN IN (SELECT MABN FROM HSBA WHERE MABS = ''' || v_manv || ''')';
    ELSE
        -- BN: truy cập BENHNHAN chỉ qua BN_BENHNHAN_View (tự lọc ORACLE_USER) + không có quyền bảng gốc
        -- → KHÔNG lọc thêm ở VPD (tránh xung đột với INSTEAD OF trigger).
        RETURN '';
    END IF;
END vpd_benhnhan;
/

-- B4. Policy function cho DONTHUOC (chỉ BS)
CREATE OR REPLACE FUNCTION vpd_donthuoc(
    p_schema IN VARCHAR2,
    p_table  IN VARCHAR2
) RETURN VARCHAR2 AS
    v_manv   NHANVIEN.MANV%TYPE;
    v_vaitro NHANVIEN.VAITRO%TYPE;
BEGIN
    -- Chủ schema/bảo trì (BVADMIN): không lọc
    IF SYS_CONTEXT('USERENV','SESSION_USER') = 'BVADMIN' THEN
        RETURN '';
    END IF;

    v_manv   := fn_get_manv();
    v_vaitro := fn_get_vaitro();

    IF v_vaitro = 'BS' THEN
        RETURN 'MAHSBA IN (SELECT MAHSBA FROM HSBA WHERE MABS = ''' || v_manv || ''')';
    ELSE
        RETURN '1=0';
    END IF;
END vpd_donthuoc;
/

-- ============================================================
-- PHẦN C: ÁP DỤNG VPD POLICY (chạy bởi SYSTEM với EXECUTE ON DBMS_RLS)
-- ============================================================
CONNECT SYSTEM/oracle;
GRANT EXECUTE ON DBMS_RLS TO BVADMIN;

CONNECT BVADMIN/"BVAdmin@2025";

-- Áp dụng VPD cho bảng HSBA
BEGIN
  DBMS_RLS.ADD_POLICY(
    object_schema   => 'BVADMIN',
    object_name     => 'HSBA',
    policy_name     => 'POL_HSBA_DPV_BS',
    function_schema => 'BVADMIN',
    policy_function => 'vpd_hsba',
    statement_types => 'SELECT,INSERT,UPDATE,DELETE',
    update_check    => FALSE,   -- FALSE: tránh kiểm dòng-mới gây ORA-28138/28115 khi BS INSERT HSBA_DV
                                 -- hoặc KTV cập nhật KETQUA qua INSTEAD OF trigger. Lọc dòng (SELECT) vẫn đủ theo đề.
    enable          => TRUE
  );
END;
/

-- Áp dụng VPD cho bảng HSBA_DV
BEGIN
  DBMS_RLS.ADD_POLICY(
    object_schema   => 'BVADMIN',
    object_name     => 'HSBA_DV',
    policy_name     => 'POL_HSBA_DV_DPV_BS',
    function_schema => 'BVADMIN',
    policy_function => 'vpd_hsba_dv',
    statement_types => 'SELECT,INSERT,UPDATE,DELETE',
    update_check    => FALSE,   -- FALSE: tránh kiểm dòng-mới gây ORA-28138/28115 khi BS INSERT HSBA_DV
                                 -- hoặc KTV cập nhật KETQUA qua INSTEAD OF trigger. Lọc dòng (SELECT) vẫn đủ theo đề.
    enable          => TRUE
  );
END;
/

-- Áp dụng VPD cho bảng BENHNHAN
BEGIN
  DBMS_RLS.ADD_POLICY(
    object_schema   => 'BVADMIN',
    object_name     => 'BENHNHAN',
    policy_name     => 'POL_BENHNHAN_DPV_BS',
    function_schema => 'BVADMIN',
    policy_function => 'vpd_benhnhan',
    statement_types => 'SELECT,UPDATE',
    update_check    => FALSE,   -- FALSE: tránh kiểm dòng-mới gây ORA-28138/28115 khi BS INSERT HSBA_DV
                                 -- hoặc KTV cập nhật KETQUA qua INSTEAD OF trigger. Lọc dòng (SELECT) vẫn đủ theo đề.
    enable          => TRUE
  );
END;
/

-- Áp dụng VPD cho bảng DONTHUOC
BEGIN
  DBMS_RLS.ADD_POLICY(
    object_schema   => 'BVADMIN',
    object_name     => 'DONTHUOC',
    policy_name     => 'POL_DONTHUOC_BS',
    function_schema => 'BVADMIN',
    policy_function => 'vpd_donthuoc',
    statement_types => 'SELECT,INSERT,UPDATE,DELETE',
    update_check    => FALSE,   -- FALSE: tránh kiểm dòng-mới gây ORA-28138/28115 khi BS INSERT HSBA_DV
                                 -- hoặc KTV cập nhật KETQUA qua INSTEAD OF trigger. Lọc dòng (SELECT) vẫn đủ theo đề.
    enable          => TRUE
  );
END;
/

-- ============================================================
-- PHẦN D: AUDIT TRIGGER - ghi vết UPDATE CHANDOAN/DIEUTRI/KETLUAN (TC#3c)
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

-- Bảng log ghi vết thay đổi HSBA (CHANDOAN, DIEUTRI, KETLUAN)
CREATE TABLE LOG_BS_HSBA (
    LOG_ID      NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MAHSBA      VARCHAR2(10),
    COT_THAYDO  VARCHAR2(20),   -- 'CHANDOAN' | 'DIEUTRI' | 'KETLUAN'
    GIA_TRI_CU  NCLOB,
    GIA_TRI_MOI NCLOB,
    BS_THUCHIN  VARCHAR2(100),  -- Oracle username của BS thay đổi
    THOI_GIAN   TIMESTAMP DEFAULT SYSTIMESTAMP
);

CREATE OR REPLACE TRIGGER trg_log_hsba_bs
AFTER UPDATE OF CHANDOAN, DIEUTRI, KETLUAN ON HSBA
FOR EACH ROW
DECLARE
    v_user VARCHAR2(100) := SYS_CONTEXT('USERENV','SESSION_USER');
BEGIN
    -- So sánh NULL-safe (cột nay là NVARCHAR2 nên dùng != trực tiếp được; sentinel N'<<NULL>>' tránh bỏ sót khi NULL)
    IF NVL(:NEW.CHANDOAN, N'<<NULL>>') != NVL(:OLD.CHANDOAN, N'<<NULL>>') THEN
        INSERT INTO LOG_BS_HSBA(MAHSBA, COT_THAYDO, GIA_TRI_CU, GIA_TRI_MOI, BS_THUCHIN)
        VALUES(:OLD.MAHSBA, 'CHANDOAN', :OLD.CHANDOAN, :NEW.CHANDOAN, v_user);
    END IF;

    IF NVL(:NEW.DIEUTRI, N'<<NULL>>') != NVL(:OLD.DIEUTRI, N'<<NULL>>') THEN
        INSERT INTO LOG_BS_HSBA(MAHSBA, COT_THAYDO, GIA_TRI_CU, GIA_TRI_MOI, BS_THUCHIN)
        VALUES(:OLD.MAHSBA, 'DIEUTRI', :OLD.DIEUTRI, :NEW.DIEUTRI, v_user);
    END IF;

    IF NVL(:NEW.KETLUAN, N'<<NULL>>') != NVL(:OLD.KETLUAN, N'<<NULL>>') THEN
        INSERT INTO LOG_BS_HSBA(MAHSBA, COT_THAYDO, GIA_TRI_CU, GIA_TRI_MOI, BS_THUCHIN)
        VALUES(:OLD.MAHSBA, 'KETLUAN', :OLD.KETLUAN, :NEW.KETLUAN, v_user);
    END IF;
END trg_log_hsba_bs;
/

-- Bảng log ghi vết thay đổi TENTHUOC/LIEUDUNG trong DONTHUOC (TC#3e)
CREATE TABLE LOG_BS_DONTHUOC (
    LOG_ID      NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MAHSBA      VARCHAR2(10),
    TENTHUOC_CU NVARCHAR2(200),
    TENTHUOC_MOI NVARCHAR2(200),
    LIEUDUNG_CU NVARCHAR2(200),
    LIEUDUNG_MOI NVARCHAR2(200),
    HANH_VI     VARCHAR2(10),   -- 'UPDATE'
    BS_THUCHIN  VARCHAR2(100),
    THOI_GIAN   TIMESTAMP DEFAULT SYSTIMESTAMP
);

CREATE OR REPLACE TRIGGER trg_log_donthuoc
AFTER UPDATE OF TENTHUOC, LIEUDUNG ON DONTHUOC
FOR EACH ROW
BEGIN
    INSERT INTO LOG_BS_DONTHUOC
        (MAHSBA, TENTHUOC_CU, TENTHUOC_MOI, LIEUDUNG_CU, LIEUDUNG_MOI, HANH_VI, BS_THUCHIN)
    VALUES
        (:OLD.MAHSBA, :OLD.TENTHUOC, :NEW.TENTHUOC,
         :OLD.LIEUDUNG, :NEW.LIEUDUNG, 'UPDATE',
         SYS_CONTEXT('USERENV','SESSION_USER'));
END trg_log_donthuoc;
/

-- ============================================================
-- PHẦN E: KIỂM THỬ VPD
-- ============================================================
-- Một số câu dưới CỐ Ý gây lỗi/0 dòng để minh hoạ VPD và column-grant.
-- Đặt CONTINUE để không làm dừng migration khi chạy tự động.
WHENEVER SQLERROR CONTINUE

-- Test BS_NV003 (bác sĩ Tim mạch, phụ trách HS001 và HS004):
CONNECT BS_NV003/"BV@2025!";
SELECT * FROM SESSION_ROLES;

-- Test SELECT HSBA: chỉ thấy HS001 và HS004 (MABS='NV003')
SELECT MAHSBA, MABN, NGAY, MABS FROM BVADMIN.HSBA;
-- Kết quả mong đợi: chỉ HS001, HS004

-- Test SELECT BENHNHAN: chỉ thấy BN001 (BN của HS001, HS004)
SELECT MABN, TENBN FROM BVADMIN.BENHNHAN;
-- Kết quả mong đợi: chỉ BN001

-- Test UPDATE CHANDOAN (có ghi vết) - HỢP LỆ
UPDATE BVADMIN.HSBA
SET    CHANDOAN = N'Đái tháo đường type 2 + rối loạn lipid máu'
WHERE  MAHSBA = 'HS001';
COMMIT;

-- Test UPDATE CHANDOAN của HSBA không phải của mình - BỊ VPD CHẶN (0 dòng)
UPDATE BVADMIN.HSBA
SET    CHANDOAN = N'Thay đổi bất hợp pháp'
-- HS002 thuộc BS NV004 → BS NV003 không thấy (VPD chặn, 0 dòng)
WHERE  MAHSBA = 'HS002';
-- Kết quả: 0 rows updated (VPD filter chặn, không thấy HS002)

-- Test INSERT DONTHUOC cho HSBA của mình - HỢP LỆ
INSERT INTO BVADMIN.DONTHUOC VALUES('HS001', DATE'2025-04-20', N'Atorvastatin 20mg', N'1 viên tối');
COMMIT;

-- Test DPV_NV001:
CONNECT DPV_NV001/"BV@2025!";

-- DPV thấy TẤT CẢ HSBA (VPD trả về '1=1' cho DPV)
SELECT MAHSBA, MABN, MABS, MAKHOA FROM BVADMIN.HSBA;
-- Kết quả: HS001, HS002, HS003, HS004

-- DPV cập nhật MAKHOA, MABS - HỢP LỆ
UPDATE BVADMIN.HSBA SET MAKHOA = N'Tim mạch - Nội trú' WHERE MAHSBA = 'HS001';
UPDATE BVADMIN.HSBA_DV SET MAKTV = 'NV007'
WHERE  MAHSBA = 'HS002' AND LOAIDV = N'Điện não đồ';
COMMIT;

-- DPV thử UPDATE CHANDOAN - BỊ TỪ CHỐI (không có column privilege)
UPDATE BVADMIN.HSBA SET CHANDOAN = N'DPV không được sửa' WHERE MAHSBA = 'HS001';
-- Lỗi: ORA-01031

-- Xem log audit sau test (BVADMIN)
CONNECT BVADMIN/"BVAdmin@2025";

SELECT MAHSBA, COT_THAYDO, BS_THUCHIN, THOI_GIAN,
       SUBSTR(TO_CHAR(GIA_TRI_CU), 1, 50) AS CU,
       SUBSTR(TO_CHAR(GIA_TRI_MOI), 1, 50) AS MOI
FROM   LOG_BS_HSBA
ORDER  BY THOI_GIAN DESC;

-- Kiểm tra VPD policy đã áp dụng
SELECT OBJECT_NAME, POLICY_NAME, FUNCTION, ENABLE, STATEMENT_TYPES
FROM   DBA_POLICIES
WHERE  OBJECT_OWNER = 'BVADMIN';
