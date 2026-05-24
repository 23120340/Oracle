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
    private const int CardWidth = 400;
    private const int FieldWidth = 328;
    private const int CollapsedHeight = 610;
    private const int ExpandedHeight = 745;

    private Panel _card = null!;
    private Panel _advancedPanel = null!;
    private TextBox _txtUser = null!, _txtPass = null!;
    private TextBox _txtHost = null!, _txtPort = null!, _txtSid = null!;
    private Button _btnLogin = null!, _btnAdvanced = null!, _btnTogglePass = null!;
    private Label _lblStatus = null!, _btnClose = null!;
    private bool _advancedVisible;

    private static readonly Dictionary<string, (int count, DateTime until)> _failTracker = new();
    private const int MaxFail = 5;
    private const int LockoutSeconds = 60;

    public LoginForm()
    {
        Text = "Đăng nhập – HospitalApp";
        ClientSize = new Size(500, CollapsedHeight);
        MinimumSize = new Size(500, CollapsedHeight);
        MaximumSize = new Size(500, ExpandedHeight);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = UiTheme.Primary;
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
        _btnClose = new Label
        {
            Text = IconRegistry.Close,
            Size = new Size(34, 34),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = IconRegistry.Icon(13f),
            ForeColor = Color.White,
            BackColor = UiTheme.Primary,
            Cursor = Cursors.Hand
        };
        _btnClose.Click += (_, _) => Application.Exit();
        Controls.Add(_btnClose);

        _card = new Panel
        {
            Size = new Size(CardWidth, 558),
            BackColor = UiTheme.Surface,
            Padding = new Padding(36, 26, 36, 22)
        };
        _card.Paint += (_, e) => PaintRoundPanel(e.Graphics, _card.ClientRectangle, 20, UiTheme.Surface, null);
        Controls.Add(_card);

        var emblem = new Panel { Location = new Point(168, 28), Size = new Size(64, 64), BackColor = UiTheme.Surface };
        emblem.Paint += (_, e) => DrawEmblem(e.Graphics, emblem.ClientRectangle);
        _card.Controls.Add(emblem);

        // Title: chiều cao 50px + font 17pt cho an toàn diacritics dưới (như "ệ")
        _card.Controls.Add(new Label
        {
            Text = "Quản lý Bệnh viện",
            Location = new Point(8, 105),
            Size = new Size(CardWidth - 16, 50),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Heading1(17f),
            ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface,
            AutoEllipsis = false,
            UseCompatibleTextRendering = true   // GDI+ cho phép render đúng diacritics
        });

        _card.Controls.Add(new Label
        {
            Text = "Đăng nhập để tiếp tục",
            Location = new Point(8, 156),
            Size = new Size(CardWidth - 16, 24),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Body(10.5f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            UseCompatibleTextRendering = true
        });

        AddFieldLabel("Tài khoản", 190);
        _txtUser = NewInput(false);
        _card.Controls.Add(WrapInput(_txtUser, 36, 214, FieldWidth));

        AddFieldLabel("Mật khẩu", 272);
        _txtPass = NewInput(true);
        _txtPass.ShortcutsEnabled = false;
        _card.Controls.Add(WrapPasswordInput(_txtPass, 36, 296, FieldWidth));

        _lblStatus = new Label
        {
            Location = new Point(36, 350),
            Size = new Size(FieldWidth, 38),
            Font = UiTheme.Body(9f),
            ForeColor = UiTheme.Danger,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = UiTheme.Surface
        };
        _card.Controls.Add(_lblStatus);

        _btnLogin = new Button
        {
            Text = "Đăng nhập",
            Location = new Point(36, 404),
            Size = new Size(FieldWidth, 44),
            BackColor = UiTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Button(11f),
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.FlatAppearance.MouseOverBackColor = UiTheme.PrimaryDark;
        _btnLogin.Click += BtnLogin_Click;
        _btnLogin.Resize += (_, _) => RoundCorners(_btnLogin, 11);
        RoundCorners(_btnLogin, 11);
        _card.Controls.Add(_btnLogin);

        _btnAdvanced = new Button
        {
            Text = "▸  Tùy chọn nâng cao",
            Location = new Point(36, 464),
            Size = new Size(FieldWidth, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.Body(9f),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        _btnAdvanced.FlatAppearance.BorderSize = 0;
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        _card.Controls.Add(_btnAdvanced);

        _advancedPanel = new Panel
        {
            Location = new Point(36, 498),
            Size = new Size(FieldWidth, 148),
            BackColor = UiTheme.Surface,
            Visible = false
        };
        BuildAdvancedPanel();
        _card.Controls.Add(_advancedPanel);

        _card.Controls.Add(new Label
        {
            Text = "© 2026 HospitalApp · ATBM HTTT",
            Location = new Point(36, 512),
            Size = new Size(FieldWidth, 22),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Body(8.5f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            Name = "Footer"
        });
    }

    private void AddFieldLabel(string text, int y)
    {
        _card.Controls.Add(new Label
        {
            Text = text,
            Location = new Point(36, y),
            Size = new Size(FieldWidth, 20),
            Font = UiTheme.LabelBold(9.5f),
            ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface
        });
    }

    private void BuildAdvancedPanel()
    {
        _advancedPanel.Controls.Add(MakeSmallLabel("Host", 0, 0, 154));
        _advancedPanel.Controls.Add(MakeSmallLabel("Port", 174, 0, 154));

        _txtHost = NewInput(false);
        _txtHost.Text = "localhost";
        _advancedPanel.Controls.Add(WrapInput(_txtHost, 0, 22, 154));

        _txtPort = NewInput(false);
        _txtPort.Text = "1521";
        _advancedPanel.Controls.Add(WrapInput(_txtPort, 174, 22, 154));

        _advancedPanel.Controls.Add(MakeSmallLabel("Service / SID", 0, 76, FieldWidth));
        _txtSid = NewInput(false);
        _txtSid.Text = "XEPDB1";
        _advancedPanel.Controls.Add(WrapInput(_txtSid, 0, 98, FieldWidth));
    }

    private static Label MakeSmallLabel(string text, int x, int y, int width) => new()
    {
        Text = text,
        Location = new Point(x, y),
        Size = new Size(width, 18),
        Font = UiTheme.LabelBold(8.5f),
        ForeColor = UiTheme.TextMuted,
        BackColor = UiTheme.Surface
    };

    private static TextBox NewInput(bool isPassword) => new()
    {
        BorderStyle = BorderStyle.None,
        Font = UiTheme.Body(11f),
        PasswordChar = isPassword ? '●' : '\0',
        BackColor = Color.FromArgb(248, 250, 253)
    };

    private static Panel WrapInput(TextBox tb, int x, int y, int width)
    {
        var panel = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, 42),
            Padding = new Padding(14, 9, 14, 8),
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
            // nếu đang hiện → bấm để ẩn (đặt char '●'); ngược lại
            tb.PasswordChar = nowVisible ? '●' : '\0';
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
        _advancedPanel.Visible = _advancedVisible;
        _btnAdvanced.Text = _advancedVisible
            ? "▾  Ẩn tùy chọn nâng cao"
            : "▸  Tùy chọn nâng cao";

        var footer = _card.Controls.Find("Footer", false).FirstOrDefault();
        if (footer != null)
            footer.Location = new Point(36, _advancedVisible ? 654 : 512);

        ClientSize = new Size(500, _advancedVisible ? ExpandedHeight : CollapsedHeight);
        _card.Size = new Size(CardWidth, _advancedVisible ? 690 : 558);
        RoundCorners(this, 18);
        LayoutShell();
    }

    private void LayoutShell()
    {
        _btnClose.Location = new Point(ClientSize.Width - 46, 12);
        _card.Location = new Point((ClientSize.Width - _card.Width) / 2,
            Math.Max(22, (ClientSize.Height - _card.Height) / 2 + 8));
        RoundCorners(this, 18);
        RoundCorners(_card, 20);
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
