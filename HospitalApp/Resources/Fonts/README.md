# Montserrat Font

Đặt các file `.ttf` Montserrat vào thư mục này. App sẽ tự động embed và load.

## Cách lấy font

1. Vào https://fonts.google.com/specimen/Montserrat
2. Click **"Get font"** → **"Download all"**
3. Giải nén, copy các file sau vào thư mục này:
   - `Montserrat-Regular.ttf`
   - `Montserrat-Medium.ttf`
   - `Montserrat-SemiBold.ttf`
   - `Montserrat-Bold.ttf`
   - `Montserrat-Italic.ttf` (tùy chọn)

## Fallback

Nếu không có font ở đây, [UiTheme.cs](../../Theme/UiTheme.cs) sẽ:

1. Thử tìm Montserrat đã cài trên hệ thống
2. Cuối cùng fallback về `SystemFonts.DefaultFont` (Segoe UI)

→ Ứng dụng vẫn build & chạy được kể cả khi quên copy font, chỉ là không có Montserrat.

## Vì sao embed?

- Build distributable không phụ thuộc máy người dùng đã cài Montserrat hay chưa
- Đồng bộ giao diện qua mọi máy chấm đồ án
- Font đi kèm assembly trong file `HospitalApp.exe` duy nhất
