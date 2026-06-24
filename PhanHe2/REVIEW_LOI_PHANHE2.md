# 🔍 Báo cáo rà soát lỗi — Đồ án Phân hệ 2 (Oracle / Quản lý dữ liệu y tế)

> **Mục đích:** danh sách lỗi chi tiết để **sửa dần dần**. Mỗi mục có ô tick `[ ]`, vị trí `file:dòng`, mức độ, trạng thái kiểm chứng, lý do và cách sửa.
> **Phạm vi:** toàn bộ thư mục `PhanHe2/` (18 file SQL + `run_migrations.ps1`) và phần tích hợp với `HospitalApp` (WinForms).
> **Ngày rà soát:** 2026-06-23.

## Cách đọc trạng thái kiểm chứng
| Ký hiệu | Ý nghĩa |
|---|---|
| ✅ | **Đã xác nhận** — chắc chắn lỗi theo ngữ nghĩa Oracle + đã đối chiếu code. |
| ⚠️ | **Cần kiểm chứng khi chạy thật** — phụ thuộc môi trường (XE/EE, công cụ chạy, charset, undo…). |
| ℹ️ | **Ghi nhận / quyết định thiết kế** — không phải lỗi runtime, nên thuyết minh trong báo cáo. |
| ✏️ | **Đính chính** — claim ban đầu chưa chính xác, đã sửa lại cho đúng. |

## Mức độ
- **BLOCKER** — script không chạy được, hoặc một yêu cầu cốt lõi (TC/YC) **không đạt**.
- **HIGH** — sai logic/bảo mật nghiêm trọng hoặc dễ vỡ khi chạy.
- **MEDIUM** — sai mô hình/nhất quán, ảnh hưởng demo/đúng đắn.
- **LOW / INFO** — bảo trì, trình bày, hoặc khác đặc tả cần thuyết minh.

---

## 🛠️ TRẠNG THÁI SỬA LỖI — ĐỢT 1 (2026-06-23)

✔ = đã sửa trong code · ◑ = sửa một phần / phụ thuộc môi trường · ⏳ = hoãn (thiết kế hoặc cần xác nhận thêm).

| ID | TT | Tóm tắt việc đã làm |
|---|---|---|
| B1 | ✔ | Thêm nhánh KTV/BN vào 3 policy function VPD + miễn lọc cho BVADMIN (file 04) |
| B2 | ✔ | Bọc 4 `DBMS_RLS.ADD_POLICY` trong `BEGIN/END` (file 04) |
| B3 | ✔ | Đổi cột sang NVARCHAR2 (H1) + so sánh `NVL(...)` trong `trg_log_hsba_bs` (file 04) |
| B4 | ✔ | Tách tạo user (SYSTEM) / gán nhãn (LBACSYS) + bọc mọi `SA_*` trong block (file 05) |
| B5 | ✔ | `AUDIT UPDATE ON HSBA … WHENEVER NOT SUCCESSFUL` (bỏ mức cột) (file 06) |
| B6 | ✔ | `GRANT CREATE SYNONYM TO KTV_NV006/7` (file 03) |
| B7 | ✔ | Hợp nhất `CHECKPOINT_LOG` (EVENT_NAME PK, CREATED_AT) + idempotent (file 07) |
| B8 | ✔ | `run_migrations.ps1` chạy đủ 01→13 + setup_all đúng thứ tự |
| B9 | ◑ | Bảo đảm bằng thứ tự chạy (B8) + header cảnh báo; dọn phụ thuộc thừa trong setup_all |
| B10 | ◑ | `SYS/password`→`SYS/&&sys_pwd`; mật khẩu cố định khác (SYSTEM/oracle, LBACSYS/lbacsys, BVADMIN) **vẫn cần chỉnh theo môi trường** |
| H1 | ✔ | NCLOB→NVARCHAR2(2000) cho 6 cột (file 01) |
| H2 | ✔ | Bỏ `NV_NHANVIEN_View` trùng ở file 08 (giữ bản 12 cột ở 09) |
| H3 | ✔ | File 12 sửa HSBA_DV theo `(MAHSBA,NGAYDV)` thay vì LIKE trên cột PK LOAIDV |
| H4 | ✔ | Audit mức bảng cho UPDATE thất bại → bắt cập nhật bất hợp pháp (file 06) |
| H5 | ◑ | Thêm PHẦN 1B (file 06): kiểm `v$option` + mẫu Unified Audit Policy + đọc `UNIFIED_AUDIT_TRAIL` |
| H6 | ◑ | `WHENEVER SQLERROR CONTINUE` quanh phần test; vẫn cần SQL*Plus/SQLcl cho multi-CONNECT |
| H7 | ✔ | Gán nhãn OLS chuyển hẳn về LBACSYS (file 05/09); bỏ khỏi setup_all |
| H8 | ✔ | `WHENEVER SQLERROR EXIT` + kiểm `$LASTEXITCODE` (run_migrations) |
| H9 | ✔ | Job DataPump gọi `.bat`, không tự ENABLE (file 07) |
| H10 | ✔ | Job RMAN gọi `.bat`, không tự ENABLE (file 07) |
| H11 | ✔ | `GRANT EXECUTE ON DBMS_FLASHBACK TO BVADMIN` (file 07) |
| M2 | ✔ | So sánh NULL-safe trong INSTEAD OF trigger (file 03, 09) |
| M3 | ✔ | Thêm dữ liệu `TIENSUBENHGD` (file 01, 12) |
| M10 | ✔ | Bỏ phần CAPBAC/COSO/KHOA_NHAN + OLS trùng trong setup_all |
| M11 | ◑ | Bỏ chú thích mâu thuẫn trong setup_all; grant log theo file 13 (cân nhắc thu hẹp sau) |
| M12 | ✔ | FGA 3a audit đủ 4 cột MAHSBA/NGAYDT/TENTHUOC/LIEUDUNG (file 06) |
| L6 | ✔ | Verify dùng literal `N'...'` (file 12) |
| L10 | ✔ | Đổi alias `ROWS`→`SO_DONG` (setup_all) |
| RBAC-6 | ✔ | `BN_HSBA_View` lọc 1 bảng bằng `fn_get_mabn()` (file 03) |
| RBAC-11 | ✔ | `WHENEVER SQLERROR CONTINUE` cô lập phần test (file 03/04/06) |
| M7 | ✔ | Bỏ grant `NV_LOOKUP_View` cho KTV (DPV/BS dùng, KTV không) — setup_all + file 11 |
| M14 | ✔ | Giải quyết qua B1 (form KTV/BN có dữ liệu) — không sửa code app |
| L5 | ✔ | File 09 ADD cột idempotent từng cột (`add_col_if_missing`) |
| L7 | ✔ | `trg_log_ketqua` chỉ ghi khi KETQUA thực sự đổi |
| L11 | ✔ | 4 `CREATE ROLE` idempotent (bỏ qua ORA-01921) — phần CREATE TABLE/USER còn lại tùy chọn |
| M1, M5, M13, M16, L1–L4, L8, L9, L12–L14 | ⏳ | Hoãn (thiết kế / code app / môi trường) — xem ghi chú "HOÃN" ở từng mục bên dưới |

**Sau khi sửa, cần biết:**
- App `KTVForm`/`BNForm` giờ sẽ hiển thị dữ liệu (B1 đã thông VPD) → **không cần sửa code app** (M14 tự giải quyết).
- **Vẫn phải chỉnh mật khẩu** trong các lệnh `CONNECT SYSTEM/oracle`, `LBACSYS/lbacsys`, `BVADMIN/"BVAdmin@2025"` cho khớp DB của bạn (B10).
- Nên chạy thử trên Oracle thật một lần để xác nhận (OLS cần đã cài `catols.sql`; kiểm chế độ Unified Auditing cho H5).
- Vì mục đích "sửa dần", các ô `[ ]` bên dưới đã được tick `[x]` cho mục đã sửa; mục ⏳ kèm dòng **HOÃN** giải thích lý do chưa đổi code.

---

## ⭐ TÓM TẮT ĐIỀU HÀNH (đọc trước)

> ✅ **Cả 4 nhóm blocker mô tả dưới đây ĐÃ ĐƯỢC SỬA trong ĐỢT 1** (xem bảng "TRẠNG THÁI SỬA LỖI" phía trên). Phần mô tả dưới giữ nguyên để hiểu bối cảnh/nguyên nhân.

Hệ thống (ban đầu) có **một xung đột kiến trúc lớn** và **nhiều lỗi cú pháp PL/SQL khiến script không chạy**. Bốn việc phải sửa đầu tiên:

1. **`B1` — VPD (file 04) "khóa chết" KTV và Bệnh nhân.** Policy trả `1=0` cho mọi vai trò ngoài DPV/BS, nhưng KTV và BN lại đọc đúng các bảng có VPD → **thấy 0 dòng, lưu 0 dòng (không báo lỗi)**. ⇒ TC#4, TC#5 và các form `KTVForm`/`BNForm` hỏng hoàn toàn.
2. **`B2`/`B4`/`B5` — gọi thủ tục "trần" ngoài `BEGIN/END`.** `DBMS_RLS.ADD_POLICY` (file 04), `SA_USER_ADMIN.SET_USER_LABELS`/`SA_SESSION.SET_ROW_LABEL` (file 05), `AUDIT UPDATE(cột)` (file 06) đều **không hợp lệ** trong SQL*Plus/SQLcl ⇒ VPD không được tạo, nhãn OLS không được gán, audit cột lỗi cú pháp ⇒ **YC1‑Câu3, YC2, một phần YC3 không chạy như mô tả**.
3. **`B3` — so sánh NCLOB bằng `!=`** trong trigger ghi vết của Bác sĩ (file 04) ⇒ `ORA-00932` ⇒ **BS không UPDATE được CHANDOAN/DIEUTRI/KETLUAN** và TC#3(c) ghi vết hỏng.
4. **`B8`/`B9` — quy trình cài đặt sai thứ tự.** `run_migrations.ps1` chỉ chạy 2 file (12 + setup_all) và bỏ qua 01–10; `setup_all` lại phụ thuộc các đối tượng do 03/04/08 tạo ⇒ chạy theo hướng dẫn sẽ **fail hàng loạt**.

> ⚠️ **Lưu ý quan trọng về `B1` vs `B2`:** Hai lỗi này "che" nhau. Nếu sửa cú pháp `ADD_POLICY` (B2) cho VPD chạy thật → KTV/BN bị khóa (B1) lộ ra. Nếu để nguyên B2 → VPD không tạo → BS **thấy tất cả HSBA** (lỗ hổng bảo mật, YC1‑C3 không đạt). **Phải sửa cả hai cùng lúc.**

---

# PHẦN A — LỖI CHẶN (BLOCKER)

### [x] B1 · ✔ ĐÃ SỬA · Xung đột VPD ↔ RBAC: KTV và Bệnh nhân thấy 0 dòng `(RBAC-1)`
- **File:** `04_YC1_C3_VPD_DPV_BS.sql` (hàm `vpd_hsba`/`vpd_hsba_dv`/`vpd_benhnhan`, dòng 66–131; `ADD_POLICY` 162–207) ⨯ `03_YC1_C2_RBAC_KTV_BN.sql` (`KTV_HSBA_DV_View` 27–35, `BN_BENHNHAN_View` 113–120, `BN_HSBA_View` 155–164)
- **Mức:** BLOCKER · **Trạng thái:** ✅
- **Vì sao:** VPD (DBMS_RLS) được enforce **cả khi truy cập gián tiếp qua view của chính owner** (BVADMIN), và **cả với chính owner** (trừ `SYS` hoặc user có `EXEMPT ACCESS POLICY`). Với session là KTV → `fn_get_vaitro()='KTV'` → nhánh `ELSE` trả `'1=0'`. Với Bệnh nhân (`BN_BNxxx`) → không có dòng trong `NHANVIEN` → `fn_get_vaitro()` trả `NULL` → cũng `'1=0'`. Hệ quả: `KTV_HSBA_DV_View`, `BN_BENHNHAN_View`, `BN_HSBA_View` luôn trả **0 dòng**; `INSTEAD OF` trigger của KTV cũng vô tác dụng vì câu `UPDATE HSBA_DV … WHERE MAKTV=v_manv` bên trong cũng bị VPD lọc về `1=0` ⇒ **lưu KETQUA cập nhật 0 dòng, không báo lỗi**.
- **Cách sửa:** Cho KTV/BN một predicate đúng trong cả 3 policy function trước khi rơi vào `ELSE '1=0'`. Ví dụ:
  ```sql
  -- vpd_hsba_dv: bổ sung nhánh KTV
  ELSIF v_vaitro = 'KTV' THEN
      RETURN 'MAKTV = ''' || v_manv || '''';
  ```
  ```sql
  -- vpd_benhnhan & vpd_hsba: bổ sung nhánh "là bệnh nhân"
  -- (v_vaitro NULL nhưng SESSION_USER khớp BENHNHAN.ORACLE_USER)
  IF v_vaitro IS NULL THEN
      RETURN 'MABN IN (SELECT MABN FROM BENHNHAN
                       WHERE ORACLE_USER = SYS_CONTEXT(''USERENV'',''SESSION_USER''))';
  END IF;
  ```
  *Hoặc* tách bạch kiến trúc: KTV/BN dùng RBAC‑view (không đặt VPD trên `HSBA_DV`/`BENHNHAN`), DPV/BS dùng VPD trên bảng riêng — nhưng vì các bảng dùng chung nên giải pháp gọn nhất là mở rộng policy function như trên.

### [x] B2 · ✔ ĐÃ SỬA · `DBMS_RLS.ADD_POLICY` gọi trần ngoài `BEGIN/END` → VPD không được tạo `(VPD-2, mới)`
- **File:** `04_YC1_C3_VPD_DPV_BS.sql:162–207` (cả 4 lời gọi `ADD_POLICY`)
- **Mức:** BLOCKER · **Trạng thái:** ✅
- **Vì sao:** Trong SQL*Plus/SQLcl/SQL Developer "Run Script", gọi thủ tục dạng `DBMS_RLS.ADD_POLICY(...)` như một câu lệnh độc lập là **không hợp lệ** → `SP2-0734: unknown command` / `ORA-00900: invalid SQL statement`. ⇒ **4 policy VPD không được tạo** ⇒ YC1‑Câu3 (VPD cho DPV/BS) không được hiện thực; BS sẽ thấy **toàn bộ** HSBA/HSBA_DV/BENHNHAN (mất kiểm soát hàng).
- **Cách sửa:** bọc mỗi lời gọi trong khối ẩn danh:
  ```sql
  BEGIN
    DBMS_RLS.ADD_POLICY(object_schema=>'BVADMIN', object_name=>'HSBA',
      policy_name=>'POL_HSBA_DPV_BS', function_schema=>'BVADMIN',
      policy_function=>'vpd_hsba',
      statement_types=>'SELECT,INSERT,UPDATE,DELETE',
      update_check=>TRUE, enable=>TRUE);
  END;
  /
  ```
  (Làm tương tự cho 3 policy còn lại.)

### [x] B3 · ✔ ĐÃ SỬA · So sánh NCLOB bằng `!=` trong `trg_log_hsba_bs` → ORA‑00932 `(VPD-3, mới)`
- **File:** `04_YC1_C3_VPD_DPV_BS.sql:231, 239, 246` (trigger `trg_log_hsba_bs`)
- **Mức:** BLOCKER · **Trạng thái:** ✅
- **Vì sao:** `CHANDOAN/DIEUTRI/KETLUAN` là **NCLOB**. PL/SQL **không cho** so sánh LOB bằng `=`/`!=`/`<>` → `ORA-00932: inconsistent datatypes: expected - got NCLOB`. Trigger là `AFTER UPDATE OF CHANDOAN,DIEUTRI,KETLUAN` ⇒ khi BS thực hiện đúng hành vi TC#3(c) (cập nhật chẩn đoán), trigger nổ lỗi → **toàn bộ câu UPDATE bị rollback** → BS không cập nhật được, và ghi vết TC#3(c) không hoạt động.
- **Cách sửa:** dùng `DBMS_LOB.COMPARE`, hoặc bỏ điều kiện so sánh (vì trigger đã chỉ kích hoạt khi 3 cột đó nằm trong `UPDATE OF`):
  ```sql
  IF DBMS_LOB.COMPARE(NVL(:NEW.CHANDOAN, EMPTY_CLOB()),
                      NVL(:OLD.CHANDOAN, EMPTY_CLOB())) != 0 THEN
     INSERT INTO LOG_BS_HSBA(...) VALUES(:OLD.MAHSBA,'CHANDOAN',:OLD.CHANDOAN,:NEW.CHANDOAN,v_user);
  END IF;
  ```
  *Lưu ý:* `EMPTY_CLOB()` dùng cho NCLOB vẫn chấp nhận; hoặc đổi 3 cột sang `NVARCHAR2(2000)` (xem `H1`) để so sánh trực tiếp được.

### [x] B4 · ✔ ĐÃ SỬA · Các lệnh OLS `SA_*` gọi trần ngoài `BEGIN/END` → nhãn u1‑u8, t1‑t7 không được gán `(OLS-1 / SCHEMA-11)`
- **File:** `05_YC2_OLS_ThongBao.sql` — `SET_USER_LABELS` (180,191,202,213,224,235,246,257), `SET_USER_PRIVS` (270), `SA_SESSION.SET_ROW_LABEL` (278,283,288,293,298,303,308), `RESTORE_DEFAULT_LABELS` (315)
- **Mức:** BLOCKER · **Trạng thái:** ✅
- **Vì sao:** Giống B2 — gọi thủ tục trần là `SP2-0734`/`ORA-00900`. Các khối `CREATE_LEVEL/CREATE_LABEL/APPLY_TABLE_POLICY` phía trên **có** bọc `BEGIN/END`, nhưng phần gán nhãn user và gán nhãn dòng (`SET_ROW_LABEL` trước mỗi `INSERT`) thì **không** ⇒ **u1‑u8 không có nhãn đọc, t1‑t7 không có nhãn dữ liệu** ⇒ toàn bộ kiểm thử OLS (YC2) sai/không chạy.
- **Cách sửa:** bọc từng lời gọi trong `BEGIN … END; /` hoặc dùng `EXEC`. Ví dụ:
  ```sql
  BEGIN SA_USER_ADMIN.SET_USER_LABELS('BV_LABEL_POLICY','U1_GIAMDOC','BGD:HCM,HPN,HNI:TH,TK,TM'); END;
  /
  ```
  Với `SET_ROW_LABEL` + `INSERT`: gom vào một block: `BEGIN SA_SESSION.SET_ROW_LABEL(...); INSERT ...; END; /`.

### [x] B5 · ✔ ĐÃ SỬA · `AUDIT UPDATE(cột)` — Standard Audit không hỗ trợ mức cột `(AUDIT-1, mới)`
- **File:** `06_YC3_Audit.sql:133–135` (`AUDIT UPDATE(CHANDOAN) ON BVADMIN.HSBA …`, tương tự DIEUTRI/KETLUAN)
- **Mức:** BLOCKER (cho YC3‑3c phần standard audit) · **Trạng thái:** ✅
- **Vì sao:** Lệnh `AUDIT` chuẩn **chỉ** nhận `AUDIT <option> ON <object>`, **không** nhận danh sách cột. `AUDIT UPDATE(CHANDOAN) ON …` là sai cú pháp → lỗi parse. Audit mức cột chỉ làm được bằng **FGA** (`DBMS_FGA.ADD_POLICY` với `audit_column`).
- **Cách sửa:** bỏ phần cột: `AUDIT UPDATE ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;` (theo dõi mọi UPDATE thất bại trên HSBA), và để FGA đảm nhiệm phần "theo cột". Xem thêm `H4` về logic 3c.

### [x] B6 · ✔ ĐÃ SỬA · `KTV_NV006` thiếu quyền `CREATE SYNONYM` → ORA‑01031 `(RBAC-2)`
- **File:** `03_YC1_C2_RBAC_KTV_BN.sql:104–105`
- **Mức:** BLOCKER (dừng script tại đây) · **Trạng thái:** ✅
- **Vì sao:** Tài khoản `KTV_NV006` tạo ở file 02 chỉ được `GRANT CREATE SESSION` (02:54). Tạo **private synonym** cần quyền hệ thống `CREATE SYNONYM` → `CREATE OR REPLACE SYNONYM MY_HSBA_DV …` báo `ORA-01031: insufficient privileges`.
- **Cách sửa:** hoặc cấp quyền trước khi `CONNECT KTV_NV006` (chạy bởi SYSTEM): `GRANT CREATE SYNONYM TO KTV_NV006, KTV_NV007;` — *nhớ cấp cho cả NV007 cho nhất quán*; **hoặc** bỏ synonym và để form/SQL dùng tiền tố `BVADMIN.KTV_HSBA_DV_View`; **hoặc** DBA tạo `PUBLIC SYNONYM`.

### [x] B7 · ✔ ĐÃ SỬA · `CHECKPOINT_LOG` định nghĩa 2 lần, cấu trúc cột mâu thuẫn `(BACKUP-1)`
- **File:** `07_YC4_Backup_Recovery.sql:197–202` (cột `EVENT_NAME, SCN, EVENT_TIME, CREATED_BY`, không PK) ⨯ `09_Recovery_Demo.sql:17–25` (cột `EVENT_NAME PK, SCN NOT NULL, CREATED_AT`)
- **Mức:** BLOCKER (cho demo recovery) · **Trạng thái:** ✅
- **Vì sao:** Nếu 07 chạy trước, bảng đã tồn tại nên khối `IF v_count=0` trong 09 **bỏ qua** việc tạo lại → bảng thực tế **không có cột `CREATED_AT` và không có PK**. Khi đó `MERGE … UPDATE SET c.CREATED_AT` (09:37) và `SELECT … CREATED_AT` (09:67) báo `ORA-00904: invalid identifier`; `MERGE … ON (EVENT_NAME)` cũng không có UNIQUE để upsert đúng.
- **Cách sửa:** chỉ giữ **một** định nghĩa `CHECKPOINT_LOG` (đặt ở 07 hoặc file riêng) với `EVENT_NAME` PK, `SCN NUMBER NOT NULL`, một cột thời gian thống nhất tên (`CREATED_AT`). File 09 chỉ tham chiếu, không tạo lại với schema khác.

### [x] B8 · ✔ ĐÃ SỬA · `run_migrations.ps1` bỏ qua hoàn toàn 01–10 `(ORCHESTRATION-1)`
- **File:** `run_migrations.ps1:33–36` (mảng `$migrations = @("12_Fix_UTF8_Data.sql","setup_all.sql")`)
- **Mức:** BLOCKER · **Trạng thái:** ✅
- **Vì sao:** Wrapper chỉ chạy 2 file. Toàn bộ schema/account/RBAC/VPD/OLS/audit (01–10) **không** được chạy. Người dùng theo hướng dẫn `run_migrations.ps1` sẽ có DB rỗng → 12 và setup_all đều fail.
- **Cách sửa:** liệt kê đầy đủ và đúng thứ tự `01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11 → 12 → 13 → setup_all` (đồng thời xử lý các file cần chạy bằng SYSTEM/SYS/LBACSYS — xem `H6`/`B10`). Hoặc ghi rõ `run_migrations` chỉ là bước "vá" sau khi đã cài tay 01–10.

### [~] B9 · ◑ MỘT PHẦN · `setup_all.sql` phụ thuộc đối tượng chưa tồn tại → ORA‑01919 / ORA‑00942 `(ORCHESTRATION-2)`
> **Đã làm:** dọn phụ thuộc thừa (bỏ phần OLS/cột trùng), thêm header bắt buộc chạy sau 01–13; thứ tự được bảo đảm bởi `run_migrations` (B8). **Còn lại:** chưa thêm guard kiểm tra tồn tại role/bảng (BVADMIN không chắc có quyền đọc `DBA_ROLES`).
- **File:** `setup_all.sql:31–43` (GRANT … TO `DPV_Role`/`BS_Role`/`KTV_Role`), `120–124` (SELECT từ `APP_LOGIN_LOG`, `LOG_BS_HSBA`, `LOG_BS_DONTHUOC`, `LOG_KTV_KETQUA`)
- **Mức:** BLOCKER (nếu chạy độc lập) · **Trạng thái:** ✅
- **Vì sao:** Các role tạo ở 03/04, các bảng log tạo ở 03/04/08. Nếu chưa chạy → `GRANT … TO DPV_Role` báo `ORA-01919: role 'DPV_ROLE' does not exist`; `SELECT … FROM APP_LOGIN_LOG` báo `ORA-00942: table or view does not exist`.
- **Cách sửa:** đảm bảo `setup_all` chạy **sau** 03/04/08, hoặc thêm kiểm tra tồn tại (PL/SQL `EXECUTE IMMEDIATE` + xử lý exception) trước khi GRANT/SELECT.

### [~] B10 · ◑ MỘT PHẦN · `CONNECT` mật khẩu giả/cứng xuyên suốt → đứt session, lệnh sau chạy sai `(BACKUP-6 / RBAC-3 / ORCHESTRATION-7,8)`
> **Đã làm:** `SYS/password` → `SYS/&&sys_pwd` (06/07/10) + `run_migrations` cấp `DEFINE sys_pwd`; thêm `WHENEVER SQLERROR EXIT`. **Còn lại (bạn phải tự làm):** chỉnh các mật khẩu cố định `SYSTEM/oracle`, `LBACSYS/lbacsys`, `BVADMIN/"BVAdmin@2025"` cho khớp DB của bạn (không thể tự đoán).
- **File:** `06,07,09` dùng `CONNECT SYS/password AS SYSDBA`; nhiều file dùng `CONNECT SYSTEM/oracle`, `CONNECT LBACSYS/lbacsys`. (07:86,143,166,194,211,234,290; 06:19,36,83,132,207; 03:94,167; 05:24,267,357)
- **Mức:** BLOCKER (theo môi trường) · **Trạng thái:** ✅
- **Vì sao:** `SYS/password` là placeholder → `ORA-01017: invalid username/password`; khi `CONNECT` thất bại trong SQL*Plus, các lệnh tiếp theo **chạy trên session cũ** hoặc fail im lặng → ví dụ phần "PHẦN 1: kích hoạt audit" sau `CONNECT SYS/password` sẽ chạy sai. Ngoài ra mật khẩu cứng `BVAdmin@2025`, `SYSTEM/oracle` nhúng trong file là rủi ro và không khớp DB thực của người chấm.
- **Cách sửa:** thay placeholder bằng hướng dẫn rõ (hoặc biến thay thế SQL*Plus `&&sys_pwd`), và **tách file theo user** (một file phần SYS, một phần SYSTEM, một phần BVADMIN, một phần LBACSYS). Thêm `WHENEVER SQLERROR EXIT FAILURE` ở đầu các script cài đặt để dừng ngay khi `CONNECT`/lệnh lỗi.

---

# PHẦN B — LỖI NẶNG (HIGH)

### [x] H1 · ✔ ĐÃ SỬA · Dùng NCLOB cho các cột cần so sánh/ghi vết `(SCHEMA-6)`
- **File:** `01_schema_data.sql:40–41` (`TIENSUBENH`, `TIENSUBENHGD`), `68–72` (`CHANDOAN/DIEUTRI/KETLUAN`), `81` (`KETQUA`)
- **Mức:** HIGH · **Trạng thái:** ✅ (đây là **gốc rễ** của B3)
- **Vì sao:** TC#3(c), TC#4, YC3 yêu cầu phát hiện thay đổi các trường này; mọi so sánh `:OLD`/`:NEW` bằng toán tử thường trên NCLOB đều `ORA-00932`.
- **Cách sửa:** nếu nội dung không quá dài → đổi sang `NVARCHAR2(2000)`/`VARCHAR2(4000)` (so sánh trực tiếp được, đơn giản hóa trigger). Nếu giữ NCLOB → mọi nơi phải dùng `DBMS_LOB.COMPARE`.

### [x] H2 · ✔ ĐÃ SỬA · `NV_NHANVIEN_View` (+trigger) định nghĩa lại ở cả 08 và 09, khác cấu trúc `(SCHEMA-7)`
- **File:** `08_App_Migrations.sql:40–69` (9 cột) ⨯ `09_OLS_NhanVien_Unified.sql:43–75` (12 cột, thêm `CAPBAC/COSO/KHOA_NHAN`)
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** Phụ thuộc thứ tự chạy. Nếu chạy 09 trước 08, bản cuối là 9 cột (mất nhãn OLS); nếu 08 chạy lại sau 09 → view co về 9 cột → form đọc 12 cột vỡ.
- **Cách sửa:** chỉ giữ **một** định nghĩa (bản 12 cột ở 09); xóa định nghĩa trùng ở 08. Bảo đảm thứ tự 08 → 09.

### [x] H3 · ✔ ĐÃ SỬA · File 12 sửa encoding bằng `UPDATE … LOAIDV LIKE '%…%'` trên cột thuộc PRIMARY KEY `(SCHEMA-8)`
- **File:** `12_Fix_UTF8_Data.sql:92–104`; PK `PK_HSBA_DV(MAHSBA,LOAIDV,NGAYDV)` tại `01:82`
- **Mức:** HIGH · **Trạng thái:** ⚠️
- **Vì sao:** (a) nếu dữ liệu đã đúng UTF‑8, pattern `'%t nghi%'`/`'%i%u %m%'` có thể không khớp hoặc khớp nhầm cả 2 dòng của HS001 → cập nhật sai dòng PK hoặc 0 dòng; (b) nếu dữ liệu bị mojibake, pattern tiếng Việt không khớp byte hỏng → `UPDATE` 0 dòng (không sửa được); (c) sửa chính cột PK bằng điều kiện trên cột PK có thể gây `ORA-00001` nếu trùng.
- **Cách sửa:** xác định dòng bằng khóa ổn định không-tự-sửa: HS001 có 2 dịch vụ khác `NGAYDV` → dùng `WHERE MAHSBA='HS001' AND NGAYDV=DATE'2025-04-01'`. **Tốt nhất:** đặt `NLS_LANG=.AL32UTF8` trước khi chạy 01 (hoặc dùng `UNISTR('\xxxx')`) để INSERT đúng ngay từ đầu, khỏi cần file 12.

### [x] H4 · ✔ ĐÃ SỬA · YC3‑3c "cập nhật bất hợp pháp" thực tế **không bắt được** `(AUDIT-2, mới)`
- **File:** `06_YC3_Audit.sql:128–153` (standard audit cột + `FGA_HSBA_ILLEGAL_UPDATE`)
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** Người không phải BS **không có** column‑grant UPDATE trên CHANDOAN/DIEUTRI/KETLUAN → câu UPDATE bị chặn ngay ở tầng quyền (`ORA-01031`) **trước khi** truy cập dòng → **FGA không kích hoạt** (FGA chỉ ghi khi truy cập hàng thành công). Còn standard audit phần cột thì sai cú pháp (B5). ⇒ "hành vi cập nhật bất hợp pháp" gần như không sinh bản ghi nào. (BS cập nhật HSBA của BN khác bị VPD lọc 0 dòng → cũng không có audit.)
- **Cách sửa:** dùng standard audit mức **bảng** cho thất bại: `AUDIT UPDATE ON BVADMIN.HSBA BY ACCESS WHENEVER NOT SUCCESSFUL;` (bắt `ORA-01031`). Muốn bắt cả "vượt VPD thành công" thì giữ FGA `FGA_HSBA_ILLEGAL_UPDATE` (điều kiện `fn_get_vaitro()!='BS'`). Ghi rõ trong báo cáo hai lớp bắt: quyền‑bị‑từ‑chối (standard) + truy cập‑bất‑thường (FGA).

### [~] H5 · ◑ MỘT PHẦN · Oracle 21c XE mặc định Unified Auditing → `DBA_AUDIT_TRAIL` có thể rỗng `(AUDIT-3, mới)`
> **Đã làm:** thêm **PHẦN 1B** vào file 06 — câu kiểm tra `v$option`, mẫu **Unified Audit Policy** tương đương 5 ngữ cảnh, và cách đọc `UNIFIED_AUDIT_TRAIL`. **Còn lại (theo môi trường):** học viên chọn nhánh phù hợp khi demo tùy chế độ DB.
- **File:** `06_YC3_Audit.sql` (toàn bộ phần đọc `DBA_AUDIT_TRAIL`/`DBA_FGA_AUDIT_TRAIL`, `audit_trail=DB,EXTENDED`)
- **Mức:** HIGH · **Trạng thái:** ⚠️
- **Vì sao:** Trên 21c, nếu DB ở chế độ **pure unified auditing**, các lệnh `AUDIT` kiểu cũ vẫn chạy nhưng bản ghi vào **`UNIFIED_AUDIT_TRAIL`**, còn `DBA_AUDIT_TRAIL`/`DBA_FGA_AUDIT_TRAIL` **rỗng** → các câu "đọc xuất nhật ký" ở Phần 5 trả 0 dòng dù đã audit. `DBMS_FGA.DB+EXTENDED` cũng bị bỏ qua ở pure unified mode.
- **Cách sửa:** kiểm tra chế độ: `SELECT value FROM v$option WHERE parameter='Unified Auditing';`. Nếu `TRUE` → tạo **Unified Audit Policy** (`CREATE AUDIT POLICY … ; AUDIT POLICY …;`) và đọc từ `UNIFIED_AUDIT_TRAIL`. Ghi rõ trong báo cáo bạn dùng mixed‑mode hay pure unified.

### [~] H6 · ◑ MỘT PHẦN · Nhiều `CONNECT` đan xen → chỉ chạy được trên SQL*Plus/SQLcl `(RBAC-3 / ORCHESTRATION-7)`
> **Đã làm:** thêm `WHENEVER SQLERROR CONTINUE` cô lập phần test cố ý lỗi (03/04/06) để chạy tự động không gãy. **Còn lại (theo thiết kế):** vẫn phải chạy bằng SQL*Plus/SQLcl vì các file dùng `CONNECT` (SQL Developer "Run Script" có thể bỏ qua) — đã ghi rõ trong header `run_migrations.ps1`.
- **File:** `03,04,05,06,07,09,10` (đan xen BVADMIN/SYSTEM/SYS/LBACSYS/u‑user)
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** `CONNECT` là lệnh client SQL*Plus/SQLcl, **không** phải SQL. Trên SQL Developer "Run Script" có thể bị bỏ qua/đòi mật khẩu tương tác → các `CREATE ROLE`/`GRANT` chạy nhầm dưới BVADMIN (BVADMIN không có `CREATE ROLE` trực tiếp → `ORA-01031`).
- **Cách sửa:** ghi rõ "chạy bằng SQL*Plus hoặc SQLcl", hoặc tách script theo từng user. (BVADMIN ở file 01 chỉ có `CONNECT,RESOURCE,CREATE VIEW/PROCEDURE/SEQUENCE/TRIGGER/ANY CONTEXT` — **không** có `CREATE ROLE`, nên các `CREATE ROLE` buộc phải do SYSTEM chạy ⇒ càng cần `CONNECT` đúng.)

### [x] H7 · ✔ ĐÃ SỬA · `setup_all` gọi `SA_USER_ADMIN.SET_USER_LABELS` dưới BVADMIN → thiếu quyền OLS, lỗi bị nuốt `(ORCHESTRATION-3)`
- **File:** `setup_all.sql:85–115` (block `BEGIN … SET_USER_LABELS … EXCEPTION WHEN OTHERS`)
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** Quản trị nhãn user khác phải do `LBACSYS` hoặc user có quyền admin của policy (vai trò `BV_LABEL_POLICY_DBA`). BVADMIN chỉ được `SET_USER_PRIVS … FULL` (quyền dùng nhãn cho **chính mình**, không phải quyền gán nhãn cho user khác). Khối có `EXCEPTION WHEN OTHERS` nuốt lỗi ⇒ **nhãn nhân viên không được gán** mà không có cảnh báo.
- **Cách sửa:** chạy phần gán nhãn dưới `LBACSYS` (như file 09 đã làm đúng với `sp_apply_ols_label_for_nv`), hoặc cấp `BV_LABEL_POLICY_DBA` cho BVADMIN. Bỏ `WHEN OTHERS` nuốt lỗi (ít nhất log `SQLERRM`).

### [x] H8 · ✔ ĐÃ SỬA · `run_migrations.ps1` dựa `$LASTEXITCODE` của sqlplus nhưng thiếu `WHENEVER SQLERROR EXIT` `(ORCHESTRATION-9)`
- **File:** `run_migrations.ps1:46–50`
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** `sqlplus` trả exit code 0 ngay cả khi lệnh SQL lỗi (trừ khi đặt `WHENEVER SQLERROR EXIT SQL.SQLCODE`). ⇒ migration báo "thành công" trong khi thực ra fail.
- **Cách sửa:** thêm `WHENEVER SQLERROR EXIT SQL.SQLCODE` (và `WHENEVER OSERROR EXIT`) ở đầu mỗi file `.sql`, hoặc `SET ECHO ON` + kiểm tra log.

### [x] H9 · ✔ ĐÃ SỬA · `JOB_DATAPUMP_BACKUP` gọi `expdp.exe` không truyền tham số `(BACKUP-3)`
- **File:** `07_YC4_Backup_Recovery.sql:149–161`
- **Mức:** HIGH · **Trạng thái:** ✅
- **Vì sao:** Job `EXECUTABLE` gọi `expdp.exe` mà **không** có `number_of_arguments`/`SET_JOB_ARGUMENT_VALUE` → `expdp` chạy không tham số → in usage rồi thoát, không export gì (và cần credential/parfile).
- **Cách sửa:** đóng gói lệnh `expdp` vào file `.bat`/parfile và trỏ `job_action` tới đó; hoặc khai báo đủ argument (`SCHEMAS=BVADMIN DIRECTORY=DATA_PUMP_DIR DUMPFILE=… LOGFILE=…`) và truyền credential qua wallet. (Tương tự xem `H10` cho RMAN job.)

### [x] H10 · ✔ ĐÃ SỬA · `JOB_RMAN_FULL_BACKUP` truyền tham số `'TARGET','/','@script'` sai cách `(BACKUP-2)`
- **File:** `07_YC4_Backup_Recovery.sql:89–106`
- **Mức:** HIGH · **Trạng thái:** ⚠️
- **Vì sao:** `rman` mong dòng lệnh dạng `rman TARGET / CMDFILE=…`. Tách `'TARGET'` và `'/'` thành 2 argument rời dễ khiến RMAN parse sai. Job `enabled=>FALSE` rồi `ENABLE` ngay (105) ⇒ tới lịch sẽ chạy với chuỗi dễ lỗi.
- **Cách sửa:** gói toàn bộ vào `.bat` (`rman target / cmdfile=C:\scripts\rman_full_backup.rcv log=…`) và để `job_action` trỏ `.bat`; hoặc `number_of_arguments=>2` với `arg1='TARGET=/'`, `arg2='CMDFILE=…'`.

### [x] H11 · ✔ ĐÃ SỬA · BVADMIN có thể thiếu `EXECUTE ON DBMS_FLASHBACK` `(BACKUP-4)`
- **File:** `07:205–207`, `09:32` (`DBMS_FLASHBACK.GET_SYSTEM_CHANGE_NUMBER`)
- **Mức:** HIGH · **Trạng thái:** ⚠️
- **Vì sao:** `EXECUTE ON DBMS_FLASHBACK` không cấp mặc định cho user thường ở các bản gần đây → `ORA-00904`/`PLS-00201: identifier 'DBMS_FLASHBACK' must be declared` khi BVADMIN gọi.
- **Cách sửa:** `GRANT EXECUTE ON DBMS_FLASHBACK TO BVADMIN;` (chạy bởi SYS). Cũng nên `GRANT FLASHBACK ANY TABLE`/`EXECUTE ON DBMS_FLASHBACK` cho rõ ràng nếu demo phục hồi.

---

# PHẦN C — LỖI TRUNG BÌNH (MEDIUM)

### [ ] M1 · ⏳ HOÃN · `MAKHOA` lưu tên khoa thay vì mã, không có bảng `KHOA`/FK `(SCHEMA-5)`
> **HOÃN:** thêm `CHECK`/bảng `KHOA` sẽ làm hỏng câu test ở file 04 (`MAKHOA = N'Tim mạch - Nội trú'`). Là quyết định mô hình hoá; nên xử lý kèm refactor dữ liệu + test. Hiện chỉ cần thuyết minh.
- **File:** `01:71` (định nghĩa), `01:125–128` & `12:65–88` (dữ liệu `N'Tim mạch'…`) · **Mức:** MEDIUM · ✅
- **Vì sao:** tên cột là "mã khoa" nhưng lưu tên đầy đủ, không kiểm soát giá trị, không nhất quán với mã khoa OLS (`TM/TK/TH` ở 05/09). TC#2 cho DPV "cập nhật MAKHOA" mà không có danh mục hợp lệ → nhập sai không bị chặn.
- **Cách sửa:** tạo bảng `KHOA(MAKHOA PK, TENKHOA)` (TM/TK/TH) + FK từ `HSBA.MAKHOA`; đồng bộ mã với OLS. Tối thiểu thêm `CHECK (MAKHOA IN (N'Tim mạch',N'Thần kinh',N'Tiêu hóa'))`.

### [x] M2 · ✔ ĐÃ SỬA · So sánh `!=` trên cột có thể NULL trong `INSTEAD OF` trigger → bỏ sót kiểm soát (logic 3‑trị) `(SCHEMA-14 / RBAC-5)`
- **File:** `03:47–54` & `129–137`; `08:52–58`; `09:55–64` · **Mức:** MEDIUM · ✅
- **Vì sao:** `NULL != value` cho `UNKNOWN` (không TRUE). Nếu `NGAYSINH`/`CHUYENKHOA`/`MAKTV` cũ là NULL, người dùng có thể đặt giá trị mới mà trigger **không** chặn (vi phạm TC#5 "không sửa ngày sinh/chuyên khoa…"). File 09 đã `NVL` cho `CAPBAC/COSO/KHOA_NHAN` nhưng vẫn bỏ sót `NGAYSINH/CHUYENKHOA`.
- **Cách sửa:** so sánh an toàn NULL cho **mọi** cột cấm sửa, vd `NVL(:NEW.CHUYENKHOA,'∅') != NVL(:OLD.CHUYENKHOA,'∅')`; hoặc đơn giản: **chỉ SET các cột được phép** trong câu UPDATE thực thi và bỏ phần kiểm tra `!=` (vì trigger BN/NV đã chỉ SET cột cho phép).

### [x] M3 · ✔ ĐÃ SỬA · Thiếu dữ liệu mẫu `TIENSUBENHGD` `(SCHEMA-1)`
- **File:** `01:117–122` (INSERT BENHNHAN bỏ cột này); `12` cũng không UPDATE · **Mức:** MEDIUM · ✅
- **Vì sao:** TC#3(d) cần demo BS đọc/sửa `TIENSUBENHGD`; không có dữ liệu → khó minh họa 1 trong 3 trường bắt buộc.
- **Cách sửa:** thêm `TIENSUBENHGD` vào danh sách cột + giá trị `N'…'` cho vài BN, và dòng UPDATE tương ứng ở 12.

### [~] M4 · ◑ MỘT PHẦN (theo thứ tự chạy) · File 12 UPDATE `THONGBAO` phụ thuộc 05 đã chạy thành công `(SCHEMA-9)`
> **Đã làm:** `run_migrations` chạy 05 trước 12 nên dữ liệu THONGBAO đã có; B4 giúp 05 chạy đúng. **Còn lại (tùy chọn):** thêm kiểm `SQL%ROWCOUNT` trong 12 để cảnh báo nếu cập nhật 0 dòng.
- **File:** `12:122–129` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** TB001‑TB007 do 05 INSERT (mà 05 đang lỗi vì B4). Nếu 05 chưa chạy/đang lỗi → UPDATE 0 dòng. Ngoài ra sau `APPLY_TABLE_POLICY READ_CONTROL`, BVADMIN cần nhãn/`FULL` để UPDATE THONGBAO.
- **Cách sửa:** gộp tạo + sửa encoding THONGBAO vào cùng 05; trong 12 kiểm tra `SQL%ROWCOUNT` và cảnh báo nếu 0 dòng.

### [ ] M5 · ⏳ HOÃN · `DONTHUOC` PK gồm `TENTHUOC` → UPDATE tên thuốc là sửa cột PK `(SCHEMA-12)`
> **HOÃN:** đổi PK (thêm `MADT` surrogate) là thay đổi schema lan rộng (FGA 3a, app). Khóa tự nhiên hiện tại chấp nhận được cho demo; nên thuyết minh, đổi khi có thời gian.
- **File:** `01:86–92` (`PK_DONTHUOC(MAHSBA,NGAYDT,TENTHUOC)`) · **Mức:** MEDIUM · ✅
- **Vì sao:** YC3‑3a yêu cầu audit UPDATE `TENTHUOC`; nhưng `TENTHUOC` thuộc PK → 2 thuốc cùng HSBA cùng ngày trùng tên sẽ `ORA-00001`; cũng không phân biệt 2 đơn cùng thuốc khác liều cùng ngày.
- **Cách sửa:** thêm cột định danh `MADT` (mã đơn) làm PK; hoặc chấp nhận PK hiện tại nhưng xử lý đổi tên thuốc qua DELETE+INSERT có ghi vết.

### [~] M6 · ◑ MỘT PHẦN (theo thứ tự chạy) · BN mẫu có `ORACLE_USER=NULL` ở file 01 (phụ thuộc 02) `(SCHEMA-16)`
> **Đã làm:** `run_migrations` bảo đảm 01→02 nên file 02 sẽ điền `ORACLE_USER`. **Còn lại (tùy chọn):** thêm bước verify `COUNT(*) WHERE ORACLE_USER IS NULL = 0` sau 02.
- **File:** `01:117–122`; account tạo ở `02:118–120` · **Mức:** MEDIUM · ✅
- **Vì sao:** chỉ chạy 01 mà chưa 02 → BENHNHAN không có account (vi phạm TC#1) và mọi RBAC/VPD theo `WHERE ORACLE_USER=SYS_CONTEXT(...)` trả rỗng.
- **Cách sửa:** bắt buộc thứ tự 01→02; thêm verify `SELECT COUNT(*) FROM BENHNHAN WHERE ORACLE_USER IS NULL` phải = 0 sau 02.

### [x] M7 · ✔ ĐÃ SỬA · `NV_LOOKUP_View` lộ toàn bộ `NHANVIEN` cho cả KTV `(ORCHESTRATION-5)`
> Đã đối chiếu form: **DPVForm** (chọn BS) và **BSForm** (chọn KTV) có dùng view; **KTVForm KHÔNG dùng**. → Bỏ `GRANT … TO KTV_Role` ở setup_all + file 11 (giữ DPV/BS). Giảm lộ thông tin nhân viên cho KTV.
- **File:** `setup_all.sql:27–33`, `11_NV_Lookup_Grants.sql:19–26` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** view trả `MANV/HOTEN/VAITRO/CHUYENKHOA` của **mọi** nhân viên, GRANT cho cả `KTV_Role`. TC#5 nói NV chỉ xem thông tin chính mình; KTV không cần danh sách toàn bộ NV.
- **Cách sửa:** chỉ GRANT `NV_LOOKUP_View` cho `DPV_Role` (DPV cần tra BS/KTV để điều phối); cân nhắc bỏ `CHUYENKHOA` nếu không cần. Thuyết minh đây là "danh bạ điều phối" có kiểm soát.

### [x] M8 · ✔ ĐÃ SỬA · File 11 nhắc chạy `11_NV_Lookup_Grants_GRANTS.sql` không tồn tại `(ORCHESTRATION-6)`
- **File:** `11_NV_Lookup_Grants.sql:13–14` (comment) · **Mức:** MEDIUM · ✅
- **Cách sửa:** sửa hướng dẫn (GRANT đã nằm ngay trong file 11, do BVADMIN owner cấp trực tiếp được), bỏ tham chiếu file ma.

### [x] M9 · ✔ ĐÃ SỬA · `SET DEFINE OFF` chỉ có ở `setup_all` `(ORCHESTRATION-10)`
> Đã thêm `SET DEFINE OFF` vào 05/09/11/12. (KHÔNG thêm vào 06/07/10 vì các file đó dùng `&&sys_pwd`.)
- **File:** thiếu ở `05`, `09`, `12` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** nếu nội dung/định danh chứa `&`, SQL*Plus hiểu là biến thay thế → hỏng chuỗi/đòi nhập biến. (Hiện dữ liệu mẫu không có `&`, nhưng demo nhập liệu thực dễ dính.)
- **Cách sửa:** thêm `SET DEFINE OFF;` ở đầu mọi file có INSERT/UPDATE chuỗi.

### [x] M10 · ✔ ĐÃ SỬA · `setup_all` (Bước 3/4) trùng & mâu thuẫn `09` về `CAPBAC/COSO/KHOA_NHAN` `(ORCHESTRATION-11)`
- **File:** `setup_all.sql:48–79` (ADD **không** CHECK) ⨯ `09:14–41` (ADD **có** CHECK) · **Mức:** MEDIUM · ✅
- **Vì sao:** hai nơi cùng ALTER/UPDATE; nếu setup_all chạy trước thì cột không có ràng buộc CHECK; nếu 09 chạy trước thì setup_all bỏ qua (cột đã có) — dữ liệu/ràng buộc không nhất quán.
- **Cách sửa:** gộp về một nơi (ưu tiên bản có CHECK ở 09); bỏ phần ADD/UPDATE trùng trong setup_all.

### [~] M11 · ◑ MỘT PHẦN · `13_Audit_Grants` mâu thuẫn chú thích trong `setup_all` `(ORCHESTRATION-16)`
> **Đã làm:** bỏ chú thích mâu thuẫn ("KHÔNG grant cho DPV/BS/KTV") trong setup_all → không còn xung đột; giữ grant log theo file 13 cho app. **Còn lại (cân nhắc bảo mật):** có thể thu hẹp để BS/DPV chỉ xem log liên quan mình (xem M-note), nhưng cần đối chiếu form xem-log trước khi đổi.
- **File:** `13:15–24` (GRANT SELECT bảng log cho DPV/BS/KTV) ⨯ `setup_all:41–43` (ghi "KHÔNG grant cho DPV/BS/KTV; chỉ DBA xem") · **Mức:** MEDIUM · ✅
- **Vì sao:** quyết định trái ngược về việc ai được xem nhật ký. Nhật ký kiểm toán (YC3) thường **chỉ DBA** xem; cho BS/DPV xem `LOG_BS_DONTHUOC`/`LOG_BS_HSBA` của người khác là lộ thông tin.
- **Cách sửa:** chọn một chính sách. Khuyến nghị: chỉ DBA (qua AdminDashboard) xem log audit; bỏ file 13 hoặc giới hạn phạm vi (vd KTV chỉ xem log của chính mình qua view có lọc).

### [x] M12 · ✔ ĐÃ SỬA · FGA 3a thiếu cột `MAHSBA, NGAYDT` `(AUDIT-4, mới)`
- **File:** `06:91–106` (`FGA_DONTHUOC_UPDATE` chỉ `audit_column=>'TENTHUOC,LIEUDUNG'`) · **Mức:** MEDIUM · ✅
- **Vì sao:** YC3‑3a liệt kê audit UPDATE trên **`MAHSBA, NGAYDT, TENTHUOC, LIEUDUNG`**; thiếu 2 cột PK.
- **Cách sửa:** `audit_column => 'MAHSBA,NGAYDT,TENTHUOC,LIEUDUNG'` (với `audit_column_opts=>DBMS_FGA.ANY_COLUMNS`).

### [ ] M13 · ⏳ HOÃN · `setup_admin_user` cấp `DBA` cho `HOSPITAL_DBA` + mật khẩu cứng `(ORCHESTRATION-14)`
> **HOÃN:** chấp nhận cho mục đích demo AdminDashboard; thuyết minh trong báo cáo. Thu hẹp quyền là cải tiến không bắt buộc.
- **File:** `setup_admin_user.sql:32–41` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** cấp `DBA` (toàn quyền) cho tài khoản đăng nhập app + mật khẩu cứng `Hospital@DBA2025` — đi ngược tinh thần least‑privilege của môn ATBM.
- **Cách sửa:** chấp nhận cho mục đích demo nhưng **thuyết minh rõ**; hoặc tạo role hẹp hơn (chỉ các quyền AdminDashboard cần) thay vì `DBA`.

### [x] M14 · ✔ GIẢI QUYẾT (qua B1) · App `KTVForm`/`BNForm` phụ thuộc view bị VPD khóa → hỏng runtime `(APP-1, mới)`
> Sau khi sửa B1, các view trả đúng dữ liệu → 2 form hoạt động. **Không cần sửa code app.** (Tùy chọn nâng cao: kiểm `SQL%ROWCOUNT` khi lưu để cảnh báo 0 dòng.)
- **File:** `HospitalApp/Forms/Hospital/KTVForm.cs:298–352` (`KTV_HSBA_DV_View`), `BNForm.cs:342–397, 428–436` (`BN_BENHNHAN_View`/`BN_HSBA_View`) · **Mức:** MEDIUM (hệ quả của B1) · ✅
- **Vì sao:** các form query đúng các view mà B1 làm trả 0 dòng → KTV "Tổng: 0 dịch vụ", BN hiện "Không tìm thấy thông tin bệnh nhân", lưu KETQUA/thông tin **0 dòng nhưng báo "Đã lưu"** (sai lệch, không có phản hồi lỗi).
- **Cách sửa:** sau khi sửa B1, kiểm thử lại 2 form. Cân nhắc kiểm tra `SQL%ROWCOUNT`/số dòng cập nhật ở tầng app để cảnh báo khi lưu 0 dòng.

### [ ] M15 · ⏳ HOÃN (code app) · `GetHospitalRole` fallback theo prefix tên user che lỗi nhận diện vai trò `(APP-2, mới)`
> **HOÃN:** đây là code C# (OracleHelper.cs) — fallback là phòng thủ hợp lý, không gây lỗi. Sau khi B1 thông VPD, nhánh truy vấn DB chính sẽ chạy đúng. Tùy chọn: thêm log cảnh báo khi rơi vào fallback.
- **File:** `HospitalApp/Database/OracleHelper.cs:138–174` · **Mức:** MEDIUM · ✅
- **Vì sao:** nếu truy vấn `BVADMIN.NHANVIEN/BENHNHAN` thất bại hoặc rỗng, hàm đoán vai trò từ tiền tố tên (`DPV_`,`BS_`,`KTV_`,`BN_`,`U…`). App "vào được" form đúng kể cả khi cơ chế DB hỏng → **giấu lỗi** khi chấm. Ngoài ra `CurrentOlsLabel()`/`GetHospitalRole` đọc `DBA_SA_USER_LABELS` mà user thường **không có quyền** → luôn rơi vào catch.
- **Cách sửa:** giữ fallback nhưng log cảnh báo rõ "role detected by name prefix (DB lookup failed)"; cấp quyền đọc nhãn của chính mình qua một view/`SA_SESSION.LABEL` thay vì `DBA_SA_USER_LABELS`.

### [ ] M16 · ⏳ HOÃN · Phục hồi "dựa vào nhật ký kiểm toán" chưa liên kết tự động audit → SCN → flashback `(BACKUP-11)`
> **HOÃN:** demo hiện tại (CHECKPOINT_LOG → SCN → FLASHBACK) đã chạy được sau B7/H11. Liên kết tự động audit→SCN là cải tiến trình bày; nên viết rõ chuỗi `EXTENDED_TIMESTAMP → TIMESTAMP_TO_SCN → FLASHBACK` trong báo cáo.
- **File:** `07:232–245`, `09:55–80` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** code truy vấn audit và flashback **rời rạc** (SCN lấy từ `CHECKPOINT_LOG` ghi thủ công, không phải từ thời điểm sự cố trong audit). Đề YC4 yêu cầu phục hồi **dựa vào** nhật ký kiểm toán ở YC3.
- **Cách sửa:** demo mạch lạc: đọc `EXTENDED_TIMESTAMP` của bản ghi sự cố trong `(*_)AUDIT_TRAIL` → `TIMESTAMP_TO_SCN(...)` → `FLASHBACK TABLE … TO SCN`/`AS OF SCN`. Trình bày đúng chuỗi nhân–quả.

### [ ] M17 · ⏳ HOÃN (môi trường) · Flashback Database / RMAN‑FRA phụ thuộc edition `(BACKUP-7)`
> **HOÃN:** phần demo dùng Flashback **Table**/**Query** (chạy mọi edition) — OK. Flashback **Database** chỉ là phần lý thuyết; ghi rõ edition đã test trong báo cáo.
- **File:** `07:168–183, 273–277` · **Mức:** MEDIUM · ⚠️
- **Vì sao:** `ALTER DATABASE FLASHBACK ON` (Flashback **Database**) là tính năng cấp DB cần ARCHIVELOG + FRA; trên XE cần kiểm chứng. **Tuy nhiên** phần **demo thực thi** (file 09) dùng **Flashback Table + Flashback Query** chạy bằng undo + row movement → hoạt động ở mọi edition.
- **Cách sửa:** giữ Flashback Table/Query làm phần hiện thực chính; coi Flashback Database là phần "tìm hiểu lý thuyết". Ghi rõ edition đã test.

> Các mục Flashback phụ khác (mức thấp, ⚠️): `BACKUP-5` (privilege FLASHBACK — owner thường đủ quyền trên bảng của mình nếu đã bật row movement, **cần kiểm chứng**), `BACKUP-8` (`ALTER SYSTEM SET DB_RECOVERY_FILE_DEST*` cần thứ tự/SCOPE đúng), `BACKUP-9/10/12/16` (Flashback Query/VERSIONS với LOB & FK & subquery — cần đủ undo).

---

# PHẦN D — LỖI NHẸ / GHI NHẬN (LOW / INFO)

### [ ] L1 · ✏️ Đính chính `SCHEMA-2`: INSERT positional thực ra **đủ** giá trị
- **File:** `01:108–114` (NHANVIEN), `125–128` (HSBA), `131–134` (HSBA_DV), `137–140` (DONTHUOC) · **Mức:** LOW · ✏️
- **Đính chính:** workflow báo "NHANVIEN chỉ cấp 9 giá trị" — **sai**. Đếm lại dòng 108 có **đúng 10 giá trị cho 10 cột** (`…,'DPV',N'Tiếp nhận',NULL`), HSBA 8/8, nên các INSERT này **chạy đúng**. Vấn đề còn lại chỉ là **rủi ro bảo trì**: nếu sau này đổi thứ tự cột thì vỡ.
- **Cách sửa (không bắt buộc):** liệt kê cột tường minh cho mọi INSERT cho an toàn lâu dài.

### [ ] L2 · `VAITRO` lưu mã `DPV/BS/KTV` thay vì tên đề bài, thiếu "Bệnh nhân" `(SCHEMA-3)`
- `01:56–58` · INFO · ℹ️ — Quyết định thiết kế hợp lý (bệnh nhân ở bảng riêng). **Thuyết minh** ánh xạ mã↔tên trong báo cáo để không bị trừ "không khớp đặc tả".

### [ ] L3 · `PHAI CHECK('M','F')` thay vì Nam/Nữ `(SCHEMA-4)`
- `01:33, 51` · INFO · ℹ️ — App đã map `M→Nam, F→Nữ` (`BNForm.cs:363`). Giữ được, chỉ cần thuyết minh.

### [ ] L4 · `THONGBAO` thêm `MATB` (PK) ngoài đặc tả `(SCHEMA-10)`
- `01:95–101` · INFO · ℹ️ — Thêm khóa để định danh/OLS là hợp lý; ghi chú trong báo cáo.

### [x] L5 · ✔ ĐÃ SỬA · `09` idempotent một phần (chỉ kiểm cột `CAPBAC`) `(SCHEMA-13)`
> Đổi sang kiểm tra & ADD **từng cột** (`add_col_if_missing`) — chạy lại sau lỗi giữa chừng không bị thiếu cột.
- `09:14–31` · LOW · ⚠️ — nếu lần chạy trước lỗi giữa chừng (có CAPBAC, thiếu COSO/KHOA_NHAN) → bỏ qua ADD → `ORA-00904` về sau. Sửa: kiểm tra & ADD từng cột.

### [x] L6 · ✔ ĐÃ SỬA · Verify cuối file 12 dùng `LIKE '%ễ%'` thiếu tiền tố `N` `(SCHEMA-15)`
- `12:132–135` · LOW · ⚠️ — `HOTEN` là NVARCHAR2; literal không‑N có thể không khớp (âm tính giả). Sửa: `LIKE N'%ễ%'`.

### [x] L7 · ✔ ĐÃ SỬA · `trg_log_ketqua` ghi vết cả khi KETQUA không đổi `(RBAC-8)`
> KETQUA nay là NVARCHAR2 → thêm `IF NVL(:NEW,…) != NVL(:OLD,…)` để chỉ ghi vết khi thực sự đổi.
- `03:80–91` · LOW · ℹ️ — audit noise. Vì KETQUA là NCLOB, **không** lọc bằng `!=` (sẽ ORA‑00932); nếu muốn lọc phải `DBMS_LOB.COMPARE`. Chấp nhận ghi mọi lần cũng hợp lý cho mục đích "đầy đủ vết".

### [ ] L8 · `trg_log_ketqua` lấy `MAKTV` từ `:OLD` `(RBAC-9)`
- `03:84–89` · LOW · ℹ️ — `CHANGED_BY=SESSION_USER` mới là người thực hiện; `MAKTV` chỉ là dữ liệu hàng. Chấp nhận được.

### [ ] L9 · Kiểm soát cột chỉ dựa `INSTEAD OF` trigger (file 03) vs column‑grant (file 04) — không nhất quán `(RBAC-4, RBAC-13)`
- `03:96–97,169–170` · LOW · ℹ️ — hai cách tiếp cận khác nhau trong cùng đồ án. Không lỗi (INSTEAD OF an toàn), nhưng nên thống nhất/thuyết minh.

### [x] L10 · ✔ ĐÃ SỬA · `setup_all` Bước 5 dùng alias `AS ROWS` `(ORCHESTRATION-12)`
- `setup_all.sql:119` · LOW · ⚠️ — `ROWS` là từ khóa; có thể `ORA-00923`. Sửa: `AS ROW_COUNT` hoặc `"ROWS"`.

### [x] L11 · ✔ ĐÃ SỬA (phần role) · Chạy lại không idempotent (CREATE ROLE/USER/policy/label trùng) `(ORCHESTRATION-13)`
> Đã bọc 4 `CREATE ROLE` (DPV/BS/KTV/BenhNhan) idempotent (bỏ qua ORA-01921). **Còn lại (tùy chọn):** CREATE TABLE log & CREATE USER u1-u8 / CREATE_POLICY OLS chưa guard — khi demo lại nên drop schema/policy trước, hoặc thêm guard tương tự.
- `03,04,05` · LOW · ✅ — chạy lần 2 sẽ `ORA-01921 (role exists)`, `ORA-01920 (user exists)`, lỗi policy/label đã tồn tại. Sửa: thêm DROP‑if‑exists hoặc `EXCEPTION` cho phép re‑run khi demo.

### [ ] L12 · `BNForm` có tab "Thông báo" là **code chết** (không gắn vào UI) `(APP-3, mới)`
- `HospitalApp/Forms/Hospital/BNForm.cs:31, 463–505` · INFO · ℹ️ — `BuildThongBaoTab()` định nghĩa nhưng không add vào `_tabs`. Bệnh nhân không xem được thông báo OLS qua app. Nếu muốn BN nhận thông báo → gắn lại tab; nếu không → xóa code chết.

### [ ] L13 · OLS: "Lãnh đạo phòng" (u6, u7) không thuộc 3 cấp đề `(OLS-2, mới)`
- `05:230–250` · INFO · ℹ️ — đề chỉ có 3 cấp `BGD > LDK > NV`; "Lãnh đạo phòng" được map vào level `LDK`. Hợp lý nhưng **phải thuyết minh** cách mô hình hóa u6/u7 trong báo cáo.

### [x] L14 · ✏️ ĐÍNH CHÍNH (không phải lỗi) · OLS: file 10 gán nhãn nhân viên khác mapping ở 05/09 `(OLS-3, mới)`
> Đã đối chiếu lại: nhãn ở file 10 **nhất quán** với 09 (vd `KTV_NV006 → 'NV:HCM:TM'` khớp `CAPBAC=NV,COSO=HCM,KHOA_NHAN=TM`; `DPV_NV001 → 'NV:HCM'`). Không cần sửa — chỉ nên thống nhất MỘT nguồn gán nhãn để dễ bảo trì.
- `10:82–93` (vd `KTV_NV006 → 'NV:HCM:TM'`) vs `09:34–40`/`setup_all` · LOW · ⚠️ — nhiều nguồn gán nhãn cho cùng user; cần thống nhất một nguồn để tránh ghi đè nhầm. (Logic nhãn t/u **đúng** — xem ghi chú dưới.)

---

# PHẦN E — MA TRẬN ĐỘ PHỦ YÊU CẦU

| Yêu cầu | Trạng thái | Ghi chú |
|---|---|---|
| **TC#1** – tạo account, nối 1 account ↔ 1 dòng (1 bảng) | 🟡 Đạt nhưng mong manh | Cơ chế `ORACLE_USER + SYS_CONTEXT` **đúng** (1 bảng). Nhưng phụ thuộc thứ tự 01→02 (M6), mật khẩu/CONNECT cứng (B10), chưa tạo account hàng loạt cho quy mô 20/100/50/100000. |
| **YC1‑C2 RBAC – Kỹ thuật viên (TC#4)** | 🔴→🟢 sau ĐỢT 1 | Đã sửa B1 (VPD có nhánh KTV) + B6 (CREATE SYNONYM). Cần test trên DB thật. |
| **YC1‑C2 RBAC – Bệnh nhân (TC#5)** | 🔴→🟢 sau ĐỢT 1 | Đã sửa B1 (VPD có nhánh BN). View/trigger BN đúng cột; nay trả đúng dữ liệu. |
| **TC#5 – Nhân viên tự xem/sửa thông tin mình** | 🟡 Một phần | Có `NV_NHANVIEN_View`+trigger (08/09) nhưng định nghĩa trùng (H2) và so sánh NULL (M2). |
| **YC1‑C3 VPD – Điều phối viên (TC#2)** | 🔴→🟢 sau ĐỢT 1 | Đã sửa B2 (ADD_POLICY bọc BEGIN/END); DPV predicate `''` (thấy tất cả) đúng yêu cầu. |
| **YC1‑C3 VPD – Bác sĩ/Y sĩ (TC#3)** | 🔴→🟢 sau ĐỢT 1 | Đã sửa B2 + B3 (NCLOB→NVARCHAR2 + NVL) → BS UPDATE được + ghi vết TC#3c chạy. |
| **YC2 – OLS THONGBAO** | 🔴→🟢 sau ĐỢT 1 | Đã sửa B4 (bọc SA_* + tạo user bằng SYSTEM) + H7 (gán nhãn bằng LBACSYS). Thiết kế nhãn ĐÚNG. Cần đã cài `catols.sql`. |
| **YC3‑(1) bật audit** | 🟡 Một phần | `AUDIT SESSION` ổn; phụ thuộc B10 (CONNECT SYS). |
| **YC3‑(2) Standard audit 5 ngữ cảnh** | 🟡 Một phần | 5 ngữ cảnh có, nhưng đề yêu cầu cả **view/procedure/function** — hiện chỉ audit trên **bảng**; cần bổ sung ≥1 ngữ cảnh trên view/proc/func. |
| **YC3‑3a FGA DONTHUOC** | 🟡→🟢 sau ĐỢT 1 | Đã bổ sung đủ 4 cột (M12). |
| **YC3‑3b FGA BS cập nhật HSBA (thành công)** | 🟢 Gần đạt | Điều kiện `fn_get_vaitro()='BS'` hợp lý (sau khi sửa B2/B3 để BS UPDATE được). |
| **YC3‑3c cập nhật bất hợp pháp** | 🔴→🟡 sau ĐỢT 1 | Đã sửa B5/H4 (audit mức bảng `WHENEVER NOT SUCCESSFUL` bắt ORA‑01031). Cần xác nhận trên DB (xem H5 Unified Auditing). |
| **YC3‑3d thao tác bất hợp pháp HSBA_DV** | 🟡 Một phần | FGA `FGA_HSBA_DV_ILLEGAL` có; nhưng thao tác bị chặn quyền (ORA‑01031) không kích hoạt FGA — cần thêm standard audit `WHENEVER NOT SUCCESSFUL`. |
| **YC3‑(4) đọc xuất nhật ký** | 🟡 Rủi ro | H5 (21c XE Unified Auditing → `DBA_AUDIT_TRAIL` có thể rỗng). |
| **YC4 – Sao lưu (chủ động/tự động)** | 🟡 Một phần | RMAN/DataPump phần lớn là comment; 2 job scheduler có lỗi tham số (H9/H10). |
| **YC4 – Phục hồi dựa nhật ký** | 🟡 Một phần | Demo Flashback Table/Query chạy được sau khi sửa B7/H11; nhưng liên kết audit→SCN còn rời rạc (M16). |

> **Ghi chú OLS (đã tự kiểm chứng logic):** thiết kế nhãn 3 thành phần — **LEVEL** (NV<LDK<BGD), **COMPARTMENT=cơ sở** (AND: phải đúng cơ sở), **GROUP=khoa** (OR: có ≥1 khoa) — **đúng** với mô tả đề. Đối chiếu t1‑t7/u1‑u8 (vd `t7='LDK:HPN:TH,TK'` cho phép lãnh đạo có TH **hoặc** TK ở Hải Phòng đọc; `t3='LDK'` cho mọi lãnh đạo khoa; `t2='BGD'` chỉ giám đốc) đều khớp. **Vấn đề của YC2 hoàn toàn nằm ở khâu thực thi (B4, H7), không phải ở thiết kế nhãn.**

---

# PHẦN F — GHI CHÚ VỀ QUÁ TRÌNH RÀ SOÁT

- **Phương pháp:** dùng workflow nhiều tầng (9 nhóm tìm lỗi theo mảng → kiểm chứng đối nghịch từng lỗi → rà soát độ phủ).
- **Giới hạn gặp phải:** workflow **chạm giới hạn phiên (session limit)** giữa chừng. Các nhóm chạy xong: **Schema, RBAC, Backup, Orchestration** (65 phát hiện). Các nhóm bị hủy: **VPD (file 04), TC#1 (file 02), OLS (file 05), Audit (file 06), App**, cùng **toàn bộ tầng kiểm chứng** và critic độ phủ.
- **Bù đắp:** vì đã **tự đọc toàn bộ 18 file SQL + các form** (`OracleHelper`, `LoginForm`, `KTVForm`, `BNForm`), tôi tự đóng vai người kiểm chứng:
  - **Tự xác nhận** các lỗi của 4 nhóm chạy xong (đánh dấu ✅/⚠️/ℹ️) và **đính chính** chỗ sai (vd `L1`/SCHEMA‑2).
  - **Bổ sung** các lỗi chặn mà nhóm bị hủy chưa kịp tìm: **B2** (ADD_POLICY trần), **B3** (NCLOB `!=` trong trigger BS), **B5/H4/M12** (audit), **H5** (Unified Auditing), **M14/M15/L12** (app), **L13/L14** (OLS).
- **Việc nên làm tiếp khi có lại quota:** chạy lại workflow (đã lưu script) để **kiểm chứng đối nghịch độc lập** các phát hiện ⚠️ (đặc biệt nhóm Backup phụ thuộc edition và các claim môi trường), và xác nhận trên **DB thật** (Oracle XE 21c) các lỗi cú pháp/quyền.

---

## ✅ Thứ tự sửa đề xuất (gợi ý)
1. **B2, B4, B5** (sửa cú pháp gọi thủ tục/audit → để script chạy được).
2. **B1 + B3 + H1** (sửa logic VPD↔RBAC và NCLOB → để KTV/BN/BS hoạt động).
3. **B7, B8, B9, B10, H6, H8** (quy trình cài đặt & CONNECT → cài được một mạch).
4. **B6, H7, H2** (synonym, quyền gán nhãn OLS, view trùng).
5. **H4, H5, M12** (hoàn thiện audit YC3) → **H9, H10, H11, M16** (backup/recovery YC4).
6. Phần **MEDIUM/LOW** còn lại — sửa dần + ghi chú thuyết minh trong báo cáo.
