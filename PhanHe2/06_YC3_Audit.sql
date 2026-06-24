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
CONNECT SYS/&&sys_pwd AS SYSDBA;

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
-- PHẦN 1B (QUAN TRỌNG): kiểm tra chế độ Unified Auditing — Oracle 12c+/21c XE
-- ============================================================
-- Nếu DB ở chế độ PURE UNIFIED AUDITING:
--   • Lệnh AUDIT kiểu cũ + DBMS_FGA vẫn chạy nhưng GHI VÀO UNIFIED_AUDIT_TRAIL,
--   • DBA_AUDIT_TRAIL / DBA_FGA_AUDIT_TRAIL sẽ RỖNG (PHẦN 5 đọc ra 0 dòng).
-- Kiểm tra chế độ hiện tại:
SELECT VALUE AS UNIFIED_AUDITING FROM V$OPTION WHERE PARAMETER = 'Unified Auditing';
-- Nếu kết quả = TRUE → dùng Unified Audit Policy (mẫu tương đương 5 ngữ cảnh dưới đây)
-- và ĐỌC nhật ký từ UNIFIED_AUDIT_TRAIL thay vì DBA_AUDIT_TRAIL/DBA_FGA_AUDIT_TRAIL.
/*
  CREATE AUDIT POLICY pol_dpv_benhnhan ACTIONS SELECT, INSERT, UPDATE ON BVADMIN.BENHNHAN;
  AUDIT POLICY pol_dpv_benhnhan BY DPV_NV001;

  CREATE AUDIT POLICY pol_bs_hsba     ACTIONS UPDATE ON BVADMIN.HSBA;
  AUDIT POLICY pol_bs_hsba BY BS_NV003;

  CREATE AUDIT POLICY pol_ktv_hsbadv  ACTIONS SELECT, UPDATE ON BVADMIN.HSBA_DV;
  AUDIT POLICY pol_ktv_hsbadv BY KTV_NV006 WHENEVER NOT SUCCESSFUL;

  CREATE AUDIT POLICY pol_bn_benhnhan ACTIONS UPDATE ON BVADMIN.BENHNHAN;
  AUDIT POLICY pol_bn_benhnhan BY BN_BN001 WHENEVER NOT SUCCESSFUL;

  CREATE AUDIT POLICY pol_logon_fail  ACTIONS LOGON;
  AUDIT POLICY pol_logon_fail WHENEVER NOT SUCCESSFUL;

  -- Đọc nhật ký (cả audit chuẩn lẫn FGA gộp chung 1 view):
  SELECT DBUSERNAME, ACTION_NAME, OBJECT_SCHEMA, OBJECT_NAME, RETURN_CODE, EVENT_TIMESTAMP, SQL_TEXT
  FROM   UNIFIED_AUDIT_TRAIL
  WHERE  OBJECT_SCHEMA = 'BVADMIN'
  ORDER  BY EVENT_TIMESTAMP DESC FETCH FIRST 50 ROWS ONLY;
*/

-- ============================================================
-- PHẦN 2: STANDARD AUDIT - 5 ngữ cảnh kiểm toán
-- ============================================================
CONNECT SYSTEM/oracle;

-- ════════════════════════════════════════════════════════════════════════════
-- Spec yêu cầu: Standard Audit "theo dõi hành vi của những USER CỤ THỂ
-- trên những đối tượng cụ thể". Vì vậy dùng AUDIT BY <username>.
-- ════════════════════════════════════════════════════════════════════════════

-- --- Ngữ cảnh 1 ---
-- DPV_NV001 truy cập BENHNHAN (đúng vai trò) - log cả thành công lẫn thất bại
-- để giám sát hoạt động hàng ngày + phát hiện thử nghiệm vượt quyền
AUDIT SELECT, INSERT, UPDATE ON BVADMIN.BENHNHAN
    BY DPV_NV001 BY ACCESS;

-- --- Ngữ cảnh 2 ---
-- BS_NV003 cập nhật HSBA - theo dõi tất cả thay đổi chẩn đoán/điều trị
-- (kết hợp với FGA bên dưới để có audit chi tiết)
AUDIT UPDATE ON BVADMIN.HSBA
    BY BS_NV003 BY ACCESS WHENEVER SUCCESSFUL;
AUDIT UPDATE ON BVADMIN.HSBA
    BY BS_NV003 BY ACCESS WHENEVER NOT SUCCESSFUL;

-- --- Ngữ cảnh 3 ---
-- KTV_NV006 cập nhật KETQUA trên HSBA_DV
-- Đặc biệt theo dõi thao tác KHÔNG thành công (cố vượt quyền)
AUDIT SELECT, UPDATE ON BVADMIN.HSBA_DV
    BY KTV_NV006 BY ACCESS WHENEVER NOT SUCCESSFUL;

-- --- Ngữ cảnh 4 ---
-- BN_BN001 - bệnh nhân tự cập nhật thông tin
-- Theo dõi để phát hiện hành vi bất thường (vd. cố sửa CCCD/TENBN)
AUDIT UPDATE ON BVADMIN.BENHNHAN
    BY BN_BN001 BY ACCESS WHENEVER NOT SUCCESSFUL;

-- --- Ngữ cảnh 5 ---
-- Mọi kết nối không thành công (phát hiện brute-force toàn hệ thống)
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

CONNECT BVADMIN/"BVAdmin@2025";

-- FIX (ORA-28138 = "error in evaluating the fine grained audit policy predicate"):
-- audit_condition của FGA phải là MỘT predicate đơn giản (1 toán tử), KHÔNG được chứa
-- AND / OR / IN. Bọc logic nhiều toán tử vào hàm trả về 'Y'/'N' rồi so sánh đơn.
-- fn_is_illegal_hsba: 'Y' khi user KHÔNG phải BS (gồm cả VAITRO IS NULL)
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

-- fn_is_illegal_hsba_dv: 'Y' khi user KHÔNG phải BS lẫn DPV (gồm cả VAITRO IS NULL)
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

-- Idempotent: gỡ FGA policy cũ nếu đã tồn tại (tránh ORA-28101 khi chạy lại mà không -Reset)
DECLARE
    PROCEDURE drop_fga(p_obj VARCHAR2, p_pol VARCHAR2) IS
    BEGIN
        DBMS_FGA.DROP_POLICY('BVADMIN', p_obj, p_pol);
    EXCEPTION WHEN OTHERS THEN NULL;   -- policy chưa tồn tại
    END;
BEGIN
    drop_fga('DONTHUOC', 'FGA_DONTHUOC_UPDATE');
    drop_fga('HSBA',     'FGA_HSBA_BS_UPDATE');
    drop_fga('HSBA',     'FGA_HSBA_ILLEGAL_UPDATE');
    drop_fga('HSBA_DV',  'FGA_HSBA_DV_ILLEGAL');
END;
/

-- --- FGA 3a ---
-- Ghi vết UPDATE TENTHUOC hoặc LIEUDUNG trong DONTHUOC
-- (sau khi đơn thuốc đã tạo, bác sĩ điều chỉnh tên thuốc hoặc liều dùng)
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'DONTHUOC',
        policy_name     => 'FGA_DONTHUOC_UPDATE',
        audit_condition => NULL,          -- luôn audit khi có access
        audit_column    => 'MAHSBA,NGAYDT,TENTHUOC,LIEUDUNG',  -- đủ 4 cột theo YC3-3a
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
-- FIX (B5/H4): AUDIT chuẩn KHÔNG hỗ trợ mức cột "AUDIT UPDATE(CHANDOAN)..." (lỗi cú pháp).
-- Dùng audit mức BẢNG cho UPDATE thất bại → bắt được hành vi cập nhật bất hợp pháp
-- khi user không phải BS bị từ chối quyền cột (ORA-01031) trên CHANDOAN/DIEUTRI/KETLUAN.
AUDIT UPDATE ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;

-- Kết hợp FGA: audit khi user KHÔNG phải BS mà vẫn UPDATE thành công
-- (trường hợp bỏ qua VPD - cần ghi vết)
CONNECT BVADMIN/"BVAdmin@2025";
BEGIN
    DBMS_FGA.ADD_POLICY(
        object_schema   => 'BVADMIN',
        object_name     => 'HSBA',
        policy_name     => 'FGA_HSBA_ILLEGAL_UPDATE',
        audit_condition => 'BVADMIN.fn_is_illegal_hsba() = ''Y''',   -- FIX ORA-28138: predicate đơn
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
        audit_condition => 'BVADMIN.fn_is_illegal_hsba_dv() = ''Y''',   -- FIX ORA-28138: predicate đơn
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
-- Tình huống C/D CỐ Ý gây lỗi (ORA-01031) để sinh bản ghi audit thất bại.
-- Đặt CONTINUE để không làm dừng migration khi chạy tự động.
WHENEVER SQLERROR CONTINUE

-- Tình huống A: BS_NV003 cập nhật CHANDOAN (hợp lệ - trigger + FGA ghi vết)
CONNECT BS_NV003/"BV@2025!";
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
CONNECT DPV_NV001/"BV@2025!";
UPDATE BVADMIN.HSBA
SET    CHANDOAN = N'DPV cố thay đổi - bất hợp pháp'
WHERE  MAHSBA = 'HS001';
-- Lỗi: ORA-01031: insufficient privileges → ghi vào DBA_AUDIT_TRAIL

-- Tình huống D: KTV_NV006 cố DELETE trên HSBA_DV (thất bại - FGA 3d ghi vết)
CONNECT KTV_NV006/"BV@2025!";
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
CONNECT BVADMIN/"BVAdmin@2025";

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
