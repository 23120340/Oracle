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
        table_options => 'READ_CONTROL',     -- chỉ kiểm soát READ
        label_function => NULL,
        predicate      => NULL
    );
END;
/

-- ============================================================
-- BƯỚC 7: Tạo user u1–u8 và thiết lập nhãn
-- ============================================================
-- u1: Giám đốc - đọc được toàn bộ
CREATE USER u1_giamdoc IDENTIFIED BY U1@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u1_giamdoc;
GRANT SELECT ON BVADMIN.THONGBAO TO u1_giamdoc;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U1_GIAMDOC',
    max_read_label => 'BGD:HCM,HPN,HNI:TH,TK,TM'  -- Đọc mọi nhãn
);

-- u2: Lãnh đạo Khoa tim mạch tại HCM
CREATE USER u2_ldtm_hcm IDENTIFIED BY U2@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u2_ldtm_hcm;
GRANT SELECT ON BVADMIN.THONGBAO TO u2_ldtm_hcm;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U2_LDTM_HCM',
    max_read_label => 'LDK:HCM:TM'
);

-- u3: Lãnh đạo Khoa thần kinh tại Hà Nội
CREATE USER u3_ldtk_hni IDENTIFIED BY U3@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u3_ldtk_hni;
GRANT SELECT ON BVADMIN.THONGBAO TO u3_ldtk_hni;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U3_LDTK_HNI',
    max_read_label => 'LDK:HNI:TK'
);

-- u4: Nhân viên Khoa thần kinh tại HCM
CREATE USER u4_nvtk_hcm IDENTIFIED BY U4@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u4_nvtk_hcm;
GRANT SELECT ON BVADMIN.THONGBAO TO u4_nvtk_hcm;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U4_NVTK_HCM',
    max_read_label => 'NV:HCM:TK'
);

-- u5: Nhân viên Khoa tim mạch tại HCM
CREATE USER u5_nvtm_hcm IDENTIFIED BY U5@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u5_nvtm_hcm;
GRANT SELECT ON BVADMIN.THONGBAO TO u5_nvtm_hcm;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U5_NVTM_HCM',
    max_read_label => 'NV:HCM:TM'
);

-- u6: Lãnh đạo phòng - Khoa tim mạch tại HCM
CREATE USER u6_ldp_tm_hcm IDENTIFIED BY U6@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u6_ldp_tm_hcm;
GRANT SELECT ON BVADMIN.THONGBAO TO u6_ldp_tm_hcm;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U6_LDP_TM_HCM',
    max_read_label => 'LDK:HCM:TM'    -- Lãnh đạo phòng TM tại HCM
);

-- u7: Lãnh đạo phòng - đọc toàn bộ thông báo cấp lãnh đạo
CREATE USER u7_ldp_all IDENTIFIED BY U7@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u7_ldp_all;
GRANT SELECT ON BVADMIN.THONGBAO TO u7_ldp_all;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U7_LDP_ALL',
    max_read_label => 'LDK:HCM,HPN,HNI:TH,TK,TM'  -- LDK level, mọi cơ sở và khoa
);

-- u8: Nhân viên Khoa tiêu hóa tại Hà Nội
CREATE USER u8_nvth_hni IDENTIFIED BY U8@2025 DEFAULT TABLESPACE USERS QUOTA 0 ON USERS;
GRANT CREATE SESSION TO u8_nvth_hni;
GRANT SELECT ON BVADMIN.THONGBAO TO u8_nvth_hni;

SA_USER_ADMIN.SET_USER_LABELS(
    policy_name    => 'BV_LABEL_POLICY',
    user_name      => 'U8_NVTH_HNI',
    max_read_label => 'NV:HNI:TH'
);

-- ============================================================
-- BƯỚC 8: Insert dữ liệu thông báo t1–t7 với nhãn OLS
-- Phải chạy với tài khoản có quyền WRITEUP hoặc BVADMIN được exempt
-- ============================================================
CONNECT BVADMIN/BVAdmin@2025;

-- Cấp quyền FULL để BVADMIN có thể gán nhãn bất kỳ
SA_USER_ADMIN.SET_USER_PRIVS(
    policy_name => 'BV_LABEL_POLICY',
    user_name   => 'BVADMIN',
    privileges  => 'FULL'
);

-- Chèn thông báo kèm nhãn OLS bằng cách SET SESSION LABEL
-- t1: NV (tag=1001)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'NV');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB001', N'Thông báo họp toàn viện ngày 05/05/2025', SYSTIMESTAMP, N'Hội trường lớn');

-- t2: BGD (tag=1002)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'BGD');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB002', N'Họp khẩn Ban Giám đốc - Kế hoạch mở rộng 2025', SYSTIMESTAMP, N'Phòng họp BGD');

-- t3: LDK (tag=1003)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'LDK');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB003', N'Họp lãnh đạo khoa - Báo cáo quý 2', SYSTIMESTAMP, N'Phòng họp A');

-- t4: LDK::TH (tag=1004)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'LDK::TH');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB004', N'Họp lãnh đạo Khoa Tiêu hóa - Cải tiến quy trình', SYSTIMESTAMP, N'Phòng D2.01');

-- t5: NV:HCM:TH (tag=1005)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'NV:HCM:TH');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB005', N'Tập huấn nội soi tiêu hóa - Cơ sở HCM', SYSTIMESTAMP, N'Phòng kỹ năng HCM');

-- t6: NV:HNI:TH (tag=1006)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'NV:HNI:TH');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB006', N'Tập huấn nội soi tiêu hóa - Cơ sở Hà Nội', SYSTIMESTAMP, N'Phòng kỹ năng HN');

-- t7: LDK:HPN:TH,TK (tag=1007)
SA_SESSION.SET_ROW_LABEL('BV_LABEL_POLICY', 'LDK:HPN:TH,TK');
INSERT INTO THONGBAO(MATB, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB007', N'Họp khẩn Lãnh đạo Khoa TH và TK - Cơ sở Hải Phòng', SYSTIMESTAMP, N'Phòng họp HP');

COMMIT;

-- Khôi phục session label về mặc định
SA_SESSION.RESTORE_DEFAULT_LABELS('BV_LABEL_POLICY');

-- ============================================================
-- BƯỚC 9: Kiểm thử - mỗi user thấy thông báo nào?
-- ============================================================

-- u1 (BGD) - thấy tất cả t1-t7
CONNECT u1_giamdoc/U1@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả: 7 dòng

-- u2 (LDK, HCM, TM) - thấy t1, t3 (không thấy t4 vì group TH, không thấy t5/t6/t7)
CONNECT u2_ldtm_hcm/U2@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB003 (t3)

-- u3 (LDK, HNI, TK) - thấy t1, t3 (t6 yêu cầu group TH, t7 cần HPN)
CONNECT u3_ldtk_hni/U3@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB003 (t3)

-- u4 (NV, HCM, TK) - chỉ thấy t1 (group TK, không phải TH)
CONNECT u4_nvtk_hcm/U4@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1)

-- u5 (NV, HCM, TM) - chỉ thấy t1
CONNECT u5_nvtm_hcm/U5@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1)

-- u7 (LDK, mọi cơ sở, mọi khoa) - thấy t1, t3, t4, t5, t6, t7 (trừ t2=BGD)
CONNECT u7_ldp_all/U7@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001,TB003,TB004,TB005,TB006,TB007

-- u8 (NV, HNI, TH) - thấy t1 và t6
CONNECT u8_nvth_hni/U8@2025;
SELECT MATB, SUBSTR(NOIDUNG,1,60) AS NOIDUNG FROM BVADMIN.THONGBAO;
-- Kết quả mong đợi: TB001 (t1), TB006 (t6)

-- Xem cấu hình nhãn
CONNECT LBACSYS/lbacsys;
SELECT * FROM DBA_SA_LEVELS         WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_COMPARTMENTS   WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_GROUPS         WHERE POLICY_NAME = 'BV_LABEL_POLICY';
SELECT * FROM DBA_SA_USER_LABELS    WHERE POLICY_NAME = 'BV_LABEL_POLICY';
