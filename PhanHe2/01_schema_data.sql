-- ============================================================
-- PHÂN HỆ 2 - Ứng dụng Quản lý Dữ liệu Y tế
-- File 01: Tạo Schema và Dữ liệu mẫu
-- Chạy với quyền DBA (SYSTEM/SYS)
-- ============================================================

-- ============================================================
-- BƯỚC 1: Tạo schema owner cho bệnh viện
-- ============================================================
CREATE USER BVADMIN IDENTIFIED BY BVAdmin@2025
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CONNECT, RESOURCE              TO BVADMIN;
GRANT CREATE VIEW                    TO BVADMIN;
GRANT CREATE PROCEDURE               TO BVADMIN;
GRANT CREATE SEQUENCE                TO BVADMIN;
GRANT CREATE TRIGGER                 TO BVADMIN;
GRANT CREATE ANY CONTEXT             TO BVADMIN;
GRANT EXECUTE ON DBMS_RLS            TO BVADMIN;
GRANT EXECUTE ON DBMS_SESSION        TO BVADMIN;

-- ============================================================
-- BƯỚC 2: Tạo bảng dữ liệu (chạy bởi BVADMIN)
-- ============================================================
CONNECT BVADMIN/BVAdmin@2025;

-- BỆNHNHÂN: thông tin bệnh nhân
-- Cột ORACLE_USER: lưu tên tài khoản Oracle → kết nối 1 bảng (TC#1)
CREATE TABLE BENHNHAN (
    MABN         VARCHAR2(10)    PRIMARY KEY,
    TENBN        NVARCHAR2(100)  NOT NULL,
    PHAI         CHAR(1)         CHECK (PHAI IN ('M','F')),
    NGAYSINH     DATE,
    CCCD         VARCHAR2(12)    UNIQUE NOT NULL,
    SONHA        NVARCHAR2(20),
    TENDUONG     NVARCHAR2(100),
    QUANHUYEN    NVARCHAR2(100),
    TINHTP       NVARCHAR2(100),
    TIENSUBENH   NCLOB,
    TIENSUBENHGD NCLOB,
    DIUNGTHUOC   NVARCHAR2(500),
    ORACLE_USER  VARCHAR2(100)   UNIQUE  -- ánh xạ 1-1 với Oracle account
);

-- NHÂNVIÊN: tất cả nhân viên bệnh viện
-- ORACLE_USER: ánh xạ 1-1 với Oracle account (TC#1)
CREATE TABLE NHANVIEN (
    MANV         VARCHAR2(10)    PRIMARY KEY,
    HOTEN        NVARCHAR2(100)  NOT NULL,
    PHAI         CHAR(1)         CHECK (PHAI IN ('M','F')),
    NGAYSINH     DATE,
    CMND         VARCHAR2(12)    UNIQUE NOT NULL,
    QUEQUAN      NVARCHAR2(200),
    SODT         VARCHAR2(15),
    VAITRO       VARCHAR2(20)    NOT NULL
        CHECK (VAITRO IN ('DPV','BS','KTV')),
        -- DPV = Điều phối viên, BS = Bác sĩ/Y sĩ, KTV = Kỹ thuật viên
    CHUYENKHOA   NVARCHAR2(100),
    ORACLE_USER  VARCHAR2(100)   UNIQUE  -- ánh xạ 1-1 với Oracle account
);

-- HSBA: Hồ sơ bệnh án
CREATE TABLE HSBA (
    MAHSBA   VARCHAR2(10)  PRIMARY KEY,
    MABN     VARCHAR2(10)  NOT NULL REFERENCES BENHNHAN(MABN),
    NGAY     DATE          NOT NULL,
    CHANDOAN NCLOB,
    DIEUTRI  NCLOB,
    MABS     VARCHAR2(10)  REFERENCES NHANVIEN(MANV),  -- bác sĩ điều trị
    MAKHOA   NVARCHAR2(50),
    KETLUAN  NCLOB
);

-- HSBA_DV: Dịch vụ hỗ trợ chẩn đoán trong hồ sơ bệnh án
CREATE TABLE HSBA_DV (
    MAHSBA  VARCHAR2(10)    NOT NULL REFERENCES HSBA(MAHSBA),
    LOAIDV  NVARCHAR2(100)  NOT NULL,
    NGAYDV  DATE            NOT NULL,
    MAKTV   VARCHAR2(10)    REFERENCES NHANVIEN(MANV),  -- kỹ thuật viên
    KETQUA  NCLOB,
    CONSTRAINT PK_HSBA_DV PRIMARY KEY (MAHSBA, LOAIDV, NGAYDV)
);

-- ĐƠNTHUỐC: đơn thuốc theo hồ sơ bệnh án
CREATE TABLE DONTHUOC (
    MAHSBA   VARCHAR2(10)    NOT NULL REFERENCES HSBA(MAHSBA),
    NGAYDT   DATE            NOT NULL,
    TENTHUOC NVARCHAR2(200)  NOT NULL,
    LIEUDUNG NVARCHAR2(200),
    CONSTRAINT PK_DONTHUOC PRIMARY KEY (MAHSBA, NGAYDT, TENTHUOC)
);

-- THÔNG BÁO: dùng cho OLS (Yêu cầu 2)
CREATE TABLE THONGBAO (
    MATB     VARCHAR2(10)    PRIMARY KEY,
    NOIDUNG  NCLOB           NOT NULL,
    NGAYGIO  TIMESTAMP       DEFAULT SYSTIMESTAMP,
    DIADIEM  NVARCHAR2(200)
    -- Cột nhãn OLS (OLS_LABEL) sẽ được thêm tự động bởi SA_POLICY_ADMIN
);

-- ============================================================
-- BƯỚC 3: Dữ liệu mẫu
-- ============================================================

-- Nhân viên mẫu (2 DPV, 3 BS, 2 KTV)
INSERT INTO NHANVIEN VALUES ('NV001','Nguyen Van An','M',DATE'1980-05-10','201234567890','HCM','0901234567','DPV',N'Tiếp nhận',NULL);
INSERT INTO NHANVIEN VALUES ('NV002','Tran Thi Bich','F',DATE'1985-03-22','202345678901','HN','0912345678','DPV',N'Tiếp nhận',NULL);
INSERT INTO NHANVIEN VALUES ('NV003','Le Van Cuong','M',DATE'1975-11-15','203456789012','HCM','0923456789','BS',N'Tim mạch',NULL);
INSERT INTO NHANVIEN VALUES ('NV004','Pham Thi Dung','F',DATE'1982-07-30','204567890123','HN','0934567890','BS',N'Thần kinh',NULL);
INSERT INTO NHANVIEN VALUES ('NV005','Hoang Van Em','M',DATE'1990-02-14','205678901234','Da Nang','0945678901','BS',N'Tiêu hóa',NULL);
INSERT INTO NHANVIEN VALUES ('NV006','Vu Thi Phuong','F',DATE'1992-09-08','206789012345','HCM','0956789012','KTV',N'Xét nghiệm',NULL);
INSERT INTO NHANVIEN VALUES ('NV007','Do Van Quang','M',DATE'1988-12-25','207890123456','HN','0967890123','KTV',N'Chẩn đoán hình ảnh',NULL);

-- Bệnh nhân mẫu
INSERT INTO BENHNHAN(MABN,TENBN,PHAI,NGAYSINH,CCCD,SONHA,TENDUONG,QUANHUYEN,TINHTP,TIENSUBENH,DIUNGTHUOC,ORACLE_USER)
VALUES('BN001',N'Mai Thi Hoa','F',DATE'1970-04-20','300112345678','12',N'Lê Lợi',N'Q.1','HCM',N'Tiểu đường type 2',N'Penicillin',NULL);
INSERT INTO BENHNHAN(MABN,TENBN,PHAI,NGAYSINH,CCCD,SONHA,TENDUONG,QUANHUYEN,TINHTP,TIENSUBENH,DIUNGTHUOC,ORACLE_USER)
VALUES('BN002',N'Nguyen Van Binh','M',DATE'1985-08-12','300223456789','5A',N'Trần Hưng Đạo',N'Q.5','HCM',N'Không',NULL,NULL);
INSERT INTO BENHNHAN(MABN,TENBN,PHAI,NGAYSINH,CCCD,SONHA,TENDUONG,QUANHUYEN,TINHTP,TIENSUBENH,DIUNGTHUOC,ORACLE_USER)
VALUES('BN003',N'Tran Thi Cam','F',DATE'1995-01-30','300334567890','88',N'Hoàng Diệu',N'Hải Châu','Da Nang',N'Hen suyễn',NULL,NULL);

-- Hồ sơ bệnh án
INSERT INTO HSBA VALUES('HS001','BN001',DATE'2025-04-01',N'Đái tháo đường type 2 biến chứng',N'Insulin + Metformin','NV003',N'Tim mạch',NULL);
INSERT INTO HSBA VALUES('HS002','BN002',DATE'2025-04-05',N'Đau đầu căng thẳng',N'Nghỉ ngơi + Paracetamol','NV004',N'Thần kinh',N'Ổn định');
INSERT INTO HSBA VALUES('HS003','BN003',DATE'2025-04-10',N'Hen phế quản',N'Ventolin + Corticoid khí dung','NV005',N'Tiêu hóa',NULL);
INSERT INTO HSBA VALUES('HS004','BN001',DATE'2025-04-15',N'Cao huyết áp',N'Amlodipine 5mg',   'NV003',N'Tim mạch',NULL);

-- Dịch vụ hỗ trợ chẩn đoán
INSERT INTO HSBA_DV VALUES('HS001',N'Xét nghiệm máu tổng quát',DATE'2025-04-01','NV006',N'Glucose: 12.5 mmol/L, HbA1c: 9%');
INSERT INTO HSBA_DV VALUES('HS001',N'Siêu âm tim',             DATE'2025-04-02','NV007',N'Tim bình thường, EF 65%');
INSERT INTO HSBA_DV VALUES('HS002',N'Điện não đồ',             DATE'2025-04-05','NV007',NULL);
INSERT INTO HSBA_DV VALUES('HS003',N'Đo chức năng hô hấp',     DATE'2025-04-10','NV006',NULL);

-- Đơn thuốc
INSERT INTO DONTHUOC VALUES('HS001',DATE'2025-04-01',N'Metformin 500mg',N'2 lần/ngày sau ăn');
INSERT INTO DONTHUOC VALUES('HS001',DATE'2025-04-01',N'Insulin Glargine',N'10 đơn vị, tiêm dưới da tối');
INSERT INTO DONTHUOC VALUES('HS002',DATE'2025-04-05',N'Paracetamol 500mg',N'3 lần/ngày khi đau');
INSERT INTO DONTHUOC VALUES('HS003',DATE'2025-04-10',N'Ventolin 100mcg',N'2 nhát xịt khi cần');

COMMIT;
