-- ============================================================
-- SETUP: Hệ thống Quản lý Bệnh viện - Schema & Dữ liệu mẫu
-- Chạy file này với quyền DBA (SYS hoặc SYSTEM) trước tất cả
-- ============================================================

-- 1. Tạo schema owner
CREATE USER HOSPITAL_ADMIN IDENTIFIED BY Admin@12345
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CONNECT, RESOURCE, CREATE VIEW, CREATE ROLE TO HOSPITAL_ADMIN;
GRANT CREATE USER TO HOSPITAL_ADMIN;

-- ============================================================
-- 2. Tạo bảng (chạy với HOSPITAL_ADMIN)
-- ============================================================
CONNECT HOSPITAL_ADMIN/Admin@12345;

CREATE TABLE Patient (
    ID               NUMBER PRIMARY KEY,
    Name             VARCHAR2(100)  NOT NULL,
    DOB              DATE,
    Address          VARCHAR2(200),
    Medical_History  VARCHAR2(500),
    Sensitivity_Level VARCHAR2(20)
        CHECK (Sensitivity_Level IN ('Public', 'Confidential', 'Secret'))
);

CREATE TABLE Doctor (
    ID          NUMBER PRIMARY KEY,
    Name        VARCHAR2(100) NOT NULL,
    Specialty   VARCHAR2(100),
    Department  VARCHAR2(100)
);

CREATE TABLE Appointment (
    ID          NUMBER PRIMARY KEY,
    Patient_ID  NUMBER REFERENCES Patient(ID),
    Doctor_ID   NUMBER REFERENCES Doctor(ID),
    Appt_Date   DATE,
    Status      VARCHAR2(20)
        CHECK (Status IN ('Pending', 'Completed', 'Cancelled')),
    Notes       VARCHAR2(500)
);

CREATE TABLE Medical_Record (
    ID          NUMBER PRIMARY KEY,
    Patient_ID  NUMBER REFERENCES Patient(ID),
    Doctor_ID   NUMBER REFERENCES Doctor(ID),
    Diagnosis   VARCHAR2(500),
    Treatment   VARCHAR2(500),
    Record_Date DATE
);

CREATE TABLE Medication (
    ID            NUMBER PRIMARY KEY,
    Name          VARCHAR2(100) NOT NULL,
    Dosage        VARCHAR2(100),
    Patient_ID    NUMBER REFERENCES Patient(ID),
    Prescribed_By NUMBER REFERENCES Doctor(ID)
);

-- ============================================================
-- 3. Dữ liệu mẫu
-- ============================================================
INSERT INTO Doctor VALUES (1, 'BS. Nguyen Van An', 'Cardiology',  'Cardiology');
INSERT INTO Doctor VALUES (2, 'BS. Tran Thi Bich', 'Neurology',   'Neurology');
INSERT INTO Doctor VALUES (3, 'BS. Le Van Cuong',  'General',     'Cardiology');

INSERT INTO Patient VALUES (1, 'Pham Thi Dung',  DATE '1985-03-10', 'Hanoi',    'Diabetes, Hypertension',   'Secret');
INSERT INTO Patient VALUES (2, 'Nguyen Van Em',   DATE '1990-07-22', 'HCM City', 'Asthma',                   'Confidential');
INSERT INTO Patient VALUES (3, 'Hoang Thi Phuong',DATE '2000-01-15', 'Da Nang',  'No chronic conditions',    'Public');

INSERT INTO Appointment VALUES (1, 1, 1, DATE '2025-05-01', 'Completed', 'Checkup done');
INSERT INTO Appointment VALUES (2, 2, 1, DATE '2025-05-03', 'Pending',   'Follow-up needed');
INSERT INTO Appointment VALUES (3, 3, 2, DATE '2025-05-05', 'Pending',   'First visit');
INSERT INTO Appointment VALUES (4, 1, 2, DATE '2025-05-10', 'Cancelled', 'Patient request');

INSERT INTO Medical_Record VALUES (1, 1, 1, 'Type 2 Diabetes',   'Metformin 500mg daily',   DATE '2025-05-01');
INSERT INTO Medical_Record VALUES (2, 2, 1, 'Mild Hypertension',  'Amlodipine 5mg daily',    DATE '2025-05-03');
INSERT INTO Medical_Record VALUES (3, 3, 2, 'Tension Headache',   'Rest and Paracetamol',    DATE '2025-05-05');

INSERT INTO Medication VALUES (1, 'Metformin',   '500mg',  1, 1);
INSERT INTO Medication VALUES (2, 'Amlodipine',  '5mg',    2, 1);
INSERT INTO Medication VALUES (3, 'Paracetamol', '500mg',  3, 2);

COMMIT;
