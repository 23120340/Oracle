-- ============================================================
-- PHÂN HỆ 2c: RBAC - Manager_Role
-- ============================================================
-- Mục tiêu:
--   Tạo role Manager_Role chỉ cho phép SELECT trên báo cáo
--   tổng hợp thông qua VIEW - không truy cập bảng gốc, không
--   xem Medical_History hay Diagnosis (thông tin y tế nhạy cảm).
-- Chạy với quyền DBA (SYSTEM) sau khi đã chạy 00_setup.sql
-- ============================================================

-- ============================================================
-- BƯỚC 1: Tạo các VIEW tổng hợp cho Manager (chạy bởi HOSPITAL_ADMIN)
-- ============================================================
CONNECT HOSPITAL_ADMIN/Admin@12345;

-- VIEW 1: Tổng quan lịch khám - không có thông tin y tế chi tiết
CREATE OR REPLACE VIEW Manager_Appointment_View AS
SELECT
    a.ID            AS Appointment_ID,
    p.Name          AS Patient_Name,
    -- Không bao gồm: Medical_History, Diagnosis, Treatment
    d.Name          AS Doctor_Name,
    d.Department,
    a.Appt_Date,
    a.Status,
    a.Notes
FROM Appointment a
JOIN Patient p ON a.Patient_ID = p.ID
JOIN Doctor  d ON a.Doctor_ID  = d.ID;

-- VIEW 2: Thống kê theo khoa/phòng ban (aggregated - không có dữ liệu cá nhân)
CREATE OR REPLACE VIEW Manager_Dept_Stats_View AS
SELECT
    d.Department,
    COUNT(DISTINCT a.Patient_ID)                                   AS Total_Patients,
    COUNT(a.ID)                                                    AS Total_Appointments,
    SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)       AS Completed,
    SUM(CASE WHEN a.Status = 'Pending'   THEN 1 ELSE 0 END)       AS Pending,
    SUM(CASE WHEN a.Status = 'Cancelled' THEN 1 ELSE 0 END)       AS Cancelled,
    ROUND(
        SUM(CASE WHEN a.Status = 'Completed' THEN 1 ELSE 0 END)
        / NULLIF(COUNT(a.ID), 0) * 100, 2
    )                                                              AS Completion_Rate_Pct
FROM Appointment a
JOIN Doctor d ON a.Doctor_ID = d.ID
GROUP BY d.Department;

-- VIEW 3: Thống kê thuốc theo khoa (chỉ tổng hợp, không có Patient_ID)
CREATE OR REPLACE VIEW Manager_Medication_View AS
SELECT
    d.Department,
    m.Name     AS Medication_Name,
    m.Dosage,
    -- Không bao gồm Patient_ID để bảo vệ quyền riêng tư
    COUNT(*)   AS Prescription_Count
FROM Medication m
JOIN Doctor d ON m.Prescribed_By = d.ID
GROUP BY d.Department, m.Name, m.Dosage;

-- VIEW 4: Báo cáo bác sĩ trong cùng department (Manager xem được)
CREATE OR REPLACE VIEW Manager_Doctor_View AS
SELECT
    ID          AS Doctor_ID,
    Name        AS Doctor_Name,
    Specialty,
    Department
FROM Doctor;

-- ============================================================
-- BƯỚC 2: Tạo Manager_Role và cấp quyền ONLY trên VIEW
-- ============================================================
CONNECT SYSTEM/oracle;

CREATE ROLE Manager_Role;

-- Chỉ grant SELECT trên VIEW, không phải bảng gốc
GRANT SELECT ON HOSPITAL_ADMIN.Manager_Appointment_View  TO Manager_Role;
GRANT SELECT ON HOSPITAL_ADMIN.Manager_Dept_Stats_View   TO Manager_Role;
GRANT SELECT ON HOSPITAL_ADMIN.Manager_Medication_View   TO Manager_Role;
GRANT SELECT ON HOSPITAL_ADMIN.Manager_Doctor_View       TO Manager_Role;

-- ============================================================
-- BƯỚC 3: Tạo user quản lý bệnh viện và gán Manager_Role
-- ============================================================
CREATE USER manager_le IDENTIFIED BY Manager@12345
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO manager_le;
GRANT Manager_Role   TO manager_le;

-- ============================================================
-- BƯỚC 4: Kiểm tra cấu hình role
-- ============================================================
SELECT GRANTEE, PRIVILEGE, TABLE_NAME, GRANTABLE
FROM   DBA_TAB_PRIVS
WHERE  GRANTEE = 'MANAGER_ROLE'
ORDER  BY TABLE_NAME, PRIVILEGE;

SELECT GRANTEE, GRANTED_ROLE, DEFAULT_ROLE
FROM   DBA_ROLE_PRIVS
WHERE  GRANTEE = 'MANAGER_LE';

-- ============================================================
-- BƯỚC 5: Kiểm thử quyền (chạy với manager_le)
-- ============================================================
CONNECT manager_le/Manager@12345;

-- Test 1: Xem role active
SELECT * FROM SESSION_ROLES;
-- Kết quả mong đợi: MANAGER_ROLE

-- Test 2: Xem lịch khám tổng hợp - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Manager_Appointment_View;

-- Test 3: Xem thống kê theo khoa - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Manager_Dept_Stats_View;

-- Test 4: Xem thống kê thuốc - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Manager_Medication_View;

-- Test 5: Truy cập bảng Patient trực tiếp - KHÔNG HỢP LỆ
-- Lỗi: ORA-01031: insufficient privileges
SELECT * FROM HOSPITAL_ADMIN.Patient;

-- Test 6: Truy cập Medical_Record - KHÔNG HỢP LỆ
-- Lỗi: ORA-01031: insufficient privileges
SELECT * FROM HOSPITAL_ADMIN.Medical_Record;

-- Test 7: Thử INSERT qua VIEW - KHÔNG HỢP LỆ
-- Lỗi: ORA-01031: insufficient privileges (không có INSERT grant)
INSERT INTO HOSPITAL_ADMIN.Manager_Doctor_View VALUES (99, 'Test', 'Test', 'Test');

-- Test 8: Query hữu ích - lọc theo department cụ thể
SELECT *
FROM   HOSPITAL_ADMIN.Manager_Appointment_View
WHERE  Department = 'Cardiology'
ORDER  BY Appt_Date;

-- ============================================================
-- GIẢI THÍCH
-- ============================================================
-- Manager_Role áp dụng nguyên tắc "need-to-know":
-- 1. Chỉ GRANT trên VIEW, không grant trực tiếp trên bảng
--    → Manager không thể bypass view để đọc dữ liệu thô.
-- 2. VIEW loại bỏ các cột y tế nhạy cảm (Medical_History,
--    Diagnosis, Treatment) trước khi expose cho Manager.
-- 3. VIEW thống kê (GROUP BY) thêm lớp bảo vệ: Manager chỉ
--    thấy số liệu tổng hợp, không thấy từng bản ghi cá nhân.
-- 4. Không có INSERT/UPDATE/DELETE → Manager chỉ đọc, không
--    thể chỉnh sửa dữ liệu y tế.
-- ============================================================
