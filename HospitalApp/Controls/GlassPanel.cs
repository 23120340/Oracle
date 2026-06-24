using System.Drawing.Drawing2D;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Panel mô phỏng glassmorphism cho WinForms (không có backdrop-filter native).
/// Vẽ: rounded rect + glass tint semi-transparent + border highlight + shadow nhiều lớp.
/// </summary>
public class GlassPanel : Panel
{
    public int  CornerRadius { get; set; } = 18;
    public int  ShadowDepth  { get; set; } = 14;
    public int  BorderWidth  { get; set; } = 1;
    public Color GlassTint   { get; set; } = Color.FromArgb(170, 255, 255, 255);
    public Color BorderTint  { get; set; } = Color.FromArgb(120, 255, 255, 255);
    public Color HighlightTint { get; set; } = Color.FromArgb(200, 255, 255, 255);

    public GlassPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = UiTheme.BgLight;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode      = SmoothingMode.AntiAlias;
        g.InterpolationMode  = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode    = PixelOffsetMode.HighQuality;

        var rect = new Rectangle(
            ShadowDepth, ShadowDepth,
            Width - ShadowDepth * 2, Height - ShadowDepth * 2);

        // 1) Shadow nhiều lớp — depth perception
        for (int i = ShadowDepth; i > 0; i--)
        {
            var alpha = Math.Max(2, (ShadowDepth - i + 1) * 4);
            using var shadow = new SolidBrush(Color.FromArgb(alpha, 0, 0, 0));
            var shadowRect = new Rectangle(
                rect.X - i / 2, rect.Y + i / 2,
                rect.Width + i, rect.Height + i);
            using var path = RoundedRect(shadowRect, CornerRadius + i);
            g.FillPath(shadow, path);
        }

        // 2) Glass body — semi-transparent tint
        using var body = RoundedRect(rect, CornerRadius);
        using (var brush = new SolidBrush(GlassTint))
            g.FillPath(brush, body);

        // 3) Top highlight — gradient overlay tạo cảm giác ánh sáng
        var hiRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height / 2);
        using (var hiBrush = new LinearGradientBrush(
            hiRect,
            Color.FromArgb(70, 255, 255, 255),
            Color.FromArgb(0,  255, 255, 255),
            LinearGradientMode.Vertical))
        {
            using var hiPath = RoundedRect(rect, CornerRadius);
            var prevClip = g.Clip;
            g.SetClip(hiPath);
            g.FillRectangle(hiBrush, hiRect);
            g.Clip = prevClip;
        }

        // 4) Border highlight
        using (var pen = new Pen(BorderTint, BorderWidth))
            g.DrawPath(pen, body);

        base.OnPaint(e);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(r.X,            r.Y,            d, d, 180, 90);
        path.AddArc(r.Right - d,    r.Y,            d, d, 270, 90);
        path.AddArc(r.Right - d,    r.Bottom - d,   d, d,   0, 90);
        path.AddArc(r.X,            r.Bottom - d,   d, d,  90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>
/// Background gradient + decorative blur orbs cho Login/Splash screens.
/// </summary>
public class GradientBackdrop : UserControl
{
    public Color ColorTop    { get; set; } = UiTheme.Primary;
    public Color ColorMiddle { get; set; } = Color.FromArgb(8, 145, 178);     // #0891B2
    public Color ColorBottom { get; set; } = Color.FromArgb(6,  78,  59);     // #064E3B
    public bool ShowOrbs     { get; set; } = true;

    public GradientBackdrop()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Dock = DockStyle.Fill;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 3-stop linear gradient bằng 2 brush nối tiếp
        var topHalf = new Rectangle(0, 0, Width, Height / 2);
        var botHalf = new Rectangle(0, Height / 2, Width, Height - Height / 2);
        using (var b1 = new LinearGradientBrush(topHalf, ColorTop,    ColorMiddle, LinearGradientMode.Vertical))
            g.FillRectangle(b1, topHalf);
        using (var b2 = new LinearGradientBrush(botHalf, ColorMiddle, ColorBottom, LinearGradientMode.Vertical))
            g.FillRectangle(b2, botHalf);

        if (!ShowOrbs) return;

        // Orb 1 — top-left
        DrawOrb(g, new Point(Width / 6, Height / 4),
                Math.Min(Width, Height) / 3,
                Color.FromArgb(50, 34, 211, 238));      // cyan-300 glow
        // Orb 2 — bottom-right
        DrawOrb(g, new Point(Width * 5 / 6, Height * 4 / 5),
                Math.Min(Width, Height) / 4,
                Color.FromArgb(45, 16, 185, 129));      // emerald-500 glow
        // Orb 3 — center-far (nhỏ)
        DrawOrb(g, new Point(Width / 2, Height / 8),
                Math.Min(Width, Height) / 6,
                Color.FromArgb(35, 255, 255, 255));
    }

    private static void DrawOrb(Graphics g, Point center, int radius, Color glow)
    {
        // Radial gradient orb dùng PathGradientBrush
        var rect = new Rectangle(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using var path = new GraphicsPath();
        path.AddEllipse(rect);
        using var brush = new PathGradientBrush(path)
        {
            CenterColor   = glow,
            SurroundColors = new[] { Color.FromArgb(0, glow) }
        };
        g.FillEllipse(brush, rect);
    }
}
