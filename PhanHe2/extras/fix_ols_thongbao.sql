-- ============================================================
-- HOTFIX: THONGBAO không hiện thông báo nào (kể cả u1_giamdoc)
-- ============================================================
-- NGUYÊN NHÂN: gán nhãn dòng OLS (SET_ROW_LABEL + INSERT) PHẢI chạy bằng phiên
-- của BVADMIN (đã được cấp FULL). Nếu chạy file 05 qua setup.ps1 (vốn đổi CONNECT
-- thành ALTER SESSION SET CURRENT_SCHEMA → mọi thứ chạy dưới SYS), các dòng THONGBAO
-- KHÔNG nhận đúng nhãn → user thường (u1..u8) đọc ra 0 dòng.
--
-- CÁCH CHẠY (BẮT BUỘC dùng CONNECT thật, KHÔNG chạy qua setup.ps1):
--   PowerShell:  $env:NLS_LANG = ".AL32UTF8"     <-- PHẢI dùng cú pháp này
--                sqlplus /nolog "@PhanHe2/extras/fix_ols_thongbao.sql"
--   ⚠️ `set NLS_LANG=...` (cú pháp cmd) KHÔNG có tác dụng trong PowerShell → các literal N'...'
--      bị ghi SAI charset (mojibake "ThÃ´ng bÃ¡o"). File này cũng phải được lưu dạng UTF-8.
--   ⚠️ Cách chống charset tuyệt đối (không phụ thuộc NLS_LANG/BOM): chèn bằng UNISTR('\xxxx')
--      thuần ASCII — vd N'Hội' -> UNISTR('H\1ed9i'). Dữ liệu lỗi trên DB này đã được vá lại
--      bằng cách đó (sinh UNISTR từ chuỗi UTF-8 rồi chạy qua sqlplus).
-- (Sửa mật khẩu LBACSYS/BVADMIN bên dưới cho khớp DB của bạn nếu khác.)
-- ============================================================
SET DEFINE OFF

-- 1) Đảm bảo BVADMIN có quyền FULL trên policy (để gán nhãn dòng bất kỳ)
--    >>> SỬA mật khẩu LBACSYS cho khớp DB của bạn. Nếu không biết: đăng nhập SYS rồi
--        ALTER USER LBACSYS IDENTIFIED BY "Lbac@2025" ACCOUNT UNLOCK;  rồi dùng "Lbac@2025".
--    >>> PHẢI có @localhost:1521/XEPDB1 vì policy nằm trong PDB XEPDB1.
CONNECT LBACSYS/"Lbac@2025"@localhost:1521/XEPDB1
BEGIN
    SA_USER_ADMIN.SET_USER_PRIVS('BV_LABEL_POLICY', 'BVADMIN', 'FULL');
END;
/

-- 2) Chèn lại 7 thông báo kèm nhãn — chạy DƯỚI PHIÊN BVADMIN (không phải SYS)
CONNECT BVADMIN/"BVAdmin@2025"@localhost:1521/XEPDB1

DELETE FROM THONGBAO;
COMMIT;

-- FIX QUYẾT ĐỊNH: policy chỉ bật READ_CONTROL → SET_ROW_LABEL KHÔNG tự gán nhãn khi INSERT
-- (cần option LABEL_DEFAULT). Vì vậy gán nhãn TRỰC TIẾP vào cột OLS_LABEL bằng CHAR_TO_LABEL.
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB001', CHAR_TO_LABEL('BV_LABEL_POLICY','NV'),
       N'Thông báo họp toàn viện ngày 05/05/2025', SYSTIMESTAMP, N'Hội trường lớn');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB002', CHAR_TO_LABEL('BV_LABEL_POLICY','BGD'),
       N'Họp khẩn Ban Giám đốc - Kế hoạch mở rộng 2025', SYSTIMESTAMP, N'Phòng họp BGD');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB003', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK'),
       N'Họp lãnh đạo khoa - Báo cáo quý 2', SYSTIMESTAMP, N'Phòng họp A');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB004', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK::TH'),
       N'Họp lãnh đạo Khoa Tiêu hóa - Cải tiến quy trình', SYSTIMESTAMP, N'Phòng D2.01');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB005', CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HCM:TH'),
       N'Tập huấn nội soi tiêu hóa - Cơ sở HCM', SYSTIMESTAMP, N'Phòng kỹ năng HCM');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB006', CHAR_TO_LABEL('BV_LABEL_POLICY','NV:HNI:TH'),
       N'Tập huấn nội soi tiêu hóa - Cơ sở Hà Nội', SYSTIMESTAMP, N'Phòng kỹ năng HN');
INSERT INTO THONGBAO(MATB, OLS_LABEL, NOIDUNG, NGAYGIO, DIADIEM)
VALUES('TB007', CHAR_TO_LABEL('BV_LABEL_POLICY','LDK:HPN:TH,TK'),
       N'Họp khẩn Lãnh đạo Khoa TH và TK - Cơ sở Hải Phòng', SYSTIMESTAMP, N'Phòng họp HP');
COMMIT;

-- 3) Kiểm tra: BVADMIN (FULL) phải thấy đủ 7 dòng; cột OLS_LABEL có giá trị (khác NULL) = đã gán nhãn
SELECT MATB, OLS_LABEL, SUBSTR(NOIDUNG,1,40) AS NOIDUNG
FROM   THONGBAO ORDER BY MATB;

PROMPT >>> Xong. Dang nhap lai u1_giamdoc -> phai thay du 7 thong bao.
