-- ============================================================
-- PHÂN HỆ 2 - File 06: Yêu cầu 3 - Cơ chế Kiểm toán (Audit)
-- ============================================================
-- 1. Kích hoạt kiểm toán hệ thống
-- 2. Standard Audit: 5 ngữ cảnh theo dõi user/object cụ thể
-- 3. Fine-Grained Audit (FGA):
--    a. UPDATE DONTHUOC (MAHSBA, NGAYDT, TENTHUOC, LIEUDUNG) sau khi tạo
--    b. Cập nhật thành công CHANDOAN/DIEUTRI/KETLUAN bởi BS
--    c. Cập nhật bất hợp pháp CHANDOAN/DIEUTRI/KETLUAN
--    d. Thêm/xóa/sửa bất hợp pháp trên HSBA_DV
-- 4. Đọc xuất dữ liệu kiểm toán
-- ============================================================
-- Chạy với quyền SYS (phần kích hoạt) hoặc SYSTEM (phần audit)
-- ============================================================

-- ============================================================
-- PHẦN 1: KÍCH HOẠT KIỂM TOÁN HỆ THỐNG
-- ============================================================
CONNECT SYS/password AS SYSDBA;

-- Đảm bảo audit đang bật (Oracle 11g: audit_trail = DB | DB,EXTENDED)
-- Kiểm tra tham số hiện tại
SHOW PARAMETER audit_trail;
-- Nếu cần thay đổi (cần restart DB):
-- ALTER SYSTEM SET audit_trail = DB,EXTENDED SCOPE = SPFILE;

-- Kiểm tra trạng thái audit
SELECT VALUE FROM V$PARAMETER WHERE NAME = 'audit_trail';

-- Kích hoạt kiểm toán phiên đăng nhập (tất cả user)
AUDIT SESSION;

-- ============================================================
-- PHẦN 2: STANDARD AUDIT - 5 ngữ cảnh kiểm toán
-- ============================================================
CONNECT SYSTEM/oracle;

-- --- Ngữ cảnh 1 ---
-- Theo dõi mọi SELECT trên BENHNHAN (phát hiện truy cập trái phép dữ liệu BN)
-- Theo dõi cả thành công và thất bại
AUDIT SELECT ON BVADMIN.BENHNHAN BY ACCESS WHENEVER NOT SUCCESSFUL;
AUDIT SELECT ON BVADMIN.BENHNHAN BY ACCESS WHENEVER SUCCESSFUL;

-- --- Ngữ cảnh 2 ---
-- Theo dõi UPDATE trên HSBA bởi tất cả user (ghi vết thay đổi HSBA)
AUDIT UPDATE ON BVADMIN.HSBA BY ACCESS;

-- --- Ngữ cảnh 3 ---
-- Theo dõi INSERT/DELETE trên HSBA_DV (thêm/xóa dịch vụ chẩn đoán)
AUDIT INSERT, DELETE ON BVADMIN.HSBA_DV BY ACCESS;

-- --- Ngữ cảnh 4 ---
-- Theo dõi các thao tác trên DONTHUOC (an toàn đơn thuốc)
AUDIT INSERT, UPDATE, DELETE ON BVADMIN.DONTHUOC BY ACCESS;

-- --- Ngữ cảnh 5 ---
-- Theo dõi kết nối không thành công của tất cả user (phát hiện tấn công brute-force)
AUDIT CREATE SESSION WHENEVER NOT SUCCESSFUL;

-- Kiểm tra audit đã cấu hình
SELECT OBJECT_NAME, OBJECT_TYPE, ALT, AUD, COM, DEL, GRA, IND, INS, LOC,
       REN, SEL, UPD, FBK, REF
FROM   DBA_OBJ_AUDIT_OPTS
WHERE  OWNER = 'BVADMIN'
ORDER  BY OBJECT_NAME;

-- ============================================================
-- PHẦN 3: FINE-GRAINED AUDIT (FGA) - 4 tình huống đặc biệt
-- ============================================================
CONNECT SYSTEM/oracle;
GRANT EXECUTE ON DBMS_FGA TO BVADMIN;

CONNECT BVADMIN/BVAdmin@2025;

-- --- FGA 3a ---
-- Ghi vết UPDATE TENTHUOC hoặc LIEUDUNG trong DONTHUOC
-- (sau khi đơn thuốc đã tạo, bác sĩ điều chỉnh tên thuốc hoặc liều dùng)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'DONTHUOC',
        policy_name     => 'FGA_DONTHUOC_UPDATE',
        audit_condition => NULL,          -- luôn audit khi có access
        audit_column    => 'TENTHUOC,LIEUDUNG',  -- chỉ khi truy cập 2 cột này
        handler_schema  => NULL,
        handler_module  => NULL,
        enable          => TRUE,
        statement_types => 'UPDATE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED,
        audit_column_opts => DBMS_FGA.ANY_COLUMNS  -- khi có bất kỳ cột nào trong danh sách
    );
END;
/

-- --- FGA 3b ---
-- Ghi vết UPDATE CHANDOAN/DIEUTRI/KETLUAN bởi user có BS_Role (thành công)
-- Điều kiện: chỉ audit khi user có role BS (thông qua kiểm tra VAITRO)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA',
        policy_name     => 'FGA_HSBA_BS_UPDATE',
        audit_condition => 'BVADMIN.fn_get_vaitro() = ''BS''',
        audit_column    => 'CHANDOAN,DIEUTRI,KETLUAN',
        handler_schema  => NULL,
        handler_module  => NULL,
        enable          => TRUE,
        statement_types => 'UPDATE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED,
        audit_column_opts => DBMS_FGA.ANY_COLUMNS
    );
END;
/

-- --- FGA 3c ---
-- Ghi vết UPDATE bất hợp pháp trên CHANDOAN/DIEUTRI/KETLUAN
-- "Bất hợp pháp" = user KHÔNG phải BS nhưng cố UPDATE
-- Sử dụng Standard Audit WHENEVER NOT SUCCESSFUL để bắt lỗi quyền
CONNECT SYSTEM/oracle;
AUDIT UPDATE(CHANDOAN) ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;
AUDIT UPDATE(DIEUTRI)  ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;
AUDIT UPDATE(KETLUAN)  ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;

-- Kết hợp FGA: audit khi user KHÔNG phải BS mà vẫn UPDATE thành công
-- (trường hợp bỏ qua VPD - cần ghi vết)
CONNECT BVADMIN/BVAdmin@2025;
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA',
        policy_name     => 'FGA_HSBA_ILLEGAL_UPDATE',
        audit_condition => 'BVADMIN.fn_get_vaitro() != ''BS'' OR BVADMIN.fn_get_vaitro() IS NULL',
        audit_column    => 'CHANDOAN,DIEUTRI,KETLUAN',
        enable          => TRUE,
        statement_types => 'UPDATE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED,
        audit_column_opts => DBMS_FGA.ANY_COLUMNS
    );
END;
/

-- --- FGA 3d ---
-- Ghi vết INSERT/UPDATE/DELETE bất hợp pháp trên HSBA_DV
-- "Bất hợp pháp" = không phải BS hoặc DPV mà cố thao tác
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA_DV',
        policy_name     => 'FGA_HSBA_DV_ILLEGAL',
        audit_condition => 'BVADMIN.fn_get_vaitro() NOT IN (''BS'',''DPV'') OR BVADMIN.fn_get_vaitro() IS NULL',
        audit_column    => NULL,  -- audit mọi cột
        enable          => TRUE,
        statement_types => 'INSERT,UPDATE,DELETE',
        audit_trail     => DBMS_FGA.DB + DBMS_FGA.EXTENDED
    );
END;
/

-- ============================================================
-- PHẦN 4: TẠO TÌNH HUỐNG KIỂM CHỨNG
-- ============================================================

-- Tình huống A: BS_NV003 cập nhật CHANDOAN (hợp lệ - trigger + FGA ghi vết)
CONNECT BS_NV003/BV@2025!;
UPDATE BVADMIN.HSBA
SET    CHANDOAN = N'Đái tháo đường type 2 có biến chứng thận - cập nhật lần 2'
WHERE  MAHSBA = 'HS001';
COMMIT;

-- Tình huống B: BS_NV003 cập nhật LIEUDUNG trong DONTHUOC (FGA 3a ghi vết)
UPDATE BVADMIN.DONTHUOC
SET    LIEUDUNG = N'3 lần/ngày trước ăn (điều chỉnh)'
WHERE  MAHSBA = 'HS001' AND TENTHUOC = N'Metformin 500mg';
COMMIT;

-- Tình huống C: DPV_NV001 cố UPDATE CHANDOAN (thất bại - Standard Audit 3c ghi lỗi)
CONNECT DPV_NV001/BV@2025!;
UPDATE BVADMIN.HSBA
SET    CHANDOAN = N'DPV cố thay đổi - bất hợp pháp'
WHERE  MAHSBA = 'HS001';
-- Lỗi: ORA-01031: insufficient privileges → ghi vào DBA_AUDIT_TRAIL

-- Tình huống D: KTV_NV006 cố DELETE trên HSBA_DV (thất bại - FGA 3d ghi vết)
CONNECT KTV_NV006/BV@2025!;
DELETE FROM BVADMIN.HSBA_DV WHERE MAHSBA = 'HS001';
-- Lỗi: ORA-01031 (KTV không có DELETE) → ghi vào DBA_FGA_AUDIT_TRAIL

-- Tình huống E: Đăng nhập sai mật khẩu (Standard Audit phiên 5)
-- CONNECT fake_user/wrongpassword;  -- ORA-01017 → ghi vào DBA_AUDIT_TRAIL

-- ============================================================
-- PHẦN 5: ĐỌC XUẤT DỮ LIỆU KIỂM TOÁN
-- ============================================================
CONNECT SYSTEM/oracle;

-- 5.1 Standard Audit - tất cả
SELECT USERNAME,
       ACTION_NAME,
       OBJ_NAME,
       TIMESTAMP,
       RETURNCODE,            -- 0=thành công, <> 0=thất bại
       SQL_TEXT
FROM   DBA_AUDIT_TRAIL
WHERE  OBJ_OWNER = 'BVADMIN'
ORDER  BY TIMESTAMP DESC
FETCH FIRST 50 ROWS ONLY;

-- 5.2 Standard Audit - chỉ thao tác thất bại (bất hợp pháp)
SELECT USERNAME, ACTION_NAME, OBJ_NAME, TIMESTAMP, RETURNCODE
FROM   DBA_AUDIT_TRAIL
WHERE  OBJ_OWNER = 'BVADMIN'
  AND  RETURNCODE != 0
ORDER  BY TIMESTAMP DESC;

-- 5.3 Đăng nhập thất bại
SELECT USERNAME, TIMESTAMP, RETURNCODE, USERHOST, OS_USERNAME
FROM   DBA_AUDIT_TRAIL
WHERE  ACTION_NAME = 'LOGON'
  AND  RETURNCODE != 0
ORDER  BY TIMESTAMP DESC;

-- 5.4 FGA Audit - tất cả thao tác được FGA ghi lại
SELECT DB_USER,
       POLICY_NAME,
       OBJECT_NAME,
       SQL_TEXT,
       EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  OBJECT_SCHEMA = 'BVADMIN'
ORDER  BY EXTENDED_TIMESTAMP DESC;

-- 5.5 FGA Audit - cụ thể từng policy
-- FGA 3a: UPDATE TENTHUOC/LIEUDUNG trong DONTHUOC
SELECT DB_USER, SQL_TEXT, EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  POLICY_NAME = 'FGA_DONTHUOC_UPDATE'
ORDER  BY EXTENDED_TIMESTAMP DESC;

-- FGA 3b: BS cập nhật CHANDOAN/DIEUTRI/KETLUAN
SELECT DB_USER, SQL_TEXT, EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  POLICY_NAME = 'FGA_HSBA_BS_UPDATE'
ORDER  BY EXTENDED_TIMESTAMP DESC;

-- FGA 3d: Thao tác bất hợp pháp trên HSBA_DV
SELECT DB_USER, SQL_TEXT, EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  POLICY_NAME = 'FGA_HSBA_DV_ILLEGAL'
ORDER  BY EXTENDED_TIMESTAMP DESC;

-- 5.6 Xem log trigger (bảng tự tạo trong file 04)
CONNECT BVADMIN/BVAdmin@2025;

-- Log thay đổi HSBA bởi BS
SELECT MAHSBA, COT_THAYDO, BS_THUCHIN,
       SUBSTR(TO_CHAR(GIA_TRI_CU),  1, 50) AS CU,
       SUBSTR(TO_CHAR(GIA_TRI_MOI), 1, 50) AS MOI,
       THOI_GIAN
FROM   LOG_BS_HSBA
ORDER  BY THOI_GIAN DESC;

-- Log thay đổi DONTHUOC bởi BS
SELECT MAHSBA, TENTHUOC_CU, TENTHUOC_MOI, LIEUDUNG_CU, LIEUDUNG_MOI, THOI_GIAN
FROM   LOG_BS_DONTHUOC
ORDER  BY THOI_GIAN DESC;

-- Log thay đổi KETQUA bởi KTV
SELECT MAHSBA, LOAIDV, MAKTV, CHANGED_BY, CHANGED_AT
FROM   LOG_KTV_KETQUA
ORDER  BY CHANGED_AT DESC;
