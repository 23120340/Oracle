-- ============================================================
-- PHÂN HỆ 2 - File 05: Yêu cầu 2 - Oracle Label Security (OLS)
-- ============================================================
-- Bệnh viện có 3 khoa: Tiêu hóa, Thần kinh, Tim mạch
-- Bệnh viện có 3 cơ sở: Hồ Chí Minh, Hải Phòng, Hà Nội
-- Phân cấp: Ban Giám đốc > Lãnh đạo khoa > Nhân viên
--
-- Thiết kế nhãn 3 thành phần:
--   LEVEL      : cấp bậc  (NV=10 < LDK=20 < BGD=30)
--   COMPARTMENT: cơ sở    (HCM, HPN, HNI) - AND semantics: user phải có compartment của data
--   GROUP      : khoa     (TH, TK, TM)    - OR semantics: user cần ít nhất 1 group của data
--
-- Kết quả đọc data (user có thể đọc nếu thỏa 3 điều kiện):
--   1. user.MAX_LEVEL >= data.LEVEL
--   2. user CÓ tất cả COMPARTMENT của data (hoặc data không có compartment)
--   3. user CÓ ít nhất 1 GROUP của data (hoặc data không có group)
-- ============================================================
-- Chạy với SYS hoặc LBACSYS
-- ============================================================

SET DEFINE OFF
-- (tránh '&' trong chuỗi bị hiểu là biến thay thế; KHÔNG đặt ';' sau SET → tránh SP2-0158)

-- ============================================================
-- BƯỚC 1: Tạo policy OLS
-- ============================================================
CONNECT LBACSYS/lbacsys;
-- Nếu LBACSYS chưa có password, dùng SYS:
-- CONNECT SYS/password AS SYSDBA

-- Cài đặt OLS nếu chưa có:
-- @$ORACLE_HOME/rdbms/admin/catols.sql

BEGIN
    SA_SYSDBA.CREATE_POLICY(
        policy_name    => 'BV_LABEL_POLICY',
        column_name    => 'OLS_LABEL',        -- tên cột nhãn sẽ được thêm vào bảng THONGBAO
        default_options => 'READ_CONTROL'     -- áp dụng kiểm soát đọc
    );
END;
/

-- ============================================================
-- BƯỚC 2: Tạo các thành phần LEVEL (cấp bậc)
-- ============================================================
BEGIN
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name => 'BV_LABEL_POLICY',
        level_num   => 10,
        short_name  => 'NV',
        long_name   => 'Nhan vien'
    );
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name => 'BV_LABEL_POLICY',
        level_num   => 20,
        short_name  => 'LDK',
        long_name   => 'Lanh dao khoa'
    );
    SA_COMPONENTS.CREATE_LEVEL(
        policy_name => 'BV_LABEL_POLICY',
        level_num   => 30,
        short_name  => 'BGD',
        long_name   => 'Ban Giam doc'
    );
END;
/

-- ============================================================
-- BƯỚC 3: Tạo các thành phần COMPARTMENT (cơ sở địa điểm)
-- Dùng AND semantics: user phải có compartment tương ứng mới đọc được
-- ============================================================
BEGIN
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name => 'BV_LABEL_POLICY',
        comp_num    => 100,
        short_name  => 'HCM',
        long_name   => 'Ho Chi Minh'
    );
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name => 'BV_LABEL_POLICY',
        comp_num    => 200,
        short_name  => 'HPN',
        long_name   => 'Hai Phong'
    );
    SA_COMPONENTS.CREATE_COMPARTMENT(
        policy_name => 'BV_LABEL_POLICY',
        comp_num    => 300,
        short_name  => 'HNI',
        long_name   => 'Ha Noi'
    );
END;
/

-- ============================================================
-- BƯỚC 4: Tạo các thành phần GROUP (khoa)
-- Dùng OR semantics: user cần ít nhất 1 group của data
-- ============================================================
BEGIN
    -- Group gốc: HOSPITAL (parent cho mọi khoa)
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'BV_LABEL_POLICY',
        group_num    => 1000,
        short_name   => 'HOSPITAL',
        long_name    => 'Toan benh vien',
        parent_name  => NULL
    );
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'BV_LABEL_POLICY',
        group_num    => 1100,
        short_name   => 'TH',
        long_name    => 'Tieu hoa',
        parent_name  => 'HOSPITAL'
    );
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'BV_LABEL_POLICY',
        group_num    => 1200,
        short_name   => 'TK',
        long_name    => 'Than kinh',
        parent_name  => 'HOSPITAL'
    );
    SA_COMPONENTS.CREATE_GROUP(
        policy_name  => 'BV_LABEL_POLICY',
        group_num    => 1300,
        short_name   => 'TM',
        long_name    => 'Tim mach',
        parent_name  => 'HOSPITAL'
    );
END;
/

-- ============================================================
-- BƯỚC 5: Tạo giá trị nhãn (Label Values) cho dữ liệu t1–t7
-- Format: LEVEL[:COMPARTMENTS][:GROUPS]
-- ============================================================
BEGIN
    -- t1: Gửi đến toàn bộ nhân viên (NV, không giới hạn cơ sở/khoa)
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1001, 'NV',          TRUE);

    -- t2: Gửi đến Ban giám đốc (chỉ BGD đọc được)
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1002, 'BGD',         TRUE);

    -- t3: Gửi đến tất cả lãnh đạo khoa (không giới hạn cơ sở/khoa)
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1003, 'LDK',         TRUE);

    -- t4: Gửi đến lãnh đạo Khoa tiêu hóa (ở mọi cơ sở)
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1004, 'LDK::TH',     TRUE);

    -- t5: Gửi đến nhân viên Khoa tiêu hóa ở HCM
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1005, 'NV:HCM:TH',   TRUE);

    -- t6: Gửi đến nhân viên Khoa tiêu hóa ở Hà Nội
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1006, 'NV:HNI:TH',   TRUE);

    -- t7: Gửi đến lãnh đạo Khoa TH và Khoa TK tại Hải Phòng
    --     Group TH,TK → OR semantics: có TH hoặc TK đều đọc được
    SA_LABEL_ADMIN.CREATE_LABEL('BV_LABEL_POLICY', 1007, 'LDK:HPN:TH,TK', TRUE);
END;
/

-- ============================================================
-- BƯỚC 6: Áp dụng policy lên bảng THONGBAO
-- ============================================================
BEGIN
    SA_POLICY_ADMIN.APPLY_TABLE_POLICY(
        policy_name   => 'BV_LABEL_POLICY',
        schema_name   => 'BVADMIN',
        table_name    => 'THONGBAO',
        -- Chỉ READ_CONTROL (kiểm soát ĐỌC theo nhãn).
        -- KHÔNG dùng LABEL_DEFAULT: BVADMIN có quyền FULL (BƯỚC 8a) nên OLS BỎ QUA việc
        --   tự gán nhãn khi user FULL insert ⇒ SET_ROW_LABEL/LABEL_DEFAULT đều VÔ HIỆU,
        --   cột OLS_LABEL sẽ NULL ⇒ u1–u8 đọc 0 dòng. (Đã kiểm chứng trên Oracle 21c XE.)
        --   Vì vậy BƯỚC 8b gán nhãn TRỰC TIẾP bằng CHAR_TO_LABEL.
        table_options => 'READ_CONTROL',
        label_function => NULL,
        predicate      => NULL
    );
END;
/

-- ============================================================
-- BƯỚC 7: Tạo user u1–u8 và thiết lập nhãn
-- ============================================================
-- 7a. Tạo user + cấp quyền — chạy bằng SYSTEM/DBA (LBACSYS KHÔNG có CREATE USER)
CONNECT SYSTEM/oracle;

CREATE USER u1_giamdoc    IDENTIFIED BY "U1@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u2_ldtm_hcm   IDENTIFIED BY "U2@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u3_ldtk_hni   IDENTIFIED BY "U3@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u4_nvtk_hcm   IDENTIFIED BY "U4@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u5_nvtm_hcm   IDENTIFIED BY "U5@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u6_ldp_tm_hcm IDENTIFIED BY "U6@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u7_ldp_all    IDENTIFIED BY "U7@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
CREATE USER u8_nvth_hni   IDENTIFIED BY "U8@2025" DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;

GRANT CREATE SESSION TO u1_giamdoc, u2_ldtm_hcm, u3_ldtk_hni, u4_nvtk_hcm,
                        u5_nvtm_hcm, u6_ldp_tm_hcm, u7_ldp_all, u8_nvth_hni;
GRANT SELECT ON BVADMIN.THONGBAO TO u1_giamdoc, u2_ldtm_hcm, u3_ldtk_hni, u4_nvtk_hcm,
                                    u5_nvtm_hcm, u6_ldp_tm_hcm, u7_ldp_all, u8_nvth_hni;

-- 7b. Gán nhãn đọc (max_read_label) cho từng user — chạy bằng LBACSYS (chủ policy OLS)
--   Ngữ nghĩa nhãn: LEVEL=cấp bậc (NV<LDK<BGD); COMPARTMENT=cơ sở (AND); GROUP=khoa (OR)
--   u1: Giám đốc đọc toàn bộ        u2: LĐ Tim mạch @HCM     u3: LĐ Thần kinh @HN
--   u4: NV Thần kinh @HCM           u5: NV Tim mạch @HCM     u6: LĐ phòng Tim mạch @HCM
--   u7: LĐ phòng mọi cơ sở/khoa     u8: NV Tiêu hóa @HN
CONNECT LBACSYS/lbacsys;

BEGIN
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U1_GIAMDOC',    'BGD:HCM,HPN,HNI:TH,TK,TM');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U2_LDTM_HCM',   'LDK:HCM:TM');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U3_LDTK_HNI',   'LDK:HNI:TK');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U4_NVTK_HCM',   'NV:HCM:TK');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U5_NVTM_HCM',   'NV:HCM:TM');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U6_LDP_TM_HCM', 'LDK:HCM:TM');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U7_LDP_ALL',    'LDK:HCM,HPN,HNI:TH,TK,TM');
  SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U8_NVTH_HNI',   'NV:HNI:TH');
END;
/

-- ============================================================
-- BƯỚC 8: Insert dữ liệu thông báo t1–t7 với nhãn OLS
-- ============================================================
-- 8a. Cấp quyền FULL cho BVADMIN để gán nhãn dòng bất kỳ — PHẢI do LBACSYS thực hiện
CONNECT LBACSYS/lbacsys;
BEGIN
  SA_USER_ADMIN.SET_USER_PRIVS('BV_LABEL_POLICY', 'BVADMIN', 'FULL');
END;
/

-- 8b. Chèn thông báo, gán nhãn OLS TRỰC TIẾP bằng CHAR_TO_LABEL.
--     KHÔNG dùng SA_SESSION.SET_ROW_LABEL: BVADMIN có quyền FULL nên OLS không tự gán
--     nhãn dòng (xem ghi chú BƯỚC 6) ⇒ phải ghi thẳng giá trị nhãn vào cột OLS_LABEL.
CONNECT BVADMIN/"BVAdmin@2025";

-- t1: gửi toàn bộ nhân viên (NV)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB001', CHAR_TO_LABEL('BV_LABEL_POLICY','NV'),
       N'Thông báo họp toàn viện ngày 05/05/2025', SYSTIMESTAMP, N'Hội trường lớn');
-- t2: Ban giám đốc (BGD)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB002', CHAR_TO_LABEL('BV_LABEL_POLICY','BGD'),
       N'Họp khẩn Ban Giám đốc - Kế hoạch mở rộng 2025', SYSTIMESTAMP, N'Phòng họp BGD');
-- t3: các lãnh đạo khoa (LDK)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB003', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK'),
       N'Họp lãnh đạo khoa - Báo cáo quý 2', SYSTIMESTAMP, N'Phòng họp A');
-- t4: lãnh đạo Khoa Tiêu hóa (LDK::TH)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB004', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK::TH'),
       N'Họp lãnh đạo Khoa Tiêu hóa - Cải tiến quy trình', SYSTIMESTAMP, N'Phòng D2.01');
-- t5: NV Khoa Tiêu hóa @HCM (NV:HCM:TH)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB005', CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HCM:TH'),
       N'Tập huấn nội soi tiêu hóa - Cơ sở HCM', SYSTIMESTAMP, N'Phòng kỹ năng HCM');
-- t6: NV Khoa Tiêu hóa @Hà Nội (NV:HNI:TH)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB006', CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HNI:TH'),
       N'Tập huấn nội soi tiêu hóa - Cơ sở Hà Nội', SYSTIMESTAMP, N'Phòng kỹ năng HN');
-- t7: LĐ Khoa TH và TK @Hải Phòng (LDK:HPN:TH,TK)
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB007', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK:HPN:TH,TK'),
       N'Họp khẩn Lãnh đạo Khoa TH và TK - Cơ sở Hải Phòng', SYSTIMESTAMP, N'Phòng họp HP');

COMMIT;

-- ============================================================
-- BƯỚC 9: Kiểm thử - mỗi user thấy thông báo nào?
-- ============================================================

-- u1 (BGD) - thấy tất cả t1-t7
CONNECT u1_giamdoc/"U1@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả: 7 dòng

-- u2 (LDK, HCM, TM) - thấy t1, t3 (không thấy t4 vì group TH, không thấy t5/t6/t7)
CONNECT u2_ldtm_hcm/"U2@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB003 (t3)

-- u3 (LDK, HNI, TK) - thấy t1, t3 (t6 yêu cầu group TH, t7 cần HPN)
CONNECT u3_ldtk_hni/"U3@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB003 (t3)

-- u4 (NV, HCM, TK) - chỉ thấy t1 (group TK, không phải TH)
CONNECT u4_nvtk_hcm/"U4@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1)

-- u5 (NV, HCM, TM) - chỉ thấy t1
CONNECT u5_nvtm_hcm/"U5@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1)

-- u7 (LDK, mọi cơ sở, mọi khoa) - thấy t1, t3, t4, t5, t6, t7 (trừ t2=BGD)
CONNECT u7_ldp_all/"U7@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001,TB003,TB004,TB005,TB006,TB007

-- u8 (NV, HNI, TH) - thấy t1 và t6
CONNECT u8_nvth_hni/"U8@2025";
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB006 (t6)

-- Xem cấu hình nhãn
CONNECT LBACSYS/lbacsys;
SELECT * FROM DBA_SA_LEVELS         WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_COMPARTMENTS   WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_GROUPS         WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_USER_LABELS    WHERE POLICY_NAME = 'BV_LABEL_POLICY';
