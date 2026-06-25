-- ============================================================
-- PHAN HE 2 (extras) -- Demo NNE + TDE
-- Chay bang SQL*Plus:
--   chcp 65001
--   $env:NLS_LANG = "AMERICAN_AMERICA.AL32UTF8"
--   sqlplus /nolog @"d:\repos\Oracle\PhanHe2\extras\NNE_TDE_demo.sql"
-- Sua SYS_PWD neu mat khau SYS khac.
-- ============================================================

DEFINE SYS_PWD = "Phamminhquan611*"
DEFINE BVA_PWD = "BVAdmin@2025"
DEFINE DB_URL  = "//localhost:1521/XEPDB1"

SET LINESIZE  130
SET PAGESIZE   60
SET FEEDBACK   ON
SET ECHO       OFF
SET WRAP       OFF
SET HEADING    ON

PROMPT
PROMPT ####################################################################
PROMPT #  DEMO MA HOA: NNE (duong truyen) + TDE (du lieu at-rest)
PROMPT #  Chung minh he thong co ca CRYPTOGRAPHY ben canh ACCESS CONTROL
PROMPT ####################################################################

-- ============================================================
-- PHAN 1: NNE -- Native Network Encryption
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  PHAN 1/3 : MA HOA DUONG TRUYEN (NNE)
PROMPT ==================================================================

CONNECT BVADMIN/"&BVA_PWD"@&DB_URL

PROMPT
PROMPT [1.1] Toan bo banner ket noi cua phien hien tai (V$SESSION_CONNECT_INFO):
PROMPT       --> Chu y 2 dong CUOI: "Encryption service" + "Crypto-checksumming"
PROMPT           => chung to ket noi TCP da duoc ma hoa (NNE dang BAT).

COL NETWORK_SERVICE_BANNER FORMAT A115 HEADING "NETWORK_SERVICE_BANNER"

SELECT NETWORK_SERVICE_BANNER
FROM   V$SESSION_CONNECT_INFO
WHERE  SID = SYS_CONTEXT('USERENV', 'SID');

PROMPT
PROMPT [1.2] Loc rieng 2 dong chung minh NNE (de chi cho hoi dong xem):

SELECT NETWORK_SERVICE_BANNER
FROM   V$SESSION_CONNECT_INFO
WHERE  SID = SYS_CONTEXT('USERENV', 'SID')
  AND  (
         UPPER(NETWORK_SERVICE_BANNER) LIKE '%ENCRYPTION%'
      OR UPPER(NETWORK_SERVICE_BANNER) LIKE '%CRYPTO%'
       );

-- ============================================================
-- PHAN 2: TDE -- metadata (ket noi SYSDBA)
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  PHAN 2/3 : MA HOA DU LIEU AT-REST (TDE) -- xem metadata
PROMPT ==================================================================

CONNECT SYS/"&SYS_PWD"@&DB_URL AS SYSDBA

PROMPT
PROMPT [2.1] Trang thai Keystore/Wallet -- can STATUS = OPEN thi moi giai ma duoc:

COL WRL_TYPE      FORMAT A10   HEADING "TYPE"
COL STATUS        FORMAT A10   HEADING "STATUS"
COL WALLET_TYPE   FORMAT A15   HEADING "WALLET_TYPE"
COL WRL_PARAMETER FORMAT A60   HEADING "PATH"

SELECT WRL_TYPE,
       STATUS,
       WALLET_TYPE,
       WRL_PARAMETER
FROM   V$ENCRYPTION_WALLET;

PROMPT
PROMPT [2.2] Cac cot dang duoc TDE bao ve (DBA_ENCRYPTED_COLUMNS):
PROMPT       --> CCCD, CMND ma hoa NO SALT (giu UNIQUE/tim "="); DIUNGTHUOC co SALT.

COL TABLE_NAME     FORMAT A15   HEADING "TABLE"
COL COLUMN_NAME    FORMAT A18   HEADING "COLUMN"
COL ENCRYPTION_ALG FORMAT A20   HEADING "ALGORITHM"
COL SALT           FORMAT A6    HEADING "SALT"

SELECT TABLE_NAME,
       COLUMN_NAME,
       ENCRYPTION_ALG,
       SALT
FROM   DBA_ENCRYPTED_COLUMNS
ORDER BY TABLE_NAME, COLUMN_NAME;

-- ============================================================
-- PHAN 3: TDE trong suot -- doc plaintext bang BVADMIN
-- ============================================================
PROMPT
PROMPT ==================================================================
PROMPT  PHAN 3/3 : TDE TRONG SUOT VOI APP -- doc ra plaintext binh thuong
PROMPT  (Tren dia la ciphertext; phien hop le tu dong giai ma khi doc)
PROMPT ==================================================================

CONNECT BVADMIN/"&BVA_PWD"@&DB_URL

PROMPT
PROMPT [3.1] CCCD benh nhan (cot ma hoa AES, NO SALT) -- van ra so CCCD ro:

COL MABN  FORMAT A8    HEADING "MA BN"
COL TENBN FORMAT A25   HEADING "HO TEN"
COL CCCD  FORMAT A15   HEADING "CCCD"

SELECT MABN, TENBN, CCCD
FROM   BENHNHAN
WHERE  ROWNUM <= 5
ORDER BY MABN;

PROMPT
PROMPT [3.2] CMND nhan vien (cot ma hoa AES, NO SALT) -- van ra so CMND ro:

COL MANV  FORMAT A8    HEADING "MA NV"
COL HOTEN FORMAT A25   HEADING "HO TEN"
COL CMND  FORMAT A15   HEADING "CMND"

SELECT MANV, HOTEN, CMND
FROM   NHANVIEN
WHERE  ROWNUM <= 5
ORDER BY MANV;

PROMPT
PROMPT [3.3] DIUNGTHUOC (cot ma hoa AES, CO SALT) -- van ra noi dung ro:

COL MABN       FORMAT A8    HEADING "MA BN"
COL TENBN      FORMAT A25   HEADING "HO TEN"
COL DIUNGTHUOC FORMAT A35   HEADING "DI UNG THUOC"

SELECT MABN, TENBN, DIUNGTHUOC
FROM   BENHNHAN
WHERE  DIUNGTHUOC IS NOT NULL
ORDER BY MABN;

PROMPT
PROMPT ####################################################################
PROMPT #  KET THUC DEMO: NNE bao ve duong truyen, TDE bao ve du lieu at-rest,
PROMPT #  va TDE trong suot nen app khong can sua code.
PROMPT ####################################################################
EXIT
