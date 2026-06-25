-- ============================================================
-- PHÂN HỆ 2 - File 13: Mã hóa DỮ LIỆU NHẠY CẢM at-rest bằng TDE
-- (Transparent Data Encryption) — bổ sung tầng "cryptography" cho access-control
-- ============================================================
-- ⚠️ File này CHỨA MẬT KHẨU THẬT (chỉ dùng local) — KHÔNG đẩy lên repo công khai.
--
-- Mã hóa cột nhạy cảm: CCCD (BENHNHAN), CMND (NHANVIEN), DIUNGTHUOC.
-- TDE = trong suốt với app: vẫn SELECT/INSERT plaintext, Oracle mã hóa khi ghi đĩa và
-- giải mã khi đọc cho phiên có keystore mở → KHÔNG sửa app, UNIQUE/tìm-"=" vẫn chạy.
--
-- ⚠️ VỊ TRÍ KEYSTORE — phải KHỚP nơi instance đang tìm, nếu không sẽ ORA-28367.
--   Cách an toàn (KHÔNG cần restart): dùng đúng vị trí mặc định instance đang dùng.
--   Tìm vị trí đó:  SELECT WRL_PARAMETER FROM V$ENCRYPTION_WALLET WHERE CON_ID=1;
--   Trên máy này = D:\Oracle\admin\XE\wallet  → tạo sẵn thư mục đó (rỗng):
--       New-Item -ItemType Directory D:\Oracle\admin\XE\wallet -Force
--   (KHÔNG cần đặt ENCRYPTION_WALLET_LOCATION trong sqlnet.ora vì đã trùng mặc định.
--    Phương án khác: ALTER SYSTEM SET WALLET_ROOT='D:\Oracle\wallet' SCOPE=SPFILE; rồi RESTART DB.)
--
-- ⚠️ Mật khẩu keystore (TdeWallet2025x) TỐI QUAN TRỌNG: mất = MẤT dữ liệu mã hóa.
--    Sao lưu cả thư mục wallet (ewallet.p12, cwallet.sso) cùng dữ liệu.
--
-- Cách chạy (PowerShell):  $env:NLS_LANG=".AL32UTF8"; sqlplus /nolog "@...\13_TDE_Encryption.sql"
-- ============================================================
-- >>> ĐIỀN MẬT KHẨU CỦA MÁY BẠN VÀO 2 DÒNG NÀY (chỉ sửa ở đây) <<<
DEFINE SYS_PWD    = "oracle"          -- mật khẩu SYS của DB (đổi cho khớp máy bạn)
DEFINE WALLET_PWD = "TdeWallet2025x"  -- mật khẩu keystore TDE (giữ kỹ — mất = mất dữ liệu)
SET DEFINE ON
WHENEVER SQLERROR CONTINUE

-- ── B1. Tạo + mở keystore tại CDB ROOT (đúng vị trí instance dùng) ───────────
CONNECT sys/"&SYS_PWD"@localhost:1521/XE AS SYSDBA

ADMINISTER KEY MANAGEMENT CREATE KEYSTORE 'D:\Oracle\admin\XE\wallet' IDENTIFIED BY "&WALLET_PWD";
ADMINISTER KEY MANAGEMENT SET KEYSTORE OPEN IDENTIFIED BY "&WALLET_PWD" CONTAINER=ALL;

-- ── B2. Đặt master key: ROOT trước, rồi PDB XEPDB1 ──────────────────────────
ADMINISTER KEY MANAGEMENT SET KEY IDENTIFIED BY "&WALLET_PWD" WITH BACKUP;
ALTER SESSION SET CONTAINER = XEPDB1;
ADMINISTER KEY MANAGEMENT SET KEY IDENTIFIED BY "&WALLET_PWD" WITH BACKUP;

-- ── B3. AUTO-LOGIN keystore (DB tự mở keystore khi khởi động) ───────────────
-- Thiếu bước này: sau restart phải mở keystore tay, nếu không cột mã hóa không đọc được.
ALTER SESSION SET CONTAINER = CDB$ROOT;
ADMINISTER KEY MANAGEMENT CREATE AUTO_LOGIN KEYSTORE FROM KEYSTORE 'D:\Oracle\admin\XE\wallet' IDENTIFIED BY "&WALLET_PWD";

-- ── B4. Mã hóa cột nhạy cảm (trong PDB, bảng của BVADMIN) ────────────────────
ALTER SESSION SET CONTAINER = XEPDB1;

-- CCCD/CMND là UNIQUE → ENCRYPT NO SALT để giữ UNIQUE + tìm theo "=".
-- (Thêm USING 'AES256' nếu muốn AES256; mặc định TDE là AES192 — đủ mạnh.)
ALTER TABLE BVADMIN.BENHNHAN MODIFY (CCCD ENCRYPT NO SALT);
ALTER TABLE BVADMIN.NHANVIEN MODIFY (CMND ENCRYPT NO SALT);

-- Dị ứng thuốc: NVARCHAR2(500) → mã hóa CÓ salt (an toàn hơn), không cần index.
ALTER TABLE BVADMIN.BENHNHAN MODIFY (DIUNGTHUOC ENCRYPT);

-- LƯU Ý: TIENSUBENH / TIENSUBENHGD là NVARCHAR2(2000) → mã hóa sẽ báo ORA-28331
-- ("encrypted column size too long") vì 2000 ký tự (~4000 byte) + overhead vượt giới hạn
-- NVARCHAR2. Muốn mã hóa phải bật MAX_STRING_SIZE=EXTENDED hoặc thu nhỏ cột → BỎ QUA.

-- ── B5. Kiểm tra ────────────────────────────────────────────────────────────
COL TABLE_NAME FORMAT A12
COL COLUMN_NAME FORMAT A14
COL ENCRYPTION_ALG FORMAT A18
SELECT TABLE_NAME, COLUMN_NAME, ENCRYPTION_ALG, SALT
FROM   DBA_ENCRYPTED_COLUMNS ORDER BY TABLE_NAME, COLUMN_NAME;

SELECT CON_ID, STATUS, WALLET_TYPE FROM V$ENCRYPTION_WALLET;

PROMPT >>> TDE xong. App KHONG can sua: van doc/ghi CCCD/CMND nhu thuong (Oracle tu giai ma).
PROMPT >>> GIU KY mat khau keystore + sao luu thu muc wallet.
