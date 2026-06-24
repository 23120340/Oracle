using System.Data;
using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Admin;

/// <summary>
/// Phân hệ 1: Ứng dụng Quản trị CSDL Oracle
/// Tabs: Users | Roles | Grant | Revoke | View Privileges
/// </summary>
public class AdminDashboard : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private TabControl _tabs = null!;

    // ── User tab controls ──────────────────────────────────────────────────────
    private DataGridView _dgvUsers   = null!;
    private TextBox _txtNewUser      = null!, _txtNewPass = null!;
    private Button _btnCreateUser    = null!, _btnDropUser = null!,
                   _btnLockUser      = null!, _btnUnlockUser = null!,
                   _btnRefreshUsers  = null!;

    // ── Role tab controls ──────────────────────────────────────────────────────
    private DataGridView _dgvRoles   = null!;
    private TextBox _txtNewRole      = null!;
    private Button _btnCreateRole    = null!, _btnDropRole = null!,
                   _btnRefreshRoles  = null!;

    // ── Grant tab controls ─────────────────────────────────────────────────────
    private ComboBox _cmbGrantee     = null!, _cmbGrantType = null!,
                     _cmbObjType     = null!, _cmbPrivilege = null!,
                     _cmbObjSchema   = null!, _cmbObjName   = null!,
                     _cmbSysPriv     = null!, _cmbGrantRole = null!;
    private CheckedListBox _clbColumns = null!;
    private CheckBox _chkGrantOption = null!;
    private Button _btnGrant         = null!;
    private Label _lblColNote        = null!;
    private Panel _pnlObjectPriv     = null!, _pnlSysPriv = null!, _pnlRole = null!;

    // ── Revoke tab controls ────────────────────────────────────────────────────
    private ComboBox _cmbRevokeFrom  = null!;
    private DataGridView _dgvGranted = null!;
    private Button _btnRevoke        = null!, _btnLoadGranted = null!;

    // ── View Privileges tab ────────────────────────────────────────────────────
    private ComboBox _cmbViewTarget  = null!;
    private TabControl _tabPrivDetail= null!;
    private DataGridView _dgvSysPrivs= null!, _dgvObjPrivs = null!,
                         _dgvColPrivs= null!, _dgvRolePrivs= null!;
    private Button _btnViewRefresh   = null!;

    public AdminDashboard(OracleHelper db)
    {
        _db   = db;
        Text  = $"Quản trị CSDL Oracle – {db.Username}@{db.Host}/{db.Sid}";
        Size  = new Size(1200, 780);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize   = new Size(1000, 650);
        BackColor     = UiTheme.BgLight;
        Font          = UiTheme.Body();

        BuildUI();

        // DBA có nhiều quyền → idle timeout ngắn hơn (5 phút)
        _session = new SessionManager(this, db.Username, TimeSpan.FromMinutes(5));
        FormClosed += (_, _) => _session.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UI BUILDER — Sidebar + KPI + Status bar
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        // Hidden TabControl: chứa nội dung 6 tab, sidebar control SelectedIndex
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = UiTheme.Body(),
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(0, 1)  // ẩn tab headers
        };
        _tabs.TabPages.Add(BuildUserTab());
        _tabs.TabPages.Add(BuildRoleTab());
        _tabs.TabPages.Add(BuildGrantTab());
        _tabs.TabPages.Add(BuildRevokeTab());
        _tabs.TabPages.Add(BuildViewPrivTab());
        _tabs.TabPages.Add(BuildAuditTab());

        // KPI row
        var kpiRow = BuildKpiRow();

        // Top header bar
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTheme.Surface,
            Padding = new Padding(24, 12, 24, 12)
        };
        var title = new Label
        {
            Text = "Quản trị Cơ sở dữ liệu",
            Dock = DockStyle.Left, Width = 420,
            Font = UiTheme.Heading1(16f),
            ForeColor = UiTheme.TextDark,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        var roleChip = new RoleChip
        {
            Text = "DBA",
            AccentColor = UiTheme.Primary,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        var btnLogout = new RoundedButton
        {
            Text = "Đăng xuất",
            Glyph = IconRegistry.SignOut,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.TextDark,
            GlyphColor = UiTheme.Danger,
            BorderThickness = 1, BorderTint = UiTheme.BorderStrong,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Width = 152, Height = 38
        };
        btnLogout.Click += (_, _) =>
        {
            if (MessageBox.Show("Đăng xuất khỏi hệ thống?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        };
        void layoutHeader()
        {
            btnLogout.Location = new Point(header.Width - btnLogout.Width - 16, 12);
            roleChip.Location  = new Point(btnLogout.Left - roleChip.Width - 12, 17);
            title.Width = Math.Max(160, roleChip.Left - title.Left - 16);
        }
        header.Resize += (_, _) => layoutHeader();
        roleChip.HandleCreated += (_, _) => layoutHeader();
        header.Controls.Add(roleChip);
        header.Controls.Add(btnLogout);
        header.Controls.Add(title);
        header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiTheme.Border });
        layoutHeader();

        // Sidebar
        var sidebar = new Sidebar
        {
            AccentColor = UiTheme.HealthCyanLight,
            Dock = DockStyle.Left
        };
        sidebar.AddBrand("HospitalApp", $"Quản trị · {_db.Username}");
        sidebar.AddSection("Quản lý");
        sidebar.AddItem("users",  IconRegistry.People,    "Người dùng");
        sidebar.AddItem("roles",  IconRegistry.Tag,       "Vai trò");
        sidebar.AddSection("Quyền truy cập");
        sidebar.AddItem("grant",  IconRegistry.Key,       "Cấp quyền");
        sidebar.AddItem("revoke", IconRegistry.Lock,      "Thu hồi");
        sidebar.AddItem("view",   IconRegistry.Document,  "Xem quyền");
        sidebar.AddSection("Hệ thống");
        sidebar.AddItem("audit",  IconRegistry.Shield,    "Nhật ký audit");

        sidebar.ItemSelected += key =>
        {
            _tabs.SelectedIndex = key switch
            {
                "users"  => 0, "roles" => 1, "grant" => 2,
                "revoke" => 3, "view"  => 4, "audit" => 5,
                _ => 0
            };
        };

        // Status bar
        var status = new StatusBar
        {
            LeftText   = $"{_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"Đã đăng nhập: {_db.Username}  ·  Vai trò: DBA"
        };

        // Add theo thứ tự: Fill trước, Dock zone sau (later dock = inner)
        Controls.Add(_tabs);
        Controls.Add(kpiRow);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("users");
    }

    private Panel BuildKpiRow()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top, Height = 148,           // tăng để chứa card 118 + padding
            BackColor = UiTheme.BgLight,
            Padding = new Padding(24, 12, 24, 12)
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = UiTheme.BgLight
        };

        var kpiUsers = new KpiCard
        {
            Glyph = IconRegistry.People, GlyphColor = UiTheme.HealthCyan,
            Label = "Người dùng", Value = "—", Subtext = "tổng số tài khoản",
            Width = 248, Height = 118,
            Margin = new Padding(0, 0, 14, 0)
        };
        var kpiRoles = new KpiCard
        {
            Glyph = IconRegistry.Tag, GlyphColor = UiTheme.HealthEmerald,
            Label = "Vai trò", Value = "—", Subtext = "roles đã định nghĩa",
            Width = 248, Height = 118,
            Margin = new Padding(0, 0, 14, 0)
        };
        var kpiGrants = new KpiCard
        {
            Glyph = IconRegistry.Key, GlyphColor = UiTheme.StatusWarning,
            Label = "Cấp quyền", Value = "—", Subtext = "object/role privileges",
            Width = 248, Height = 118,
            Margin = new Padding(0, 0, 14, 0)
        };
        var kpiAudit = new KpiCard
        {
            Glyph = IconRegistry.Shield, GlyphColor = UiTheme.StatusDanger,
            Label = "Audit hôm nay", Value = "—", Subtext = "lượt thao tác đã ghi",
            Width = 248, Height = 118,
            Margin = new Padding(0, 0, 14, 0)
        };

        flow.Controls.AddRange(new Control[] { kpiUsers, kpiRoles, kpiGrants, kpiAudit });
        panel.Controls.Add(flow);

        void resizeCards()
        {
            var cards = new[] { kpiUsers, kpiRoles, kpiGrants, kpiAudit };
            var totalMargins = cards.Sum(c => c.Margin.Left + c.Margin.Right);
            var w = Math.Max(168, (flow.ClientSize.Width - totalMargins - 6) / cards.Length);
            foreach (var card in cards) card.Width = w;
        }
        flow.Resize += (_, _) => resizeCards();
        panel.Resize += (_, _) => resizeCards();
        resizeCards();

        // Refresh KPI numbers khi form shown
        Shown += (_, _) => RefreshKpis(kpiUsers, kpiRoles, kpiGrants, kpiAudit);
        return panel;
    }

    private void RefreshKpis(KpiCard kU, KpiCard kR, KpiCard kG, KpiCard kA)
    {
        TryCatch(() =>
        {
            try
            {
                kU.Value = _db.Scalar("SELECT COUNT(*) FROM DBA_USERS")?.ToString() ?? "—";
                kR.Value = _db.Scalar("SELECT COUNT(*) FROM DBA_ROLES")?.ToString() ?? "—";
                kG.Value = _db.Scalar("SELECT COUNT(*) FROM DBA_TAB_PRIVS WHERE GRANTEE NOT IN ('SYS','SYSTEM','PUBLIC')")?.ToString() ?? "—";
                kA.Value = _db.Scalar("SELECT COUNT(*) FROM DBA_AUDIT_TRAIL WHERE TRUNC(TIMESTAMP)=TRUNC(SYSDATE)")?.ToString() ?? "—";
            }
            catch { /* may not have full DBA privs, ignore */ }
            kU.Invalidate(); kR.Invalidate(); kG.Invalidate(); kA.Invalidate();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 6 (NEW): AUDIT LOG VIEWER — Đọc DBA_AUDIT_TRAIL + APP_LOGIN_LOG
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildAuditTab()
    {
        var page = new TabPage("Audit");
        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };

        var card = new Card
        {
            Dock = DockStyle.Fill,
            ShowShadow = true,
            Padding = new Padding(16)
        };

        var titleRow = new Panel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0, 0, 0, 12)
        };
        var lblTitle = new Label
        {
            Text = "Nhật ký kiểm toán hệ thống",
            Dock = DockStyle.Left, Width = 520,
            Font = UiTheme.Heading3(),
            TextAlign = ContentAlignment.MiddleLeft
        };
        var btnRefresh = new RoundedButton
        {
            Text = "Làm mới", Glyph = IconRegistry.Refresh,
            BackColor = UiTheme.HealthCyan,
            Width = 130, Height = 40,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var grid = UiTheme.Grid();
        grid.Dock = DockStyle.Fill;

        btnRefresh.Click += (_, _) => TryCatch(() =>
        {
            grid.DataSource = _db.Query(
                "SELECT TO_CHAR(TIMESTAMP, 'DD/MM HH24:MI:SS') AS TIME, " +
                "USERNAME, OBJ_NAME AS OBJECT, ACTION_NAME AS ACTION, " +
                "DECODE(RETURNCODE, 0, 'OK', 'FAIL') AS RESULT " +
                "FROM DBA_AUDIT_TRAIL " +
                "WHERE TIMESTAMP > SYSDATE - 7 " +
                "ORDER BY TIMESTAMP DESC " +
                "FETCH FIRST 200 ROWS ONLY");
        });

        titleRow.Controls.Add(lblTitle);
        titleRow.Controls.Add(btnRefresh);
        void layoutAuditHeader()
        {
            btnRefresh.Location = new Point(
                Math.Max(0, titleRow.ClientSize.Width - btnRefresh.Width - 8),
                8);
            lblTitle.Width = Math.Max(180, titleRow.ClientSize.Width - btnRefresh.Width - 24);
        }
        titleRow.Resize += (_, _) => layoutAuditHeader();
        layoutAuditHeader();
        card.Controls.Add(grid);
        card.Controls.Add(titleRow);
        container.Controls.Add(card);
        page.Controls.Add(container);
        page.Enter += (_, _) => btnRefresh.PerformClick();
        return page;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildUserTab()
    {
        var page = new TabPage("Users");

        _dgvUsers = MakeGrid(new[]
        {
            "USERNAME", "ACCOUNT_STATUS", "DEFAULT_TABLESPACE",
            "CREATED", "EXPIRY_DATE"
        });
        _dgvUsers.Dock = DockStyle.Fill;
        _dgvUsers.Margin = Padding.Empty;

        var panel = new Panel { Dock = DockStyle.Fill, Height = 132, Padding = new Padding(14, 12, 14, 12), BackColor = UiTheme.BgLight };
        panel.Margin = Padding.Empty;

        var row1 = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Top,
            Height        = 50,
            Padding       = new Padding(0, 2, 0, 4),
            WrapContents  = false,
            AutoScroll    = true
        };
        row1.Controls.Add(Label("Username mới:"));
        _txtNewUser = TB(150); row1.Controls.Add(_txtNewUser);
        row1.Controls.Add(Label("Password:"));
        _txtNewPass = TB(120, isPass: true); row1.Controls.Add(_txtNewPass);
        _btnCreateUser = Btn("+ Tạo User", UiTheme.HealthGreen, 140);
        _btnCreateUser.Click += BtnCreateUser_Click;
        row1.Controls.Add(_btnCreateUser);

        var row2 = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Fill,
            Padding       = new Padding(0, 4, 0, 0),
            WrapContents  = false,
            AutoScroll    = true
        };
        _btnDropUser    = Btn("Drop User", UiTheme.Danger, 130);       _btnDropUser.Click    += BtnDropUser_Click;
        _btnLockUser    = Btn("Lock", UiTheme.StatusWarning, 105);     _btnLockUser.Click    += (_, _) => LockUser(true);
        _btnUnlockUser  = Btn("Unlock", UiTheme.HealthCyan, 115);      _btnUnlockUser.Click  += (_, _) => LockUser(false);
        _btnRefreshUsers= Btn("Refresh", UiTheme.Primary, 120);        _btnRefreshUsers.Click+= (_, _) => LoadUsers();
        row2.Controls.AddRange(new Control[] { _btnDropUser, _btnLockUser, _btnUnlockUser, _btnRefreshUsers });

        panel.Controls.AddRange(new Control[] { row2, row1 });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(panel, 0, 0);
        layout.Controls.Add(_dgvUsers, 0, 1);
        page.Controls.Add(layout);

        page.Enter += (_, _) => LoadUsers();
        return page;
    }

    private void LoadUsers()
    {
        TryCatch(() =>
        {
            var dt = _db.Query(
                "SELECT USERNAME, ACCOUNT_STATUS, DEFAULT_TABLESPACE, " +
                "TO_CHAR(CREATED,'DD/MM/YYYY') AS CREATED, " +
                "TO_CHAR(EXPIRY_DATE,'DD/MM/YYYY') AS EXPIRY_DATE " +
                "FROM DBA_USERS " +
                "WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','DIP','ORACLE_OCM'," +
                "'XDB','ANONYMOUS','APEX_040000','DBSNMP','WMSYS','EXFSYS') " +
                "ORDER BY USERNAME");
            _dgvUsers.DataSource = dt;
        });
    }

    private void BtnCreateUser_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = OracleHelper.SafeIdentifier(_txtNewUser.Text);
            var pass = _txtNewPass.Text;
            if (string.IsNullOrEmpty(pass)) { Error("Nhập password."); return; }

            _db.Execute($"CREATE USER {name} IDENTIFIED BY \"{pass}\" " +
                        $"DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS");
            _db.Execute($"GRANT CREATE SESSION TO {name}");
            Success($"Đã tạo user '{name}' và cấp CREATE SESSION.");
            _txtNewUser.Clear(); _txtNewPass.Clear();
            LoadUsers();
        });
    }

    private void BtnDropUser_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvUsers, "USERNAME"); if (name is null) return;
            if (MessageBox.Show($"Xóa user '{name}'? (CASCADE)", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _db.Execute($"DROP USER {name} CASCADE");
            Success($"Đã xóa user '{name}'."); LoadUsers();
        });
    }

    private void LockUser(bool doLock)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvUsers, "USERNAME"); if (name is null) return;
            var action = doLock ? "ACCOUNT LOCK" : "ACCOUNT UNLOCK";
            _db.Execute($"ALTER USER {name} {action}");
            Success($"Đã {(doLock ? "khóa" : "mở khóa")} user '{name}'."); LoadUsers();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: ROLE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildRoleTab()
    {
        var page = new TabPage("Roles");

        _dgvRoles = MakeGrid(new[] { "ROLE", "AUTHENTICATION_TYPE", "COMMON" });
        _dgvRoles.Dock = DockStyle.Fill;
        _dgvRoles.Margin = Padding.Empty;

        var panel = new Panel { Dock = DockStyle.Fill, Height = 76, Padding = new Padding(14, 12, 14, 12), BackColor = UiTheme.BgLight };
        panel.Margin = Padding.Empty;
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Fill,
            WrapContents  = false,
            AutoScroll    = true
        };
        row.Controls.Add(Label("Tên role mới:"));
        _txtNewRole = TB(150); row.Controls.Add(_txtNewRole);
        _btnCreateRole  = Btn("+ Tạo Role", UiTheme.HealthGreen, 140);
        _btnDropRole    = Btn("Drop Role", UiTheme.Danger, 130);
        _btnRefreshRoles= Btn("Refresh", UiTheme.Primary, 120);
        _btnCreateRole.Click   += BtnCreateRole_Click;
        _btnDropRole.Click     += BtnDropRole_Click;
        _btnRefreshRoles.Click += (_, _) => LoadRoles();
        row.Controls.AddRange(new Control[] { _btnCreateRole, _btnDropRole, _btnRefreshRoles });
        panel.Controls.Add(row);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(panel, 0, 0);
        layout.Controls.Add(_dgvRoles, 0, 1);
        page.Controls.Add(layout);
        page.Enter += (_, _) => LoadRoles();
        return page;
    }

    private void LoadRoles()
    {
        TryCatch(() =>
        {
            _dgvRoles.DataSource = _db.Query(
                "SELECT ROLE, AUTHENTICATION_TYPE, COMMON " +
                "FROM DBA_ROLES ORDER BY ROLE");
        });
    }

    private void BtnCreateRole_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = OracleHelper.SafeIdentifier(_txtNewRole.Text);
            _db.Execute($"CREATE ROLE {name}");
            Success($"Đã tạo role '{name}'."); _txtNewRole.Clear(); LoadRoles();
        });
    }

    private void BtnDropRole_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvRoles, "ROLE"); if (name is null) return;
            if (MessageBox.Show($"Xóa role '{name}'?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _db.Execute($"DROP ROLE {name}");
            Success($"Đã xóa role '{name}'."); LoadRoles();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: GRANT PRIVILEGES
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildGrantTab()
    {
        var page = new TabPage("Grant") { BackColor = UiTheme.BgLight };

        // Helper cục bộ — style nhất quán, tránh lặp.
        Panel GrantCard() => new()
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = UiTheme.Surface, Padding = new Padding(UiTheme.Spacing4),
            Margin = new Padding(0, 0, 0, UiTheme.Spacing4)
        };
        Label GLbl(string t) => new()
        {
            Text = t, AutoSize = true, Font = UiTheme.LabelBold(9f),
            ForeColor = UiTheme.TextDark, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, UiTheme.Spacing2, 0)
        };
        Label GTitle(string t) => new()
        {
            Text = t, AutoSize = true, Font = UiTheme.Heading3(10.5f),
            ForeColor = UiTheme.Primary, Margin = new Padding(0, 0, 0, UiTheme.Spacing2)
        };
        void Stretch(ComboBox c)
        {
            c.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            c.Margin = new Padding(0, 4, UiTheme.Spacing2, 4);
        }

        // Khung dọc: [grantee + loại] + 1 trong 3 thẻ quyền (toggled Visible -> hàng AutoSize tự co).
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(UiTheme.Spacing5, UiTheme.Spacing4, UiTheme.Spacing5, UiTheme.Spacing4),
            BackColor = UiTheme.BgLight, AutoScroll = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 0 head
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 1 object priv
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 2 system priv
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // 3 role
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // 4 slack

        // ── Head: chọn grantee + loại quyền ──
        var head = GrantCard();
        var headTbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2, RowCount = 2, BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        headTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        headTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
        headTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
        headTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        headTbl.Controls.Add(GLbl("Cấp cho (User / Role)"), 0, 0);
        headTbl.Controls.Add(GLbl("Loại quyền"), 1, 0);
        _cmbGrantee = Cmb(0, 0, 200); Stretch(_cmbGrantee);
        headTbl.Controls.Add(_cmbGrantee, 0, 1);
        _cmbGrantType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() };
        _cmbGrantType.Items.AddRange(new[] { "Object Privilege", "System Privilege", "Role" });
        Stretch(_cmbGrantType);
        _cmbGrantType.SelectedIndex = 0;
        _cmbGrantType.SelectedIndexChanged += CmbGrantType_Changed;
        headTbl.Controls.Add(_cmbGrantType, 1, 1);
        head.Controls.Add(headTbl);
        root.Controls.Add(head, 0, 0);

        // ── Object Privilege card ──
        _pnlObjectPriv = GrantCard();
        var objTbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 6, BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        objTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        objTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // title
        objTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // obj selection
        objTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));  // priv + grant option
        objTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // col note
        objTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 120)); // columns list
        objTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));      // grant button
        objTbl.Controls.Add(GTitle("Object Privilege"), 0, 0);

        // Hàng chọn đối tượng (6 cột: nhãn/combo × 3)
        var objSel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 1,
            BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        objSel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        objSel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        _cmbObjType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() };
        _cmbObjType.Items.AddRange(new[] { "TABLE", "VIEW", "PROCEDURE", "FUNCTION" });
        Stretch(_cmbObjType);
        _cmbObjType.SelectedIndex = 0;
        _cmbObjType.SelectedIndexChanged += CmbObjType_Changed;
        _cmbObjSchema = Cmb(0, 0, 190); Stretch(_cmbObjSchema);
        _cmbObjSchema.SelectedIndexChanged += LoadObjectNames;
        _cmbObjName = Cmb(0, 0, 240); Stretch(_cmbObjName);
        _cmbObjName.SelectedIndexChanged += LoadColumns;
        objSel.Controls.Add(GLbl("Loại đối tượng"), 0, 0);
        objSel.Controls.Add(_cmbObjType, 1, 0);
        objSel.Controls.Add(GLbl("Schema"), 2, 0);
        objSel.Controls.Add(_cmbObjSchema, 3, 0);
        objSel.Controls.Add(GLbl("Đối tượng"), 4, 0);
        objSel.Controls.Add(_cmbObjName, 5, 0);
        objTbl.Controls.Add(objSel, 0, 1);

        // Hàng quyền + WITH GRANT OPTION (tạo _clbColumns/_lblColNote TRƯỚC khi
        // UpdatePrivilegesForObjectType() vì nó set SelectedIndex -> kích CmbPrivilege_Changed).
        _lblColNote = new Label
        {
            Text = "Cột (SELECT/UPDATE mới chọn được cột – bỏ trống = tất cả):",
            AutoSize = true, ForeColor = Color.DimGray, Anchor = AnchorStyles.Left,
            Margin = new Padding(0, UiTheme.Spacing2, 0, 2)
        };
        _clbColumns = new CheckedListBox
        {
            Dock = DockStyle.Fill, CheckOnClick = true, ScrollAlwaysVisible = false,
            Margin = new Padding(0, 0, 0, UiTheme.Spacing2), Font = UiTheme.Body()
        };
        var privRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        privRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        privRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        privRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        privRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _cmbPrivilege = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() };
        Stretch(_cmbPrivilege);
        _cmbPrivilege.SelectedIndexChanged += CmbPrivilege_Changed;
        _chkGrantOption = new CheckBox { Text = "WITH GRANT OPTION", AutoSize = true,
                                         Font = UiTheme.Body(), Anchor = AnchorStyles.Left };
        privRow.Controls.Add(GLbl("Quyền"), 0, 0);
        privRow.Controls.Add(_cmbPrivilege, 1, 0);
        privRow.Controls.Add(_chkGrantOption, 2, 0);
        objTbl.Controls.Add(privRow, 0, 2);
        UpdatePrivilegesForObjectType();

        objTbl.Controls.Add(_lblColNote, 0, 3);
        objTbl.Controls.Add(_clbColumns, 0, 4);

        _btnGrant = Btn("Thực hiện GRANT", UiTheme.HealthGreen, width: 190);
        _btnGrant.Anchor = AnchorStyles.Left;
        _btnGrant.Margin = new Padding(0, UiTheme.Spacing2, 0, 0);
        _btnGrant.Click += BtnGrant_Click;
        objTbl.Controls.Add(_btnGrant, 0, 5);
        _pnlObjectPriv.Controls.Add(objTbl);
        root.Controls.Add(_pnlObjectPriv, 0, 1);

        // ── System Privilege card ──
        _pnlSysPriv = GrantCard();
        _pnlSysPriv.Visible = false;
        var sysTbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        sysTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sysTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sysTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        sysTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sysTbl.Controls.Add(GTitle("System Privilege"), 0, 0);
        var sysRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
            BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        sysRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sysRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        sysRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        sysRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _cmbSysPriv = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() };
        _cmbSysPriv.Items.AddRange(new[]
        {
            "CREATE SESSION","CREATE TABLE","CREATE VIEW","CREATE PROCEDURE",
            "CREATE TRIGGER","CREATE SEQUENCE","CREATE USER","DROP USER",
            "ALTER USER","GRANT ANY PRIVILEGE","DBA","CONNECT","RESOURCE"
        });
        Stretch(_cmbSysPriv);
        var chkAdminOpt = new CheckBox { Text = "WITH ADMIN OPTION", AutoSize = true,
                                         Anchor = AnchorStyles.Left, Font = UiTheme.Body() };
        sysRow.Controls.Add(GLbl("Quyền hệ thống"), 0, 0);
        sysRow.Controls.Add(_cmbSysPriv, 1, 0);
        sysRow.Controls.Add(chkAdminOpt, 2, 0);
        sysTbl.Controls.Add(sysRow, 0, 1);
        var btnGrantSys = Btn("GRANT", UiTheme.HealthGreen);
        btnGrantSys.Anchor = AnchorStyles.Left;
        btnGrantSys.Margin = new Padding(0, UiTheme.Spacing2, 0, 0);
        btnGrantSys.Click += (_, _) =>
        {
            TryCatch(() =>
            {
                var grantee  = _cmbGrantee.Text.Trim().ToUpper();
                var priv     = _cmbSysPriv.Text;
                var adminOpt = chkAdminOpt.Checked ? " WITH ADMIN OPTION" : "";
                _db.Execute($"GRANT {priv} TO {grantee}{adminOpt}");
                Success($"GRANT {priv} TO {grantee} thành công.");
            });
        };
        sysTbl.Controls.Add(btnGrantSys, 0, 2);
        _pnlSysPriv.Controls.Add(sysTbl);
        root.Controls.Add(_pnlSysPriv, 0, 2);

        // ── Role grant card ──
        _pnlRole = GrantCard();
        _pnlRole.Visible = false;
        var roleTbl = new TableLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1, RowCount = 3, BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        roleTbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        roleTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        roleTbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        roleTbl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        roleTbl.Controls.Add(GTitle("Grant Role"), 0, 0);
        var roleRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = UiTheme.Surface, Margin = Padding.Empty
        };
        roleRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        roleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        roleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _cmbGrantRole = Cmb(0, 0, 200); Stretch(_cmbGrantRole);
        roleRow.Controls.Add(GLbl("Role"), 0, 0);
        roleRow.Controls.Add(_cmbGrantRole, 1, 0);
        roleTbl.Controls.Add(roleRow, 0, 1);
        var btnGrantRole = Btn("GRANT ROLE", UiTheme.HealthGreen, 150);
        btnGrantRole.Anchor = AnchorStyles.Left;
        btnGrantRole.Margin = new Padding(0, UiTheme.Spacing2, 0, 0);
        btnGrantRole.Click += (_, _) =>
        {
            TryCatch(() =>
            {
                var grantee = _cmbGrantee.Text.Trim().ToUpper();
                var role    = _cmbGrantRole.Text.Trim().ToUpper();
                _db.Execute($"GRANT {role} TO {grantee}");
                Success($"GRANT {role} TO {grantee} thành công.");
            });
        };
        roleTbl.Controls.Add(btnGrantRole, 0, 2);
        _pnlRole.Controls.Add(roleTbl);
        root.Controls.Add(_pnlRole, 0, 3);

        page.Controls.Add(root);

        page.Enter += (_, _) =>
        {
            RefreshGrantees();
            LoadSchemas();
            LoadRolesForGrant();
        };
        return page;
    }

    private void CmbGrantType_Changed(object? s, EventArgs e)
    {
        _pnlObjectPriv.Visible = _cmbGrantType.SelectedIndex == 0;
        _pnlSysPriv.Visible    = _cmbGrantType.SelectedIndex == 1;
        _pnlRole.Visible       = _cmbGrantType.SelectedIndex == 2;
    }

    private void CmbObjType_Changed(object? s, EventArgs e)
    {
        UpdatePrivilegesForObjectType();
        LoadObjectNames(s, e);
    }

    private void UpdatePrivilegesForObjectType()
    {
        _cmbPrivilege.Items.Clear();
        var type = _cmbObjType?.Text ?? "TABLE";
        if (type is "TABLE" or "VIEW")
            _cmbPrivilege.Items.AddRange(new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "REFERENCES" });
        else
            _cmbPrivilege.Items.AddRange(new[] { "EXECUTE" });
        if (_cmbPrivilege.Items.Count > 0) _cmbPrivilege.SelectedIndex = 0;
    }

    private void CmbPrivilege_Changed(object? s, EventArgs e)
    {
        // Guard: handler có thể fire khi BuildGrantTab() set SelectedIndex
        // TRƯỚC khi _clbColumns/_lblColNote được khởi tạo.
        if (_clbColumns is null || _lblColNote is null) return;

        var priv = _cmbPrivilege.Text;
        bool canCol = priv is "SELECT" or "UPDATE";
        _clbColumns.Enabled = canCol;
        _lblColNote.ForeColor = canCol ? Color.DimGray : Color.LightGray;
    }

    private void LoadObjectNames(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            _cmbObjName.Items.Clear();
            _clbColumns.Items.Clear();
            var schema = _cmbObjSchema.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(schema)) return;

            var objType = _cmbObjType.Text switch
            {
                "TABLE"     => "'TABLE'",
                "VIEW"      => "'VIEW'",
                "PROCEDURE" => "'PROCEDURE'",
                "FUNCTION"  => "'FUNCTION'",
                _           => "'TABLE'"
            };

            var dt = _db.Query(
                $"SELECT OBJECT_NAME FROM DBA_OBJECTS " +
                $"WHERE OWNER = '{schema}' AND OBJECT_TYPE = {objType} " +
                $"ORDER BY OBJECT_NAME");
            foreach (DataRow row in dt.Rows)
                _cmbObjName.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadColumns(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            _clbColumns.Items.Clear();
            var schema = _cmbObjSchema.Text.Trim().ToUpper();
            var obj    = _cmbObjName.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(obj)) return;

            var dt = _db.Query(
                "SELECT COLUMN_NAME FROM DBA_TAB_COLUMNS " +
                $"WHERE OWNER = '{schema}' AND TABLE_NAME = '{obj}' " +
                "ORDER BY COLUMN_ID");
            foreach (DataRow row in dt.Rows)
                _clbColumns.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadSchemas()
    {
        TryCatch(() =>
        {
            _cmbObjSchema.Items.Clear();
            var dt = _db.Query(
                "SELECT DISTINCT OWNER FROM DBA_OBJECTS " +
                "WHERE OWNER NOT IN ('SYS','SYSTEM','OUTLN','XDB') " +
                "ORDER BY OWNER");
            foreach (DataRow row in dt.Rows)
                _cmbObjSchema.Items.Add(row[0].ToString()!);
        });
    }

    private void RefreshGrantees()
    {
        TryCatch(() =>
        {
            _cmbGrantee.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME AS NAME FROM DBA_USERS " +
                "WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB') " +
                "UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows)
                _cmbGrantee.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadRolesForGrant()
    {
        TryCatch(() =>
        {
            _cmbGrantRole.Items.Clear();
            var dt = _db.Query("SELECT ROLE FROM DBA_ROLES ORDER BY ROLE");
            foreach (DataRow row in dt.Rows)
                _cmbGrantRole.Items.Add(row[0].ToString()!);
        });
    }

    private void BtnGrant_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var grantee   = _cmbGrantee.Text.Trim().ToUpper();
            var schema    = _cmbObjSchema.Text.Trim().ToUpper();
            var obj       = _cmbObjName.Text.Trim().ToUpper();
            var priv      = _cmbPrivilege.Text.Trim().ToUpper();
            var withGrant = _chkGrantOption.Checked ? " WITH GRANT OPTION" : "";

            if (string.IsNullOrEmpty(grantee) || string.IsNullOrEmpty(obj) || string.IsNullOrEmpty(priv))
            { Error("Vui lòng chọn đủ Grantee, Object và Privilege."); return; }

            // Xây chuỗi cột (nếu có chọn và privilege hỗ trợ column-level)
            var cols = new List<string>();
            if (priv is "SELECT" or "UPDATE")
            {
                foreach (int i in _clbColumns.CheckedIndices)
                    cols.Add(_clbColumns.Items[i].ToString()!);
            }

            string sql;
            if (cols.Count > 0)
                sql = $"GRANT {priv}({string.Join(",", cols)}) ON {schema}.{obj} TO {grantee}{withGrant}";
            else
                sql = $"GRANT {priv} ON {schema}.{obj} TO {grantee}{withGrant}";

            _db.Execute(sql);
            Success($"Thực hiện thành công:\n{sql}");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 4: REVOKE
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildRevokeTab()
    {
        var page = new TabPage("Revoke");

        var topPanel = new FlowLayoutPanel
        {
            Dock      = DockStyle.Top,
            Height    = 62,
            Padding   = new Padding(8, 8, 8, 10),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UiTheme.BgLight
        };
        topPanel.Controls.Add(Lbl("User/Role:"));
        _cmbRevokeFrom = Cmb(0, 0, 200);
        topPanel.Controls.Add(_cmbRevokeFrom);
        _btnLoadGranted = Btn("Tải quyền", UiTheme.Primary, 120);
        _btnLoadGranted.Click += (_, _) => LoadGrantedPrivileges();
        topPanel.Controls.Add(_btnLoadGranted);

        _dgvGranted = MakeGrid(new[]
        {
            "TYPE", "PRIVILEGE", "OBJECT", "GRANTABLE", "COLUMNS"
        });
        _dgvGranted.Dock = DockStyle.Fill;

        var botPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(8, 6, 8, 10),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UiTheme.BgLight
        };
        _btnRevoke = Btn("Revoke đã chọn", UiTheme.Danger, width: 175);
        _btnRevoke.Click += BtnRevoke_Click;
        botPanel.Controls.Add(_btnRevoke);

        // FIX: dùng TableLayoutPanel (lọc / bảng-Fill / nút) để bảng quyền hiển thị tối đa, không bị che
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Margin = Padding.Empty, Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topPanel.Dock = DockStyle.Fill;   topPanel.Margin = Padding.Empty;
        _dgvGranted.Margin = Padding.Empty;
        botPanel.Dock = DockStyle.Fill;   botPanel.Margin = Padding.Empty;
        layout.Controls.Add(topPanel,    0, 0);
        layout.Controls.Add(_dgvGranted, 0, 1);
        layout.Controls.Add(botPanel,    0, 2);
        page.Controls.Add(layout);

        page.Enter += (_, _) => RefreshRevokeGrantees();
        return page;
    }

    private void RefreshRevokeGrantees()
    {
        TryCatch(() =>
        {
            _cmbRevokeFrom.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME FROM DBA_USERS WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB')" +
                " UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows) _cmbRevokeFrom.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadGrantedPrivileges()
    {
        TryCatch(() =>
        {
            var target = _cmbRevokeFrom.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(target)) return;

            var dt = _db.Query(
                "SELECT 'Object' AS TYPE, PRIVILEGE, OWNER||'.'||TABLE_NAME AS OBJECT, " +
                "GRANTABLE, NULL AS COLUMNS " +
                $"FROM DBA_TAB_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'Column', PRIVILEGE, OWNER||'.'||TABLE_NAME||'.'||COLUMN_NAME, 'NO', NULL " +
                $"FROM DBA_COL_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'Role', GRANTED_ROLE, NULL, ADMIN_OPTION, NULL " +
                $"FROM DBA_ROLE_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'System', PRIVILEGE, NULL, ADMIN_OPTION, NULL " +
                $"FROM DBA_SYS_PRIVS WHERE GRANTEE = '{target}' " +
                "ORDER BY 1,2");
            _dgvGranted.DataSource = dt;
        });
    }

    private void BtnRevoke_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvGranted.CurrentRow is null) { Error("Chọn quyền cần thu hồi."); return; }
            var row    = _dgvGranted.CurrentRow;
            var type   = row.Cells["TYPE"].Value?.ToString() ?? "";
            var priv   = row.Cells["PRIVILEGE"].Value?.ToString() ?? "";
            var obj    = row.Cells["OBJECT"].Value?.ToString() ?? "";
            var target = _cmbRevokeFrom.Text.Trim().ToUpper();

            string sql = type switch
            {
                "Object" => $"REVOKE {priv} ON {obj} FROM {target}",
                "Column" => $"REVOKE {priv}({obj.Split('.')[2]}) ON {obj.Split('.')[0]}.{obj.Split('.')[1]} FROM {target}",
                "Role"   => $"REVOKE {priv} FROM {target}",
                "System" => $"REVOKE {priv} FROM {target}",
                _        => throw new InvalidOperationException("Loại quyền không xác định.")
            };

            if (MessageBox.Show($"Thực hiện:\n{sql}", "Xác nhận Revoke",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _db.Execute(sql);
            Success($"Revoke thành công."); LoadGrantedPrivileges();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 5: VIEW PRIVILEGES
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildViewPrivTab()
    {
        var page = new TabPage("View Privileges");

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 62, Padding = new Padding(8, 8, 8, 10),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = UiTheme.BgLight
        };
        top.Controls.Add(Lbl("User/Role:"));
        _cmbViewTarget = Cmb(0, 0, 200); top.Controls.Add(_cmbViewTarget);
        _btnViewRefresh = Btn("Xem quyền", UiTheme.Primary, 120);
        _btnViewRefresh.Click += (_, _) => LoadPrivilegeDetail();
        top.Controls.Add(_btnViewRefresh);

        _tabPrivDetail = new TabControl { Dock = DockStyle.Fill };
        _dgvSysPrivs = MakeGrid(new[] { "PRIVILEGE", "ADMIN_OPTION" });
        _dgvObjPrivs = MakeGrid(new[] { "OWNER", "TABLE_NAME", "PRIVILEGE", "GRANTABLE", "GRANTOR" });
        _dgvColPrivs = MakeGrid(new[] { "OWNER", "TABLE_NAME", "COLUMN_NAME", "PRIVILEGE", "GRANTABLE" });
        _dgvRolePrivs = MakeGrid(new[] { "GRANTED_ROLE", "ADMIN_OPTION", "DEFAULT_ROLE" });

        var tSys  = new TabPage("System Privs");  tSys.Controls.Add(_dgvSysPrivs);  _dgvSysPrivs.Dock  = DockStyle.Fill;
        var tObj  = new TabPage("Object Privs");  tObj.Controls.Add(_dgvObjPrivs);  _dgvObjPrivs.Dock  = DockStyle.Fill;
        var tCol  = new TabPage("Column Privs");  tCol.Controls.Add(_dgvColPrivs);  _dgvColPrivs.Dock  = DockStyle.Fill;
        var tRole = new TabPage("Roles Granted"); tRole.Controls.Add(_dgvRolePrivs); _dgvRolePrivs.Dock = DockStyle.Fill;
        _tabPrivDetail.TabPages.AddRange(new[] { tSys, tObj, tCol, tRole });

        page.Controls.Add(_tabPrivDetail);
        page.Controls.Add(top);
        page.Enter += (_, _) => RefreshViewTargets();
        return page;
    }

    private void RefreshViewTargets()
    {
        TryCatch(() =>
        {
            _cmbViewTarget.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME FROM DBA_USERS WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB')" +
                " UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows) _cmbViewTarget.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadPrivilegeDetail()
    {
        TryCatch(() =>
        {
            var target = _cmbViewTarget.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(target)) return;

            _dgvSysPrivs.DataSource = _db.Query(
                $"SELECT PRIVILEGE, ADMIN_OPTION FROM DBA_SYS_PRIVS WHERE GRANTEE='{target}' ORDER BY PRIVILEGE");
            _dgvObjPrivs.DataSource = _db.Query(
                $"SELECT OWNER,TABLE_NAME,PRIVILEGE,GRANTABLE,GRANTOR FROM DBA_TAB_PRIVS WHERE GRANTEE='{target}' ORDER BY TABLE_NAME,PRIVILEGE");
            _dgvColPrivs.DataSource = _db.Query(
                $"SELECT OWNER,TABLE_NAME,COLUMN_NAME,PRIVILEGE,GRANTABLE FROM DBA_COL_PRIVS WHERE GRANTEE='{target}' ORDER BY TABLE_NAME,COLUMN_NAME");
            _dgvRolePrivs.DataSource = _db.Query(
                $"SELECT GRANTED_ROLE,ADMIN_OPTION,DEFAULT_ROLE FROM DBA_ROLE_PRIVS WHERE GRANTEE='{target}' ORDER BY GRANTED_ROLE");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    private static DataGridView MakeGrid(string[] columns)
    {
        var dgv = UiTheme.Grid();
        dgv.MultiSelect = false;
        return dgv;
    }

    private static string? GetSelectedCell(DataGridView dgv, string col)
    {
        if (dgv.CurrentRow is null) { MessageBox.Show("Chọn 1 dòng."); return null; }
        return dgv.CurrentRow.Cells[col].Value?.ToString();
    }

    private static Label Lbl(string text, int x = 0, int y = 0) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true,
                Font = UiTheme.Body() };

    private static Label Label(string text) =>
        new() { Text = text, AutoSize = true, Font = UiTheme.Body(),
                Padding = new Padding(0, 8, 0, 0) };

    private static TextBox TB(int width, bool isPass = false) =>
        UiTheme.Pad(new() { Width = width, Height = 34, Font = UiTheme.Body(),
                PasswordChar = isPass ? '•' : '\0', BorderStyle = BorderStyle.FixedSingle });

    private static Button Btn(string text, Color backColor, int width = 120)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            MinimumSize = new Size(width, 38),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Body(),
            Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static ComboBox Cmb(int x, int y, int width) =>
        new() { Location = new Point(x, y), Size = new Size(width, 30),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body() };

    private void TryCatch(Action action,
        [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        try { action(); }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, $"Admin.{caller}", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Success(string msg) =>
        MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void Error(string msg) =>
        MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
