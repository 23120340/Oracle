-- ============================================================
-- PHÂN HỆ 2d: RBAC - Session Roles & Dynamic RBAC
-- ============================================================
-- Mục tiêu:
--   Triển khai session roles để một Doctor có thể switch sang
--   Nurse_Role tạm thời trong phiên làm việc. Minh họa cách
--   SET ROLE hoạt động và nhận xét về dynamic RBAC.
-- Chạy sau khi đã chạy 2a và 2b (Doctor_Role, Nurse_Role đã tồn tại)
-- ============================================================

-- ============================================================
-- BƯỚC 1: Cấu hình - Grant thêm Nurse_Role cho doctor_nguyen
-- Doctor giữ cả 2 role nhưng có thể switch giữa chúng
-- ============================================================
CONNECT SYSTEM/oracle;

-- Grant Nurse_Role cho doctor_nguyen (thêm vào Doctor_Role đã có)
GRANT Nurse_Role TO doctor_nguyen;

-- Xác nhận: doctor_nguyen hiện có 2 role
SELECT GRANTEE, GRANTED_ROLE, DEFAULT_ROLE
FROM   DBA_ROLE_PRIVS
WHERE  GRANTEE = 'DOCTOR_NGUYEN';
-- Kết quả mong đợi: DOCTOR_ROLE (YES), NURSE_ROLE (YES)

-- ============================================================
-- BƯỚC 2: Kết nối với doctor_nguyen - trạng thái mặc định
-- Mặc định: tất cả DEFAULT_ROLE = YES đều active
-- ============================================================
CONNECT doctor_nguyen/Doc@12345;

-- Kiểm tra các role đang active trong session
SELECT * FROM SESSION_ROLES;
-- Kết quả mong đợi: DOCTOR_ROLE, NURSE_ROLE (cả hai active)

-- Quyền tổng hợp từ cả 2 role → toàn bộ quyền Doctor + Nurse
SELECT * FROM HOSPITAL_ADMIN.Medical_Record;    -- Từ Doctor_Role
SELECT * FROM HOSPITAL_ADMIN.Patient_Nurse_View;-- Từ Nurse_Role

-- ============================================================
-- BƯỚC 3: Switch sang Nurse_Role (tắt Doctor_Role tạm thời)
-- SET ROLE chỉ ảnh hưởng trong session hiện tại, không thay
-- đổi cấu hình role lâu dài trong database
-- ============================================================
SET ROLE Nurse_Role;

-- Kiểm tra lại role active sau SET ROLE
SELECT * FROM SESSION_ROLES;
-- Kết quả mong đợi: NURSE_ROLE (Doctor_Role đã bị tắt)

-- Bây giờ chỉ có quyền của Nurse_Role
-- Test: SELECT Patient qua view - HỢP LỆ (Nurse_Role)
SELECT * FROM HOSPITAL_ADMIN.Patient_Nurse_View;

-- Test: SELECT Medical_Record - KHÔNG HỢP LỆ (Doctor_Role đã tắt)
-- Lỗi: ORA-01031: insufficient privileges
SELECT * FROM HOSPITAL_ADMIN.Medical_Record;

-- Test: UPDATE Appointment Notes qua Nurse view - HỢP LỆ
UPDATE HOSPITAL_ADMIN.Appointment_Nurse_View
SET    Notes = 'Bác sĩ đang giả lập role y tá để hỗ trợ'
WHERE  ID = 3;
COMMIT;

-- Test: INSERT Appointment - KHÔNG HỢP LỆ (Doctor_Role đã tắt)
-- Lỗi: ORA-01031: insufficient privileges
INSERT INTO HOSPITAL_ADMIN.Appointment (ID, Patient_ID, Doctor_ID, Appt_Date, Status)
VALUES (100, 1, 1, SYSDATE, 'Pending');

-- ============================================================
-- BƯỚC 4: Khôi phục về Doctor_Role
-- ============================================================
SET ROLE Doctor_Role;

SELECT * FROM SESSION_ROLES;
-- Kết quả: DOCTOR_ROLE (chỉ Doctor_Role active)

-- Bây giờ có quyền Doctor trở lại
INSERT INTO HOSPITAL_ADMIN.Appointment (ID, Patient_ID, Doctor_ID, Appt_Date, Status, Notes)
VALUES (101, 2, 1, SYSDATE, 'Pending', 'Appointment mới từ Doctor_Role');
COMMIT;

-- ============================================================
-- BƯỚC 5: Kích hoạt TẤT CẢ role (trạng thái mặc định ban đầu)
-- ============================================================
SET ROLE ALL;

SELECT * FROM SESSION_ROLES;
-- Kết quả: DOCTOR_ROLE, NURSE_ROLE

-- ============================================================
-- BƯỚC 6: Tắt TẤT CẢ role (chế độ bảo mật tối đa)
-- ============================================================
SET ROLE NONE;

SELECT * FROM SESSION_ROLES;
-- Kết quả: (trống - không có role nào active)

-- Bây giờ không có bất kỳ quyền nào từ role
-- Lỗi: ORA-01031 cho mọi truy vấn
SELECT * FROM HOSPITAL_ADMIN.Medical_Record;
SELECT * FROM HOSPITAL_ADMIN.Patient_Nurse_View;

-- Khôi phục lại trạng thái đầy đủ trước khi kết thúc
SET ROLE ALL;

-- Cleanup test data
DELETE FROM HOSPITAL_ADMIN.Appointment WHERE ID = 101;
COMMIT;

-- ============================================================
-- BƯỚC 7: Xem session role log bằng AUDIT (tùy chọn - cần DBA)
-- ============================================================
CONNECT SYSTEM/oracle;

-- Bật audit theo dõi thay đổi session role (Oracle 12c+)
AUDIT SET ROLE;

-- Xem log audit
-- SELECT OS_USERNAME, USERNAME, ACTION_NAME, TIMESTAMP
-- FROM   DBA_AUDIT_TRAIL
-- WHERE  ACTION_NAME = 'SET ROLE'
-- ORDER  BY TIMESTAMP DESC;

-- ============================================================
-- NHẬN XÉT VỀ DYNAMIC RBAC VỚI SET ROLE
-- ============================================================
--
-- ƯU ĐIỂM:
-- 1. LINH HOẠT: Cho phép một user hoạt động với nhiều
--    "danh nghĩa" khác nhau trong cùng một session mà không
--    cần tạo nhiều tài khoản.
--
-- 2. PHẠM VI SESSION: SET ROLE chỉ ảnh hưởng đến session
--    hiện tại → không thay đổi cấu hình lâu dài trong DB.
--    Khi session kết thúc, role tự động khôi phục về mặc định.
--
-- 3. PRINCIPLE OF LEAST PRIVILEGE: Bác sĩ có thể chủ động
--    tắt bớt quyền khi không cần dùng (SET ROLE Nurse_Role),
--    giảm rủi ro nếu session bị compromise.
--
-- NHƯỢC ĐIỂM / RỦI RO:
-- 1. KHÓ KIỂM SOÁT: Admin không thể từ xa biết user đang
--    hoạt động với role nào trong session → khó audit.
--
-- 2. SAI MỤC ĐÍCH: Nếu Doctor switch sang Nurse_Role để "né"
--    các ràng buộc của Doctor_Role (ví dụ: bypass audit trigger
--    chỉ giám sát Doctor), đây là lỗ hổng thiết kế.
--
-- 3. PHỤ THUỘC NGƯỜI DÙNG: Bảo mật dựa vào hành vi tự giác
--    của user, không phải cơ chế kỹ thuật bắt buộc.
--
-- KHUYẾN NGHỊ:
-- - Dùng SET ROLE trong ứng dụng (application layer), không
--   để user trực tiếp SET ROLE tùy ý.
-- - Kết hợp AUDIT để theo dõi mọi lần SET ROLE.
-- - Cân nhắc SECURE APPLICATION ROLE (role chỉ được bật qua
--   stored procedure xác thực) thay vì SET ROLE tự do.
-- ============================================================
