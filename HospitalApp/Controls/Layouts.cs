using System.Drawing.Drawing2D;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

// ═══════════════════════════════════════════════════════════════════════════════
// KPI CARD — Số lớn + label + icon nhỏ
// ═══════════════════════════════════════════════════════════════════════════════
public class KpiCard : Card
{
    public string Glyph { get; set; } = IconRegistry.Chart;
    public Color  GlyphColor { get; set; } = UiTheme.HealthCyan;
    public string Label { get; set; } = "Label";
    public string Value { get; set; } = "0";
    public string Subtext { get; set; } = "";
    public Color  ValueColor { get; set; } = UiTheme.TextDark;

    public KpiCard()
    {
        Padding = new Padding(20, 14, 20, 14);
        ShowShadow = true;
        ShadowDepth = 4;
        CornerRadius = UiTheme.RadiusLg;
        BorderWidth = 0;
        Height = 118;          // tăng từ 100 → 118 để 3 dòng không đè nhau
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var pad = Padding;

        // 1) Icon top-right (pill nền nhạt)
        using var iconFont = IconRegistry.Icon(20f);
        var iconBg = new Rectangle(Width - 50, pad.Top, 34, 34);
        using (var b = new SolidBrush(Color.FromArgb(35, GlyphColor)))
        using (var p = Card.RoundedRect(iconBg, 10)) g.FillPath(b, p);
        using (var b = new SolidBrush(GlyphColor))
        {
            var sz = g.MeasureString(Glyph, iconFont);
            g.DrawString(Glyph, iconFont, b,
                iconBg.X + (iconBg.Width - sz.Width) / 2,
                iconBg.Y + (iconBg.Height - sz.Height) / 2);
        }

        // 2) Label (top) — không đè icon: max width = card width - 60 (icon zone)
        var labelMaxW = Width - pad.Left - 60;
        using var labelFont = UiTheme.LabelBold(9.5f);
        using var labelBrush = new SolidBrush(UiTheme.TextMuted);
        g.DrawString(Label, labelFont, labelBrush,
            new RectangleF(pad.Left, pad.Top, labelMaxW, 18));

        // 3) Value (số to) — y cố định từ top, font 22pt (giảm từ 26pt)
        using var valueFont = UiTheme.Heading1(22f);
        using var valueBrush = new SolidBrush(ValueColor);
        g.DrawString(Value, valueFont, valueBrush, pad.Left, pad.Top + 24);

        // 4) Subtext — y cố định, đảm bảo cách value 1 dòng
        if (!string.IsNullOrEmpty(Subtext))
        {
            using var subFont = UiTheme.Body(8.5f);
            using var subBrush = new SolidBrush(UiTheme.TextMuted);
            g.DrawString(Subtext, subFont, subBrush, pad.Left, pad.Top + 68);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// STATUS BAR — Footer mọi form: connection + role + clock + extra
// ═══════════════════════════════════════════════════════════════════════════════
public class StatusBar : Panel
{
    private readonly Label _lblLeft, _lblCenter, _lblRight;
    private readonly System.Windows.Forms.Timer _clock;

    public string LeftText
    {
        get => _lblLeft.Text;
        set => _lblLeft.Text = value;
    }

    public string CenterText
    {
        get => _lblCenter.Text;
        set => _lblCenter.Text = value;
    }

    public StatusBar()
    {
        Dock = DockStyle.Bottom;
        Height = 30;
        BackColor = UiTheme.BgLight;
        Padding = new Padding(16, 6, 16, 6);

        _lblLeft = new Label
        {
            Dock = DockStyle.Left, Width = 350,
            Font = UiTheme.Body(8.5f), ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        _lblCenter = new Label
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Body(8.5f), ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true
        };
        _lblRight = new Label
        {
            Dock = DockStyle.Right, Width = 120,
            Font = UiTheme.Body(8.5f), ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleRight,
            AutoEllipsis = true
        };
        Controls.Add(_lblCenter);
        Controls.Add(_lblRight);
        Controls.Add(_lblLeft);

        _clock = new System.Windows.Forms.Timer { Interval = 1000 };
        _clock.Tick += (_, _) =>
        {
            _lblRight.Text = $"{IconRegistry.Clock}  {DateTime.Now:HH:mm:ss}";
        };
        _clock.Start();
        _lblRight.Text = $"{IconRegistry.Clock}  {DateTime.Now:HH:mm:ss}";
        _lblRight.Font = IconRegistry.Icon(9f);
        Resize += (_, _) => LayoutLabels();
        LayoutLabels();
    }

    private void LayoutLabels()
    {
        var available = Math.Max(0, Width - Padding.Horizontal);
        _lblRight.Width = available < 520 ? 86 : 120;
        _lblLeft.Width = available < 520 ? Math.Max(120, available / 2) : Math.Min(350, Math.Max(180, available / 3));
        _lblCenter.Visible = available >= 420;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        // Top border 1px
        using var pen = new Pen(UiTheme.Border, 1);
        e.Graphics.DrawLine(pen, 0, 0, Width, 0);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _clock?.Dispose();
        base.Dispose(disposing);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// EMPTY STATE — Hiển thị khi grid/list không có data
// ═══════════════════════════════════════════════════════════════════════════════
public class EmptyState : Panel
{
    public string Glyph    { get; set; } = IconRegistry.Folder;
    public string Title    { get; set; } = "Chưa có dữ liệu";
    public string Subtitle { get; set; } = "";
    public string CtaText  { get; set; } = "";
    public Action? OnCta   { get; set; }

    private Button? _btnCta;

    public EmptyState()
    {
        BackColor = UiTheme.BgLight;
        Dock = DockStyle.Fill;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
    }

    public void Build()
    {
        Controls.Clear();
        if (!string.IsNullOrEmpty(CtaText))
        {
            _btnCta = new RoundedButton
            {
                Text = CtaText,
                Width = 180, Height = 38,
                BackColor = UiTheme.HealthCyan
            };
            _btnCta.Click += (_, _) => OnCta?.Invoke();
            Controls.Add(_btnCta);
        }
        DoLayout();
        Resize += (_, _) => DoLayout();
    }

    private void DoLayout()
    {
        if (_btnCta != null)
        {
            _btnCta.Location = new Point(
                (Width - _btnCta.Width) / 2,
                Height / 2 + 30);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

        using var iconFont = IconRegistry.Icon(48f);
        var iconSz = g.MeasureString(Glyph, iconFont);
        using (var b = new SolidBrush(UiTheme.TextMuted))
            g.DrawString(Glyph, iconFont, b,
                (Width - iconSz.Width) / 2,
                Height / 2 - 100);

        using var titleFont = UiTheme.Heading3();
        using var titleBrush = new SolidBrush(UiTheme.TextDark);
        var titleSz = g.MeasureString(Title, titleFont);
        g.DrawString(Title, titleFont, titleBrush,
            (Width - titleSz.Width) / 2,
            Height / 2 - 40);

        if (!string.IsNullOrEmpty(Subtitle))
        {
            using var subFont = UiTheme.Body(9.5f);
            using var subBrush = new SolidBrush(UiTheme.TextMuted);
            var subSz = g.MeasureString(Subtitle, subFont);
            g.DrawString(Subtitle, subFont, subBrush,
                (Width - subSz.Width) / 2,
                Height / 2 - 10);
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DRAWER — Slide-in panel từ phải, semi-modal
// ═══════════════════════════════════════════════════════════════════════════════
public class Drawer : Panel
{
    public int  DrawerWidth { get; set; } = 480;
    public Action? OnClose   { get; set; }

    private readonly Panel _overlay;
    private readonly Panel _body;
    public Panel Content { get; }

    public Drawer(Form host)
    {
        Dock = DockStyle.Fill;
        BackColor = UiTheme.BgLight;
        Visible = false;

        _overlay = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(78, 87, 104)
        };
        _overlay.Click += (_, _) => Close();

        _body = new Panel
        {
            Width = DrawerWidth,
            Dock = DockStyle.Right,
            BackColor = UiTheme.Surface,
            Padding = new Padding(24)
        };

        Content = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        _body.Controls.Add(Content);

        Controls.Add(_body);
        Controls.Add(_overlay);
        _overlay.SendToBack();
        _body.BringToFront();
    }

    public void Open()
    {
        Visible = true;
        BringToFront();
        _body.Left = Parent!.Width;
        // Slide-in animation
        Animator.SlideLeft(_body, Parent.Width - DrawerWidth, 220);
    }

    public void Close()
    {
        if (Parent == null) { Visible = false; return; }
        Animator.SlideLeft(_body, Parent.Width, 180, () =>
        {
            Visible = false;
            OnClose?.Invoke();
        });
    }
}
