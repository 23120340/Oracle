-- ============================================================
-- PHÂN HỆ 2b: RBAC - Nurse_Role
-- ============================================================
-- Mục tiêu:
--   Tạo role Nurse_Role với quyền hạn chế hơn Doctor_Role:
--   - Chỉ SELECT trên Patient (qua VIEW, loại bỏ Medical_History)
--   - SELECT/UPDATE trên Appointment (chỉ cột Notes)
--   - SELECT/UPDATE trên Medication (chỉ cột Dosage)
--   - Không truy cập Medical_Record đầy đủ
-- Chạy với quyền DBA (SYSTEM) sau khi đã chạy 00_setup.sql
-- ============================================================

-- ============================================================
-- BƯỚC 1: Tạo VIEW hạn chế cột trên Patient
-- Y tá chỉ được xem Name, DOB, Address - không xem Medical_History
-- ============================================================
CONNECT HOSPITAL_ADMIN/Admin@12345;

CREATE OR REPLACE VIEW Patient_Nurse_View AS
SELECT
    ID,
    Name,
    DOB,
    Address
    -- Medical_History và Sensitivity_Level bị ẩn khỏi y tá
FROM Patient;

-- VIEW cho phép y tá cập nhật Notes của Appointment
-- (Dùng INSTEAD OF trigger nếu cần giới hạn cột UPDATE)
CREATE OR REPLACE VIEW Appointment_Nurse_View AS
SELECT ID, Patient_ID, Doctor_ID, Appt_Date, Status, Notes
FROM   Appointment;

-- VIEW cho phép y tá xem/cập nhật Dosage của Medication
CREATE OR REPLACE VIEW Medication_Nurse_View AS
SELECT ID, Name, Dosage, Patient_ID, Prescribed_By
FROM   Medication;

-- VIEW giới hạn Medical_Record: chỉ xem nếu Status = 'Pending'
-- (Y tá không được đọc hồ sơ đã hoàn chỉnh)
CREATE OR REPLACE VIEW MedRecord_Nurse_View AS
SELECT mr.*
FROM   Medical_Record mr
JOIN   Appointment    a  ON mr.Patient_ID = a.Patient_ID
                        AND mr.Doctor_ID  = a.Doctor_ID
WHERE  a.Status = 'Pending';

-- ============================================================
-- BƯỚC 2: Tạo INSTEAD OF TRIGGER để giới hạn UPDATE chỉ Notes
-- Khi y tá UPDATE qua view, chỉ cho phép sửa cột Notes
-- ============================================================
CREATE OR REPLACE TRIGGER trg_nurse_appt_update
INSTEAD OF UPDATE ON Appointment_Nurse_View
FOR EACH ROW
BEGIN
    -- Chỉ cho phép cập nhật cột Notes
    IF :NEW.Notes != :OLD.Notes THEN
        UPDATE Appointment
        SET    Notes = :NEW.Notes
        WHERE  ID    = :OLD.ID;
    END IF;
    -- Bỏ qua mọi thay đổi trên các cột khác (Status, Date...)
END;
/

-- ============================================================
-- BƯỚC 3: Tạo Nurse_Role và cấp quyền trên các VIEW
-- ============================================================
CONNECT SYSTEM/oracle;

CREATE ROLE Nurse_Role;

-- Grant quyền trên VIEW (không phải bảng gốc)
GRANT SELECT          ON HOSPITAL_ADMIN.Patient_Nurse_View    TO Nurse_Role;
GRANT SELECT, UPDATE  ON HOSPITAL_ADMIN.Appointment_Nurse_View TO Nurse_Role;
GRANT SELECT, UPDATE  ON HOSPITAL_ADMIN.Medication_Nurse_View  TO Nurse_Role;
GRANT SELECT          ON HOSPITAL_ADMIN.MedRecord_Nurse_View   TO Nurse_Role;

-- ============================================================
-- BƯỚC 4: Tạo user y tá và gán Nurse_Role
-- ============================================================
CREATE USER nurse_tran IDENTIFIED BY Nurse@12345
    DEFAULT TABLESPACE USERS
    QUOTA UNLIMITED ON USERS;

GRANT CREATE SESSION TO nurse_tran;
GRANT Nurse_Role     TO nurse_tran;

-- ============================================================
-- BƯỚC 5: Kiểm tra cấu hình role
-- ============================================================
SELECT GRANTEE, PRIVILEGE, TABLE_NAME, GRANTABLE
FROM   DBA_TAB_PRIVS
WHERE  GRANTEE = 'NURSE_ROLE'
ORDER  BY TABLE_NAME, PRIVILEGE;

SELECT GRANTEE, GRANTED_ROLE, DEFAULT_ROLE
FROM   DBA_ROLE_PRIVS
WHERE  GRANTEE = 'NURSE_TRAN';

-- ============================================================
-- BƯỚC 6: Kiểm thử quyền (chạy với nurse_tran)
-- ============================================================
CONNECT nurse_tran/Nurse@12345;

-- Test 1: Xem role active
SELECT * FROM SESSION_ROLES;
-- Kết quả mong đợi: NURSE_ROLE

-- Test 2: SELECT Patient qua VIEW (chỉ thấy Name, DOB, Address) - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Patient_Nurse_View;

-- Test 3: Thử đọc Medical_History trực tiếp - KHÔNG HỢP LỆ
-- Lỗi: ORA-01031: insufficient privileges
SELECT Medical_History FROM HOSPITAL_ADMIN.Patient;

-- Test 4: SELECT Appointment - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.Appointment_Nurse_View;

-- Test 5: UPDATE Notes của Appointment - HỢP LỆ
UPDATE HOSPITAL_ADMIN.Appointment_Nurse_View
SET    Notes = 'Nurse cập nhật: bệnh nhân đã được lấy sinh hiệu'
WHERE  ID = 2;
COMMIT;

-- Test 6: Thử UPDATE Status (bị INSTEAD OF trigger chặn lại) - BỊ CHẶN
UPDATE HOSPITAL_ADMIN.Appointment_Nurse_View
SET    Status = 'Completed'
WHERE  ID = 2;
-- Trigger bỏ qua thay đổi Status, không báo lỗi nhưng không áp dụng

-- Test 7: SELECT Medical_Record với Status=Pending - HỢP LỆ
SELECT * FROM HOSPITAL_ADMIN.MedRecord_Nurse_View;

-- Test 8: INSERT vào Medical_Record - KHÔNG HỢP LỆ
INSERT INTO HOSPITAL_ADMIN.Medical_Record VALUES (99,1,1,'Test','Test',SYSDATE);
-- Lỗi: ORA-01031: insufficient privileges

ROLLBACK;

-- ============================================================
-- GIẢI THÍCH
-- ============================================================
-- Nurse_Role sử dụng kết hợp hai cơ chế:
-- 1. VIEW: ẩn các cột nhạy cảm (Medical_History) khỏi y tá
--    bằng cách chỉ expose những cột được phép trong view.
-- 2. INSTEAD OF TRIGGER: kiểm soát UPDATE cột - trigger xử
--    lý UPDATE trên view và chỉ áp dụng thay đổi đối với
--    cột Notes, bỏ qua mọi thay đổi khác.
-- Cách này mạnh hơn GRANT thuần túy vì Oracle không hỗ trợ
-- GRANT UPDATE trên từng cột riêng lẻ qua role (chỉ trực tiếp).
-- ============================================================
