-- ============================================================
-- PHÂN HỆ 2a: RBAC - Doctor_Role
-- ============================================================
-- Mục tiêu:
--   Tạo role Doctor_Role với quyền SELECT/INSERT/UPDATE trên
--   Medical_Record và Appointment. Tạo user bác sĩ, gán role
--   và kiểm thử quyền.
-- Chạy với quyền DBA (SYSTEM) sau khi đã chạy 00_setup.sql
-- ============================================================

CONNECT SYSTEM/oracle;

-- ============================================================
-- BƯỚC 1: Tạo role Doctor_Role
-- ============================================================
CREATE ROLE Doctor_Role;

-- ============================================================
-- BƯỚC 2: Cấp quyền cho Doctor_Role
-- Theo yêu cầu: SELECT, INSERT, UPDATE trên Medical_Record
--               SELECT, INSERT, UPDATE trên Appointment
-- ============================================================
GRANT SELECT ON HOSPITAL_ADMIN.Medical_Record TO Doctor_Role;
GRANT INSERT ON HOSPITAL_ADMIN.Medical_Record TO Doctor_Role;
GRANT UPDATE ON HOSPITAL_ADMIN.Medical_Record TO Doctor_Role;

GRANT SELECT ON HOSPITAL_ADMIN.Appointment    TO Doctor_Role;
GRANT INSERT ON HOSPITAL_ADMIN.Appointment    TO Doctor_Role;
GRANT UPDATE ON HOSPITAL_ADMIN.Appointment    TO Doctor_Role;

-- Doctor cũng cần xem danh sách bệnh nhân mình phụ trách
-- (chỉ xem, không sửa)
GRANT SELECT ON HOSPITAL_ADMIN.Patient TO Doctor_Role;

-- ============================================================
-- BƯỚC 3: Tạo user bác sĩ và gán Doctor_Role
-- ============================================================
CREATE USER doctor_nguyen IDENTIFIED BY Doc@12345
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO doctor_nguyen;
GRANT Doctor_Role    TO doctor_nguyen;

-- Tạo thêm một bác sĩ thứ hai để test
CREATE USER doctor_tran IDENTIFIED BY Doc@12345
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO doctor_tran;
GRANT Doctor_Role    TO doctor_tran;

-- ============================================================
-- BƯỚC 4: Kiểm tra cấu hình role trong data dictionary
-- ============================================================
-- Xem toàn bộ quyền đã gán cho Doctor_Role
SELECT GRANTEE, PRIVILEGE, TABLE_NAME, GRANTABLE
FROM   DBA_TAB_PRIVS
WHERE  GRANTEE = 'DOCTOR_ROLE'
ORDER  BY TABLE_NAME, PRIVILEGE;

-- Xem role đã gán cho các user
SELECT GRANTEE, GRANTED_ROLE, DEFAULT_ROLE
FROM   DBA_ROLE_PRIVS
WHERE  GRANTEE IN ('DOCTOR_NGUYEN', 'DOCTOR_TRAN');

-- ============================================================
-- BƯỚC 5: Kiểm thử quyền (chạy với doctor_nguyen)
-- ============================================================
CONNECT doctor_nguyen/Doc@12345;

-- Test 1: Xem role đang active trong session
SELECT * FROM SESSION_ROLES;
-- Kết quả mong đợi: DOCTOR_ROLE

-- Test 2: SELECT Medical_Record - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Medical_Record;

-- Test 3: INSERT Appointment - HỢP LỆ
INSERT INTO HOSPITAL_ADMIN.Appointment (ID, Patient_ID, Doctor_ID, Appt_Date, Status, Notes)
VALUES (99, 1, 1, SYSDATE, 'Pending', 'Test by doctor_nguyen');
COMMIT;

-- Test 4: UPDATE Medical_Record - HỢP LỆ
UPDATE HOSPITAL_ADMIN.Medical_Record
SET    Treatment = 'Metformin 1000mg daily (updated)'
WHERE  ID = 1;
COMMIT;

-- Test 5: DELETE Medical_Record - KHÔNG HỢP LỆ (Doctor không có DELETE)
-- Lệnh dưới đây sẽ báo lỗi ORA-01031: insufficient privileges
DELETE FROM HOSPITAL_ADMIN.Medical_Record WHERE ID = 1;

-- Test 6: INSERT Patient - KHÔNG HỢP LỆ (Doctor không có quyền ghi Patient)
INSERT INTO HOSPITAL_ADMIN.Patient VALUES (99,'Test','01-JAN-00','Test','None','Public');

-- Rollback test data
ROLLBACK;

-- ============================================================
-- GIẢI THÍCH
-- ============================================================
-- Doctor_Role được thiết kế theo nguyên tắc least privilege:
-- - Chỉ cấp đúng quyền cần thiết để bác sĩ ghi nhận và
--   cập nhật hồ sơ khám bệnh.
-- - Không có DELETE để bảo toàn lịch sử y tế.
-- - Không INSERT/UPDATE Patient để tách biệt trách nhiệm:
--   thông tin hành chính do Admin quản lý.
-- ============================================================
