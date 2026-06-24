-- ============================================================
-- SETUP ADMIN USER cho HospitalApp AdminDashboard
-- ============================================================
-- LoginForm route → AdminDashboard nếu role = "DBA"
-- (check qua: SELECT COUNT(*) FROM USER_ROLE_PRIVS WHERE GRANTED_ROLE='DBA'
--  — DBA là ROLE nên KHÔNG xuất hiện trong SESSION_PRIVS; xem OracleHelper.IsDba())
--
-- BVADMIN không có DBA role → không vào được AdminDashboard.
-- File này tạo user HOSPITAL_DBA với DBA role + password ổn định.
-- ============================================================
-- CÁCH CHẠY (cần OS authentication hoặc SYSTEM/SYS):
--
--   Cách 1 — OS auth (đơn giản nhất, không cần password):
--     sqlplus / as sysdba
--     SQL> @setup_admin_user.sql
--
--   Cách 2 — Nếu nhớ SYSTEM password:
--     sqlplus SYSTEM/<system_pass>@//localhost:1521/XEPDB1
--     SQL> @setup_admin_user.sql
-- ============================================================

-- Tạo user HOSPITAL_DBA với DBA role (drop trước nếu đã tồn tại để re-run an toàn)
DECLARE
    v_cnt NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_cnt FROM DBA_USERS WHERE USERNAME = 'HOSPITAL_DBA';
    IF v_cnt > 0 THEN
        EXECUTE IMMEDIATE 'DROP USER HOSPITAL_DBA CASCADE';
    END IF;
END;
/

CREATE USER HOSPITAL_DBA IDENTIFIED BY "Hospital@DBA2025"
    DEFAULT TABLESPACE USERS
    TEMPORARY TABLESPACE TEMP
    QUOTA UNLIMITED ON USERS;

-- Cấp DBA role → toàn quyền hệ thống
GRANT DBA TO HOSPITAL_DBA;

-- Cho phép login
GRANT CREATE SESSION TO HOSPITAL_DBA;

-- Verify
SELECT GRANTED_ROLE FROM DBA_ROLE_PRIVS WHERE GRANTEE = 'HOSPITAL_DBA';

PROMPT ───────────────────────────────────────────────────────────
PROMPT  Đã tạo user HOSPITAL_DBA / Hospital@DBA2025
PROMPT  Login HospitalApp với credentials này → AdminDashboard
PROMPT ───────────────────────────────────────────────────────────
