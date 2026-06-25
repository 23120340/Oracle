# PhanHe2/extras — File phụ (không bắt buộc để cài đặt)

Thư mục này chứa các script **phụ trợ**: demo và hotfix. **KHÔNG cần** chúng để dựng được CSDL Phân hệ 2 đúng — tập **chính** nằm ở `PhanHe2/` (`01`→`13` + `setup_all.sql`, `setup_admin_user.sql`, `run_migrations.ps1`).

Các file ở đây **chạy thủ công** khi cần — runner (`run_migrations.ps1`, `scripts/setup.ps1`) **không** còn tự gọi chúng nữa.

| File | Loại | Khi nào dùng |
|---|---|---|
| `recovery_demo.sql` | Demo | Trình diễn Flashback recovery (YC4) khi vấn đáp. Chạy tay: `@PhanHe2/extras/recovery_demo.sql`. |
| `fix_utf8_data.sql` | Fix | UPDATE lại dữ liệu Việt về giá trị chuẩn. **Dư thừa** nếu đã `SET NLS_LANG=.AL32UTF8` trước khi chạy `01` (cả 2 runner đều đã set). Chỉ cần khi dữ liệu mẫu bị mojibake. |
| `fix_fga_ora28138.sql` | Hotfix | Vá `ORA-28138` (FGA cần predicate đơn). Nội dung đã nằm trong `06_YC3_Audit.sql`; file này để áp nhanh không cần `-Reset`. |
| `fix_ols_thongbao.sql` | Hotfix | Gán lại nhãn OLS cho `THONGBAO` khi `u1–u8` không thấy thông báo. Chạy bằng CONNECT thật (không qua setup.ps1). |
| `fix_benhnhan_account.sql` | Hotfix | Cấp `CREATE SESSION ... WITH ADMIN OPTION` + viết lại `sp_create_benhnhan_full` (auto-MABN). Nội dung đã nằm trong `01`/`08`. |

> Triệu chứng → cách dùng từng hotfix: xem bảng "Khắc phục sự cố" trong `README.md` gốc.
