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
    private TableLayoutPanel _cardLayout = null!;
    private Panel _advancedPanel = null!;
    private Label _footer = null!;
    private TextBox _txtUser = null!, _txtPass = null!;
    private TextBox _txtHost = null!, _txtPort = null!, _txtSid = null!;
    private Button _btnLogin = null!, _btnAdvanced = null!, _btnTogglePass = null!;
    private Label _lblStatus = null!, _btnClose = null!;
    private bool _advancedVisible;

    // ─── Layout tokens (row heights of the card's TableLayoutPanel) ──────────────
    // Card chrome = card.Padding (top+bottom) = 26 + 22 = 48
    private const int CardChrome   = 48;
    private const int RowEmblem    = 72;
    private const int RowTitle     = 50;
    private const int RowSubtitle  = 24;
    private const int RowGapTop    = UiTheme.Spacing4;   // 16  (emblem block → fields)
    private const int RowFieldLbl  = 22;
    private const int RowInput     = 44;
    private const int RowGapField  = UiTheme.Spacing3;   // 12  (between field groups)
    private const int RowStatus    = 40;
    private const int RowLogin     = 46;
    private const int RowAdvBtn    = 30;
    private const int RowAdvanced  = 150;                // advanced sub-table height
    private const int RowFooter    = 24;

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
        // ── Close (window chrome) — floats over the form, outside the card ────────
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

        // ── Glass card — its size is computed from the layout's preferred height ──
        _card = new Panel
        {
            Size = new Size(CardWidth, CollapsedCardHeight()),
            BackColor = UiTheme.Surface,
            Padding = new Padding(36, 26, 36, 22)
        };
        _card.Paint += (_, e) => PaintRoundPanel(e.Graphics, _card.ClientRectangle, 20, UiTheme.Surface, null);
        Controls.Add(_card);

        // ════════════════════════════════════════════════════════════════════════
        //  ROOT LAYOUT inside the card: a single-column TableLayoutPanel.
        //  EVERY logical block is its OWN row → no two controls ever share a cell,
        //  so overlap / clipping is structurally impossible. AutoScroll on the
        //  layout is the safety net if a future font makes content overflow.
        // ════════════════════════════════════════════════════════════════════════
        _cardLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            BackColor   = UiTheme.Surface,
            ColumnCount = 1,
            RowCount    = 14,
            AutoScroll  = true,
            Margin      = new Padding(0),
            Padding     = new Padding(0)
        };
        _cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        // 0 emblem | 1 title | 2 subtitle | 3 gap | 4 userLbl | 5 userInput
        // 6 passLbl | 7 passInput | 8 status | 9 login | 10 advBtn
        // 11 advanced (collapsible) | 12 footer | 13 slack (absorbs leftover)
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowEmblem));
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
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));               // advanced (0 when hidden)
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, RowFooter));
        _cardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));          // slack absorber
        _card.Controls.Add(_cardLayout);

        // ── Row 0: emblem (centered, drawn) ──────────────────────────────────────
        var emblem = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = new Padding(0) };
        emblem.Paint += (_, e) => DrawEmblem(e.Graphics, emblem.ClientRectangle);
        _cardLayout.Controls.Add(emblem, 0, 0);

        // ── Row 1: title ─────────────────────────────────────────────────────────
        _cardLayout.Controls.Add(new Label
        {
            Text = "Quản lý Bệnh viện",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Heading1(17f),
            ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface,
            AutoEllipsis = false,
            Margin = new Padding(0),
            UseCompatibleTextRendering = true   // GDI+ render đúng diacritics ("ệ")
        }, 0, 1);

        // ── Row 2: subtitle ──────────────────────────────────────────────────────
        _cardLayout.Controls.Add(new Label
        {
            Text = "Đăng nhập để tiếp tục",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Body(10.5f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            UseCompatibleTextRendering = true
        }, 0, 2);

        // ── Row 3: gap (empty) — handled by RowStyle height ──────────────────────

        // ── Rows 4-5: username ───────────────────────────────────────────────────
        AddFieldLabel("Tài khoản", 4);
        _txtUser = NewInput(false);
        _cardLayout.Controls.Add(WrapInput(_txtUser, 0, 0, FieldWidth), 0, 5);

        // ── Rows 6-7: password ───────────────────────────────────────────────────
        AddFieldLabel("Mật khẩu", 6);
        _txtPass = NewInput(true);
        _txtPass.ShortcutsEnabled = false;
        _cardLayout.Controls.Add(WrapPasswordInput(_txtPass, 0, 0, FieldWidth), 0, 7);

        // ── Row 8: status / error message ────────────────────────────────────────
        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Body(9f),
            ForeColor = UiTheme.Danger,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = UiTheme.Surface,
            Margin = new Padding(2, 0, 2, 0)
        };
        _cardLayout.Controls.Add(_lblStatus, 0, 8);

        // ── Row 9: login button ──────────────────────────────────────────────────
        _btnLogin = new Button
        {
            Text = "Đăng nhập",
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Primary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Button(11f),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 2)
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.FlatAppearance.MouseOverBackColor = UiTheme.PrimaryDark;
        _btnLogin.Click += BtnLogin_Click;
        _btnLogin.Resize += (_, _) => RoundCorners(_btnLogin, 11);
        RoundCorners(_btnLogin, 11);
        _cardLayout.Controls.Add(_btnLogin, 0, 9);

        // ── Row 10: advanced toggle ──────────────────────────────────────────────
        _btnAdvanced = new Button
        {
            Text = "▸  Tùy chọn nâng cao",
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextMuted,
            Font = UiTheme.Body(9f),
            Cursor = Cursors.Hand,
            TabStop = false,
            Margin = new Padding(0)
        };
        _btnAdvanced.FlatAppearance.BorderSize = 0;
        _btnAdvanced.Click += (_, _) => ToggleAdvanced();
        _cardLayout.Controls.Add(_btnAdvanced, 0, 10);

        // ── Row 11: collapsible advanced panel (its own row → never overlaps) ─────
        _advancedPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Height = RowAdvanced,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            Visible = false
        };
        BuildAdvancedPanel();
        _cardLayout.Controls.Add(_advancedPanel, 0, 11);

        // ── Row 12: footer (its OWN row — cannot be covered by the advanced block) ─
        _footer = new Label
        {
            Text = "© 2026 HospitalApp · ATBM HTTT",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Body(8.5f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0),
            Name = "Footer"
        };
        _cardLayout.Controls.Add(_footer, 0, 12);
    }

    // Card height when advanced panel is hidden / shown — derived from row tokens
    // so the form, card and layout can never disagree (the source of the old overlap).
    private static int CollapsedCardHeight() =>
        CardChrome + RowEmblem + RowTitle + RowSubtitle + RowGapTop +
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
            TextAlign = ContentAlignment.BottomLeft,
            Font = UiTheme.LabelBold(9.5f),
            ForeColor = UiTheme.TextDark,
            BackColor = UiTheme.Surface,
            Margin = new Padding(2, 0, 2, 2)
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
        PasswordChar = isPassword ? '●' : '\0',
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
        ClientSize = new Size(500, _advancedVisible ? ExpandedHeight : CollapsedHeight);
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
