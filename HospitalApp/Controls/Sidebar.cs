using System.Drawing.Drawing2D;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Sidebar dọc thay TabControl. Chứa nhiều SidebarItem, mỗi item có icon + text.
/// Item active có background highlight + accent bar bên trái.
/// </summary>
public class Sidebar : Panel
{
    public event Action<string>? ItemSelected;

    private readonly FlowLayoutPanel _items;
    private SidebarItem? _activeItem;
    private readonly List<SidebarItem> _itemList = new();

    public Color AccentColor { get; set; } = UiTheme.HealthCyanLight;

    public Sidebar()
    {
        Width = 240;
        Dock = DockStyle.Left;
        BackColor = UiTheme.SidebarBg;

        _items = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8, 16, 8, 16),
            BackColor = UiTheme.SidebarBg
        };
        Controls.Add(_items);
    }

    public void AddBrand(string title, string subtitle = "")
    {
        var p = new Panel
        {
            Width = Width - 16, Height = 70,
            BackColor = UiTheme.SidebarBg,
            Padding = new Padding(8, 6, 8, 6)
        };
        var lblT = new Label
        {
            Text = title, Dock = DockStyle.Top, Height = 28,
            Font = UiTheme.Heading3(12f), ForeColor = UiTheme.SidebarActive,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var lblS = new Label
        {
            Text = subtitle, Dock = DockStyle.Top, Height = 18,
            Font = UiTheme.Body(8.5f), ForeColor = UiTheme.SidebarTextDim,
            TextAlign = ContentAlignment.MiddleLeft
        };
        p.Controls.Add(lblS);
        p.Controls.Add(lblT);
        _items.Controls.Add(p);
        _items.Controls.Add(MakeSeparator());
    }

    public SidebarItem AddItem(string key, string glyph, string text)
    {
        var item = new SidebarItem(key, glyph, text)
        {
            Width = Width - 16,
            AccentColor = AccentColor
        };
        item.Click += (_, _) => SelectInternal(item);
        _items.Controls.Add(item);
        _itemList.Add(item);
        return item;
    }

    public void AddSection(string label)
    {
        var l = new Label
        {
            Text = label.ToUpper(),
            Width = Width - 16, Height = 24,
            Font = UiTheme.LabelBold(8f),
            ForeColor = UiTheme.SidebarTextDim,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 8, 4, 0)
        };
        _items.Controls.Add(l);
    }

    public void AddFiller()
    {
        var filler = new Panel { Width = Width - 16, Height = 1, BackColor = UiTheme.SidebarBg };
        _items.Controls.Add(filler);
    }

    public SidebarItem AddFooterItem(string key, string glyph, string text)
    {
        // Footer items dock bottom — push them to bottom via large filler approach
        return AddItem(key, glyph, text);
    }

    public void SelectByKey(string key)
    {
        var item = _itemList.FirstOrDefault(i => i.Key == key);
        if (item != null) SelectInternal(item);
    }

    private void SelectInternal(SidebarItem item)
    {
        if (_activeItem == item) return;
        if (_activeItem != null) _activeItem.IsActive = false;
        _activeItem = item;
        item.IsActive = true;
        ItemSelected?.Invoke(item.Key);
    }

    private static Panel MakeSeparator() => new()
    {
        Width = 220, Height = 1,
        BackColor = Color.FromArgb(66, 86, 116),
        Margin = new Padding(0, 6, 0, 10)
    };
}

/// <summary>
/// Item trong sidebar — icon + text + active indicator bar trái.
/// </summary>
public class SidebarItem : Control
{
    public string Key { get; }
    public string Glyph { get; set; }
    public Color AccentColor { get; set; } = UiTheme.HealthCyanLight;

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; Invalidate(); }
    }

    private bool _hover;

    public SidebarItem(string key, string glyph, string text)
    {
        Key = key;
        Glyph = glyph;
        Text = text;
        Height = 44;
        Cursor = Cursors.Hand;
        Font = UiTheme.Body(9.5f);
        ForeColor = UiTheme.SidebarText;
        BackColor = UiTheme.SidebarBg;
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.SupportsTransparentBackColor |
                 ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        Margin = new Padding(0, 2, 0, 2);
        MouseEnter += (_, _) => { _hover = true;  Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Background: subtle hover / active
        var rect = new Rectangle(2, 1, Width - 4, Height - 2);
        using var path = Card.RoundedRect(rect, 8);
        if (_isActive)
        {
            using var b = new SolidBrush(Color.FromArgb(40, AccentColor));
            g.FillPath(b, path);
        }
        else if (_hover)
        {
            using var b = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
            g.FillPath(b, path);
        }

        // Accent bar trái (chỉ khi active)
        if (_isActive)
        {
            var bar = new Rectangle(0, Height / 2 - 10, 3, 20);
            using var b = new SolidBrush(AccentColor);
            g.FillRectangle(b, bar);
        }

        // Icon
        using var iconFont = IconRegistry.Icon(13f);
        using var iconBrush = new SolidBrush(
            _isActive ? UiTheme.SidebarActive : UiTheme.SidebarText);
        g.DrawString(Glyph, iconFont, iconBrush, 14, (Height - iconFont.Height) / 2f);

        // Text
        using var textFont = _isActive ? UiTheme.BodyBold() : UiTheme.Body();
        using var textBrush = new SolidBrush(
            _isActive ? UiTheme.SidebarActive : UiTheme.SidebarText);
        var textRect = new RectangleF(40, 0, Math.Max(0, Width - 52), Height);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        g.DrawString(Text, textFont, textBrush, textRect, format);
    }
}
