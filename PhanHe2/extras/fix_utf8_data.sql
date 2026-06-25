-- ============================================================
-- PHÂN HỆ 2 - (extras) SỬA dữ liệu tiếng Việt bị hỏng encoding
-- ============================================================
-- Khi user chạy 01_schema_data.sql qua SQL*Plus mà KHÔNG set
-- NLS_LANG=.AL32UTF8 trước, bytes UTF-8 trong file bị Oracle
-- decode bằng codepage hệ thống (Windows-1252) → INSERT lưu byte sai.
--
-- Triệu chứng: app hiển thị "LÃª Lá»£i" thay "Lê Lợi",
-- "Tim máº¡ch" thay "Tim mạch", v.v.
--
-- File này UPDATE các record với giá trị chuẩn (N'...' literal).
-- ============================================================
-- BẮT BUỘC: set NLS_LANG TRƯỚC sqlplus / SQL Developer phải dùng UTF-8
--
-- Windows CMD:   set NLS_LANG=.AL32UTF8
-- PowerShell:    $env:NLS_LANG = ".AL32UTF8"
--
-- Chạy với user BVADMIN (chủ schema):
--   sqlplus BVADMIN/<BVADMIN_pass>@//localhost:1521/XEPDB1
--   SQL> @extras/fix_utf8_data.sql   (đang nằm trong thư mục extras/)
-- ============================================================
-- KHÔNG dùng CONNECT — script chạy với session hiện tại
SET DEFINE OFF
-- (tránh SQL*Plus hiểu '&' trong dữ liệu là biến thay thế; KHÔNG đặt ';' sau SET)

-- ── NHANVIEN ────────────────────────────────────────────────────────────────
UPDATE NHANVIEN SET HOTEN=N'Nguyễn Văn An',  QUEQUAN=N'TP. Hồ Chí Minh',     CHUYENKHOA=N'Tiếp nhận'         WHERE MANV='NV001';
UPDATE NHANVIEN SET HOTEN=N'Trần Thị Bích',  QUEQUAN=N'Hà Nội',              CHUYENKHOA=N'Tiếp nhận'         WHERE MANV='NV002';
UPDATE NHANVIEN SET HOTEN=N'Lê Văn Cường',   QUEQUAN=N'TP. Hồ Chí Minh',     CHUYENKHOA=N'Tim mạch'          WHERE MANV='NV003';
UPDATE NHANVIEN SET HOTEN=N'Phạm Thị Dung',  QUEQUAN=N'Hà Nội',              CHUYENKHOA=N'Thần kinh'         WHERE MANV='NV004';
UPDATE NHANVIEN SET HOTEN=N'Hoàng Văn Em',   QUEQUAN=N'Đà Nẵng',             CHUYENKHOA=N'Tiêu hóa'          WHERE MANV='NV005';
UPDATE NHANVIEN SET HOTEN=N'Vũ Thị Phương',  QUEQUAN=N'TP. Hồ Chí Minh',     CHUYENKHOA=N'Xét nghiệm'        WHERE MANV='NV006';
UPDATE NHANVIEN SET HOTEN=N'Đỗ Văn Quang',   QUEQUAN=N'Hà Nội',              CHUYENKHOA=N'Chẩn đoán hình ảnh' WHERE MANV='NV007';
COMMIT;

-- ── BENHNHAN ────────────────────────────────────────────────────────────────
UPDATE BENHNHAN SET
    TENBN        = N'Mai Thị Hoa',
    SONHA        = N'12',
    TENDUONG     = N'Lê Lợi',
    QUANHUYEN    = N'Quận 1',
    TINHTP       = N'TP. Hồ Chí Minh',
    TIENSUBENH   = N'Tiểu đường type 2',
    TIENSUBENHGD = N'Mẹ và chị gái bị tiểu đường type 2',
    DIUNGTHUOC   = N'Penicillin'
WHERE MABN='BN001';

UPDATE BENHNHAN SET
    TENBN        = N'Nguyễn Văn Bình',
    SONHA        = N'5A',
    TENDUONG     = N'Trần Hưng Đạo',
    QUANHUYEN    = N'Quận 5',
    TINHTP       = N'TP. Hồ Chí Minh',
    TIENSUBENH   = N'Không',
    TIENSUBENHGD = N'Cha bị cao huyết áp'
WHERE MABN='BN002';

UPDATE BENHNHAN SET
    TENBN        = N'Trần Thị Cẩm',
    SONHA        = N'88',
    TENDUONG     = N'Hoàng Diệu',
    QUANHUYEN    = N'Hải Châu',
    TINHTP       = N'Đà Nẵng',
    TIENSUBENH   = N'Hen suyễn',
    TIENSUBENHGD = N'Gia đình có tiền sử hen phế quản'
WHERE MABN='BN003';
COMMIT;

-- ── HSBA ────────────────────────────────────────────────────────────────────
UPDATE HSBA SET
    CHANDOAN = N'Đái tháo đường type 2 biến chứng',
    DIEUTRI  = N'Insulin + Metformin',
    MAKHOA   = N'Tim mạch'
WHERE MAHSBA='HS001';

UPDATE HSBA SET
    CHANDOAN = N'Đau đầu căng thẳng',
    DIEUTRI  = N'Nghỉ ngơi + Paracetamol',
    KETLUAN  = N'Ổn định',
    MAKHOA   = N'Thần kinh'
WHERE MAHSBA='HS002';

UPDATE HSBA SET
    CHANDOAN = N'Hen phế quản',
    DIEUTRI  = N'Ventolin + Corticoid khí dung',
    MAKHOA   = N'Tiêu hóa'
WHERE MAHSBA='HS003';

UPDATE HSBA SET
    CHANDOAN = N'Cao huyết áp',
    DIEUTRI  = N'Amlodipine 5mg',
    MAKHOA   = N'Tim mạch'
WHERE MAHSBA='HS004';
COMMIT;

-- ── HSBA_DV ─────────────────────────────────────────────────────────────────
-- FIX (H3): KHÔNG dùng LOAIDV (cột PK) trong WHERE để tự sửa chính nó (mong manh + dễ trùng PK).
-- Định danh dòng bằng (MAHSBA, NGAYDV) — bộ giá trị duy nhất với dữ liệu mẫu.
UPDATE HSBA_DV SET LOAIDV=N'Xét nghiệm máu tổng quát',
    KETQUA=N'Glucose: 12.5 mmol/L, HbA1c: 9%'
WHERE MAHSBA='HS001' AND NGAYDV=DATE'2025-04-01';

UPDATE HSBA_DV SET LOAIDV=N'Siêu âm tim',
    KETQUA=N'Tim bình thường, EF 65%'
WHERE MAHSBA='HS001' AND NGAYDV=DATE'2025-04-02';

UPDATE HSBA_DV SET LOAIDV=N'Điện não đồ'
WHERE MAHSBA='HS002' AND NGAYDV=DATE'2025-04-05';

UPDATE HSBA_DV SET LOAIDV=N'Đo chức năng hô hấp'
WHERE MAHSBA='HS003' AND NGAYDV=DATE'2025-04-10';
COMMIT;

-- ── DONTHUOC ────────────────────────────────────────────────────────────────
UPDATE DONTHUOC SET TENTHUOC=N'Metformin 500mg', LIEUDUNG=N'2 lần/ngày sau ăn'
WHERE MAHSBA='HS001' AND TENTHUOC LIKE 'Metformin%';

UPDATE DONTHUOC SET TENTHUOC=N'Insulin Glargine', LIEUDUNG=N'10 đơn vị, tiêm dưới da tối'
WHERE MAHSBA='HS001' AND TENTHUOC LIKE 'Insulin%';

UPDATE DONTHUOC SET TENTHUOC=N'Paracetamol 500mg', LIEUDUNG=N'3 lần/ngày khi đau'
WHERE MAHSBA='HS002';

UPDATE DONTHUOC SET TENTHUOC=N'Ventolin 100mcg', LIEUDUNG=N'2 nhát xịt khi cần'
WHERE MAHSBA='HS003';
COMMIT;

-- ── THONGBAO ────────────────────────────────────────────────────────────────
UPDATE THONGBAO SET NOIDUNG=N'Thông báo họp toàn viện ngày 05/05/2025', DIADIEM=N'Hội trường lớn'      WHERE MATB='TB001';
UPDATE THONGBAO SET NOIDUNG=N'Họp khẩn Ban Giám đốc - Kế hoạch mở rộng 2025', DIADIEM=N'Phòng họp BGD'  WHERE MATB='TB002';
UPDATE THONGBAO SET NOIDUNG=N'Họp lãnh đạo khoa - Báo cáo quý 2', DIADIEM=N'Phòng họp A'                WHERE MATB='TB003';
UPDATE THONGBAO SET NOIDUNG=N'Họp lãnh đạo Khoa Tiêu hóa - Cải tiến quy trình', DIADIEM=N'Phòng D2.01'  WHERE MATB='TB004';
UPDATE THONGBAO SET NOIDUNG=N'Tập huấn nội soi tiêu hóa - Cơ sở HCM', DIADIEM=N'Phòng kỹ năng HCM'      WHERE MATB='TB005';
UPDATE THONGBAO SET NOIDUNG=N'Tập huấn nội soi tiêu hóa - Cơ sở Hà Nội', DIADIEM=N'Phòng kỹ năng HN'    WHERE MATB='TB006';
UPDATE THONGBAO SET NOIDUNG=N'Họp khẩn Lãnh đạo Khoa TH và TK - Cơ sở Hải Phòng', DIADIEM=N'Phòng họp HP' WHERE MATB='TB007';
COMMIT;

-- ── Verify ─────────────────────────────────────────────────────────────────
-- FIX (L6): dùng tiền tố N cho literal để khớp NVARCHAR2 (HOTEN/TENBN), tránh âm tính giả.
SELECT 'NHANVIEN' AS TBL, COUNT(*) FROM NHANVIEN WHERE HOTEN LIKE N'%ễ%' OR HOTEN LIKE N'%ư%'
UNION ALL
SELECT 'BENHNHAN', COUNT(*) FROM BENHNHAN WHERE TENBN LIKE N'%ị%' OR TENBN LIKE N'%ễ%';
-- Nếu COUNT > 0 → Vietnamese diacritics đã decode đúng.
