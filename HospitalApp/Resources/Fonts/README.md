# Montserrat Font

Thư mục này chứa font **Montserrat** được **nhúng** vào ứng dụng (embedded resource).

## Đã có sẵn

| File | Family | Style |
|------|--------|-------|
| `Montserrat-Regular.ttf` | Montserrat | Regular (400) |
| `Montserrat-Bold.ttf` | Montserrat | Bold (700) |

> Cả hai face đều hỗ trợ **đầy đủ tiếng Việt** (Latin Extended). `UiTheme` tự suy ra Italic/Medium
> bằng GDI+ khi cần. Bold dùng face thật (700).

## Cơ chế nạp

- [HospitalApp.csproj](../../HospitalApp.csproj) khai báo `<EmbeddedResource Include="Resources/Fonts/*.ttf" />`
  → mọi file `.ttf` ở đây được nhúng vào `HospitalApp.dll`.
- [Theme/UiTheme.cs](../../Theme/UiTheme.cs) đọc các resource `.ttf`, nạp vào `PrivateFontCollection`,
  chọn family **"Montserrat"**.
- [Program.cs](../../Program.cs) gọi `Application.SetDefaultFont(UiTheme.Body())` → Montserrat làm font mặc định toàn app.

## Thêm/đổi weight (tuỳ chọn)

Muốn dùng thêm Medium/SemiBold thật, tải từ <https://fonts.google.com/specimen/Montserrat>
và copy vào đây. **Lưu ý:** các file static `Montserrat-Medium.ttf`/`Montserrat-SemiBold.ttf`
mang family riêng ("Montserrat Medium"/"Montserrat SemiBold") — chỉ nên thêm nếu cập nhật
`UiTheme` cho khớp, tránh `FirstOrDefault` chọn nhầm family.

## Fallback

Nếu vì lý do nào đó không nạp được font ở đây, `UiTheme` sẽ:
1. Thử Montserrat đã cài trên hệ thống.
2. Cuối cùng fallback `SystemFonts.DefaultFont` (Segoe UI) — app vẫn chạy bình thường, tiếng Việt vẫn đúng.
