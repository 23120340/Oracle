using System.Drawing.Drawing2D;
using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Forms.Admin;
using HospitalApp.Forms.Hospital;
using HospitalApp.Security;
using HospitalApp.Theme;

namespace HospitalApp.Forms;

public sealed class LoginForm : Form
{
    // ─── Split-card (brand trái + form phải) — phong cách minimalism, tông teal ───
    private const int FormWidth   = 724;    // card 720 + 2px viền mảnh mỗi bên
    private const int CardWidth   = 720;
    private const int LeftWidth   = 290;    // panel thương hiệu bên trái
    private const int FieldWidth  = 360;    // (giữ chữ ký; input thực tế Dock=Fill)
    private const int CollapsedHeight = 406;   // = CollapsedCardHeight() + 4 (viền)
    private const int ExpandedHeight  = 548;   // = ExpandedCardHeight() + 4

    private Panel _card = null!;
    private TableLayoutPanel _cardLayout = null!;
    private Panel _advancedPanel = null!;
    private Label _footer = null!;
    private TextBox _txtUser = null!, _txtPass = null!;
    private TextBox _txtHost = null!, _txtPort = null!, _txtSid = null!;
    private Button _btnLogin = null!, _btnAdvanced = null!, _btnTogglePass = null!;
    private Label _lblStatus = null!, _btnClose = null!;
    private bool _advancedVisible;

    // ─── Row heights của form bên phải (TableLayoutPanel 1 cột) ──────────────────
    private const int RowTitle     = 42;
    private const int RowSubtitle  = 24;
    private const int RowGapTop    = UiTheme.Spacing3;   // 12
    private const int RowFieldLbl  = 26;    // cao hơn + nhãn canh giữa → không bị ô nhập che
    private const int RowInput     = 44;
    private const int RowStatus    = 28;
    private const int RowLogin     = 46;
    private const int RowAdvBtn    = 28;
    private const int RowAdvanced  = 142;                // bảng nâng cao (host/port/sid)
    private const int RowFooter    = 22;
    private const int RightPadX    = 40;
    private const int RightPadTop  = 34;
    private const int RightPadBot  = 26;

    private static readonly Dictionary<string, (int count, DateTime until)> _failTracker = new();
    private const int MaxFail = 5;
    private const int LockoutSeconds = 60;

    public LoginForm()
    {
        Text = "Đăng nhập – HospitalApp";
        ClientSize = new Size(FormWidth, CollapsedHeight);
        MinimumSize = new Size(FormWidth, CollapsedHeight);
        MaximumSize = new Size(FormWidth, ExpandedHeight);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = UiTheme.BorderStrong;   // lộ ra 2px quanh card = viền mảnh ngoài
        Font = UiTheme.Body();
        DoubleBuffered = true;

        BuildUi();
        AcceptButton = _btnLogin;
        Shown += (_, _) => _txtUser.Focus();
        Resize += (_, _) => LayoutShell();
        WireWindowDrag();
        LayoutShell();
    }

    private void BuildUi()
    {
        // ── Close (window chrome) — floats over the form, outside the card ────────
        _btnClose = new Label
        {
            Text = IconRegistry.Close,
            Size = new Size(34, 34),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = IconRegistry.Icon(12f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.BgLight,
            Cursor = Cursors.Hand
        };
        _btnClose.Click += (_, _) => Application.Exit();
        Controls.Add(_btnClose);

        // ── Card 2 cột: TRÁI = thương hiệu (nền teal) · PHẢI = form đăng nhập (trắng) ──
        _card = new Panel
        {
            Size = new Size(CardWidth, CollapsedCardHeight()),
            BackColor = UiTheme.Surface
        };
        _card.Paint += (_, e) =>
            PaintRoundPanel(e.Graphics, _card.ClientRectangle, 16, UiTheme.Surface, UiTheme.Border);
        Controls.Add(_card);

        // PHẢI: panel form (thêm TRƯỚC để Fill phần còn lại) ----------------------
        var right = new Panel
        {
            Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
            Padding = new Padding(RightPadX, RightPadTop, RightPadX, RightPadBot)
        };
        _card.Controls.Add(right);

        // TRÁI: panel thương hiệu nền teal (thêm SAU → dock trước → chiếm cột trái) -
        var brand = new Panel { Dock = DockStyle.Left, Width = LeftWidth, BackColor = UiTheme.Primary };
        brand.Paint += (_, e) => DrawBrand(e.Graphics, brand.ClientRectangle);
        _card.Controls.Add(brand);

        // ── Form bên phải: TableLayoutPanel 1 cột, mỗi khối một hàng riêng ────────
        _cardLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            BackColor   = UiTheme.Surface,
            ColumnCount = 1,
            RowCount    = 12,
            AutoScroll  = true,
            Margin      = new Padding(0),
            Padding     = new Padding(0)
        };
        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        // 0 title | 1 subtitle | 2 gap | 3 userLbl | 4 userInput | 5 passLbl
        // 6 passInput | 7 status | 8 login | 9 advBtn | 10 advanced | 11 footer
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowTitle));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowSubtitle));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowGapTop));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFieldLbl));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowInput));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFieldLbl));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowInput));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowStatus));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowLogin));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowAdvBtn));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));               // advanced (0 khi ẩn)
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFooter));
        right.Controls.Add(_cardLayout);

        // Row 0: title
        _cardLayout.Controls.Add(new Label
        {
            Text = "Đăng nhập",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Heading1(19f), ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface, Margin = new Padding(0),
            UseCompatibleTextRendering = true
        }, 0, 0);

        // Row 1: subtitle
        _cardLayout.Controls.Add(new Label
        {
            Text = "Nhập thông tin để tiếp tục",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Body(10f), ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface, Margin = new Padding(0),
            UseCompatibleTextRendering = true
        }, 0, 1);

        // Row 2: gap

        // Rows 3-4: username
        AddFieldLabel("Tài khoản", 3);
        _txtUser = NewInput(false);
        _cardLayout.Controls.Add(WrapInput(_txtUser, 0, 0, FieldWidth), 0, 4);

        // Rows 5-6: password
        AddFieldLabel("Mật khẩu", 5);
        _txtPass = NewInput(true);
        _txtPass.ShortcutsEnabled = false;
        _cardLayout.Controls.Add(WrapPasswordInput(_txtPass, 0, 0, FieldWidth), 0, 6);

        // Row 7: status / error
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill, Font = UiTheme.Body(9f),
            ForeColor = UiTheme.Danger, TextAlign = ContentAlignment.MiddleLeft,
            BackColor = UiTheme.Surface, Margin = new Padding(2, 0, 2, 0)
        };
        _cardLayout.Controls.Add(_lblStatus, 0, 7);

        // Row 8: login button
        _btnLogin = new Button
        {
            Text = "Đăng nhập", Dock = DockStyle.Fill,
            BackColor = UiTheme.Primary, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Button(11f),
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 2)
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.FlatAppearance.MouseOverBackColor = UiTheme.PrimaryDark;
        _btnLogin.Click += BtnLogin_Click;
        _btnLogin.Resize += (_, _) => RoundCorners(_btnLogin, 10);
        RoundCorners(_btnLogin, 10);
        _cardLayout.Controls.Add(_btnLogin, 0, 8);

        // Row 9: advanced toggle
        _btnAdvanced = new Button
        {
            Text = "▸  Tùy chọn nâng cao", Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat, BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextMuted, Font = UiTheme.Body(9f),
            Cursor = Cursors.Hand, TabStop = false, Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _btnAdvanced.FlatAppearance.BorderSize = 0;
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        _cardLayout.Controls.Add(_btnAdvanced, 0, 9);

        // Row 10: collapsible advanced panel
        _advancedPanel = new Panel
        {
            Dock = DockStyle.Fill, Height = RowAdvanced,
            BackColor = UiTheme.Surface, Margin = new Padding(0), Visible = false
        };
        BuildAdvancedPanel();
        _cardLayout.Controls.Add(_advancedPanel, 0, 10);

        // Row 11: footer
        _footer = new Label
        {
            Text = "© 2026 HospitalApp · ATBM HTTT",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
            Font = UiTheme.Body(8.5f), ForeColor = UiTheme.TextFaint,
            BackColor = UiTheme.Surface, Margin = new Padding(0), Name = "Footer"
        };
        _cardLayout.Controls.Add(_footer, 0, 11);
    }

    // Panel thương hiệu bên trái: emblem dấu cộng y tế + tên app + tagline (chữ trắng trên nền teal)
    private static void DrawBrand(Graphics g, Rectangle r)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int d = 88;
        int cx = r.Width / 2;
        int cy = r.Height / 2 - 44;
        var circle = new Rectangle(cx - d / 2, cy - d / 2, d, d);
        using (var bg = new SolidBrush(Color.FromArgb(38, 255, 255, 255)))
            g.FillEllipse(bg, circle);
        using (var pen = new Pen(Color.White, 7) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            int arm = d / 4;
            g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
            g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
        }

        using var brandFont = UiTheme.Heading1(20f);
        using var white = new SolidBrush(Color.White);
        var b = g.MeasureString("HospitalApp", brandFont);
        float by = cy + d / 2f + 14;
        g.DrawString("HospitalApp", brandFont, white, cx - b.Width / 2, by);

        using var tagFont = UiTheme.Body(9.5f);
        using var tag = new SolidBrush(Color.FromArgb(225, 255, 255, 255));
        string t1 = "Quản lý Bệnh viện";
        string t2 = "An toàn & Bảo mật HTTT";
        var s1 = g.MeasureString(t1, tagFont);
        var s2 = g.MeasureString(t2, tagFont);
        g.DrawString(t1, tagFont, tag, cx - s1.Width / 2, by + b.Height + 10);
        g.DrawString(t2, tagFont, tag, cx - s2.Width / 2, by + b.Height + 10 + s1.Height + 2);
    }

    // Card height when advanced panel is hidden / shown — derived from row tokens
    // so the form, card and layout can never disagree (the source of the old overlap).
    private static int CollapsedCardHeight() =>
        RightPadTop + RightPadBot +
        RowTitle + RowSubtitle + RowGapTop +
        RowFieldLbl + RowInput + RowFieldLbl + RowInput +
        RowStatus + RowLogin + RowAdvBtn + RowFooter;

    private static int ExpandedCardHeight() => CollapsedCardHeight() + RowAdvanced;

    // Field label lives in its OWN row (never overlapping its input row below it).
    private void AddFieldLabel(string text, int row)
    {
        _cardLayout.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,   // FIX: canh giữa, chừa khoảng dưới → ô nhập không che chữ
            Font = UiTheme.LabelBold(9.5f),
            ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface,
            Margin = new Padding(2, 0, 2, 4)
        }, 0, row);
    }

    private void BuildAdvancedPanel()
    {
        // Advanced inputs in their OWN TableLayoutPanel — Host/Port share a row via
        // two 50% columns; SID spans the width on its own row. No absolute coords.
        var adv = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            BackColor   = UiTheme.Surface,
            ColumnCount = 2,
            RowCount    = 4,
            Margin      = new Padding(0),
            Padding     = new Padding(0)
        };
        adv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        adv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        adv.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFieldLbl));   // Host/Port labels
        adv.RowStyles.Add(new RowStyle(SizeType.Absolute, RowInput));      // Host/Port inputs
        adv.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFieldLbl));   // SID label
        adv.RowStyles.Add(new RowStyle(SizeType.Absolute, RowInput));      // SID input
        _advancedPanel.Controls.Add(adv);

        adv.Controls.Add(MakeSmallLabel("Host"), 0, 0);
        adv.Controls.Add(MakeSmallLabel("Port"), 1, 0);

        _txtHost = NewInput(false);
        _txtHost.Text = "localhost";
        var hostCell = WrapInput(_txtHost, 0, 0, 154);
        hostCell.Margin = new Padding(0, 0, 6, 0);
        adv.Controls.Add(hostCell, 0, 1);

        _txtPort = NewInput(false);
        _txtPort.Text = "1521";
        var portCell = WrapInput(_txtPort, 0, 0, 154);
        portCell.Margin = new Padding(6, 0, 0, 0);
        adv.Controls.Add(portCell, 1, 1);

        var sidLbl = MakeSmallLabel("Service / SID");
        adv.SetColumnSpan(sidLbl, 2);
        adv.Controls.Add(sidLbl, 0, 2);

        _txtSid = NewInput(false);
        _txtSid.Text = "XEPDB1";
        var sidCell = WrapInput(_txtSid, 0, 0, FieldWidth);
        adv.SetColumnSpan(sidCell, 2);
        adv.Controls.Add(sidCell, 0, 3);
    }

    private static Label MakeSmallLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.BottomLeft,
        Font = UiTheme.LabelBold(8.5f),
        ForeColor = UiTheme.TextMuted,
        BackColor = UiTheme.Surface,
        Margin = new Padding(2, 0, 2, 2)
    };

    private static TextBox NewInput(bool isPassword) => new()
    {
        BorderStyle = BorderStyle.None,
        Font = UiTheme.Body(11f),
        PasswordChar = isPassword ? '•' : '\0',
        BackColor = Color.FromArgb(248, 250, 253)
    };

    // Rounded input wrapper. Docks to FILL its TableLayoutPanel cell so it always
    // stretches to the column width and never clips. (x/y kept for signature
    // compatibility — position is now owned by the parent table cell.)
    private static Panel WrapInput(TextBox tb, int x, int y, int width)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, 42),
            Padding = new Padding(14, 9, 14, 8),
            Margin = new Padding(0),
            BackColor = Color.FromArgb(248, 250, 253)
        };
        panel.Paint += (_, e) => PaintRoundPanel(e.Graphics, panel.ClientRectangle, 9,
            panel.BackColor, tb.Focused ? UiTheme.Primary : UiTheme.Border);
        tb.Dock = DockStyle.Fill;
        tb.GotFocus += (_, _) => panel.Invalidate();
        tb.LostFocus += (_, _) => panel.Invalidate();
        panel.Controls.Add(tb);
        return panel;
    }

    private Panel WrapPasswordInput(TextBox tb, int x, int y, int width)
    {
        var panel = WrapInput(tb, x, y, width);
        _btnTogglePass = new Button
        {
            Text      = IconRegistry.EyeHide,           // mặc định: mật khẩu đang ẩn → nhấn để hiện
            Dock      = DockStyle.Right,
            Width     = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(248, 250, 253),
            ForeColor = UiTheme.TextMuted,
            Cursor    = Cursors.Hand,
            TabStop   = false,
            Font      = IconRegistry.Icon(13f)
        };
        _btnTogglePass.FlatAppearance.BorderSize = 0;
        _btnTogglePass.FlatAppearance.MouseOverBackColor = Color.FromArgb(235, 240, 248);
        _btnTogglePass.Click += (_, _) =>
        {
            var nowVisible = tb.PasswordChar == '\0';
            // nếu đang hiện → bấm để ẩn (đặt char '•'); ngược lại
            tb.PasswordChar = nowVisible ? '•' : '\0';
            _btnTogglePass.Text = tb.PasswordChar == '\0'
                ? IconRegistry.Eye       // đang hiện thật → icon eye open
                : IconRegistry.EyeHide;  // đang ẩn → icon eye-strike (gợi ý click để hiện)
        };
        panel.Controls.Add(_btnTogglePass);
        _btnTogglePass.BringToFront();
        return panel;
    }

    private void ToggleAdvanced()
    {
        _advancedVisible = !_advancedVisible;
        _btnAdvanced.Text = _advancedVisible
            ? "▾  Ẩn tùy chọn nâng cao"
            : "▸  Tùy chọn nâng cao";

        // The advanced row is AutoSize: hiding the panel collapses the row to 0,
        // so the footer (its OWN row below) reflows automatically — no manual
        // Location math, no overlap. We only resize card + form to match.
        _cardLayout.SuspendLayout();
        _advancedPanel.Visible = _advancedVisible;
        _advancedPanel.Height = _advancedVisible ? RowAdvanced : 0;
        _cardLayout.ResumeLayout(true);

        _card.Size = new Size(CardWidth, _advancedVisible ? ExpandedCardHeight() : CollapsedCardHeight());
        ClientSize = new Size(FormWidth, _advancedVisible ? ExpandedHeight : CollapsedHeight);
        RoundCorners(this, 16);
        LayoutShell();
    }

    private void LayoutShell()
    {
        // Card lùi vào 2px → để lộ viền mảnh (BackColor form) bao quanh
        _card.Location = new Point(2, 2);
        _btnClose.Location = new Point(ClientSize.Width - 42, 10);
        _btnClose.BringToFront();   // nổi trên card
        RoundCorners(this, 16);
        RoundCorners(_card, 16);
        _card.Invalidate();
    }

    private static void DrawEmblem(Graphics g, Rectangle r)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var d = Math.Min(r.Width, r.Height) - 4;
        var x = (r.Width - d) / 2;
        var y = (r.Height - d) / 2;
        using var brush = new LinearGradientBrush(new Rectangle(x, y, d, d),
            UiTheme.Primary, UiTheme.HealthCyan, LinearGradientMode.ForwardDiagonal);
        g.FillEllipse(brush, x, y, d, d);

        using var pen = new Pen(Color.White, 4) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        var cx = x + d / 2;
        var cy = y + d / 2;
        var arm = d / 4;
        g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
        g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
    }

    private static void PaintRoundPanel(Graphics g, Rectangle bounds, int radius, Color fill, Color? border)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        using var path = RoundedPath(rect, radius);
        using var brush = new SolidBrush(fill);
        g.FillPath(brush, path);
        if (border.HasValue)
        {
            using var pen = new Pen(border.Value, 1);
            g.DrawPath(pen, path);
        }
    }

    private static void RoundCorners(Control c, int radius)
    {
        if (c.Width <= 0 || c.Height <= 0) return;
        using var path = RoundedPath(new Rectangle(0, 0, c.Width, c.Height), radius);
        c.Region = new Region(path);
    }

    private static GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void WireWindowDrag()
    {
        bool dragging = false;
        Point start = Point.Empty;
        MouseDown += (_, e) => { dragging = true; start = e.Location; };
        MouseUp += (_, _) => dragging = false;
        MouseMove += (_, e) =>
        {
            if (!dragging) return;
            Location = new Point(Location.X + e.X - start.X, Location.Y + e.Y - start.Y);
        };
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        _btnLogin.Enabled = false;
        _lblStatus.Text = "Đang kết nối...";
        _lblStatus.ForeColor = UiTheme.TextMuted;

        var host = _txtHost.Text.Trim();
        var port = _txtPort.Text.Trim();
        var sid = _txtSid.Text.Trim();
        var user = _txtUser.Text.Trim();
        var pass = _txtPass.Text;

        if (string.IsNullOrEmpty(user))
        {
            _lblStatus.Text = "Vui lòng nhập tên đăng nhập.";
            _lblStatus.ForeColor = UiTheme.Danger;
            _btnLogin.Enabled = true;
            return;
        }

        var userKey = user.ToUpper();
        if (_failTracker.TryGetValue(userKey, out var lockInfo) && DateTime.Now < lockInfo.until)
        {
            var sec = (int)(lockInfo.until - DateTime.Now).TotalSeconds;
            _lblStatus.Text = $"Tài khoản tạm khoá {sec}s vì sai nhiều lần.";
            _lblStatus.ForeColor = UiTheme.Danger;
            _btnLogin.Enabled = true;
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                var db = new OracleHelper(host, port, sid, user, pass);
                db.TestConnection();
                var role = db.GetHospitalRole();

                _failTracker.Remove(userKey);
                TryLogLogin(db, userKey, true, null);

                Invoke(() =>
                {
                    _txtPass.Clear();

                    Form? next = role switch
                    {
                        "DBA" => new AdminDashboard(db),
                        "DPV" => new DPVForm(db),
                        "BS" => new BSForm(db),
                        "KTV" => new KTVForm(db),
                        "BN" => new BNForm(db),
                        "OLS" => new OLSViewerForm(db),
                        _ => null
                    };

                    if (next is null)
                    {
                        _lblStatus.Text = $"Đăng nhập OK nhưng không nhận được vai trò (role='{role}'). Kiểm tra schema BVADMIN.";
                        _lblStatus.ForeColor = UiTheme.Danger;
                        _btnLogin.Enabled = true;
                        return;
                    }

                    Hide();
                    next.FormClosed += (_, _) =>
                    {
                        _txtUser.Clear();
                        _txtPass.Clear();
                        _lblStatus.Text = "Đã đăng xuất.";
                        _lblStatus.ForeColor = UiTheme.TextMuted;
                        _btnLogin.Enabled = true;
                        Show();
                        _txtUser.Focus();
                    };
                    next.Show();
                });
            }
            catch (Exception ex)
            {
                var current = _failTracker.GetValueOrDefault(userKey);
                var newCount = current.count + 1;
                var until = newCount >= MaxFail
                    ? DateTime.Now.AddSeconds(LockoutSeconds)
                    : DateTime.MinValue;
                _failTracker[userKey] = (newCount, until);

                TryLogLoginSafe(host, port, sid, userKey, false, ex.ToString());
                AppAuditLogger.Security(userKey, "login.fail",
                    $"host={host} port={port} sid={sid} ora={OracleErrorMapper.ExtractOraNumber(ex)} type={ex.GetType().Name} msg={ex.Message}");

                Invoke(() =>
                {
                    _lblStatus.Text = OracleErrorMapper.Verbose(ex);
                    _lblStatus.ForeColor = UiTheme.Danger;
                    _btnLogin.Enabled = true;
                    _txtPass.Clear();
                    _txtPass.Focus();

                    // Hiện full exception + stack trace để debug
                    if (OracleErrorMapper.ExtractOraNumber(ex) == null)
                    {
                        var stack = (ex.StackTrace ?? "").Replace("\r", "");
                        // Lấy 8 dòng đầu của stack (đủ xác định nguồn)
                        var stackShort = string.Join('\n',
                            stack.Split('\n').Take(8));

                        MessageBox.Show(this,
                            $"Type: {ex.GetType().FullName}\n" +
                            $"Message: {ex.Message}\n" +
                            $"Inner: {ex.InnerException?.Message ?? "(none)"}\n\n" +
                            $"Stack (top 8 frames):\n{stackShort}\n\n" +
                            $"Conn: {host}:{port}/{sid}  User: {userKey}",
                            "Chi tiết lỗi đăng nhập",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        });
    }

    private static void TryLogLogin(OracleHelper db, string user, bool ok, string? reason)
    {
        try
        {
            db.Execute(
                "BEGIN BVADMIN.sp_log_login(:u, :s, :o, :h, :r); END;",
                OracleHelper.Param("u", user),
                OracleHelper.Param("s", ok ? "Y" : "N"),
                OracleHelper.Param("o", Environment.UserName),
                OracleHelper.Param("h", Environment.MachineName),
                OracleHelper.Param("r", reason));
        }
        catch { }
    }

    private static void TryLogLoginSafe(string host, string port, string sid, string user, bool ok, string reason)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), "HospitalApp", "logs");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"login-{DateTime.Now:yyyyMMdd}.log");
            File.AppendAllText(path,
                $"{DateTime.Now:O}\t{user}\t{(ok ? "OK" : "FAIL")}\t{host}/{sid}\t{reason.Replace('\t', ' ')}\n");
        }
        catch { }
    }
}
