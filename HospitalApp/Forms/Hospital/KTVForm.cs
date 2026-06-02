using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Kỹ thuật viên (KTV_Role + View filter).
/// RBAC View tự động filter: chỉ thấy HSBA_DV do mình thực hiện.
/// </summary>
public class KTVForm : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private DataGridView _dgvDV   = null!;
    private TextBox      _txtKQ   = null!;
    private Label        _lblInfo = null!;
    private Button       _btnSave = null!, _btnRefresh = null!;
    private TabControl   _tabs    = null!;

    public KTVForm(OracleHelper db)
    {
        _db = db;
        Text = $"Giao diện Kỹ thuật viên – {db.Username}";
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 252, 245);
        BuildUI();

        ShortcutHelper.WireStandard(this,
            onRefresh: LoadMyDV,
            onSave:    () => BtnSave_Click(null, EventArgs.Empty));

        _session = new SessionManager(this, db.Username);
        FormClosed += (_, _) => _session.Dispose();
    }

    private void BuildUI()
    {
        Size = new Size(1180, 720);
        MinimumSize = new Size(1000, 600);
        BackColor = UiTheme.BgLight;

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = UiTheme.Body(),
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(0, 1)
        };
        _tabs.TabPages.Add(BuildWorkTab());
        _tabs.TabPages.Add(BuildThongBaoTab());
        _tabs.TabPages.Add(BuildMyProfileTab());

        var header = BuildAppHeader("Kỹ thuật viên", "KTV", UiTheme.HealthGreen);

        var sidebar = new Sidebar { AccentColor = UiTheme.HealthGreen, Dock = DockStyle.Left };
        sidebar.AddBrand("HospitalApp", _db.Username);
        sidebar.AddSection("Công việc");
        sidebar.AddItem("work",    IconRegistry.Lab,    "Dịch vụ của tôi");
        sidebar.AddSection("Thông tin");
        sidebar.AddItem("tb",      IconRegistry.Bell,   "Thông báo");
        sidebar.AddItem("profile", IconRegistry.Person, "Thông tin của tôi");
        sidebar.ItemSelected += key =>
        {
            _tabs.SelectedIndex = key switch
            { "work" => 0, "tb" => 1, "profile" => 2, _ => 0 };
        };

        var status = new StatusBar
        {
            LeftText   = $"{IconRegistry.Database}  {_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  Kỹ thuật viên  ·  {IconRegistry.Shield} RBAC view"
        };

        Controls.Add(_tabs);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("work");
        LoadMyDV();
    }

    private Panel BuildAppHeader(string title, string roleLabel, Color roleColor)
    {
        var header = new Panel
        {
            Dock = DockStyle.Top, Height = 60,
            BackColor = UiTheme.Surface,
            Padding = new Padding(24, 12, 24, 12)
        };
        var lblTitle = new Label
        {
            Text = title, Dock = DockStyle.Left, Width = 300,
            Font = UiTheme.Heading2(), ForeColor = UiTheme.TextDark,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };
        var roleChip = new RoleChip
        {
            Text = roleLabel, AccentColor = roleColor,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        var btnLogout = new RoundedButton
        {
            Text = "Đăng xuất", Glyph = IconRegistry.SignOut,
            BackColor = UiTheme.BgLight, ForeColor = UiTheme.TextDark,
            GlyphColor = UiTheme.Danger,
            Width = 130, Height = 36,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        btnLogout.Click += (_, _) =>
        {
            if (MessageBox.Show("Đăng xuất?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        };
        void layout()
        {
            btnLogout.Location = new Point(header.Width - btnLogout.Width - 16, 12);
            roleChip.Location  = new Point(btnLogout.Left - roleChip.Width - 12, 17);
            lblTitle.Width = Math.Max(140, roleChip.Left - lblTitle.Left - 16);
        }
        header.Resize += (_, _) => layout();
        roleChip.HandleCreated += (_, _) => layout();
        header.Controls.Add(roleChip);
        header.Controls.Add(btnLogout);
        header.Controls.Add(lblTitle);
        layout();
        return header;
    }

    private TabPage BuildMyProfileTab()
    {
        var p = new TabPage("Thông tin của tôi");
        p.Controls.Add(new MyProfilePanel(_db));
        return p;
    }

    private TabPage BuildThongBaoTab()
    {
        var page = new TabPage("Thông báo");
        var lblLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(8, 6, 0, 0),
            Font = UiTheme.LabelBold(),
            ForeColor = Color.FromArgb(0, 90, 40),
            Text = "Nhãn OLS: (chưa tải)"
        };
        var dgv = UiTheme.Grid();
        dgv.Dock = DockStyle.Fill;
        var btn = new Button
        {
            Text = "Tải thông báo", Dock = DockStyle.Top, Height = 38,
            BackColor = UiTheme.HealthCyan, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Body(), Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        btn.Width = 150;
        btn.Height = 38;
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => TryCatch(() =>
        {
            lblLabel.Text = "Nhãn OLS: " + CurrentOlsLabel();
            dgv.DataSource = _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG),1,100) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        lblLabel.Dock = DockStyle.Fill;
        btn.Dock = DockStyle.Fill;
        dgv.Margin = Padding.Empty;
        layout.Controls.Add(lblLabel, 0, 0);
        layout.Controls.Add(btn, 0, 1);
        layout.Controls.Add(dgv, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildWorkTab()
    {
        var page = new TabPage("Dịch vụ của tôi");

        // Toolbar
        var tool = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 44, Padding = new Padding(6),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.FromArgb(235, 250, 235)
        };
        _btnRefresh = new Button
        {
            Text = "Tải danh sách DV của tôi", Width = 240, Height = 38,
            BackColor = UiTheme.HealthCyan, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Body(), Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        _btnRefresh.Width = 240;
        _btnRefresh.Height = 38;
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.Click += (_, _) => LoadMyDV();
        tool.Controls.Add(_btnRefresh);

        _lblInfo = new Label
        {
            AutoSize = true, ForeColor = Color.DimGray,
            Font = UiTheme.Body(), Padding = new Padding(10, 8, 0, 0)
        };
        tool.Controls.Add(_lblInfo);

        // Bottom: cập nhật kết quả
        var bottom = new Panel
        {
            Dock = DockStyle.Bottom, Height = 140,
            BackColor = Color.FromArgb(245, 252, 245),
            Padding = new Padding(10)
        };
        bottom.Controls.Add(new Label
        {
            Text = "Kết quả xét nghiệm/dịch vụ:", Dock = DockStyle.Top,
            Font = UiTheme.LabelBold(), Height = 22
        });
        _txtKQ = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
            Font = UiTheme.Body(), BorderStyle = BorderStyle.FixedSingle
        };
        bottom.Controls.Add(_txtKQ);

        _btnSave = new Button
        {
            Dock = DockStyle.Bottom, Text = "Lưu kết quả",
            Height = 36, BackColor = Color.FromArgb(0, 140, 60),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Button(10f), Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        _btnSave.Height = 38;
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.Click += BtnSave_Click;
        bottom.Controls.Add(_btnSave);

        // Grid (Fill — phải add cuối cùng để chiếm phần còn lại)
        _dgvDV = UiTheme.Grid();
        _dgvDV.Dock = DockStyle.Fill;
        _dgvDV.SelectionChanged += DgvDV_SelectionChanged;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tool.Dock = DockStyle.Fill;
        tool.Margin = Padding.Empty;
        _dgvDV.Margin = Padding.Empty;
        bottom.Dock = DockStyle.Fill;
        bottom.Margin = Padding.Empty;
        layout.Controls.Add(tool, 0, 0);
        layout.Controls.Add(_dgvDV, 0, 1);
        layout.Controls.Add(bottom, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    private void LoadMyDV()
    {
        TryCatch(() =>
        {
            // KTV_HSBA_DV_View đã filter MAKTV = fn_get_manv() → chỉ thấy DV của mình
            var dt = _db.Query(
                "SELECT MAHSBA, LOAIDV, TO_CHAR(NGAYDV,'DD/MM/YYYY') AS NGAYDV, " +
                "MAKTV, SUBSTR(TO_CHAR(KETQUA),1,80) AS KETQUA " +
                "FROM BVADMIN.KTV_HSBA_DV_View " +
                "ORDER BY NGAYDV DESC, MAHSBA");
            _dgvDV.DataSource = dt;
            _lblInfo.Text = $"Tổng: {dt.Rows.Count} dịch vụ";
        });
    }

    private void DgvDV_SelectionChanged(object? s, EventArgs e)
    {
        if (_dgvDV.CurrentRow is null) return;
        TryCatch(() =>
        {
            var mahsba = _dgvDV.CurrentRow.Cells["MAHSBA"].Value?.ToString() ?? "";
            var loaidv = _dgvDV.CurrentRow.Cells["LOAIDV"].Value?.ToString() ?? "";

            // Load full KETQUA (có thể dài hơn 80 ký tự hiển thị trong grid)
            var dt = _db.Query(
                "SELECT TO_CHAR(KETQUA) AS KQ FROM BVADMIN.KTV_HSBA_DV_View " +
                "WHERE MAHSBA=:h AND LOAIDV=:l",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", loaidv));
            _txtKQ.Text = dt.Rows.Count > 0 ? dt.Rows[0]["KQ"]?.ToString() ?? "" : "";
        });
    }

    private void BtnSave_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvDV.CurrentRow is null) { ShowError("Chọn dịch vụ cần cập nhật kết quả."); return; }

            var mahsba = _dgvDV.CurrentRow.Cells["MAHSBA"].Value?.ToString() ?? "";
            var loaidv = _dgvDV.CurrentRow.Cells["LOAIDV"].Value?.ToString() ?? "";
            var ngaydv = _dgvDV.CurrentRow.Cells["NGAYDV"].Value?.ToString() ?? "";

            // Cập nhật qua VIEW (INSTEAD OF trigger xử lý + log trigger ghi vết)
            _db.Execute(
                "UPDATE BVADMIN.KTV_HSBA_DV_View SET KETQUA=:k " +
                "WHERE MAHSBA=:h AND LOAIDV=:l",
                OracleHelper.Param("k", _txtKQ.Text),
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", loaidv));

            AppAuditLogger.Info(_db.Username, "KTV.SaveKQ", $"hsba={mahsba} loaidv={loaidv}");
            Toast.Show(this, $"Đã lưu kết quả cho {loaidv}", Toast.Kind.Success);
            LoadMyDV();
        });
    }

    private void TryCatch(Action a, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        try { a(); }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, $"KTV.{caller}", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ShowSuccess(string m) =>
        MessageBox.Show(m, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(string m) =>
        MessageBox.Show(m, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);

    private string CurrentOlsLabel()
    {
        try
        {
            return _db.Scalar(
                "SELECT NVL(MAX(MAX_READ_LABEL), '(chưa gán)') " +
                "FROM DBA_SA_USER_LABELS " +
                "WHERE POLICY_NAME='BV_LABEL_POLICY' AND USER_NAME=USER")?.ToString() ?? "(chưa gán)";
        }
        catch { return "(không đọc được DBA_SA_USER_LABELS)"; }
    }
}
