# Kịch bản chi tiết — NGƯỜI 5 (E)

**Phụ trách:** YC4 Backup/Recovery (Flashback) + Mã hóa (NNE đường truyền + TDE at-rest) + Kết luận.
**Thời lượng:** ~6 phút. **Công cụ:** 2 file trong `PhanHe2/extras/`: `NNE_TDE_demo.sql` và `recovery_demo.sql`.

> Mục tiêu phần này: chứng minh đồ án có **Cryptography** (mã hóa đường truyền + at-rest) và **khả năng phục hồi** (Flashback có audit) — bổ sung cho phần access control mà Người 1–4 đã trình bày.

---

## 0. Chuẩn bị TRƯỚC khi tới lượt (làm trong lúc Người 4 nói)

- [ ] Mở **1 cửa sổ PowerShell** tại `D:\repos\Oracle`, gõ sẵn (chưa Enter dòng cuối):
  ```powershell
  chcp 65001
  $env:NLS_LANG = "AMERICAN_AMERICA.AL32UTF8"
  ```
- [ ] Mở sẵn 2 file trong VS Code để nếu cần chỉ code: `NNE_TDE_demo.sql`, `recovery_demo.sql`.
- [ ] Kiểm `SYS_PWD` trong 2 file đúng mật khẩu máy demo (tránh `ORA-01017` giữa chừng).
- [ ] Phóng to font terminal (Ctrl + cuộn) để hội đồng đọc được.

**Câu chuyển tiếp từ Người 4:** *"Phần access control và audit anh/chị vừa xem là tầng quyết định ai được làm gì. Em là phần cuối: dữ liệu được **mã hóa** thế nào, và nếu lỡ **mất/xóa nhầm** thì khôi phục ra sao."*

---

## 1. Mở đầu (≈20 giây)

> *"Đồ án bảo vệ dữ liệu y tế ở 2 hướng mà phần trước chưa nói: **mã hóa** và **sao lưu/phục hồi**.
> Mã hóa gồm 2 lớp — **NNE** bảo vệ dữ liệu trên **đường truyền** TCP, và **TDE** bảo vệ dữ liệu
> **lưu trên đĩa**. Em demo bằng 2 script, mỗi bước đều in rõ đang làm gì."*

---

## 2. DEMO A — Mã hóa NNE + TDE (≈2 phút)

**Gõ lệnh:**
```powershell
sqlplus /nolog "@d:\repos\Oracle\PhanHe2\extras\NNE_TDE_demo.sql"
```

### 2.1 NNE — mã hóa đường truyền
Khi hiện **PHAN 1/3** và bảng `NETWORK_SERVICE_BANNER`:
> *"Đây là banner của chính phiên kết nối hiện tại. Hai dòng cuối — **Encryption service** và
> **Crypto-checksumming service** — chứng tỏ kết nối TCP đã được **mã hóa và chống sửa gói**.
> Cấu hình ép `ENCRYPTION_SERVER = REQUIRED` (AES256 + SHA512) trong `sqlnet.ora`, nên client
> không thỏa thuật toán thì bị từ chối luôn."*
- 👉 Chỉ tay vào 2 dòng *Encryption* / *Crypto-checksumming* (mục `[1.2]` lọc sẵn 2 dòng đó).

> ⚠️ **Đừng nói** "banner ghi AES256" — banner trên Windows chỉ ghi *Encryption service*, không in tên thuật toán. Thuật toán là do `sqlnet.ora` ép.

### 2.2 TDE — keystore + cột được mã hóa
Khi hiện **PHAN 2/3**:
> *"Keystore `STATUS = OPEN` nghĩa là khóa master sẵn sàng để giải mã. Bảng dưới liệt kê các cột
> đang được TDE mã hóa: **CCCD, CMND mã hóa NO SALT** để vẫn giữ ràng buộc UNIQUE và tìm theo `=`;
> **DIUNGTHUOC có SALT** vì không cần index. Thuật toán **AES 192-bit**."*
- 👉 Chỉ vào cột `STATUS=OPEN` rồi bảng 3 cột `CCCD / CMND / DIUNGTHUOC`.

### 2.3 TDE trong suốt với ứng dụng
Khi hiện **PHAN 3/3** (đọc CCCD/CMND/DIUNGTHUOC):
> *"Điểm hay của TDE là **trong suốt**: trên đĩa là ciphertext, nhưng phiên hợp lệ đọc ra vẫn là
> số CCCD/CMND rõ — nên **app không cần sửa một dòng code nào**. UNIQUE và tìm-kiếm theo `=` vẫn chạy."*
- 👉 Chỉ vào cột CCCD ra số đầy đủ (vd `300112345678`), DIUNGTHUOC ra `Penicillin`.

**Chốt phần mã hóa:** *"Vậy là dữ liệu nhạy cảm được mã hóa cả khi truyền lẫn khi lưu, mà không ảnh hưởng trải nghiệm."*

---

## 3. DEMO B — Phục hồi bằng Flashback (≈2.5 phút)

**Gõ lệnh:**
```powershell
sqlplus /nolog "@d:\repos\Oracle\PhanHe2\extras\recovery_demo.sql"
```
Script tự in **BUOC 0 → 6**. Bám theo các mốc:

| Bước hiện trên màn hình | Lời thoại / chỉ vào |
|---|---|
| **BUOC 2** – Trạng thái trước sự cố | *"HSBA_DV đang có **6 dòng**, riêng HS001 có **2 dịch vụ** (xét nghiệm + siêu âm)."* 👉 chỉ count + 2 dòng HS001 |
| **BUOC 3** – Giả lập sự cố | *"Em xóa nhầm toàn bộ dịch vụ của HS001 — như KTV/DPV lỡ tay."* 👉 count tụt còn **4** |
| **BUOC 4** – Audit/FGA | *"Hệ thống đã **ghi vết** hành động xóa: thấy dòng `DELETE FROM HSBA_DV ... HS001` trong FGA trail — biết ai, lúc nào, câu lệnh gì."* 👉 chỉ dòng DELETE + timestamp |
| **BUOC 5** – Phục hồi | *"Lấy mốc **SCN** đã ghi trước sự cố, chạy `FLASHBACK TABLE HSBA_DV TO SCN`. Count quay lại **6**, 2 dịch vụ HS001 **xuất hiện lại nguyên vẹn**."* 👉 chỉ count 6 + 2 dòng HS001 |
| **BUOC 6** (nếu còn giờ) | *"Flashback Query / VERSIONS cho xem lịch sử thay đổi của đơn thuốc — phục hồi chọn lọc, không cần restore cả DB."* |

**Nói thêm khi được hỏi/để ghi điểm:**
> *"Flashback Table dùng dữ liệu **UNDO**, phục hồi trong **vài giây**, không cần restore toàn bộ DB —
> hợp với lỗi người dùng. Còn **RMAN** (sao lưu vật lý, toàn vẹn cao) và **Data Pump** (sao lưu logic,
> di động) là 2 phương pháp còn lại; em có sẵn lệnh + so sánh trong `07_YC4_Backup_Recovery.sql`."*

---

## 4. Kết luận đồ án (≈30 giây)

> *"Tổng kết: dữ liệu y tế được bảo vệ **nhiều tầng** — **RBAC + VPD + OLS** quyết định ai xem/sửa được gì,
> **Audit + FGA** ghi vết mọi thao tác nhạy cảm, **NNE + TDE** mã hóa cả đường truyền lẫn dữ liệu lưu trữ,
> và **Flashback/RMAN/Data Pump** đảm bảo phục hồi khi sự cố. Không lớp nào hoàn hảo một mình; an toàn đến từ
> việc đặt đúng lớp bảo vệ vào đúng chỗ."*

---

## 5. Câu hỏi hay gặp (dành riêng cho phần Người 5)

1. **NNE và TDE khác gì?** NNE mã hóa **đường truyền** (TCP, chống bắt gói); TDE mã hóa **dữ liệu at-rest** (file `.dbf`, backup). Hai mối đe dọa khác nhau.
2. **Đã có RBAC/VPD rồi sao còn cần mã hóa?** RBAC/VPD chặn truy cập **bên trong** DB; mã hóa bảo vệ khi **đường truyền bị nghe lén** hoặc **file/đĩa/backup bị đánh cắp** — kẻ trộm file chỉ thấy ciphertext.
3. **TDE có chặn DPV xem CCCD không?** **Không.** TDE trong suốt với phiên hợp lệ — ai có quyền `SELECT` vẫn thấy plaintext. Chặn xem là việc của **access control + masking**, không phải TDE.
4. **Flashback có cần ARCHIVELOG / Flashback Database không?** **Không.** `FLASHBACK TABLE` dùng **UNDO** (khác Flashback Database). Chỉ cần bật **ROW MOVEMENT** trên bảng + undo retention đủ — chạy được ngay trên Oracle XE.
5. **AES bao nhiêu bit?** TDE mặc định **AES-192** (output ghi "AES 192 bits key"); muốn AES-256 thì thêm `USING 'AES256'` khi `ALTER TABLE ... ENCRYPT`.
6. **Vì sao CCCD/CMND để NO SALT?** Để giữ **UNIQUE** và **tìm theo `=`** trên cột mã hóa (salt làm mỗi lần mã hóa ra khác nhau → mất index). DIUNGTHUOC không cần index nên để SALT cho an toàn hơn.
7. **Mất keystore thì sao?** **Mất toàn bộ dữ liệu đã mã hóa** — nên đã tạo **auto-login keystore** + phải sao lưu thư mục wallet cùng backup DB.
8. **Mọi cột nhạy cảm đều mã hóa được chứ?** `TIENSUBENH/TIENSUBENHGD` (NVARCHAR2 2000) **không** mã hóa được — báo `ORA-28331` (vượt giới hạn kích thước sau overhead); PII quan trọng nhất là CCCD/CMND thì đã mã hóa.

---

## 6. Phương án dự phòng (nếu trục trặc)

- **`ORA-01017` khi script connect SYS:** sai `SYS_PWD` trong file → sửa dòng `DEFINE SYS_PWD = ...` (recovery dùng `oracle`, NNE_TDE dùng `Phamminhquan611*` — chỉnh cho khớp máy).
- **BUOC 4 audit không ra dòng nào:** chưa có thao tác xóa nào được FGA bắt → cứ để script chạy tiếp (lần xóa ở BUOC 3 của chính lần chạy này vẫn được ghi).
- **BUOC 6 báo `ORA-01466`:** bảng vừa đổi cấu trúc gần đây → **bỏ qua**, đây chỉ là demo phụ; phần chính (count 6→4→6 + audit) vẫn đủ.
- **Tiếng Việt bị hỏng dấu:** quên `chcp 65001` → chạy lại sau khi `chcp 65001` + `NLS_LANG=AMERICAN_AMERICA.AL32UTF8`.
- **Thiếu thời gian:** chạy `NNE_TDE_demo.sql` (1 phút) + recovery chỉ tới **BUOC 5** (count 6→4→6); bỏ BUOC 6 và phần so sánh RMAN/Data Pump (chỉ nói 1 câu).

---

## 7. Tóm tắt "chỉ vào đâu" (in ra cầm tay)

1. NNE → 2 dòng *Encryption* / *Crypto-checksumming*.
2. TDE → `STATUS=OPEN` + bảng CCCD/CMND/DIUNGTHUOC (AES 192, NO SALT/SALT).
3. TDE trong suốt → CCCD ra số rõ.
4. Flashback → **6 → 4 → 6**, HS001 trở lại + dòng DELETE trong audit.
5. Kết luận → "nhiều tầng: access control + audit + crypto + recovery".
