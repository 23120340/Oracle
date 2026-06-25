# Demo Script

## 1. AdminDashboard

Dang nhap `SYSTEM/oracle`, mo giao dien DBA.

- Tao user demo `TEST_USER`.
- Tao role demo, grant/revoke role cho user.
- Xem lai system privilege, object privilege va role privilege.

## 2. Dieu phoi vien

Dang nhap `DPV_NV001/BV@2025!`.

- Them benh nhan moi, app goi `sp_create_benhnhan_full` de tao ca Oracle account `BN_<MABN>`.
- Tao HSBA moi, ma HSBA sinh bang sequence qua `fn_next_mahsba`.
- Gan bac si dieu tri va gan KTV cho dich vu.
- Mo tab `Thong tin cua toi`, sua que quan/so dien thoai.
- Mo tab `Thong bao`, OLS tu loc thong bao theo nhan cua nhan vien.

## 3. Bac si

Dang nhap `BS_NV003/BV@2025!`.

- VPD tren `HSBA`: chi thay ho so co `MABS = NV003`.
- Cap nhat chan doan/dieu tri/ket luan, trigger ghi log `LOG_BS_HSBA`.
- Them dich vu chan doan, chon KTV bang dropdown.
- Them/sua don thuoc, FGA va trigger ghi vet.

## 4. Ky thuat vien

Dang nhap `KTV_NV006/BV@2025!`.

- View `KTV_HSBA_DV_View` chi tra ve dich vu co `MAKTV = NV006`.
- Cap nhat cot `KETQUA`.
- Thu cap nhat cot khac bang SQL truc tiep de thay trigger chan `ORA-20001`.

## 5. Benh nhan

Dang nhap `BN_BN001/BV@2025!`.

- Chi xem duoc thong tin cua minh.
- Sua dia chi/tien su benh hop le.
- Thu sua CCCD de thay trigger chan `ORA-20002`.

## 6. OLS rieng u1-u8

Dang nhap `u4_nvtk_hcm/U4@2025`.

- Mo `OLSViewerForm`.
- Xem nhan `NV:HCM:TK`.
- Kiem tra chi thay thong bao t1/TB001.

## 7. Audit

Dang nhap DBA va chay:

```sql
SELECT USERNAME, ACTION_NAME, OBJ_NAME, TIMESTAMP
FROM DBA_AUDIT_TRAIL
ORDER BY TIMESTAMP DESC FETCH FIRST 20 ROWS ONLY;

SELECT DB_USER, OBJECT_NAME, POLICY_NAME, SQL_TEXT, EXTENDED_TIMESTAMP
FROM DBA_FGA_AUDIT_TRAIL
ORDER BY EXTENDED_TIMESTAMP DESC FETCH FIRST 20 ROWS ONLY;
```

## 8. Recovery

Chay:

```sql
@PhanHe2/extras/recovery_demo.sql
```

Chup lai cac moc: count truoc khi xoa, audit/FGA, SCN checkpoint, count sau Flashback.
