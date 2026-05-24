# Oracle Net Encryption

File nay ghi nhanh cach bat ma hoa duong truyen Oracle Net cho may client chay HospitalApp.

## 1. Tao hoac sua `sqlnet.ora`

Vi tri thuong gap:

```text
%ORACLE_HOME%\network\admin\sqlnet.ora
```

Noi dung de xuat:

```text
SQLNET.ENCRYPTION_CLIENT = REQUIRED
SQLNET.ENCRYPTION_TYPES_CLIENT = (AES256, AES192, AES128)
SQLNET.CRYPTO_CHECKSUM_CLIENT = REQUIRED
SQLNET.CRYPTO_CHECKSUM_TYPES_CLIENT = (SHA512, SHA384, SHA256)
```

Neu cau hinh tren server, dung cap `SERVER` tuong ung:

```text
SQLNET.ENCRYPTION_SERVER = REQUIRED
SQLNET.ENCRYPTION_TYPES_SERVER = (AES256, AES192, AES128)
SQLNET.CRYPTO_CHECKSUM_SERVER = REQUIRED
SQLNET.CRYPTO_CHECKSUM_TYPES_SERVER = (SHA512, SHA384, SHA256)
```

## 2. Kiem tra

Dang nhap bang SQL*Plus/SQL Developer va chay:

```sql
SELECT NETWORK_SERVICE_BANNER
FROM V$SESSION_CONNECT_INFO
WHERE SID = SYS_CONTEXT('USERENV','SID');
```

Ket qua dat yeu cau khi co dong chua `AES256 Encryption service` va dong checksum tuong ung.
