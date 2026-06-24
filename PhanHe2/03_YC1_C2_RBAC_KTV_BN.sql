-- ============================================================
-- PHÂN HỆ 2 - File 03: Yêu cầu 1, Câu 2 - RBAC cho KTV và Bệnh nhân
-- ============================================================
-- TC#4 (Kỹ thuật viên):
--   - SELECT trên HSBA_DV chỉ những dòng do mình thực hiện (MAKTV = MANV của mình)
--   - UPDATE cột KETQUA trên những dòng đó
--   - Mọi UPDATE KETQUA đều được ghi vết
-- TC#5 (Bệnh nhân):
--   - SELECT chỉ thông tin của chính mình trên BENHNHAN
--   - UPDATE các trường được phép (trừ MABN, TENBN, PHAI, NGAYSINH, CCCD, ORACLE_USER)
-- ============================================================
-- Cách tiếp cận RBAC thuần:
--   - View lọc dòng dựa theo ORACLE_USER (không cần VPD)
--   - INSTEAD OF trigger kiểm soát cột UPDATE
--   - Role chỉ GRANT trên view, không phải bảng gốc
-- ============================================================
-- Chạy với BVADMIN sau khi chạy 01 và 02
-- ============================================================

CONNECT BVADMIN/"BVAdmin@2025";

-- ============================================================
-- PHẦN A: ROLE VÀ VIEW CHO KỸ THUẬT VIÊN (TC#4)
-- ============================================================

-- A1. View lọc: KTV chỉ thấy HSBA_DV do mình được giao (MAKTV = MANV của session)
CREATE OR REPLACE VIEW KTV_HSBA_DV_View AS
SELECT
    h.MAHSBA,
    h.LOAIDV,
    h.NGAYDV,
    h.MAKTV,
    h.KETQUA
FROM HSBA_DV h
WHERE h.MAKTV = BVADMIN.fn_get_manv();
-- fn_get_manv() tra bảng NHANVIEN với ORACLE_USER = SESSION_USER → 1 bảng (TC#1)

-- A2. INSTEAD OF TRIGGER: chỉ cho phép UPDATE cột KETQUA
--     Chặn mọi thay đổi trên các cột khác
CREATE OR REPLACE TRIGGER trg_ktv_update_ketqua
INSTEAD OF UPDATE ON KTV_HSBA_DV_View
FOR EACH ROW
DECLARE
    v_manv NHANVIEN.MANV%TYPE := BVADMIN.fn_get_manv();
BEGIN
    -- Kiểm tra: KTV chỉ được update KETQUA
    -- So sánh NULL-safe cho cột có thể NULL (MAKTV); MAHSBA/LOAIDV/NGAYDV thuộc PK nên NOT NULL
    IF :NEW.MAHSBA  != :OLD.MAHSBA
    OR :NEW.LOAIDV  != :OLD.LOAIDV
    OR :NEW.NGAYDV  != :OLD.NGAYDV
    OR NVL(:NEW.MAKTV, '∅') != NVL(:OLD.MAKTV, '∅')
    THEN
        RAISE_APPLICATION_ERROR(-20001,
            N'KTV chỉ được cập nhật cột KETQUA.');
    END IF;

    -- Thực hiện UPDATE thực sự trên bảng gốc (chỉ KETQUA)
    UPDATE HSBA_DV
    SET    KETQUA = :NEW.KETQUA
    WHERE  MAHSBA = :OLD.MAHSBA
      AND  LOAIDV = :OLD.LOAIDV
      AND  NGAYDV = :OLD.NGAYDV
      AND  MAKTV  = v_manv;  -- đảm bảo chỉ update dòng của mình
END trg_ktv_update_ketqua;
/

-- A3. Bảng nhật ký ghi vết UPDATE KETQUA (TC#4 + Yêu cầu 3)
CREATE TABLE LOG_KTV_KETQUA (
    LOG_ID     NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MAHSBA     VARCHAR2(10),
    LOAIDV     NVARCHAR2(100),
    NGAYDV     DATE,
    MAKTV      VARCHAR2(10),
    OLD_KETQUA NCLOB,
    NEW_KETQUA NCLOB,
    CHANGED_BY VARCHAR2(100),
    CHANGED_AT TIMESTAMP DEFAULT SYSTIMESTAMP
);

-- Trigger ghi vết UPDATE KETQUA trực tiếp trên bảng gốc
CREATE OR REPLACE TRIGGER trg_log_ketqua
AFTER UPDATE OF KETQUA ON HSBA_DV
FOR EACH ROW
BEGIN
    -- FIX (L7): chỉ ghi vết khi KETQUA THỰC SỰ đổi (KETQUA nay là NVARCHAR2 → so sánh NULL-safe được)
    IF NVL(:NEW.KETQUA, N'<<NULL>>') != NVL(:OLD.KETQUA, N'<<NULL>>') THEN
        INSERT INTO LOG_KTV_KETQUA
            (MAHSBA, LOAIDV, NGAYDV, MAKTV, OLD_KETQUA, NEW_KETQUA, CHANGED_BY)
        VALUES
            (:OLD.MAHSBA, :OLD.LOAIDV, :OLD.NGAYDV, :OLD.MAKTV,
             :OLD.KETQUA, :NEW.KETQUA,
             SYS_CONTEXT('USERENV','SESSION_USER'));
    END IF;
END trg_log_ketqua;
/

-- A4. Tạo role KTV_Role và cấp quyền chỉ trên view
CONNECT SYSTEM/oracle;
-- FIX (L11): tạo role idempotent (bỏ qua nếu đã tồn tại — ORA-01921) để chạy lại an toàn
BEGIN EXECUTE IMMEDIATE 'CREATE ROLE KTV_Role'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1921 THEN RAISE; END IF; END;
/
GRANT SELECT ON BVADMIN.KTV_HSBA_DV_View TO KTV_Role;
-- GRANT UPDATE: INSTEAD OF trigger sẽ xử lý giới hạn cột (chỉ KETQUA)
GRANT UPDATE ON BVADMIN.KTV_HSBA_DV_View TO KTV_Role;

-- Gán role cho các KTV
GRANT KTV_Role TO KTV_NV006;
GRANT KTV_Role TO KTV_NV007;

-- FIX (B6): KTV cần quyền CREATE SYNONYM để tự tạo synonym (mặc định chỉ có CREATE SESSION)
GRANT CREATE SYNONYM TO KTV_NV006;
GRANT CREATE SYNONYM TO KTV_NV007;

-- Tạo synonym cho KTV dễ truy cập (không cần tiền tố BVADMIN)
CONNECT KTV_NV006/"BV@2025!";
CREATE OR REPLACE SYNONYM MY_HSBA_DV FOR BVADMIN.KTV_HSBA_DV_View;

-- ============================================================
-- PHẦN B: ROLE VÀ VIEW CHO BỆNH NHÂN (TC#5)
-- ============================================================
CONNECT BVADMIN/"BVAdmin@2025";

-- B1. View lọc: BN chỉ thấy dòng của chính mình trong BENHNHAN
CREATE OR REPLACE VIEW BN_BENHNHAN_View AS
SELECT
    MABN, TENBN, PHAI, NGAYSINH, CCCD,
    SONHA, TENDUONG, QUANHUYEN, TINHTP,
    TIENSUBENH, TIENSUBENHGD, DIUNGTHUOC
    -- Không expose ORACLE_USER (nội bộ hệ thống)
FROM BENHNHAN
WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');

-- B2. INSTEAD OF TRIGGER: BN chỉ được UPDATE các trường cho phép
--     Không được UPDATE: MABN, TENBN, PHAI, NGAYSINH, CCCD
CREATE OR REPLACE TRIGGER trg_bn_update_benhnhan
INSTEAD OF UPDATE ON BN_BENHNHAN_View
FOR EACH ROW
BEGIN
    -- Kiểm tra: không được đổi các trường định danh và cố định
    -- So sánh NULL-safe cho cột có thể NULL (PHAI, NGAYSINH) để không bỏ sót thay đổi từ/ sang NULL
    IF :NEW.MABN     != :OLD.MABN
    OR :NEW.TENBN    != :OLD.TENBN
    OR NVL(:NEW.PHAI, '∅') != NVL(:OLD.PHAI, '∅')
    OR NVL(:NEW.NGAYSINH, DATE'0001-01-01') != NVL(:OLD.NGAYSINH, DATE'0001-01-01')
    OR :NEW.CCCD     != :OLD.CCCD
    THEN
        RAISE_APPLICATION_ERROR(-20002,
            N'Không được phép thay đổi MABN, TÊNBN, PHÁI, NGÀYSINH, CCCD.');
    END IF;

    -- UPDATE chỉ các trường được phép (địa chỉ + tiền sử bệnh)
    UPDATE BENHNHAN
    SET
        SONHA        = :NEW.SONHA,
        TENDUONG     = :NEW.TENDUONG,
        QUANHUYEN    = :NEW.QUANHUYEN,
        TINHTP       = :NEW.TINHTP,
        TIENSUBENH   = :NEW.TIENSUBENH,
        TIENSUBENHGD = :NEW.TIENSUBENHGD,
        DIUNGTHUOC   = :NEW.DIUNGTHUOC
    WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER');
END trg_bn_update_benhnhan;
/

-- B3. View để BN xem lịch sử hồ sơ bệnh án của mình (chỉ SELECT)
--     BN không thấy CHANDOAN/DIEUTRI chi tiết - chỉ thấy trạng thái
CREATE OR REPLACE VIEW BN_HSBA_View AS
SELECT
    h.MAHSBA,
    h.NGAY,
    h.MAKHOA,
    h.KETLUAN
    -- CHANDOAN, DIEUTRI ẩn đi (thông tin nhạy cảm của bác sĩ)
FROM HSBA h
-- FIX (RBAC-6): lọc 1 bảng, bám TC#1 "không join >1 bảng"
WHERE h.MABN = BVADMIN.fn_get_mabn();

-- B4. Tạo role BenhNhan_Role và cấp quyền chỉ trên view
CONNECT SYSTEM/oracle;
-- FIX (L11): tạo role idempotent
BEGIN EXECUTE IMMEDIATE 'CREATE ROLE BenhNhan_Role'; EXCEPTION WHEN OTHERS THEN IF SQLCODE != -1921 THEN RAISE; END IF; END;
/
GRANT SELECT ON BVADMIN.BN_BENHNHAN_View TO BenhNhan_Role;
-- GRANT UPDATE: INSTEAD OF trigger sẽ xử lý giới hạn cột được sửa
GRANT UPDATE ON BVADMIN.BN_BENHNHAN_View TO BenhNhan_Role;
GRANT SELECT ON BVADMIN.BN_HSBA_View     TO BenhNhan_Role;

-- Gán role cho bệnh nhân
GRANT BenhNhan_Role TO BN_BN001;
GRANT BenhNhan_Role TO BN_BN002;
GRANT BenhNhan_Role TO BN_BN003;

-- ============================================================
-- PHẦN C: KIỂM THỬ
-- ============================================================
-- Một số câu dưới đây CỐ Ý gây lỗi (ORA-20001/20002/01031) để minh hoạ chặn quyền.
-- Đặt CONTINUE để khi chạy tự động (run_migrations) các lỗi minh hoạ không làm dừng script.
WHENEVER SQLERROR CONTINUE

-- Test KTV_NV006 (kỹ thuật viên):
CONNECT KTV_NV006/"BV@2025!";
SELECT * FROM SESSION_ROLES;
-- Kết quả: KTV_ROLE

-- Test SELECT - chỉ thấy dòng của NV006 (MAKTV='NV006')
SELECT * FROM BVADMIN.KTV_HSBA_DV_View;

-- Test UPDATE KETQUA - HỢP LỆ
UPDATE BVADMIN.KTV_HSBA_DV_View
SET    KETQUA = N'Glucose: 13.2 mmol/L (cập nhật bổ sung)'
WHERE  MAHSBA = 'HS001' AND LOAIDV = N'Xét nghiệm máu tổng quát';
COMMIT;

-- Test UPDATE cột khác - BỊ CHẶN bởi trigger (ORA-20001)
UPDATE BVADMIN.KTV_HSBA_DV_View
SET    MAKTV = 'NV007'
WHERE  MAHSBA = 'HS001';

-- Test BN_BN001 (bệnh nhân):
CONNECT BN_BN001/"BV@2025!";
SELECT * FROM SESSION_ROLES;
-- Kết quả: BENHNHAN_ROLE

-- Test SELECT - chỉ thấy 1 dòng của BN001
SELECT MABN, TENBN, TINHTP FROM BVADMIN.BN_BENHNHAN_View;

-- Test UPDATE địa chỉ - HỢP LỆ
UPDATE BVADMIN.BN_BENHNHAN_View
SET    SONHA = '14', TENDUONG = N'Nguyễn Huệ'
WHERE  MABN = 'BN001';
COMMIT;

-- Test UPDATE TENBN - BỊ CHẶN (ORA-20002)
UPDATE BVADMIN.BN_BENHNHAN_View SET TENBN = N'Tên khác' WHERE MABN = 'BN001';

-- Test SELECT bảng gốc - BỊ TỪ CHỐI (ORA-01031)
SELECT * FROM BVADMIN.BENHNHAN;

-- Xem log ghi vết KTV (chạy với BVADMIN)
CONNECT BVADMIN/"BVAdmin@2025";
SELECT MAHSBA, LOAIDV, MAKTV, OLD_KETQUA, NEW_KETQUA, CHANGED_BY, CHANGED_AT
FROM   LOG_KTV_KETQUA
ORDER  BY CHANGED_AT DESC;
