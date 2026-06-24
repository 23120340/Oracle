# Mã hóa (Cryptography) — bổ sung cho Access Control

Đồ án đã có **access control** ở tầng CSDL (RBAC + VPD + OLS + Audit). Tài liệu này thêm **tầng mã hóa**:

1. **Mã hóa đường truyền** — Oracle Native Network Encryption (NNE): mã hóa dữ liệu giữa app ↔ DB.
2. **Mã hóa dữ liệu nhạy cảm at-rest** — Transparent Data Encryption (TDE): mã hóa CCCD/CMND + tiền sử bệnh trên đĩa.

> Cả hai đều có sẵn trong Oracle Database XE 21c.

---

## 1. Mã hóa đường truyền — Native Network Encryption (NNE)

### 1.1. Cấu hình `sqlnet.ora` (phía server)
Vị trí: `%ORACLE_HOME%\network\admin\sqlnet.ora` (vd `C:\app\<user>\product\21c\dbhomeXE\network\admin\sqlnet.ora`).

Thêm (đặt `SERVER` = bắt buộc → mọi client phải mã hóa):
```text
SQLNET.ENCRYPTION_SERVER = REQUIRED
SQLNET.ENCRYPTION_TYPES_SERVER = (AES256, AES192, AES128)
SQLNET.CRYPTO_CHECKSUM_SERVER = REQUIRED
SQLNET.CRYPTO_CHECKSUM_TYPES_SERVER = (SHA512, SHA384, SHA256)
```

Tìm nhanh đường dẫn + mở file (PowerShell):
```powershell
$sqlnet = Join-Path $env:ORACLE_HOME "network\admin\sqlnet.ora"
if (-not $env:ORACLE_HOME) { Write-Host "Đặt ORACLE_HOME trước, vd: C:\app\<user>\product\21c\dbhomeXE" }
notepad $sqlnet   # tạo nếu chưa có
```

Không cần restart DB — kết nối mới sẽ tự thương lượng mã hóa. (Nếu muốn chắc: `lsnrctl reload`.)

### 1.2. Kiểm tra
**Phải kết nối qua TCP** (`@localhost:1521/XEPDB1`), KHÔNG dùng kết nối bequeath (`sqlplus / as sysdba` không có `@…`) — bequeath không đi qua tầng mạng nên không mã hóa. Mở **phiên MỚI** (sau khi sửa sqlnet.ora) rồi chạy:
```sql
SELECT NETWORK_SERVICE_BANNER
FROM   V$SESSION_CONNECT_INFO
WHERE  SID = SYS_CONTEXT('USERENV','SID');
```
Đạt yêu cầu khi xuất hiện **2 dòng**: `… Encryption service …` và `… Crypto-checksumming service …`
(một số bản in rõ `AES256 Encryption service adapter`, bản khác in gọn `Encryption service` — đều là đã bật).

> 💡 Bằng chứng mạnh nhất: vì `ENCRYPTION_SERVER = REQUIRED`, nếu client **không** thỏa thuật toán thì kết nối bị từ chối (`ORA-12660`). Do đó **kết nối thành công + có 2 dòng trên = đã mã hóa AES**.

### 1.3. Lưu ý với HospitalApp
- `Oracle.ManagedDataAccess` (app đang dùng) **hỗ trợ NNE** → tự mã hóa khi server `REQUIRED`, không cần sửa code.
- Nếu sau khi bật `REQUIRED` mà app **không kết nối được** (`ORA-12660`), tạm hạ xuống `REQUESTED` (vẫn mã hóa khi client hỗ trợ, nhưng không từ chối):
  `SQLNET.ENCRYPTION_SERVER = REQUESTED`.

---

## 2. Mã hóa dữ liệu nhạy cảm at-rest — TDE

Mã hóa cột: `BENHNHAN.CCCD`, `NHANVIEN.CMND` (NO SALT — giữ UNIQUE), `BENHNHAN.DIUNGTHUOC` (có salt).
**Trong suốt với app** — không sửa code, UNIQUE/tìm theo `=` vẫn chạy.

### 2.1. Chuẩn bị (1 lần) — KHÔNG cần restart
Keystore phải đặt **đúng vị trí mà instance đang tìm**, nếu lệch sẽ `ORA-28367 wallet does not exist`.
Tìm vị trí đó (kết nối SYS):
```sql
SELECT WRL_PARAMETER FROM V$ENCRYPTION_WALLET WHERE CON_ID = 1;
```
Trên XE mặc định ≈ `<ORACLE_BASE>\admin\<SID>\wallet` (máy demo này = `D:\Oracle\admin\XE\wallet`).
Tạo sẵn thư mục đó (rỗng) rồi dùng làm vị trí keystore:
```powershell
New-Item -ItemType Directory "D:\Oracle\admin\XE\wallet" -Force
```
> Dùng đúng vị trí mặc định ⇒ **không phải sửa `sqlnet.ora`, không restart**.
> (Phương án khác: `ALTER SYSTEM SET WALLET_ROOT='D:\Oracle\wallet' SCOPE=SPFILE;` rồi **restart DB** — bài này không dùng.)

### 2.2. Chạy script
`PhanHe2/15_TDE_Encryption.sql` đã điền sẵn giá trị cho máy demo (`D:\Oracle\admin\XE\wallet`,
keystore password `TdeWallet2025x`). Nếu máy khác, sửa lại đường dẫn + mật khẩu rồi chạy:
```powershell
$env:NLS_LANG = ".AL32UTF8"
sqlplus /nolog "@d:\repos\Oracle\PhanHe2\15_TDE_Encryption.sql"
```
Script: tạo keystore → mở (CONTAINER=ALL) → đặt master key **root rồi PDB** → tạo **auto-login**
(DB tự mở keystore khi khởi động) → `ALTER TABLE … ENCRYPT` → in danh sách cột mã hóa.

> ⚠️ `TIENSUBENH/TIENSUBENHGD` là `NVARCHAR2(2000)` → mã hóa báo **`ORA-28331`** (vượt 4000 byte sau overhead) → để nguyên (không mã hóa). PII quan trọng (CCCD/CMND) đã được mã hóa.
> Thuật toán mặc định là **AES192** (đủ mạnh); muốn AES256 thì thêm `USING 'AES256'` trong `ALTER TABLE … ENCRYPT`.

### 2.3. Kiểm tra (đã xác minh trên máy demo)
```sql
SELECT TABLE_NAME, COLUMN_NAME, ENCRYPTION_ALG, SALT FROM DBA_ENCRYPTED_COLUMNS ORDER BY 1,2;
SELECT CON_ID, STATUS, WALLET_TYPE FROM V$ENCRYPTION_WALLET;   -- XEPDB1 phải STATUS=OPEN
```
- `DBA_ENCRYPTED_COLUMNS`: `CCCD`, `CMND`, `DIUNGTHUOC` với `AES 192 bits key`.
- Đăng nhập **BVADMIN/BS/BN** rồi `SELECT MABN, CCCD FROM BENHNHAN` → CCCD hiện **plaintext** (giải mã trong suốt); `WHERE CCCD='…'` vẫn tìm được (NO SALT). Dữ liệu trên đĩa là ciphertext.

### 2.4. Cảnh báo vận hành
- **Sao lưu cả keystore** (`cwallet.sso`, `ewallet.p12`) cùng dữ liệu. Mất keystore = không giải mã được.
- Sau khi có **auto-login keystore**, DB tự mở keystore khi restart. Nếu không tạo auto-login, mỗi lần restart phải:
  `ADMINISTER KEY MANAGEMENT SET KEYSTORE OPEN IDENTIFIED BY "<KS_PWD>" CONTAINER=ALL;`
- Nếu báo lỗi *feature not available* khi tạo keystore → bản XE của bạn chưa bật TDE (hiếm với 21c). Khi đó vẫn còn tầng mã hóa đường truyền ở mục 1.

---

## Tóm tắt sau khi cấu hình
| Tầng | Cơ chế | Bảo vệ |
|------|--------|--------|
| Đường truyền | NNE (AES256 + SHA) | Chống nghe lén/sửa gói tin giữa app ↔ DB |
| Dữ liệu at-rest | TDE (AES) | CCCD/CMND/tiền sử bị mã hóa trên đĩa & backup |
| Truy cập | RBAC + VPD + OLS + Audit | Ai được đọc/sửa gì (đã có sẵn) |

→ Đồ án có đủ **access control + cryptography**.
