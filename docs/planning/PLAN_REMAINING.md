# Kế hoạch — Phần việc còn lại

> **Đồ án ATBM HTTT 2025-2026 — HospitalApp**
> Tạo ngày: 2026-05-24
> Trạng thái hiện tại sau Sprint 1-5: build OK, 0 errors, mọi yêu cầu chức năng đã được hiện thực ở DB + app layer.

---

## Tổng quan tiến độ

### ✅ Đã hoàn tất

| Sprint | Nội dung | File chính |
|--------|----------|-----------|
| 1 | Bug critical (STATUS, MAHSBA collision, account auto-create, AcceptButton, Logout) | DPV/BS/Login |
| 2 | Theme Montserrat + palette tập trung | [Theme/UiTheme.cs](../../HospitalApp/Theme/UiTheme.cs) |
| 3 | App-layer security (ErrorMapper, InputValidator, AuditLogger, SessionManager, ConfirmDelete, mask CCCD) | [Security/](../../HospitalApp/Security/), [Controls/](../../HospitalApp/Controls/) |
| 4 | UX (SearchBox, Shortcuts, Toast) | [Controls/](../../HospitalApp/Controls/) |
| 5.1 | Self-service NV (tab "Thông tin của tôi" cho DPV/BS/KTV) | [Controls/MyProfilePanel.cs](../../HospitalApp/Controls/MyProfilePanel.cs) |
| 5.2 | OLSViewerForm cho u1-u8 + LoginForm route | [Forms/Hospital/OLSViewerForm.cs](../../HospitalApp/Forms/Hospital/OLSViewerForm.cs) |
| 5.3 | Standard Audit BY user cụ thể (đúng spec) | [PhanHe2/06_YC3_Audit.sql](../../PhanHe2/06_YC3_Audit.sql) |
|    | SQL migration phụ trợ | [PhanHe2/08_App_Migrations.sql](../../PhanHe2/08_App_Migrations.sql) |

### ⏳ Còn lại

| Phần | Mục đích | Ước lượng |
|------|----------|-----------|
| I. Bắt buộc nộp bài | Báo cáo Word + đóng gói | ~6h |
| II. Tăng điểm vấn đáp | OLS hợp nhất NHANVIEN + Demo Recovery | ~5h |
| III. UX nâng cao | Pagination/dropdown/spinner/icon | ~4h |
| IV. Hardening | Oracle Net encryption, migration runner, tests | ~3h |
| V. Vấn đáp & trình bày | Slide + demo script | ~3h |

**Tổng còn lại: ~21h.** Phần I là bắt buộc; II–V theo độ ưu tiên.

---

## PHẦN I — Bắt buộc phải có

> Đây là deliverable theo quy định trang 8 của đề bài. Thiếu = trừ điểm trực tiếp.

### I.1 — Báo cáo Word (`.docx`) — 3h

**Output:** 1 file `MSSV1_MSSV2_MSSV3.docx`, đóng chung với source code.

**Cấu trúc đề xuất:**

```
1. Trang bìa
2. Mục lục
3. Bảng phân công + đánh giá thành viên (%) ← BẮT BUỘC
4. PHẦN A — Phân hệ 1: Ứng dụng quản trị Oracle
   4.1 Mô tả tính năng
   4.2 Kiến trúc (WinForms → OracleHelper → Oracle DB)
   4.3 Screenshot từng tab AdminDashboard
5. PHẦN B — Phân hệ 2: Quản lý dữ liệu y tế
   5.1 Schema CSDL (ER diagram)
   5.2 TC#1 — Account mapping (sơ đồ ORACLE_USER trong NHANVIEN/BENHNHAN)
   5.3 Yêu cầu 1 — RBAC + VPD (giải pháp + screenshot)
   5.4 Yêu cầu 2 — OLS (bảng nhãn u1-u8 vs t1-t7 + minh chứng kết quả mỗi user thấy gì)
   5.5 Yêu cầu 3 — Audit (5 ngữ cảnh Standard + 4 FGA + đọc xuất DBA_AUDIT_TRAIL)
   5.6 Yêu cầu 4 — Backup/Recovery (RMAN, DataPump, Flashback, kèm kịch bản phục hồi)
6. PHẦN C — Bảo mật lớp Application
   6.1 OracleErrorMapper (không leak ORA-xxxxx)
   6.2 InputValidator (CCCD regex, mask)
   6.3 SessionManager (idle timeout)
   6.4 AppAuditLogger (rolling log)
   6.5 ConfirmDeleteDialog (chống xoá nhầm)
   6.6 Brute-force lockout (5 fail → 60s)
7. PHẦN D — UI/UX
   7.1 Theme Montserrat
   7.2 Self-service tab
   7.3 Search/Toast/Shortcuts
8. Kết luận + Hướng phát triển
9. Tài liệu tham khảo
```

**Lưu ý:**
- Spec yêu cầu: "Trình bày giải pháp lý thuyết ngắn gọn, dễ hiểu, ghi rõ tài liệu tham khảo, **không dịch lại tài liệu**" — viết theo cách tóm lược + đánh giá, không sao chép Oracle docs.
- Mỗi YC cần có: (1) Mô tả yêu cầu, (2) Giải pháp đã chọn, (3) Code snippet quan trọng, (4) Screenshot kết quả test, (5) Nhận xét/đánh giá.

**Cách thực hiện nhanh:** Viết bằng Markdown trước rồi convert sang docx bằng Pandoc:
```bash
pandoc baocao.md -o baocao.docx --reference-doc=template.docx
```

### I.2 — Bảng phân công công việc — 30 phút

Mẫu (đóng trong báo cáo Word):

```
┌────────────────┬────────────┬────────────┬─────────────┬─────────────┐
│ MSSV           │ Họ tên     │ % Phân hệ 1│ % Phân hệ 2 │ % Tổng      │
├────────────────┼────────────┼────────────┼─────────────┼─────────────┤
│ 21120xxx       │ NV A       │ 40%        │ 35%         │ 37%         │
│ 21120yyy       │ NV B       │ 35%        │ 35%         │ 35%         │
│ 21120zzz       │ NV C       │ 25%        │ 30%         │ 28%         │
└────────────────┴────────────┴────────────┴─────────────┴─────────────┘

Chi tiết công việc từng thành viên:
- NV A: ... (DBA layer: schema, RBAC, VPD, OLS, audit, backup)
- NV B: ... (App layer: WinForms, theme, security)
- NV C: ... (Báo cáo, test, demo)
```

### I.3 — Đóng gói thư mục nộp — 1h

```
ATBM-2026-<MaNhom>/
├── MSSV1_MSSV2_MSSV3.docx          ← báo cáo
├── README.md                        ← hướng dẫn cài đặt
├── PhanHe2/                         ← scripts SQL (01-08)
├── HospitalApp/                     ← source code
└── HospitalApp.exe                  ← build sẵn (optional)
```

**Lệnh build release:**
```powershell
cd HospitalApp
dotnet publish -c Release -r win-x64 --self-contained -o ../dist
```

Zip toàn bộ → upload Moodle.

### I.4 — README.md cập nhật — 1h

Cập nhật [README.md](../../README.md) hiện tại để thêm:
- Bước cài Oracle Net encryption (sqlnet.ora)
- Thứ tự chạy script 01→08 (đã có 08 mới)
- Hướng dẫn copy Montserrat font
- Mục bảo mật lớp app (tham chiếu các file Security/)
- Tài khoản mẫu sau migration: thêm `u1_giamdoc`...`u8_nvth_hni` cho OLS

### I.5 — Hospital icon — 15 phút

Hiện `HospitalApp.csproj` đã bỏ `<ApplicationIcon>` (Sprint 2 fix build). Tạo file `hospital.ico` (1 file 32×32 PNG → convert ICO) và khôi phục dòng `<ApplicationIcon>hospital.ico</ApplicationIcon>`.

---

## PHẦN II — Tăng điểm vấn đáp

> Hai phần này không bắt buộc nhưng tăng đáng kể chất lượng đồ án khi GV chấm/vấn đáp.

### II.1 — Hợp nhất OLS vào NHANVIEN — 2.5h

**Vấn đề hiện tại:** u1-u8 là user OLS rời rạc, không nằm trong NHANVIEN → khi vấn đáp GV có thể hỏi "tại sao tách 2 nhóm user?".

**Giải pháp:** Thêm 2 cột vào NHANVIEN + procedure tự gán nhãn OLS dựa vào cột đó.

**File mới:** `PhanHe2/09_OLS_NhanVien_Unified.sql`

```sql
-- Bổ sung cột vào NHANVIEN
ALTER TABLE BVADMIN.NHANVIEN ADD (
    CAPBAC  VARCHAR2(10) CHECK (CAPBAC IN ('NV','LDK','BGD')),
    COSO    VARCHAR2(10) CHECK (COSO   IN ('HCM','HPN','HNI')),
    KHOA_NHAN  VARCHAR2(10) CHECK (KHOA_NHAN IN ('TH','TK','TM','ALL'))
);

-- Procedure tự gán nhãn OLS khi tạo NV
CREATE OR REPLACE PROCEDURE sp_apply_ols_label(
    p_manv IN VARCHAR2
) AS
    v_oracle_user VARCHAR2(100);
    v_capbac      VARCHAR2(10);
    v_coso        VARCHAR2(10);
    v_khoa        VARCHAR2(10);
    v_label       VARCHAR2(100);
BEGIN
    SELECT ORACLE_USER, CAPBAC, COSO, KHOA_NHAN
    INTO   v_oracle_user, v_capbac, v_coso, v_khoa
    FROM   NHANVIEN WHERE MANV = p_manv;

    -- Tạo nhãn dạng: LEVEL[:COMPARTMENT][:GROUP]
    v_label := v_capbac;
    IF v_coso IS NOT NULL  THEN v_label := v_label || ':' || v_coso; END IF;
    IF v_khoa IS NOT NULL
       AND v_khoa != 'ALL' THEN v_label := v_label || ':' || v_khoa; END IF;

    SA_USER_ADMIN.SET_USER_LABELS(
        policy_name    => 'BV_LABEL_POLICY',
        user_name      => UPPER(v_oracle_user),
        max_read_label => v_label
    );
END;
/

-- Update NV mẫu hiện có với CAPBAC/COSO/KHOA_NHAN
UPDATE BVADMIN.NHANVIEN SET CAPBAC='NV',  COSO='HCM', KHOA_NHAN='ALL'  WHERE MANV='NV001';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='NV',  COSO='HNI', KHOA_NHAN='ALL'  WHERE MANV='NV002';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='LDK', COSO='HCM', KHOA_NHAN='TM'   WHERE MANV='NV003';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='LDK', COSO='HNI', KHOA_NHAN='TK'   WHERE MANV='NV004';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='LDK', COSO='HNI', KHOA_NHAN='TH'   WHERE MANV='NV005';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='NV',  COSO='HCM', KHOA_NHAN='TM'   WHERE MANV='NV006';
UPDATE BVADMIN.NHANVIEN SET CAPBAC='NV',  COSO='HNI', KHOA_NHAN='TH'   WHERE MANV='NV007';

-- Gán nhãn OLS cho tất cả NV hiện có
BEGIN
    FOR r IN (SELECT MANV FROM BVADMIN.NHANVIEN) LOOP
        sp_apply_ols_label(r.MANV);
    END LOOP;
END;
/

-- Cấp SELECT trên THONGBAO cho 3 role
GRANT SELECT ON BVADMIN.THONGBAO TO DPV_Role;
GRANT SELECT ON BVADMIN.THONGBAO TO BS_Role;
GRANT SELECT ON BVADMIN.THONGBAO TO KTV_Role;
```

**Code C# cần sửa:**
- [Controls/MyProfilePanel.cs](../../HospitalApp/Controls/MyProfilePanel.cs) — bổ sung label hiển thị CAPBAC/COSO/KHOA_NHAN (chỉ đọc — chỉ DBA mới đổi được)
- Tab Thông báo trong DPV/BS/KTV → hiển thị nhãn OLS hiện tại của user

**Tác động:** Sau migration này, mọi nhân viên trong NHANVIEN tự động có nhãn OLS và xem được thông báo phù hợp. u1-u8 không còn cần thiết (giữ lại để demo nếu muốn).

### II.2 — Demo Recovery thực tế — 2.5h

**Vấn đề hiện tại:** [PhanHe2/07_YC4_Backup_Recovery.sql](../../PhanHe2/07_YC4_Backup_Recovery.sql) phần lớn là comment. Spec yêu cầu "hiện thực + khôi phục dựa vào nhật ký kiểm toán" — cần demo chạy được.

**File mới:** `PhanHe2/09_Recovery_Demo.sql`

Kịch bản 1 — Phục hồi sau khi xoá nhầm HSBA_DV:

```sql
-- 1. Trạng thái ban đầu
CONNECT BVADMIN/BVAdmin@2025;
SELECT COUNT(*) FROM HSBA_DV;  -- ghi nhận con số

-- 2. Lưu checkpoint
INSERT INTO CHECKPOINT_LOG(EVENT_NAME, SCN)
VALUES('demo_before_delete', DBMS_FLASHBACK.GET_SYSTEM_CHANGE_NUMBER());
COMMIT;

-- 3. Sự cố: BVADMIN giả lập xoá nhầm
DELETE FROM HSBA_DV WHERE MAHSBA = 'HS001';
COMMIT;
SELECT COUNT(*) FROM HSBA_DV;  -- ít hơn 2 dòng

-- 4. Phát hiện qua audit FGA
SELECT DB_USER, SQL_TEXT, EXTENDED_TIMESTAMP
FROM DBA_FGA_AUDIT_TRAIL
WHERE POLICY_NAME = 'FGA_HSBA_DV_ILLEGAL'
ORDER BY EXTENDED_TIMESTAMP DESC FETCH FIRST 5 ROWS ONLY;

-- 5. Lấy SCN trước sự cố
SELECT SCN FROM CHECKPOINT_LOG WHERE EVENT_NAME = 'demo_before_delete';

-- 6. Flashback bảng về trước sự cố
FLASHBACK TABLE BVADMIN.HSBA_DV TO SCN <scn từ bước 5>;

-- 7. Verify
SELECT COUNT(*) FROM HSBA_DV;  -- khôi phục
SELECT * FROM HSBA_DV WHERE MAHSBA = 'HS001';
```

Kịch bản 2 — Phục hồi đơn thuốc bị sửa sai:

```sql
-- Tương tự, dùng Flashback Query AS OF TIMESTAMP để lấy lại liều cũ
SELECT * FROM DONTHUOC AS OF TIMESTAMP (SYSTIMESTAMP - INTERVAL '5' MINUTE)
WHERE MAHSBA = 'HS001';

-- Hoặc dùng version query xem lịch sử
SELECT VERSIONS_STARTTIME, VERSIONS_OPERATION, TENTHUOC, LIEUDUNG
FROM DONTHUOC VERSIONS BETWEEN TIMESTAMP MINVALUE AND MAXVALUE
WHERE MAHSBA = 'HS001';
```

**Yêu cầu khi nộp:** Chạy thật + screenshot từng bước → đưa vào báo cáo phần 5.6.

---

## PHẦN III — UX nâng cao

> Nếu còn thời gian. Không bắt buộc nhưng tăng điểm UX.

### III.1 — Pagination cho BN list — 1h

Spec ghi `TC#5: khoảng 100,000 bệnh nhân`. Hiện tại `LoadBN()` load toàn bộ → chậm.

**File mới:** `Controls/Pagination.cs`

```csharp
public sealed class Pagination : UserControl
{
    public int PageSize { get; set; } = 50;
    public int CurrentPage { get; private set; } = 1;
    public int TotalCount { get; private set; }
    public event Action<int>? PageChanged;
    // ... button ◄ 1 2 3 ... N ►
}
```

**Sửa [DPVForm.LoadBN()](../../HospitalApp/Forms/Hospital/DPVForm.cs):**
```sql
SELECT * FROM (
  SELECT a.*, ROWNUM rn FROM (
    SELECT ... FROM BENHNHAN ORDER BY TENBN
  ) a WHERE ROWNUM <= :end
) WHERE rn > :start
```

### III.2 — Dropdown thay TextBox cho MAKHOA — 30 phút

Hiện DPVForm dùng TextBox tự gõ. Tạo bảng `KHOA` master data hoặc enum cứng:

```csharp
_cmbKhoa = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
_cmbKhoa.Items.AddRange(new[] {
    "Tim mạch", "Thần kinh", "Tiêu hóa",
    "Hô hấp", "Nội tiết", "Cơ xương khớp"
});
```

Hoặc tạo bảng `KHOA(MAKHOA, TENKHOA)` để DBA quản lý động.

### III.3 — Loading spinner — 30 phút

**File mới:** `Controls/LoadingOverlay.cs`

```csharp
public static class Loading
{
    public static IDisposable Show(Form f, string msg = "Đang tải...")
    {
        // Overlay semi-transparent + ProgressBar
        // return IDisposable để Dispose() ẩn overlay
    }
}

// Cách dùng:
using (Loading.Show(this, "Đang tải HSBA..."))
{
    _dgvHSBA.DataSource = await Task.Run(() => _db.Query(...));
}
```

### III.4 — Status bar — 30 phút

Footer mọi form: `Đã kết nối: localhost/XEPDB1  |  User: BS_NV003  |  Vai trò: Bác sĩ  |  16:42:15`

### III.5 — Calendar widget + validate ngày sinh — 30 phút

DPVForm tạo BN: kiểm tra ngày sinh < ngày hôm nay, tuổi 0-150.

### III.6 — Inline edit trong grid — 1h

Cho phép DPV double-click cell trong grid HSBA để sửa MAKHOA/MABS thẳng trong grid (thay vì panel detail).

### III.7 — Empty state — 15 phút

Khi grid không có dữ liệu, hiển thị: icon + text "Chưa có bệnh nhân" + nút CTA "Thêm BN đầu tiên".

---

## PHẦN IV — Hardening

> Hoàn thiện security + DevOps.

### IV.1 — Oracle Net encryption — 30 phút

**File mới:** `docs/guides/SETUP_ENCRYPTION.md`

Hướng dẫn tạo `%ORACLE_HOME%/network/admin/sqlnet.ora`:

```
SQLNET.ENCRYPTION_CLIENT         = REQUIRED
SQLNET.ENCRYPTION_TYPES_CLIENT   = (AES256, AES192, AES128)
SQLNET.CRYPTO_CHECKSUM_CLIENT    = REQUIRED
SQLNET.CRYPTO_CHECKSUM_TYPES_CLIENT = (SHA512, SHA384, SHA256)
```

→ Mọi packet TCP client-server đều mã hoá AES-256.

Verify bằng `SELECT NETWORK_SERVICE_BANNER FROM V$SESSION_CONNECT_INFO WHERE SID = SYS_CONTEXT('USERENV','SID');` → phải có "AES256 Encryption service".

### IV.2 — Migration runner script — 30 phút

**File mới:** `scripts/setup.ps1` (PowerShell) hoặc `setup.bat`

```powershell
# scripts/setup.ps1
param(
    [string]$Host = "localhost",
    [string]$Port = "1521",
    [string]$Sid  = "XEPDB1",
    [string]$SysPass = "oracle"
)

$scripts = @(
    "01_schema_data.sql",
    "02_TC1_accounts.sql",
    "03_YC1_C2_RBAC_KTV_BN.sql",
    "04_YC1_C3_VPD_DPV_BS.sql",
    "05_YC2_OLS_ThongBao.sql",
    "06_YC3_Audit.sql",
    "07_YC4_Backup_Recovery.sql",
    "08_App_Migrations.sql",
    "09_OLS_NhanVien_Unified.sql",   # nếu làm Phần II.1
    "09_Recovery_Demo.sql"            # nếu làm Phần II.2
)

foreach ($s in $scripts) {
    Write-Host "Running $s..." -ForegroundColor Cyan
    sqlplus "sys/$SysPass@$Host`:$Port/$Sid AS SYSDBA" "@PhanHe2/$s"
    if ($LASTEXITCODE -ne 0) { throw "Failed: $s" }
}
```

### IV.3 — Unit tests Security/* — 1h

**Project mới:** `HospitalApp.Tests/HospitalApp.Tests.csproj` (xUnit)

Test các class không cần DB:
- `OracleErrorMapper.Friendly()` với 10 mã ORA-xxxxx khác nhau
- `InputValidator.IsValidCccd()` với valid/invalid
- `InputValidator.MaskCccd()`
- `InputValidator.CheckPasswordStrength()`
- `AppAuditLogger` write + read

```csharp
[Fact]
public void Mapper_Maps_ORA_01017_To_Invalid_Credentials()
{
    var msg = OracleErrorMapper.Friendly(
        new Exception("ORA-01017: invalid username/password"));
    Assert.Contains("không đúng", msg);
}
```

### IV.4 — Logging với Serilog — 30 phút (optional)

Thay [AppAuditLogger.cs](../../HospitalApp/Security/AppAuditLogger.cs) bằng Serilog:
- Console + File sink
- Rolling theo ngày tự động
- Structured logging (JSON)

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(Path.Combine(LogDir, "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();
```

---

## PHẦN V — Vấn đáp & Trình bày

### V.1 — Slide pitch — 2h

**File mới:** `docs/slides.pptx` (hoặc Google Slides)

Khoảng 20 slides:
1. Bìa + tên nhóm
2. Đề bài tóm tắt (5 yêu cầu)
3. Kiến trúc tổng thể (sơ đồ)
4. Schema ER
5. TC#1 — Account mapping
6. YC1 Câu 2 — RBAC demo
7. YC1 Câu 3 — VPD demo
8. YC2 — OLS bảng nhãn
9. YC2 — Demo u1 vs u4 thấy gì khác nhau
10. YC3 — 5 ngữ cảnh Standard Audit
11. YC3 — 4 FGA policies
12. YC4 — 3 chiến lược backup
13. YC4 — Demo Recovery (flashback)
14. App-layer security — defense in depth
15. UI/UX — Montserrat + Toast + Search
16. Self-service NV
17. So sánh trước/sau Sprint 1-5
18. Phân công thành viên
19. Kết luận
20. Q&A

### V.2 — Demo script — 1h

**File mới:** `docs/guides/DEMO_SCRIPT.md` — kịch bản từng bước, mỗi user demo cái gì:

```
1. Admin login → AdminDashboard
   - Tạo user mới TEST_USER
   - Gán role
   - Thu hồi

2. DPV_NV001 login → DPVForm
   - Thêm BN mới (tự tạo Oracle account)
   - Tạo HSBA mới (MAHSBA dùng sequence)
   - Giao BS, giao KTV

3. BS_NV003 login → BSForm
   - VPD: chỉ thấy HSBA của mình
   - Cập nhật CHANDOAN → trigger ghi vết LOG_BS_HSBA
   - Thêm dịch vụ → AddDVDialog dropdown KTV

4. KTV_NV006 login → KTVForm
   - View tự filter MAKTV
   - Cập nhật KETQUA → trigger ghi vết
   - Thử update cột khác → bị chặn ORA-20001

5. BN_BN001 login → BNForm
   - Chỉ thấy info của mình
   - Sửa địa chỉ OK
   - Thử sửa CCCD → bị chặn ORA-20002

6. u4_nvtk_hcm login → OLSViewerForm
   - Hiển thị nhãn NV:HCM:TK
   - Chỉ thấy thông báo t1 (NV)

7. Idle timeout demo: chờ 10 phút → tự logout

8. Brute-force demo: nhập sai 5 lần → khoá 60s

9. Audit demo: SELECT DBA_AUDIT_TRAIL → cho thấy các thao tác bất hợp pháp đã được log

10. Recovery demo: chạy 09_Recovery_Demo.sql
```

### V.3 — Talking points — 30 phút

Trả lời câu hỏi GV có thể hỏi:

| Câu hỏi | Trả lời chuẩn bị |
|---------|------------------|
| Vì sao chọn RBAC cho KTV thay vì VPD? | KTV chỉ cần lọc ROW theo MAKTV — view filter là đủ, đơn giản hơn VPD. VPD phù hợp khi cần lọc động phức tạp. |
| Vì sao VPD cho BS chứ không RBAC? | BS phải dùng cùng query trên HSBA nhưng VPD tự thêm WHERE → kể cả khi BS dùng SQL trực tiếp vẫn an toàn. |
| update_check = TRUE có nghĩa gì? | Sau UPDATE phải check lại policy → ngăn BS sửa MABS thành người khác để "chuyển" HSBA. |
| OLS Compartment vs Group khác nhau? | Compartment = AND (user phải có tất cả), Group = OR (chỉ cần 1) + có hierarchy. |
| Vì sao mask CCCD ở grid mà không ở form chi tiết? | Grid hiển thị nhiều dòng dễ bị "shoulder surfing", form chi tiết chỉ 1 BN người dùng đang focus. |
| Brute-force ở app layer khác gì ở DB FAILED_LOGIN_ATTEMPTS? | App layer chặn TRƯỚC khi gửi tới DB → giảm tải. DB là tuyến cuối. Defense in depth. |
| SessionManager tại sao không dùng DB session timeout? | DB timeout chỉ ngắt connection, nhưng UI vẫn mở → user mất việc. App timeout đóng UI rõ ràng + log app event. |

---

## Thứ tự thực hiện đề xuất

### Ngày 1 (4h) — Bắt buộc
- [ ] I.3 Đóng gói thư mục (1h)
- [ ] I.4 README cập nhật (1h)
- [ ] I.5 Hospital icon (15p)
- [ ] II.2 Demo Recovery (2.5h) ← nâng điểm YC4

### Ngày 2 (4h) — Tăng điểm
- [ ] II.1 OLS hợp nhất NHANVIEN (2.5h)
- [ ] IV.1 Oracle Net encryption (30p)
- [ ] IV.2 Migration runner (30p)
- [ ] III.3 Loading spinner (30p)

### Ngày 3 (4h) — Báo cáo
- [ ] I.1 Báo cáo Word (3h)
- [ ] I.2 Bảng phân công (30p)
- [ ] V.3 Talking points (30p)

### Ngày 4 (3h) — Trình bày
- [ ] V.1 Slides (2h)
- [ ] V.2 Demo script (1h)

### Ngày 5 (2h) — Polish & Test (nếu còn thời gian)
- [ ] III.1 Pagination (1h)
- [ ] III.2 Dropdown MAKHOA (30p)
- [ ] IV.3 Unit tests (30p)
- [ ] Tổng diễn tập demo

**Tổng: 17h trong 5 ngày**

---

## Checklist final trước khi nộp

### Code
- [ ] Build release `dotnet publish` thành công 0 errors
- [ ] Test login thành công với mỗi role (DBA/DPV/BS/KTV/BN/OLS)
- [ ] Test idle timeout (chờ thực 10p hoặc giảm xuống 30s để test)
- [ ] Test brute-force (5 fail → khoá)
- [ ] Test mỗi YC1-YC4 đúng kết quả expected
- [ ] Demo Recovery flashback chạy được
- [ ] Montserrat hiển thị đúng (so với fallback Segoe UI)

### Database
- [ ] Script 01-09 chạy tuần tự không lỗi
- [ ] OLS user u1-u8 thấy đúng thông báo expected
- [ ] FGA + Standard Audit ghi vào DBA_*_AUDIT_TRAIL đúng
- [ ] Backup RMAN có file `.bkp`
- [ ] CHECKPOINT_LOG có dòng

### Files
- [ ] Thư mục `ATBM-2026-<MãNhóm>/`
- [ ] `MSSV1_MSSV2_MSSV3.docx` đặt tên đúng
- [ ] Báo cáo có bảng phân công + %
- [ ] README.md cập nhật mới nhất
- [ ] Không có file `.env`, `.bak`, password trong source

### Submission
- [ ] Upload Moodle trước deadline
- [ ] Bản in báo cáo giấy nộp ngày chấm
- [ ] Mọi thành viên đều biết toàn bộ source (chính sách "chấm tại chỗ")

---

## Liên kết nội bộ

- [Đề bài gốc](../assignment/2025-2026%20Do%20an%20mon%20hoc%20ATBM%20HTTT.pdf)
- [README hiện tại](../../README.md)
- [SQL scripts](../../PhanHe2/)
- [Source code](../../HospitalApp/)

---

*File này có thể xoá khi đã hoàn tất toàn bộ. Hoặc giữ lại như tài liệu nội bộ của nhóm.*
