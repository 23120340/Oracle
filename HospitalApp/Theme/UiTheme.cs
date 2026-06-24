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
    // ─── MINIMALISM (Swiss style): neutral nền + DUY NHẤT 1 màu nhấn teal ───
    public static readonly Color Primary      = Color.FromArgb( 15, 118, 110);   // #0F766E teal-700 (accent chính)
    public static readonly Color PrimaryDark  = Color.FromArgb( 17,  94,  89);   // #115E59 teal-800 (hover)
    public static readonly Color Accent       = Color.FromArgb( 15, 118, 110);   // #0F766E
    public static readonly Color Warning      = Color.FromArgb(180,  83,   9);   // #B45309 amber-700
    public static readonly Color Danger       = Color.FromArgb(180,  35,  24);   // #B42318 red-700
    public static readonly Color BgLight      = Color.FromArgb(245, 246, 248);   // #F5F6F8 app background
    public static readonly Color Surface      = Color.White;                     // #FFFFFF cards/header
    public static readonly Color TextDark     = Color.FromArgb( 26,  29,  33);   // #1A1D21 primary text
    public static readonly Color TextMuted    = Color.FromArgb( 91, 100, 112);   // #5B6470 secondary text
    public static readonly Color TextFaint    = Color.FromArgb(154, 161, 172);   // #9AA1AC caption/placeholder
    public static readonly Color Border       = Color.FromArgb(232, 234, 237);   // #E8EAED hairline
    public static readonly Color BorderStrong = Color.FromArgb(215, 219, 224);   // #D7DBE0 input border
    public static readonly Color AccentTint   = Color.FromArgb(230, 244, 242);   // #E6F4F2 selected/active bg

    // Màu phụ trợ theo vai trò — chỉ dùng cho CHIP nhỏ trên header (điểm nhấn nhẹ, muted)
    public static readonly Color RoleAdmin    = Color.FromArgb( 15, 118, 110);   // teal
    public static readonly Color RoleAdminDk  = Color.FromArgb( 17,  94,  89);
    public static readonly Color RoleDPV      = Color.FromArgb(180,  83,   9);   // amber-700
    public static readonly Color RoleDPVDk    = Color.FromArgb(146,  64,  14);
    public static readonly Color RoleBS       = Color.FromArgb( 21, 128,  61);   // green-700
    public static readonly Color RoleBSDk     = Color.FromArgb( 22,  101,  52);
    public static readonly Color RoleKTV      = Color.FromArgb( 14, 116, 144);   // cyan-700
    public static readonly Color RoleKTVDk    = Color.FromArgb( 21,  94, 117);
    public static readonly Color RoleBN       = Color.FromArgb(109,  40, 217);   // violet-700
    public static readonly Color RoleBNDk     = Color.FromArgb( 91,  33, 182);

    // Button accents (semantic) — đã hoà vào palette minimalist
    public static readonly Color HealthCyan      = Color.FromArgb( 15, 118, 110);   // teal = hành động chính
    public static readonly Color HealthCyanLight = Color.FromArgb( 20, 184, 166);   // #14B8A6 teal-500 (nhấn nav)
    public static readonly Color HealthGreen     = Color.FromArgb( 21, 128,  61);   // #15803D green-700 = lưu
    public static readonly Color HealthEmerald   = Color.FromArgb( 21, 128,  61);   // #15803D
    public static readonly Color HealthBgTint    = Color.FromArgb(230, 244, 242);   // teal tint
    public static readonly Color HealthBgMint    = Color.FromArgb(236, 253, 243);   // green tint

    // Status semantic
    public static readonly Color StatusSuccess   = Color.FromArgb( 21, 128,  61);
    public static readonly Color StatusWarning   = Color.FromArgb(180,  83,   9);
    public static readonly Color StatusInfo      = Color.FromArgb( 15, 118, 110);
    public static readonly Color StatusDanger    = Color.FromArgb(180,  35,  24);

    // Sidebar palette — MINIMALISM: nav SÁNG (trắng) thay vì tối
    public static readonly Color SidebarBg       = Color.White;                  // #FFFFFF
    public static readonly Color SidebarBgAlt    = Color.FromArgb(245, 246, 248);// #F5F6F8 hover
    public static readonly Color SidebarText     = Color.FromArgb( 91, 100, 112);// #5B6470 mục thường
    public static readonly Color SidebarTextDim  = Color.FromArgb(154, 161, 172);// #9AA1AC nhãn section
    public static readonly Color SidebarActive   = Color.FromArgb( 26,  29,  33);// #1A1D21 mục active

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
    // ─── TYPE SCALE (Montserrat) — phân cấp rõ ràng, đọc tốt trên desktop ───
    //  H1 page title 16 · H2 section 13 · H3 card 11 · Body 10 · Label 9.5 · Caption 8.5
    public static Font Body(float size = 10f, FontStyle style = FontStyle.Regular)
        => new(Family, size, style, GraphicsUnit.Point);

    public static Font BodyBold(float size = 10f) => Body(size, FontStyle.Bold);

    public static Font Label(float size = 9.5f)     => Body(size, FontStyle.Regular);
    public static Font LabelBold(float size = 9.5f) => Body(size, FontStyle.Bold);

    public static Font Heading1(float size = 16f) => Body(size, FontStyle.Bold);
    public static Font Heading2(float size = 13f) => Body(size, FontStyle.Bold);
    public static Font Heading3(float size = 11f) => Body(size, FontStyle.Bold);

    public static Font Caption(float size = 8.5f) => Body(size, FontStyle.Regular);
    public static Font Button(float size = 10f)   => Body(size, FontStyle.Bold);
    public static Font Italic(float size = 9f)    => Body(size, FontStyle.Italic);

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

    public static TextBox TextField(int width = 220) => Pad(new()
    {
        Width = width, Height = 36,
        Font = Body(),
        BorderStyle = BorderStyle.FixedSingle
    });

    // ─── Text inset cho TextBox (EM_SETMARGINS) — chữ KHÔNG dính sát viền trái/phải ───
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    private const int EM_SETMARGINS  = 0x00D3;
    private const int EC_LEFTMARGIN  = 0x0001;
    private const int EC_RIGHTMARGIN = 0x0002;

    /// <summary>Thêm lề trong (px) cho TextBox để chữ thoáng, không bị viền che.</summary>
    public static TextBox Pad(TextBox tb, int left = 9, int right = 9)
    {
        void Apply()
        {
            if (!tb.IsHandleCreated) return;
            int lp = (right << 16) | (left & 0xFFFF);
            SendMessage(tb.Handle, EM_SETMARGINS,
                        (IntPtr)(EC_LEFTMARGIN | EC_RIGHTMARGIN), (IntPtr)lp);
        }
        tb.HandleCreated += (_, _) => Apply();
        Apply();
        return tb;
    }

    public static Label FieldLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = Label(),
        ForeColor = TextMuted,
        Padding = new Padding(0, 6, 6, 0)
    };

    public static Label SectionLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = Heading3(),
        ForeColor = TextDark,
        Padding = new Padding(0, 8, 0, 6)
    };

    // MINIMALISM grid: nền trắng, KHÔNG kẻ dọc, hàng cách nhau bằng đường hairline,
    // header trắng chữ muted, hàng chọn nền teal nhạt. Tự đổi tên cột HOA → dạng câu.
    public static DataGridView Grid()
    {
        var g = new DataGridView
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
            ColumnHeadersHeight = 40,
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Surface,
                ForeColor = TextMuted,
                Font      = LabelBold(9f),
                Padding   = new Padding(10, 8, 10, 8),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            },
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Font            = Body(),
                ForeColor       = TextDark,
                BackColor       = Surface,
                SelectionBackColor = AccentTint,
                SelectionForeColor = TextDark,
                Padding         = new Padding(10, 6, 10, 6)
            },
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Surface },
            GridColor = Border,
            RowTemplate = { Height = 40 }
        };
        // Đổi tên cột kỹ thuật (HOA) sang nhãn dạng câu, dễ đọc — áp dụng cho mọi lưới.
        g.DataBindingComplete += (_, _) =>
        {
            foreach (DataGridViewColumn c in g.Columns)
                c.HeaderText = FriendlyHeader(c.Name);
        };
        return g;
    }

    private static readonly Dictionary<string, string> _headerMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MAHSBA"]="Mã HSBA", ["MABN"]="Mã BN", ["MABS"]="Bác sĩ", ["MAKTV"]="KTV",
        ["MANV"]="Mã NV", ["MATB"]="Mã TB", ["NGAY"]="Ngày", ["NGAYSINH"]="Ngày sinh",
        ["NGAYGIO"]="Ngày giờ", ["NGAYDV"]="Ngày DV", ["TENBN"]="Họ tên", ["HOTEN"]="Họ tên",
        ["PHAI"]="Phái", ["CCCD"]="CCCD", ["TINHTP"]="Tỉnh/TP", ["MAKHOA"]="Khoa",
        ["TRANGTHAI"]="Trạng thái", ["NOIDUNG"]="Nội dung", ["DIADIEM"]="Địa điểm",
        ["LOAIDV"]="Loại dịch vụ", ["KETQUA"]="Kết quả", ["CHANDOAN"]="Chẩn đoán",
        ["KETLUAN"]="Kết luận", ["DIEUTRI"]="Điều trị", ["VAITRO"]="Vai trò",
        ["TENTHUOC"]="Tên thuốc", ["LIEUDUNG"]="Liều dùng", ["USERNAME"]="Tài khoản",
        ["ROLE"]="Vai trò", ["PRIVILEGE"]="Quyền", ["OBJECT"]="Đối tượng",
        ["GRANTABLE"]="Cấp lại được", ["COLUMNS"]="Cột", ["TYPE"]="Loại",
        ["ACTION"]="Hành động", ["RESULT"]="Kết quả", ["TIME"]="Thời gian",
        ["OWNER"]="Schema", ["TABLE_NAME"]="Bảng", ["COLUMN_NAME"]="Cột",
        ["GRANTOR"]="Người cấp", ["GRANTED_ROLE"]="Vai trò", ["DEFAULT_ROLE"]="Mặc định",
        ["ADMIN_OPTION"]="Quyền cấp lại", ["AUTHENTICATION_TYPE"]="Kiểu xác thực",
        ["COMMON"]="Common", ["ACCOUNT_STATUS"]="Trạng thái", ["DEFAULT_TABLESPACE"]="Tablespace",
        ["CREATED"]="Ngày tạo", ["EXPIRY_DATE"]="Ngày hết hạn",
    };

    /// <summary>Đổi tên cột HOA (vd "ACCOUNT_STATUS") sang dạng câu dễ đọc.</summary>
    public static string FriendlyHeader(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        if (_headerMap.TryGetValue(raw, out var v)) return v;
        var s = raw.Replace('_', ' ').Trim().ToLowerInvariant();
        return s.Length == 0 ? raw : char.ToUpperInvariant(s[0]) + s[1..];
    }

    // MINIMALISM header: nền trắng, tiêu đề canh TRÁI (TextDark), nút Đăng xuất kiểu ghost,
    // viền hairline dưới đáy. Tham số bg/btnDark giữ để tương thích chữ ký (không tô màu nền nữa).
    public static Panel Header(string title, Color bg, Color btnDark,
                               EventHandler? onLogout = null,
                               EventHandler? onChangePassword = null)
    {
        var p = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Surface };
        p.Paint += (_, e) =>
        {
            using var pen = new Pen(Border, 1);
            e.Graphics.DrawLine(pen, 0, p.Height - 1, p.Width, p.Height - 1);
        };
        var titleLabel = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = TextDark,
            Font = Heading1(16f),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Padding = new Padding(24, 0, 8, 0)
        };
        if (onLogout != null || onChangePassword != null)
        {
            // Cụm nút phải, xếp từ phải sang trái: Đăng xuất ngoài cùng, Đổi mật khẩu bên trái.
            var right = new FlowLayoutPanel
            {
                Dock = DockStyle.Right, AutoSize = true,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
                BackColor = Surface, Padding = new Padding(8, 13, 16, 13)
            };
            if (onLogout != null)
                right.Controls.Add(GhostHeaderButton("Đăng xuất", onLogout));
            if (onChangePassword != null)
                right.Controls.Add(GhostHeaderButton("Đổi mật khẩu", onChangePassword));
            p.Controls.Add(right);
        }
        p.Controls.Add(titleLabel);
        return p;
    }

    // Nút "khối vuông" kiểu ghost cho header dùng chung (nền trắng, viền mảnh, góc vuông).
    private static Button GhostHeaderButton(string text, EventHandler onClick)
    {
        var b = new Button
        {
            Text = text, Height = 38, MinimumSize = new Size(130, 38),
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 0, 14, 0), Margin = new Padding(8, 0, 0, 0),
            BackColor = Surface, ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat, Font = Button(), Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleCenter
        };
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = BorderStrong;
        b.FlatAppearance.MouseOverBackColor = BgLight;
        b.Click += onClick;
        return b;
    }
}
