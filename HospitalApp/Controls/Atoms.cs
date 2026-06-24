using System.Drawing.Drawing2D;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

// ═══════════════════════════════════════════════════════════════════════════════
// CARD — Container bo tròn + shadow nhẹ, dùng thay Panel cho content blocks
// ═══════════════════════════════════════════════════════════════════════════════
public class Card : Panel
{
    public int  CornerRadius { get; set; } = UiTheme.RadiusMd;   // 10 — bo nhẹ, minimalist
    public int  ShadowDepth  { get; set; } = 3;                  // shadow rất nhẹ (ưu tiên viền hairline)
    public Color FillColor   { get; set; } = UiTheme.Surface;
    public Color BorderColor { get; set; } = UiTheme.Border;
    public int  BorderWidth  { get; set; } = 1;
    public bool ShowShadow   { get; set; } = false;             // mặc định phẳng; bật khi cần nổi khối

    public Card()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = UiTheme.BgLight;
        Padding = new Padding(UiTheme.Spacing4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(ShadowDepth, ShadowDepth,
            Width - ShadowDepth * 2, Height - ShadowDepth * 2);

        if (ShowShadow)
        {
            for (int i = ShadowDepth; i > 0; i--)
            {
                var alpha = (ShadowDepth - i + 1) * 2;
                using var sh = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
                using var p = RoundedRect(new Rectangle(
                    rect.X - i / 2, rect.Y + i / 2,
                    rect.Width + i, rect.Height + i), CornerRadius + i);
                g.FillPath(sh, p);
            }
        }

        using var body = RoundedRect(rect, CornerRadius);
        using (var fill = new SolidBrush(FillColor)) g.FillPath(fill, body);
        if (BorderWidth > 0)
            using (var pen = new Pen(BorderColor, BorderWidth)) g.DrawPath(pen, body);

        base.OnPaint(e);
    }

    internal static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X,            r.Y,            d, d, 180, 90);
        path.AddArc(r.Right - d,    r.Y,            d, d, 270, 90);
        path.AddArc(r.Right - d,    r.Bottom - d,   d, d,   0, 90);
        path.AddArc(r.X,            r.Bottom - d,   d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ROUNDED BUTTON — Button bo tròn + glyph icon (Segoe Fluent)
// ═══════════════════════════════════════════════════════════════════════════════
public class RoundedButton : Button
{
    public int    CornerRadius { get; set; } = UiTheme.RadiusMd;
    public string Glyph        { get; set; } = "";
    public Color  GlyphColor   { get; set; } = Color.White;
    public Color  HoverColor   { get; set; } = Color.Empty;
    public int    BorderThickness { get; set; } = 0;            // 0 = không viền
    public Color  BorderTint      { get; set; } = UiTheme.Border;

    private bool _hover;

    public RoundedButton()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = UiTheme.Button();
        ForeColor = Color.White;
        BackColor = UiTheme.Primary;
        Cursor = Cursors.Hand;
        Height = 36;
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        // Clip 4 góc bằng Region → tránh parent BackColor "rò" qua corners
        Resize += (_, _) => ApplyRegion();
    }

    private void ApplyRegion()
    {
        if (Width <= 0 || Height <= 0) return;
        using var path = Card.RoundedRect(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyRegion();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Card.RoundedRect(rect, CornerRadius);

        var bg = _hover && Enabled
            ? (HoverColor == Color.Empty ? Darken(BackColor, 0.12f) : HoverColor)
            : BackColor;
        if (!Enabled) bg = Color.FromArgb(120, bg);

        using (var fill = new SolidBrush(bg)) g.FillPath(fill, path);

        // Viền sạch (AA), vẽ lùi vào trong để không bị Region cắt mất → mép gọn
        if (BorderThickness > 0)
        {
            var ins = BorderThickness;
            var br = new Rectangle(ins, ins, Width - 1 - ins * 2, Height - 1 - ins * 2);
            if (br.Width > 0 && br.Height > 0)
            {
                using var bpen = new Pen(BorderTint, BorderThickness);
                using var bpath = Card.RoundedRect(br, Math.Max(1, CornerRadius - ins));
                g.DrawPath(bpen, bpath);
            }
        }

        // Draw glyph + text
        var contentRect = ClientRectangle;
        if (!string.IsNullOrEmpty(Glyph))
        {
            var glyphSize = Font.Size + 2;
            using var iconFont = IconRegistry.Icon(glyphSize);
            var glyphWidth = g.MeasureString(Glyph, iconFont).Width;
            var textWidth  = g.MeasureString(Text, Font).Width;
            var totalWidth = glyphWidth + 6 + textWidth;
            var startX = (Width - totalWidth) / 2;

            using var glyphBrush = new SolidBrush(GlyphColor);
            g.DrawString(Glyph, iconFont, glyphBrush,
                startX, (Height - iconFont.Height) / 2f - 1);

            using var textBrush = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, textBrush,
                startX + glyphWidth + 6, (Height - Font.Height) / 2f);
        }
        else
        {
            using var brush = new SolidBrush(ForeColor);
            var sz = g.MeasureString(Text, Font);
            g.DrawString(Text, Font, brush,
                (Width - sz.Width) / 2, (Height - sz.Height) / 2);
        }
    }

    private static Color Darken(Color c, float amount)
        => Color.FromArgb(c.A,
            (int)(c.R * (1 - amount)),
            (int)(c.G * (1 - amount)),
            (int)(c.B * (1 - amount)));
}

// ═══════════════════════════════════════════════════════════════════════════════
// STATUS PILL — Small pill (dot + text)
// ═══════════════════════════════════════════════════════════════════════════════
public class StatusPill : Control
{
    public Color DotColor   { get; set; } = UiTheme.StatusSuccess;
    public Color FillColor  { get; set; } = Color.Empty;

    public StatusPill()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        ForeColor = UiTheme.TextDark;
        Font = UiTheme.Body(8.5f);
        Height = 22;
        Padding = new Padding(8, 2, 10, 2);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        var fill = FillColor == Color.Empty
            ? Color.FromArgb(20, DotColor.R, DotColor.G, DotColor.B)
            : FillColor;
        using var path = Card.RoundedRect(rect, Height / 2);
        using (var b = new SolidBrush(fill)) g.FillPath(b, path);

        // Dot
        var dotRect = new Rectangle(8, (Height - 8) / 2, 8, 8);
        using (var b = new SolidBrush(DotColor)) g.FillEllipse(b, dotRect);

        // Text
        using var textBrush = new SolidBrush(ForeColor);
        g.DrawString(Text, Font, textBrush,
            dotRect.Right + 5, (Height - Font.Height) / 2f);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        using var g = CreateGraphics();
        var w = (int)g.MeasureString(Text, Font).Width + 30;
        Width = w;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ROLE CHIP — Badge to hơn StatusPill, dùng cho role indicator trên header
// ═══════════════════════════════════════════════════════════════════════════════
public class RoleChip : Control
{
    public Color AccentColor { get; set; } = UiTheme.Primary;

    public RoleChip()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        Font = UiTheme.LabelBold(9f);
        ForeColor = Color.White;
        Height = 26;
        Padding = new Padding(12, 4, 12, 4);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = Card.RoundedRect(rect, Height / 2);
        using (var b = new SolidBrush(AccentColor)) g.FillPath(b, path);
        using var textBrush = new SolidBrush(ForeColor);
        var sz = g.MeasureString(Text, Font);
        g.DrawString(Text, Font, textBrush,
            (Width - sz.Width) / 2, (Height - sz.Height) / 2);
    }

    protected override void OnTextChanged(EventArgs e)
    {
        base.OnTextChanged(e);
        using var g = CreateGraphics();
        var w = (int)g.MeasureString(Text, Font).Width + 28;
        Width = Math.Max(60, w);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// AVATAR — Circle với initials + gradient bg
// ═══════════════════════════════════════════════════════════════════════════════
public class Avatar : Control
{
    public Color ColorStart { get; set; } = UiTheme.Primary;
    public Color ColorEnd   { get; set; } = UiTheme.HealthCyan;

    public Avatar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        ForeColor = Color.White;
        Size = new Size(40, 40);
        Font = UiTheme.LabelBold(11f);
    }

    public void SetName(string fullName)
    {
        Text = InitialsOf(fullName);
        // Derive color from name hash for visual distinction
        var h = (uint)fullName.GetHashCode();
        var hue = (h % 360) / 360f;
        ColorStart = FromHsl(hue, 0.55f, 0.45f);
        ColorEnd   = FromHsl((hue + 0.08f) % 1f, 0.6f, 0.55f);
        Invalidate();
    }

    private static string InitialsOf(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return parts[0][..1].ToUpper();
        return (parts[^2][..1] + parts[^1][..1]).ToUpper();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(1, 1, Width - 2, Height - 2);
        using (var brush = new LinearGradientBrush(rect, ColorStart, ColorEnd, 45f))
            g.FillEllipse(brush, rect);
        using var textBrush = new SolidBrush(ForeColor);
        var sz = g.MeasureString(Text, Font);
        g.DrawString(Text, Font, textBrush,
            (Width - sz.Width) / 2, (Height - sz.Height) / 2 - 1);
    }

    private static Color FromHsl(float h, float s, float l)
    {
        float c = (1 - Math.Abs(2 * l - 1)) * s;
        float x = c * (1 - Math.Abs((h * 6) % 2 - 1));
        float m = l - c / 2;
        float r1=0, g1=0, b1=0;
        if      (h < 1f/6) { r1 = c; g1 = x; }
        else if (h < 2f/6) { r1 = x; g1 = c; }
        else if (h < 3f/6) { g1 = c; b1 = x; }
        else if (h < 4f/6) { g1 = x; b1 = c; }
        else if (h < 5f/6) { r1 = x; b1 = c; }
        else               { r1 = c; b1 = x; }
        return Color.FromArgb(
            (int)((r1 + m) * 255),
            (int)((g1 + m) * 255),
            (int)((b1 + m) * 255));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ICON LABEL — Label hiển thị glyph Segoe Fluent + optional text
// ═══════════════════════════════════════════════════════════════════════════════
public class IconLabel : Label
{
    public string Glyph { get; set; } = "";
    public float  GlyphSize { get; set; } = 14f;

    public IconLabel()
    {
        AutoSize = false;
        TextAlign = ContentAlignment.MiddleLeft;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        if (!string.IsNullOrEmpty(Glyph))
        {
            using var iconFont = IconRegistry.Icon(GlyphSize);
            using var brush = new SolidBrush(ForeColor);
            g.DrawString(Glyph, iconFont, brush, 0, (Height - iconFont.Height) / 2f);
            var glyphW = g.MeasureString(Glyph, iconFont).Width;
            using var textBrush = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, textBrush, glyphW + 6, (Height - Font.Height) / 2f);
        }
        else
        {
            base.OnPaint(e);
        }
    }
}
