using System.Drawing.Drawing2D;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// TextBox với background bo tròn + border subtle, hợp glass style.
/// Bọc TextBox gốc trong Panel custom paint.
/// </summary>
public class GlassTextBox : Panel
{
    public TextBox Inner { get; }
    public int  CornerRadius { get; set; } = 10;
    public Color FieldBg     { get; set; } = Color.FromArgb(250, 252, 254);
    public Color FieldBorder { get; set; } = Color.FromArgb(220, 225, 232);
    public Color FieldFocus  { get; set; } = UiTheme.Primary;

    private bool _focused;

    public string Placeholder
    {
        get => Inner.PlaceholderText;
        set => Inner.PlaceholderText = value;
    }

    public char PasswordChar
    {
        get => Inner.PasswordChar;
        set => Inner.PasswordChar = value;
    }

    public new string Text { get => Inner.Text; set => Inner.Text = value; }

    public GlassTextBox()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        BackColor = FieldBg;
        Height = 44;
        Padding = new Padding(14, 12, 14, 12);

        Inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = UiTheme.Body(11f),
            ForeColor = UiTheme.TextDark,
            BackColor = FieldBg,
            Dock = DockStyle.Fill
        };
        Inner.GotFocus  += (_, _) => { _focused = true;  Invalidate(); };
        Inner.LostFocus += (_, _) => { _focused = false; Invalidate(); };
        Controls.Add(Inner);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(rect, CornerRadius);

        // Background
        using (var brush = new SolidBrush(FieldBg))
            g.FillPath(brush, path);

        // Focus ring: pen dày hơn + màu primary, ngược lại border nhạt
        var penColor = _focused ? FieldFocus : FieldBorder;
        var penWidth = _focused ? 2f         : 1f;
        using (var pen = new Pen(penColor, penWidth))
            g.DrawPath(pen, path);
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
