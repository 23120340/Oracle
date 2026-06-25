-- ============================================================
-- PHAN HE 2 (extras) -- Demo Flashback + Audit
-- Chay bang SQL*Plus:
--   chcp 65001                       (terminal UTF-8, hien tieng Viet trong du lieu)
--   $env:NLS_LANG = "AMERICAN_AMERICA.AL32UTF8"
--   sqlplus /nolog @"d:\repos\Oracle\PhanHe2\extras\recovery_demo.sql"
-- Sua SYS_PWD neu mat khau SYS khac.
-- ============================================================

DEFINE SYS_PWD = "oracle"
DEFINE BVA_PWD = "BVAdmin@2025"
DEFINE DB_URL  = "//localhost:1521/XEPDB1"

SET LINESIZE  120
SET PAGESIZE   50
SET FEEDBACK   ON
SET ECHO       OFF
SET WRAP       OFF

PROMPT
PROMPT ####################################################################
PROMPT #  DEMO YC4 -- PHUC HOI DU LIEU BANG FLASHBACK (kem AUDIT/FGA)
PROMPT #  Kich ban: xoa nham dich vu cua HS001 --> FLASHBACK TABLE khoi phuc
PROMPT ####################################################################

-- ============================================================
-- BUOC 0: Ket noi + bat ROW MOVEMENT
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 0/6 : Ket noi BVADMIN + bat ROW MOVEMENT (bat buoc cho FLASHBACK TABLE)
PROMPT ==================================================================

CONNECT BVADMIN/"&BVA_PWD"@&DB_URL

ALTER TABLE HSBA_DV ENABLE ROW MOVEMENT;

-- ============================================================
-- BUOC 0b: Re-seed du lieu HS001 vao HSBA_DV neu chua co
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 0b   : Re-seed 2 dich vu cua HS001 (dam bao luon co data de demo)
PROMPT ==================================================================

MERGE INTO HSBA_DV tgt
USING (
    SELECT 'HS001' AS MAHSBA, 'Xet nghiem mau tong quat' AS LOAIDV,
           DATE'2025-04-01' AS NGAYDV, 'NV006' AS MAKTV,
           'Glucose: 12.5 mmol/L, HbA1c: 9%' AS KETQUA FROM dual
    UNION ALL
    SELECT 'HS001', 'Sieu am tim',
           DATE'2025-04-02', 'NV007',
           'Tim binh thuong, EF 65%' FROM dual
) src
ON (tgt.MAHSBA = src.MAHSBA AND tgt.LOAIDV = src.LOAIDV AND tgt.NGAYDV = src.NGAYDV)
WHEN NOT MATCHED THEN
    INSERT (MAHSBA, LOAIDV, NGAYDV, MAKTV, KETQUA)
    VALUES (src.MAHSBA, src.LOAIDV, src.NGAYDV, src.MAKTV, src.KETQUA);
COMMIT;

-- ============================================================
-- BUOC 1: Tao / cap nhat CHECKPOINT_LOG
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 1/6 : Ghi SCN checkpoint NGAY TRUOC su co (de biet moc phuc hoi)
PROMPT ==================================================================

DECLARE
    v_count NUMBER;
BEGIN
    SELECT COUNT(*) INTO v_count
    FROM   ALL_TABLES
    WHERE  TABLE_NAME = 'CHECKPOINT_LOG'
      AND  OWNER = SYS_CONTEXT('USERENV','CURRENT_SCHEMA');
    IF v_count = 0 THEN
        EXECUTE IMMEDIATE q'[
            CREATE TABLE CHECKPOINT_LOG (
                EVENT_NAME VARCHAR2(100) PRIMARY KEY,
                SCN        NUMBER NOT NULL,
                CREATED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
            )
        ]';
    END IF;
EXCEPTION
    WHEN OTHERS THEN
        IF SQLCODE != -955 THEN RAISE; END IF;
END;
/

MERGE INTO CHECKPOINT_LOG c
USING (
    SELECT 'demo_before_delete_hsba_dv' AS event_name,
           DBMS_FLASHBACK.GET_SYSTEM_CHANGE_NUMBER AS scn
    FROM dual
) s
ON (c.EVENT_NAME = s.EVENT_NAME)
WHEN MATCHED THEN
    UPDATE SET c.SCN = s.SCN, c.CREATED_AT = SYSTIMESTAMP
WHEN NOT MATCHED THEN
    INSERT (EVENT_NAME, SCN) VALUES (s.EVENT_NAME, s.SCN);
COMMIT;

-- ============================================================
-- BUOC 2: Trang thai TRUOC su co
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 2/6 : TRANG THAI TRUOC SU CO
PROMPT  Ky vong  : tong dong >= 4, va HS001 co 2 dich vu (xet nghiem + sieu am)
PROMPT ==================================================================

COL HSBA_DV_COUNT HEADING "TONG SO DONG HSBA_DV"
SELECT COUNT(*) AS HSBA_DV_COUNT FROM HSBA_DV;

PROMPT >>> Cac dich vu cua HS001 hien tai:
COL MAHSBA FORMAT A10  HEADING "MA HSBA"
COL LOAIDV FORMAT A30  HEADING "LOAI DV"
COL NGAYDV FORMAT A14  HEADING "NGAY DV"
COL MAKTV  FORMAT A8   HEADING "MA KTV"

SELECT MAHSBA, LOAIDV, NGAYDV, MAKTV
FROM   HSBA_DV
WHERE  MAHSBA = 'HS001'
ORDER BY NGAYDV, LOAIDV;

-- ============================================================
-- BUOC 3: Gia lap su co -- xoa nham HS001
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 3/6 : GIA LAP SU CO -- xoa nham TOAN BO dich vu cua HS001
PROMPT ==================================================================

DELETE FROM HSBA_DV WHERE MAHSBA = 'HS001';
COMMIT;

PROMPT >>> Sau khi xoa, HS001 bien mat (tong dong giam):
COL HSBA_DV_COUNT_AFTER_DELETE HEADING "TONG SO DONG SAU XOA"
SELECT COUNT(*) AS HSBA_DV_COUNT_AFTER_DELETE FROM HSBA_DV;

-- ============================================================
-- BUOC 4: Xem Audit / FGA ghi nhan hanh dong xoa
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 4/6 : AUDIT/FGA da GHI VET hanh dong xoa (ket noi SYS de doc)
PROMPT  Ky vong  : co dong DELETE FROM HSBA_DV ... HS001 trong trail
PROMPT ==================================================================

CONNECT SYS/"&SYS_PWD"@&DB_URL AS SYSDBA

COL DB_USER      FORMAT A12   HEADING "USER"
COL OBJECT_NAME  FORMAT A12   HEADING "OBJECT"
COL POLICY_NAME  FORMAT A22   HEADING "POLICY"
COL SQL_TEXT     FORMAT A45   HEADING "SQL_TEXT"
COL EXTENDED_TIMESTAMP FORMAT A32 HEADING "TIMESTAMP"

SELECT DB_USER,
       OBJECT_NAME,
       POLICY_NAME,
       SUBSTR(SQL_TEXT, 1, 45) AS SQL_TEXT,
       EXTENDED_TIMESTAMP
FROM   DBA_FGA_AUDIT_TRAIL
WHERE  OBJECT_SCHEMA = 'BVADMIN'
  AND  OBJECT_NAME   = 'HSBA_DV'
ORDER BY EXTENDED_TIMESTAMP DESC
FETCH FIRST 5 ROWS ONLY;

-- ============================================================
-- BUOC 5: Doc SCN checkpoint va phuc hoi
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 5/6 : PHUC HOI -- doc SCN checkpoint roi FLASHBACK TABLE ve moc do
PROMPT ==================================================================

CONNECT BVADMIN/"&BVA_PWD"@&DB_URL

PROMPT >>> SCN checkpoint da ghi o BUOC 1:
COL EVENT_NAME FORMAT A30  HEADING "CHECKPOINT"
COL SCN        FORMAT 9999999999 HEADING "SCN"
COL CREATED_AT FORMAT A36  HEADING "GHI NHAN LUC"

SELECT EVENT_NAME, SCN, CREATED_AT
FROM   CHECKPOINT_LOG
WHERE  EVENT_NAME = 'demo_before_delete_hsba_dv';

PROMPT >>> Dang chay: FLASHBACK TABLE HSBA_DV TO SCN <checkpoint> ...
DECLARE
    v_scn NUMBER;
BEGIN
    SELECT SCN INTO v_scn
    FROM   CHECKPOINT_LOG
    WHERE  EVENT_NAME = 'demo_before_delete_hsba_dv';
    EXECUTE IMMEDIATE 'FLASHBACK TABLE HSBA_DV TO SCN ' || v_scn;
END;
/

PROMPT >>> Sau FLASHBACK: tong dong tro ve nhu cu, HS001 xuat hien lai:
COL HSBA_DV_COUNT_AFTER_FLASHBACK HEADING "TONG SO DONG SAU FLASHBACK"
SELECT COUNT(*) AS HSBA_DV_COUNT_AFTER_FLASHBACK FROM HSBA_DV;

SELECT MAHSBA, LOAIDV, NGAYDV, MAKTV
FROM   HSBA_DV
WHERE  MAHSBA = 'HS001'
ORDER BY NGAYDV, LOAIDV;

-- ============================================================
-- BUOC 6: Flashback Query / Versions (phu, neu con thoi gian)
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  BUOC 6/6 (phu) : Flashback Query + Versions -- xem lich su DONTHUOC
PROMPT  (Neu bao ORA-01466 thi bo qua: bang vua doi cau truc gan day)
PROMPT ==================================================================

PROMPT >>> Don thuoc HS001 tai thoi diem 30 phut truoc (Flashback Query):
COL MAHSBA   FORMAT A8   HEADING "MA HSBA"
COL NGAYDT   FORMAT A14  HEADING "NGAY DT"
COL TENTHUOC FORMAT A30  HEADING "TEN THUOC"
COL LIEUDUNG FORMAT A30  HEADING "LIEU DUNG"

SELECT MAHSBA, NGAYDT, TENTHUOC, LIEUDUNG
FROM   DONTHUOC AS OF TIMESTAMP (SYSTIMESTAMP - INTERVAL '30' MINUTE)
WHERE  MAHSBA = 'HS001'
ORDER BY NGAYDT, TENTHUOC;

PROMPT >>> Lich su cac phien ban dong (VERSIONS BETWEEN):
COL VERSIONS_STARTTIME FORMAT A28 HEADING "THOI GIAN"
COL VERSIONS_OPERATION FORMAT A3  HEADING "OP"

SELECT VERSIONS_STARTTIME,
       VERSIONS_OPERATION,
       MAHSBA,
       TENTHUOC,
       SUBSTR(LIEUDUNG, 1, 30) AS LIEUDUNG
FROM   DONTHUOC VERSIONS BETWEEN TIMESTAMP MINVALUE AND MAXVALUE
WHERE  MAHSBA = 'HS001'
ORDER BY VERSIONS_STARTTIME DESC NULLS LAST;

PROMPT
PROMPT ####################################################################
PROMPT #  KET THUC DEMO YC4: du lieu HS001 da duoc khoi phuc nguyen ven.
PROMPT ####################################################################
EXIT
