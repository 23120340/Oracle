using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HospitalApp.Theme;

/// <summary>
/// Hệ thống design tokens dùng chung: font Montserrat (embedded) + palette màu.
/// Mọi UI mới phải gọi qua đây thay vì hard-code Font/Color.
/// </summary>
public static class UiTheme
{
    private static readonly PrivateFontCollection _fonts = new();
    public  static FontFamily Family { get; }
    public  static bool FontLoaded   { get; }

    // ═══════════════════════════════════════════════════════════════════════════
    // PALETTE
    // ═══════════════════════════════════════════════════════════════════════════
    public static readonly Color Primary      = Color.FromArgb( 30,  90, 160);
    public static readonly Color PrimaryDark  = Color.FromArgb( 20,  60, 110);
    public static readonly Color Accent       = Color.FromArgb(  0, 140, 100);
    public static readonly Color Warning      = Color.FromArgb(220, 140,   0);
    public static readonly Color Danger       = Color.FromArgb(200,  50,  50);
    public static readonly Color BgLight      = Color.FromArgb(248, 250, 253);
    public static readonly Color Surface      = Color.White;
    public static readonly Color TextDark     = Color.FromArgb( 30,  35,  45);
    public static readonly Color TextMuted    = Color.FromArgb(120, 125, 135);
    public static readonly Color Border       = Color.FromArgb(220, 225, 230);

    // Màu phụ trợ theo vai trò (chỉ dùng cho header / role chip)
    public static readonly Color RoleAdmin    = Color.FromArgb( 30,  90, 160);
    public static readonly Color RoleAdminDk  = Color.FromArgb( 20,  60, 110);
    public static readonly Color RoleDPV      = Color.FromArgb(180, 100,   0);
    public static readonly Color RoleDPVDk    = Color.FromArgb(150,  75,   0);
    public static readonly Color RoleBS       = Color.FromArgb(  0, 120,  80);
    public static readonly Color RoleBSDk     = Color.FromArgb(  0,  90,  60);
    public static readonly Color RoleKTV      = Color.FromArgb(  0, 140,  60);
    public static readonly Color RoleKTVDk    = Color.FromArgb(  0, 100,  40);
    public static readonly Color RoleBN       = Color.FromArgb(140,  60, 140);
    public static readonly Color RoleBNDk     = Color.FromArgb(100,  40, 100);

    // Healthcare palette từ ui-ux-pro-max recommendations
    public static readonly Color HealthCyan      = Color.FromArgb(  8, 145, 178);   // #0891B2
    public static readonly Color HealthCyanLight = Color.FromArgb( 34, 211, 238);   // #22D3EE
    public static readonly Color HealthGreen     = Color.FromArgb(  5, 150, 105);   // #059669
    public static readonly Color HealthEmerald   = Color.FromArgb( 22, 163,  74);   // #16A34A
    public static readonly Color HealthBgTint    = Color.FromArgb(236, 254, 255);   // #ECFEFF
    public static readonly Color HealthBgMint    = Color.FromArgb(236, 253, 245);   // #ECFDF5

    // Status semantic
    public static readonly Color StatusSuccess   = Color.FromArgb(  5, 150, 105);
    public static readonly Color StatusWarning   = Color.FromArgb(217, 119,   6);
    public static readonly Color StatusInfo      = Color.FromArgb(  8, 145, 178);
    public static readonly Color StatusDanger    = Color.FromArgb(220,  38,  38);

    // Sidebar palette
    public static readonly Color SidebarBg       = Color.FromArgb( 22,  30,  46);
    public static readonly Color SidebarBgAlt    = Color.FromArgb( 30,  39,  58);
    public static readonly Color SidebarText     = Color.FromArgb(180, 190, 210);
    public static readonly Color SidebarTextDim  = Color.FromArgb(130, 138, 160);
    public static readonly Color SidebarActive   = Color.FromArgb(255, 255, 255);

    // Spacing scale (4/8dp rhythm)
    public const int Spacing1 = 4;
    public const int Spacing2 = 8;
    public const int Spacing3 = 12;
    public const int Spacing4 = 16;
    public const int Spacing5 = 24;
    public const int Spacing6 = 32;
    public const int Spacing7 = 48;
    public const int Spacing8 = 64;

    // Radius scale
    public const int RadiusSm   = 6;
    public const int RadiusMd   = 10;
    public const int RadiusLg   = 16;
    public const int RadiusXl   = 22;
    public const int RadiusPill = 999;

    // ═══════════════════════════════════════════════════════════════════════════
    // FONT LOADING
    // ═══════════════════════════════════════════════════════════════════════════
    static UiTheme()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var fontResources = asm.GetManifestResourceNames()
                                    .Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

            foreach (var resName in fontResources)
            {
                using var s = asm.GetManifestResourceStream(resName);
                if (s == null) continue;
                var bytes = new byte[s.Length];
                int read = 0, off = 0;
                while ((read = s.Read(bytes, off, bytes.Length - off)) > 0) off += read;
                var ptr = Marshal.AllocCoTaskMem(bytes.Length);
                try
                {
                    Marshal.Copy(bytes, 0, ptr, bytes.Length);
                    _fonts.AddMemoryFont(ptr, bytes.Length);
                }
                finally { Marshal.FreeCoTaskMem(ptr); }
            }

            if (_fonts.Families.Length > 0)
            {
                // Tìm Montserrat trong các family đã load
                Family = _fonts.Families.FirstOrDefault(f =>
                    f.Name.Contains("Montserrat", StringComparison.OrdinalIgnoreCase))
                    ?? _fonts.Families[0];
                FontLoaded = true;
            }
            else
            {
                // Fallback: tìm Montserrat đã cài trên hệ thống
                using var installed = new InstalledFontCollection();
                var sys = installed.Families.FirstOrDefault(f =>
                    f.Name.Equals("Montserrat", StringComparison.OrdinalIgnoreCase));
                if (sys != null)
                {
                    Family = sys;
                    FontLoaded = true;
                }
                else
                {
                    Family = SystemFonts.DefaultFont.FontFamily;
                    FontLoaded = false;
                }
            }
        }
        catch
        {
            Family = SystemFonts.DefaultFont.FontFamily;
            FontLoaded = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // FONT HELPERS  (single source of truth — KHÔNG hard-code "Segoe UI" nơi khác)
    // ═══════════════════════════════════════════════════════════════════════════
    public static Font Body(float size = 9.5f, FontStyle style = FontStyle.Regular)
        => new(Family, size, style, GraphicsUnit.Point);

    public static Font BodyBold(float size = 9.5f) => Body(size, FontStyle.Bold);

    public static Font Label(float size = 9f)     => Body(size, FontStyle.Regular);
    public static Font LabelBold(float size = 9f) => Body(size, FontStyle.Bold);

    public static Font Heading1(float size = 18f) => Body(size, FontStyle.Bold);
    public static Font Heading2(float size = 14f) => Body(size, FontStyle.Bold);
    public static Font Heading3(float size = 11f) => Body(size, FontStyle.Bold);

    public static Font Button(float size = 9.5f) => Body(size, FontStyle.Bold);
    public static Font Italic(float size = 9f)   => Body(size, FontStyle.Italic);

    // ═══════════════════════════════════════════════════════════════════════════
    // CONTROL FACTORIES — đảm bảo style nhất quán
    // ═══════════════════════════════════════════════════════════════════════════
    public static Button PrimaryButton(string text, EventHandler? onClick = null)
        => MakeButton(text, Primary, PrimaryDark, onClick);

    public static Button AccentButton(string text, EventHandler? onClick = null)
        => MakeButton(text, Accent, Color.FromArgb(0, 100, 70), onClick);

    public static Button DangerButton(string text, EventHandler? onClick = null)
        => MakeButton(text, Danger, Color.FromArgb(160, 30, 30), onClick);

    public static Button SubtleButton(string text, EventHandler? onClick = null)
        => MakeButton(text, Color.FromArgb(70, 130, 180), Color.FromArgb(50, 100, 140), onClick);

    private static Button MakeButton(string text, Color bg, Color border, EventHandler? onClick)
    {
        var b = new Button
        {
            Text       = text,
            Height     = 38,
            MinimumSize= new Size(112, 38),
            BackColor  = bg,
            ForeColor  = Color.White,
            FlatStyle  = FlatStyle.Flat,
            Font       = Button(),
            Cursor     = Cursors.Hand,
            AutoSize   = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding    = new Padding(14, 0, 14, 0),
            TextAlign  = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        b.FlatAppearance.BorderSize  = 0;
        b.FlatAppearance.MouseOverBackColor = border;
        if (onClick != null) b.Click += onClick;
        return b;
    }

    public static TextBox TextField(int width = 220) => new()
    {
        Width = width, Height = 30,
        Font = Body(),
        BorderStyle = BorderStyle.FixedSingle
    };

    public static Label FieldLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = Label(),
        ForeColor = TextDark,
        Padding = new Padding(0, 6, 6, 0)
    };

    public static Label SectionLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = Heading3(),
        ForeColor = Primary,
        Padding = new Padding(0, 8, 0, 6)
    };

    public static DataGridView Grid() => new()
    {
        ReadOnly = true, AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Surface,
        RowHeadersVisible = false,
        Font = Body(),
        EnableHeadersVisualStyles = false,
        BorderStyle = BorderStyle.None,
        CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        ColumnHeadersHeight = 34,
        ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(241, 245, 249),
            ForeColor = TextDark,
            Font      = BodyBold(),
            Padding   = new Padding(8, 6, 8, 6),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        },
        DefaultCellStyle = new DataGridViewCellStyle
        {
            Font            = Body(),
            ForeColor       = TextDark,
            SelectionBackColor = Color.FromArgb(220, 235, 250),
            SelectionForeColor = TextDark,
            Padding         = new Padding(8, 4, 8, 4)
        },
        AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(250, 252, 255)
        },
        GridColor = Border,
        RowTemplate = { Height = 32 }
    };

    public static Panel Header(string title, Color bg, Color btnDark,
                               EventHandler? onLogout = null)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 52, BackColor = bg };
        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = Heading2(),
            TextAlign = ContentAlignment.MiddleCenter,
            AutoEllipsis = true,
            Padding = new Padding(12, 0, onLogout == null ? 12 : 152, 0)
        };
        Button? btn = null;
        if (onLogout != null)
        {
            btn = new Button
            {
                Text = "⏻  Đăng xuất",
                Dock = DockStyle.Right, Width = 140,
                BackColor = btnDark, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = Button(), Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onLogout;
            p.Controls.Add(btn);
        }
        p.Controls.Add(titleLabel);
        btn?.BringToFront();
        return p;
    }
}
