-- ============================================================
-- PHÂN HỆ 2 - File 07: Yêu cầu 4 - Sao lưu và Phục hồi Dữ liệu
-- ============================================================
-- 1. Tìm hiểu các phương pháp sao lưu/phục hồi
-- 2. Hiện thực trên Oracle (RMAN, Data Pump, Flashback)
-- 3. Đánh giá ưu/nhược điểm
-- 4. Kết luận
-- ============================================================
-- CÁC PHƯƠNG PHÁP ĐƯỢC TRIỂN KHAI:
--   A. RMAN (Recovery Manager) - Full + Incremental
--   B. Data Pump (expdp/impdp) - Logical backup
--   C. Flashback - Point-in-time recovery
-- ============================================================

-- ============================================================
-- PHƯƠNG PHÁP A: RMAN (Recovery Manager) - Physical Backup
-- ============================================================
-- Chú ý: RMAN được chạy từ OS command line, không phải trong SQL*Plus
-- Các lệnh dưới dạy comment minh họa cú pháp RMAN shell

/*
======== A1. CẤU HÌNH RMAN (chạy 1 lần) ========

Kết nối RMAN:
  $ rman TARGET /

Cấu hình RMAN:
  RMAN> CONFIGURE RETENTION POLICY TO RECOVERY WINDOW OF 7 DAYS;
  RMAN> CONFIGURE BACKUP OPTIMIZATION ON;
  RMAN> CONFIGURE CONTROLFILE AUTOBACKUP ON;
  RMAN> CONFIGURE CONTROLFILE AUTOBACKUP FORMAT FOR DEVICE TYPE DISK TO 'C:\backup\%F';
  RMAN> CONFIGURE DEFAULT DEVICE TYPE TO DISK;
  RMAN> CONFIGURE CHANNEL DEVICE TYPE DISK FORMAT 'C:\backup\%U';

======== A2. SAO LƯU TOÀN BỘ (Full Backup) - Sao lưu chủ động ========

  RMAN> BACKUP DATABASE PLUS ARCHIVELOG;
  RMAN> BACKUP CURRENT CONTROLFILE;
  RMAN> BACKUP SPFILE;

======== A3. SAO LƯU GIA TĂNG (Incremental Level 0 + Level 1) ========

  -- Level 0: Base backup (giống full backup, nhưng cho phép level 1 tham chiếu)
  RMAN> BACKUP INCREMENTAL LEVEL 0 DATABASE;

  -- Level 1 Cumulative: backup mọi thay đổi từ Level 0
  RMAN> BACKUP INCREMENTAL LEVEL 1 CUMULATIVE DATABASE;

  -- Level 1 Differential: backup thay đổi từ lần Level 1 gần nhất
  RMAN> BACKUP INCREMENTAL LEVEL 1 DATABASE;

======== A4. SAO LƯU TỰ ĐỘNG qua Oracle Scheduler ========

  -- Tạo job chạy full backup hàng đêm 2:00 AM
  RMAN> CONFIGURE BACKUP OPTIMIZATION ON;

  -- Lệnh SQL tạo DBMS_SCHEDULER job gọi RMAN (xem SQL bên dưới)

======== A5. PHỤC HỒI SAU SỰ CỐ (Complete Recovery) ========

  RMAN> STARTUP MOUNT;
  RMAN> RESTORE DATABASE;
  RMAN> RECOVER DATABASE;
  RMAN> ALTER DATABASE OPEN;

======== A6. PHỤC HỒI TABLESPACE CỤ THỂ ========

  RMAN> SQL 'ALTER TABLESPACE USERS OFFLINE';
  RMAN> RESTORE TABLESPACE USERS;
  RMAN> RECOVER TABLESPACE USERS;
  RMAN> SQL 'ALTER TABLESPACE USERS ONLINE';

======== A7. PHỤC HỒI POINT-IN-TIME (PITR) qua RMAN ========

  RMAN> RUN {
    SET UNTIL TIME "TO_DATE('2025-05-01 08:00:00','YYYY-MM-DD HH24:MI:SS')";
    RESTORE DATABASE;
    RECOVER DATABASE;
    ALTER DATABASE OPEN RESETLOGS;
  }
*/

-- ============================================================
-- A8. Sao lưu tự động qua DBMS_SCHEDULER (SQL thuần)
-- ============================================================
CONNECT SYS/password AS SYSDBA;

BEGIN
    DBMS_SCHEDULER.CREATE_JOB(
        job_name        => 'JOB_RMAN_FULL_BACKUP',
        job_type        => 'EXECUTABLE',
        job_action      => 'C:\oracle\product\19.0\bin\rman.exe',
        number_of_arguments => 3,
        start_date      => SYSTIMESTAMP,
        repeat_interval => 'FREQ=DAILY; BYHOUR=2; BYMINUTE=0',  -- 2:00 AM mỗi ngày
        enabled         => FALSE,
        comments        => 'Full RMAN backup hàng đêm 2:00 AM'
    );

    DBMS_SCHEDULER.SET_JOB_ARGUMENT_VALUE('JOB_RMAN_FULL_BACKUP', 1, 'TARGET');
    DBMS_SCHEDULER.SET_JOB_ARGUMENT_VALUE('JOB_RMAN_FULL_BACKUP', 2, '/');
    DBMS_SCHEDULER.SET_JOB_ARGUMENT_VALUE('JOB_RMAN_FULL_BACKUP', 3,
        '@C:\scripts\rman_full_backup.rcv');

    DBMS_SCHEDULER.ENABLE('JOB_RMAN_FULL_BACKUP');
END;
/

-- Script RMAN (nội dung file C:\scripts\rman_full_backup.rcv):
/*
  BACKUP DATABASE PLUS ARCHIVELOG DELETE ALL INPUT;
  BACKUP CURRENT CONTROLFILE;
  BACKUP SPFILE;
  DELETE NOPROMPT OBSOLETE;
  EXIT;
*/

-- ============================================================
-- PHƯƠNG PHÁP B: DATA PUMP - Logical Backup (chủ động + tự động)
-- ============================================================
-- B1. Export schema BVADMIN (chạy từ OS)
/*
  $ expdp system/oracle SCHEMAS=BVADMIN \
    DIRECTORY=DATA_PUMP_DIR \
    DUMPFILE=bvadmin_%date%.dmp \
    LOGFILE=bvadmin_export_%date%.log \
    COMPRESSION=ALL

  -- Export chỉ 1 bảng
  $ expdp system/oracle TABLES=BVADMIN.HSBA,BVADMIN.DONTHUOC \
    DIRECTORY=DATA_PUMP_DIR \
    DUMPFILE=hsba_donthuoc_backup.dmp

  -- Import phục hồi
  $ impdp system/oracle SCHEMAS=BVADMIN \
    DIRECTORY=DATA_PUMP_DIR \
    DUMPFILE=bvadmin_20250501.dmp \
    LOGFILE=bvadmin_import.log \
    TABLE_EXISTS_ACTION=REPLACE
*/

-- B2. Tạo thư mục Oracle cho Data Pump
CONNECT SYS/password AS SYSDBA;
CREATE OR REPLACE DIRECTORY DATA_PUMP_DIR AS 'C:\oracle\backup\datapump';
GRANT READ, WRITE ON DIRECTORY DATA_PUMP_DIR TO SYSTEM;
GRANT READ, WRITE ON DIRECTORY DATA_PUMP_DIR TO BVADMIN;

-- B3. Backup tự động Data Pump qua Scheduler
BEGIN
    DBMS_SCHEDULER.CREATE_JOB(
        job_name        => 'JOB_DATAPUMP_BACKUP',
        job_type        => 'EXECUTABLE',
        job_action      => 'C:\oracle\product\19.0\bin\expdp.exe',
        start_date      => SYSTIMESTAMP,
        repeat_interval => 'FREQ=WEEKLY; BYDAY=SUN; BYHOUR=1; BYMINUTE=0',  -- Chủ nhật 1:00 AM
        enabled         => FALSE,
        comments        => 'Weekly Data Pump export'
    );
    DBMS_SCHEDULER.ENABLE('JOB_DATAPUMP_BACKUP');
END;
/

-- ============================================================
-- PHƯƠNG PHÁP C: FLASHBACK - Point-in-time Recovery
-- ============================================================
CONNECT SYS/password AS SYSDBA;

-- C1. Bật Flashback Database (cần ARCHIVELOG mode)
-- Kiểm tra trạng thái
SELECT LOG_MODE, FLASHBACK_ON FROM V$DATABASE;

-- Bật nếu chưa có
-- SHUTDOWN IMMEDIATE;
-- STARTUP MOUNT;
-- ALTER DATABASE ARCHIVELOG;
-- ALTER DATABASE FLASHBACK ON;
-- ALTER DATABASE OPEN;

-- C2. Cấu hình Flashback
-- Giữ flashback log 24 giờ
ALTER SYSTEM SET DB_FLASHBACK_RETENTION_TARGET = 1440;  -- phút = 24h
ALTER SYSTEM SET DB_RECOVERY_FILE_DEST_SIZE = 20G;
ALTER SYSTEM SET DB_RECOVERY_FILE_DEST = 'C:\oracle\fra';

-- C3. Bật Flashback trên bảng cụ thể (Row Movement cần được bật)
CONNECT BVADMIN/BVAdmin@2025;
ALTER TABLE DONTHUOC    ENABLE ROW MOVEMENT;
ALTER TABLE HSBA        ENABLE ROW MOVEMENT;
ALTER TABLE HSBA_DV     ENABLE ROW MOVEMENT;
ALTER TABLE BENHNHAN    ENABLE ROW MOVEMENT;

-- C4. Tình huống: KTV_NV006 xóa nhầm HSBA_DV, cần phục hồi
-- Lưu SCN trước khi sự cố
CONNECT SYS/password AS SYSDBA;

-- Lưu thời điểm trước sự cố
CREATE TABLE BVADMIN.CHECKPOINT_LOG (
    EVENT_NAME  VARCHAR2(100),
    SCN         NUMBER,
    EVENT_TIME  TIMESTAMP DEFAULT SYSTIMESTAMP,
    CREATED_BY  VARCHAR2(100) DEFAULT SYS_CONTEXT('USERENV','SESSION_USER')
);

-- Ghi checkpoint định kỳ (ví dụ: trước các thao tác quan trọng)
CONNECT BVADMIN/BVAdmin@2025;
INSERT INTO CHECKPOINT_LOG(EVENT_NAME, SCN)
VALUES('Before_batch_update', DBMS_FLASHBACK.GET_SYSTEM_CHANGE_NUMBER());
COMMIT;

-- C5. Flashback Table: phục hồi bảng về trạng thái trước sự cố
CONNECT SYS/password AS SYSDBA;

-- Phục hồi về SCN cụ thể (lấy từ CHECKPOINT_LOG hoặc DBA_FGA_AUDIT_TRAIL)
-- FLASHBACK TABLE BVADMIN.HSBA_DV TO SCN 12345678;

-- Phục hồi về thời điểm cụ thể
-- FLASHBACK TABLE BVADMIN.DONTHUOC TO TIMESTAMP
--   TO_TIMESTAMP('2025-05-01 07:30:00', 'YYYY-MM-DD HH24:MI:SS');

-- C6. Flashback Query: Xem dữ liệu tại thời điểm trước sự cố (không cần phục hồi toàn bộ)
CONNECT BVADMIN/BVAdmin@2025;

-- Xem DONTHUOC tại thời điểm trước khi bị sửa
SELECT * FROM DONTHUOC
AS OF TIMESTAMP (SYSTIMESTAMP - INTERVAL '1' HOUR);
-- Có thể dùng để so sánh và lấy lại dữ liệu bị mất

-- Xem HSBA_DV trước khi có sự cố xóa
SELECT * FROM HSBA_DV
AS OF SCN (SELECT SCN FROM CHECKPOINT_LOG WHERE EVENT_NAME = 'Before_batch_update');

-- C7. Kịch bản phục hồi dựa vào nhật ký kiểm toán (FGA + Trigger log)
-- Sau khi đọc DBA_FGA_AUDIT_TRAIL và LOG_BS_DONTHUOC, tìm SCN tại thời điểm trước sự cố
CONNECT SYSTEM/oracle;

-- Tìm thời điểm bắt đầu sự cố từ audit trail
SELECT DB_USER, SQL_TEXT, EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  POLICY_NAME = 'FGA_DONTHUOC_UPDATE'
  AND  EXTENDED_TIMESTAMP > SYSTIMESTAMP - INTERVAL '2' HOUR
ORDER  BY EXTENDED_TIMESTAMP;

-- Tìm SCN tương ứng
SELECT TIMESTAMP_TO_SCN(TO_TIMESTAMP('2025-05-01 09:15:00', 'YYYY-MM-DD HH24:MI:SS'))
FROM   DUAL;

-- ============================================================
-- PHẦN D: SO SÁNH PHƯƠNG PHÁP
-- ============================================================
/*
┌─────────────────┬──────────────────────────────────────────────────────┬──────────────────────────────────────┐
│ Phương pháp     │ Ưu điểm                                              │ Nhược điểm                           │
├─────────────────┼──────────────────────────────────────────────────────┼──────────────────────────────────────┤
│ RMAN Full       │ - Sao lưu hoàn chỉnh, khôi phục chắc chắn          │ - Tốn không gian lưu trữ lớn         │
│                 │ - Tích hợp sẵn với Oracle, nhanh                   │ - Thời gian backup dài               │
│                 │ - Hỗ trợ compression, encryption                   │ - Cần DBA có kinh nghiệm             │
├─────────────────┼──────────────────────────────────────────────────────┼──────────────────────────────────────┤
│ RMAN Incremental│ - Tiết kiệm không gian (chỉ backup thay đổi)       │ - Phục hồi phức tạp hơn Full         │
│                 │ - Backup nhanh hơn sau lần đầu                     │ - Cần giữ Level 0 + tất cả Level 1   │
│                 │ - Phù hợp backup hàng ngày                         │                                      │
├─────────────────┼──────────────────────────────────────────────────────┼──────────────────────────────────────┤
│ Data Pump       │ - Backup logic, di động giữa các DB                 │ - Chậm hơn RMAN cho DB lớn           │
│                 │ - Dễ backup từng schema/bảng                       │ - Không backup binary level          │
│                 │ - Dùng được để migrate dữ liệu                    │ - Khó phục hồi thời điểm cụ thể      │
├─────────────────┼──────────────────────────────────────────────────────┼──────────────────────────────────────┤
│ Flashback       │ - Phục hồi cực nhanh (trong vài giây)              │ - Giới hạn thời gian (retention)     │
│                 │ - Không cần restore toàn bộ DB                     │ - Tốn không gian FRA (Flashback logs)│
│                 │ - Phục hồi chọn lọc từng bảng                     │ - Không thay thế được RMAN            │
│                 │ - Tốt để đối phó với lỗi người dùng               │                                      │
└─────────────────┴──────────────────────────────────────────────────────┴──────────────────────────────────────┘

KHUYẾN NGHỊ cho hệ thống Quản lý Y tế:
  - Hàng đêm 2:00 AM: RMAN Incremental Level 1 (thay đổi trong ngày)
  - Chủ nhật 1:00 AM: RMAN Full Backup (tuần 1 lần)
  - Mỗi ngày 8:00 PM: Data Pump export schema BVADMIN (backup logic cho audit)
  - Bật Flashback Database với retention 24h: xử lý lỗi vận hành nhanh
  - Lưu backup offsite (NAS/Cloud): phòng chống thảm họa vật lý

KẾT LUẬN:
  RMAN là giải pháp backup chính cho hệ thống y tế vì tính toàn vẹn cao.
  Flashback bổ sung khả năng phục hồi nhanh các sự cố nhỏ (xóa nhầm dòng).
  Data Pump phục vụ kiểm tra dữ liệu và di chuyển dữ liệu.
  Ba phương pháp kết hợp tạo ra chiến lược backup toàn diện theo tiêu chuẩn RPO/RTO
  phù hợp với dữ liệu y tế nhạy cảm.
*/

-- ============================================================
-- PHẦN E: Kiểm tra trạng thái backup hiện tại
-- ============================================================
CONNECT SYS/password AS SYSDBA;

-- Xem các backup RMAN đã thực hiện
SELECT BS.RECID, BS.SET_STAMP,
       BP.PIECE#, BP.HANDLE,
       BS.BACKUP_TYPE, BS.STATUS,
       BS.START_TIME, BS.ELAPSED_SECONDS
FROM   V$BACKUP_SET BS
JOIN   V$BACKUP_PIECE BP ON BS.SET_STAMP = BP.SET_STAMP
ORDER  BY BS.START_TIME DESC
FETCH FIRST 20 ROWS ONLY;

-- Xem trạng thái archive log
SELECT NAME, STATUS, FIRST_CHANGE#, NEXT_CHANGE#, COMPLETION_TIME
FROM   V$ARCHIVED_LOG
ORDER  BY COMPLETION_TIME DESC
FETCH FIRST 20 ROWS ONLY;

-- Xem dung lượng FRA (Flashback Recovery Area)
SELECT SPACE_LIMIT, SPACE_USED, SPACE_RECLAIMABLE,
       ROUND(SPACE_USED/SPACE_LIMIT * 100, 2) AS USED_PCT
FROM   V$RECOVERY_FILE_DEST;
