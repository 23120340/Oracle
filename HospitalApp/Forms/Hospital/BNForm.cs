using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Bệnh nhân (BenhNhan_Role + View filter).
/// RBAC View tự động filter: chỉ thấy thông tin của chính mình.
/// INSTEAD OF trigger chặn cập nhật các trường cố định.
/// </summary>
public class BNForm : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private TabControl _tabs = null!;

    // Tab thông tin cá nhân
    private Label   _lblMABN     = null!, _lblTENBN   = null!, _lblCCCD = null!,
                    _lblPhai     = null!, _lblNgaySinh = null!;
    private TextBox _txtSoNha    = null!, _txtDuong    = null!,
                    _txtQuan     = null!, _txtTinh     = null!,
                    _txtTSB      = null!, _txtTSBGD    = null!, _txtDiung = null!;
    private Button  _btnSaveInfo = null!;

    // Tab HSBA
    private DataGridView _dgvHSBA = null!;

    // Legacy notification tab builder is no longer wired into BN UI.
    private DataGridView _dgvTB = null!;

    public BNForm(OracleHelper db)
    {
        _db = db;
        Text = $"Thông tin Bệnh nhân – {db.Username}";
        Size = new Size(900, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(255, 252, 248);
        BuildUI();

        ShortcutHelper.WireStandard(this,
            onRefresh: LoadMyInfo,
            onSave:    () => BtnSaveInfo_Click(null, EventArgs.Empty));

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
        _tabs.TabPages.Add(BuildInfoTabModern());
        _tabs.TabPages.Add(BuildHSBATab());

        var header = BuildAppHeader("Hồ sơ bệnh nhân", "BN", UiTheme.HealthCyan);

        var sidebar = new Sidebar { AccentColor = UiTheme.HealthCyan, Dock = DockStyle.Left };
        sidebar.AddBrand("HospitalApp", _db.Username);
        sidebar.AddSection("Hồ sơ");
        sidebar.AddItem("info", IconRegistry.Person,   "Thông tin của tôi");
        sidebar.AddItem("hsba", IconRegistry.Health,   "Lịch sử khám");
        sidebar.ItemSelected += key =>
        {
            _tabs.SelectedIndex = key switch
            { "info" => 0, "hsba" => 1, _ => 0 };
        };

        var status = new StatusBar
        {
            LeftText   = $"{_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  Bệnh nhân"
        };

        Controls.Add(_tabs);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("info");
    }

    private Panel BuildAppHeader(string title, string roleLabel, Color roleColor)
    {
        var header = new Panel
        {
            Dock = DockStyle.Top, Height = 64,
            BackColor = UiTheme.Surface,
            Padding = new Padding(24, 12, 24, 12)
        };
        var lblTitle = new Label
        {
            Text = title, Dock = DockStyle.Left, Width = 320,
            Font = UiTheme.Heading1(16f), ForeColor = UiTheme.TextDark,
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
            BackColor = UiTheme.Surface, ForeColor = UiTheme.TextDark,
            GlyphColor = UiTheme.Danger,
            BorderThickness = 1, BorderTint = UiTheme.BorderStrong,
            Width = 152, Height = 38,
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
        header.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UiTheme.Border });
        layout();
        return header;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: THÔNG TIN CÁ NHÂN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildInfoTabModern()
    {
        var page = new TabPage("Thông tin của tôi");
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = UiTheme.BgLight };

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 18, 24, 24),
            BackColor = UiTheme.BgLight
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var topGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.BgLight
        };
        topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        topGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));

        var identityCard = ProfileCard("Thông tin định danh");
        var gridRO = FieldGrid();
        _lblMABN = ReadonlyValue(""); _lblTENBN = ReadonlyValue(""); _lblCCCD = ReadonlyValue("");
        _lblPhai = ReadonlyValue(""); _lblNgaySinh = ReadonlyValue("");
        gridRO.Controls.Add(FieldLabel("Mã BN:")); gridRO.Controls.Add(_lblMABN);
        gridRO.Controls.Add(FieldLabel("Họ tên:")); gridRO.Controls.Add(_lblTENBN);
        gridRO.Controls.Add(FieldLabel("CCCD:")); gridRO.Controls.Add(_lblCCCD);
        gridRO.Controls.Add(FieldLabel("Phái:")); gridRO.Controls.Add(_lblPhai);
        gridRO.Controls.Add(FieldLabel("Ngày sinh:")); gridRO.Controls.Add(_lblNgaySinh);
        identityCard.Controls.Add(gridRO);
        identityCard.Controls[0].BringToFront();

        var updateCard = ProfileCard("Thông tin có thể cập nhật");
        var gridRW = FieldGrid();
        _txtSoNha = EditBox(120); _txtDuong = EditBox(280);
        _txtQuan = EditBox(220); _txtTinh = EditBox(220);
        gridRW.Controls.Add(FieldLabel("Số nhà:")); gridRW.Controls.Add(_txtSoNha);
        gridRW.Controls.Add(FieldLabel("Tên đường:")); gridRW.Controls.Add(_txtDuong);
        gridRW.Controls.Add(FieldLabel("Quận/Huyện:")); gridRW.Controls.Add(_txtQuan);
        gridRW.Controls.Add(FieldLabel("Tỉnh/TP:")); gridRW.Controls.Add(_txtTinh);
        updateCard.Controls.Add(gridRW);
        updateCard.Controls[0].BringToFront();

        topGrid.Controls.Add(identityCard, 0, 0);
        topGrid.Controls.Add(updateCard, 1, 0);

        var medicalCard = ProfileCard("Thông tin y tế");
        medicalCard.Margin = new Padding(0, 16, 0, 0);
        var medicalGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = UiTheme.Surface
        };
        medicalGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        medicalGrid.Controls.Add(FieldLabel("Tiền sử bệnh:"), 0, 0);
        _txtTSB = MemoBox(860, 76); medicalGrid.Controls.Add(_txtTSB, 0, 1);
        medicalGrid.Controls.Add(FieldLabel("Tiền sử gia đình:"), 0, 2);
        _txtTSBGD = MemoBox(860, 76); medicalGrid.Controls.Add(_txtTSBGD, 0, 3);
        medicalGrid.Controls.Add(FieldLabel("Dị ứng thuốc:"), 0, 4);
        _txtDiung = EditBox(560); medicalGrid.Controls.Add(_txtDiung, 0, 5);
        medicalCard.Controls.Add(medicalGrid);
        medicalCard.Controls[0].BringToFront();

        _btnSaveInfo = UiTheme.AccentButton("Lưu thông tin", BtnSaveInfo_Click);
        _btnSaveInfo.BackColor = UiTheme.RoleBN;
        _btnSaveInfo.MinimumSize = new Size(160, 38);
        _btnSaveInfo.Margin = new Padding(0, 16, 0, 0);

        shell.Controls.Add(topGrid, 0, 0);
        shell.Controls.Add(medicalCard, 0, 1);
        shell.Controls.Add(_btnSaveInfo, 0, 2);

        scroll.Controls.Add(shell);
        page.Controls.Add(scroll);
        page.Enter += (_, _) => LoadMyInfo();
        return page;
    }

    private static Panel ProfileCard(string title)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            BackColor = UiTheme.Surface,
            Padding = new Padding(18, 14, 18, 18),
            Margin = new Padding(0, 0, 14, 0)
        };
        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 28,
            Font = UiTheme.Heading3(10.5f),
            ForeColor = UiTheme.Primary,
            TextAlign = ContentAlignment.MiddleLeft
        };
        card.Controls.Add(label);
        return card;
    }

    private static TableLayoutPanel FieldGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0, 8, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return grid;
    }

    private TabPage BuildInfoTab()
    {
        var page = new TabPage("Thông tin của tôi");

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var fl = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20, 15, 20, 10), AutoSize = true,
            WrapContents = false
        };

        // Thông tin chỉ đọc (không được sửa)
        fl.Controls.Add(SectionLabel("Thông tin định danh"));

        var gridRO = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Padding = new Padding(0, 0, 0, 10) };
        gridRO.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        gridRO.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _lblMABN     = ReadonlyValue(""); _lblTENBN = ReadonlyValue(""); _lblCCCD = ReadonlyValue("");
        _lblPhai     = ReadonlyValue(""); _lblNgaySinh = ReadonlyValue("");
        gridRO.Controls.Add(FieldLabel("Mã BN:"));      gridRO.Controls.Add(_lblMABN);
        gridRO.Controls.Add(FieldLabel("Họ tên:"));     gridRO.Controls.Add(_lblTENBN);
        gridRO.Controls.Add(FieldLabel("CCCD:"));       gridRO.Controls.Add(_lblCCCD);
        gridRO.Controls.Add(FieldLabel("Phái:"));       gridRO.Controls.Add(_lblPhai);
        gridRO.Controls.Add(FieldLabel("Ngày sinh:"));  gridRO.Controls.Add(_lblNgaySinh);
        fl.Controls.Add(gridRO);

        // Thông tin có thể sửa
        fl.Controls.Add(SectionLabel("Thông tin có thể cập nhật"));
        var gridRW = new TableLayoutPanel { ColumnCount = 2, AutoSize = true };
        gridRW.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        gridRW.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _txtSoNha  = EditBox(120); _txtDuong  = EditBox(250);
        _txtQuan   = EditBox(200); _txtTinh   = EditBox(200);

        gridRW.Controls.Add(FieldLabel("Số nhà:"));   gridRW.Controls.Add(_txtSoNha);
        gridRW.Controls.Add(FieldLabel("Tên đường:")); gridRW.Controls.Add(_txtDuong);
        gridRW.Controls.Add(FieldLabel("Quận/Huyện:")); gridRW.Controls.Add(_txtQuan);
        gridRW.Controls.Add(FieldLabel("Tỉnh/TP:"));   gridRW.Controls.Add(_txtTinh);
        fl.Controls.Add(gridRW);

        fl.Controls.Add(SectionLabel("Thông tin y tế"));
        fl.Controls.Add(FieldLabel("Tiền sử bệnh:"));
        _txtTSB   = MemoBox(700, 70); fl.Controls.Add(_txtTSB);
        fl.Controls.Add(FieldLabel("Tiền sử gia đình:"));
        _txtTSBGD = MemoBox(700, 70); fl.Controls.Add(_txtTSBGD);
        fl.Controls.Add(FieldLabel("Dị ứng thuốc:"));
        _txtDiung = EditBox(500); fl.Controls.Add(_txtDiung);

        _btnSaveInfo = new Button
        {
            Text = "Lưu thông tin", Width = 180, Height = 38,
            BackColor = Color.FromArgb(140, 60, 140), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Button(10f),
            Cursor = Cursors.Hand, Margin = new Padding(0, 10, 0, 0),
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        _btnSaveInfo.FlatAppearance.BorderSize = 0;
        _btnSaveInfo.Click += BtnSaveInfo_Click;
        fl.Controls.Add(_btnSaveInfo);

        scroll.Controls.Add(fl);
        page.Controls.Add(scroll);
        page.Enter += (_, _) => LoadMyInfo();
        return page;
    }

    private void LoadMyInfo()
    {
        TryCatch(() =>
        {
            // BN_BENHNHAN_View filter: ORACLE_USER = SESSION_USER → chỉ 1 dòng
            var dt = _db.Query(
                "SELECT MABN, TENBN, PHAI, TO_CHAR(NGAYSINH,'DD/MM/YYYY') AS NGAYSINH, " +
                "CCCD, SONHA, TENDUONG, QUANHUYEN, TINHTP, " +
                "TO_NCHAR(TIENSUBENH) AS TSB, TO_NCHAR(TIENSUBENHGD) AS TSBGD, DIUNGTHUOC " +
                "FROM BVADMIN.BN_BENHNHAN_View");

            if (dt.Rows.Count == 0)
            {
                ShowError("Không tìm thấy thông tin bệnh nhân liên kết với tài khoản này.");
                return;
            }

            var r = dt.Rows[0];
            _lblMABN.Text      = r["MABN"]?.ToString()     ?? "";
            _lblTENBN.Text     = r["TENBN"]?.ToString()    ?? "";
            _lblCCCD.Text      = r["CCCD"]?.ToString()     ?? "";
            _lblPhai.Text      = r["PHAI"]?.ToString()     == "M" ? "Nam" : "Nữ";
            _lblNgaySinh.Text  = r["NGAYSINH"]?.ToString() ?? "";
            _txtSoNha.Text     = r["SONHA"]?.ToString()    ?? "";
            _txtDuong.Text     = r["TENDUONG"]?.ToString() ?? "";
            _txtQuan.Text      = r["QUANHUYEN"]?.ToString()?? "";
            _txtTinh.Text      = r["TINHTP"]?.ToString()   ?? "";
            _txtTSB.Text       = r["TSB"]?.ToString()      ?? "";
            _txtTSBGD.Text     = r["TSBGD"]?.ToString()    ?? "";
            _txtDiung.Text     = r["DIUNGTHUOC"]?.ToString()?? "";
        });
    }

    private void BtnSaveInfo_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            // Cập nhật qua BN_BENHNHAN_View
            // INSTEAD OF trigger chặn sửa MABN/TENBN/PHAI/NGAYSINH/CCCD
            _db.Execute(
                "UPDATE BVADMIN.BN_BENHNHAN_View SET " +
                "SONHA=:sn, TENDUONG=:td, QUANHUYEN=:qh, TINHTP=:tp, " +
                "TIENSUBENH=:tsb, TIENSUBENHGD=:tsbgd, DIUNGTHUOC=:dt " +
                "WHERE MABN = :id",
                OracleHelper.Param("sn",    _txtSoNha.Text),
                OracleHelper.Param("td",    _txtDuong.Text),
                OracleHelper.Param("qh",    _txtQuan.Text),
                OracleHelper.Param("tp",    _txtTinh.Text),
                OracleHelper.Param("tsb",   _txtTSB.Text),
                OracleHelper.Param("tsbgd", _txtTSBGD.Text),
                OracleHelper.Param("dt",    _txtDiung.Text),
                OracleHelper.Param("id",    _lblMABN.Text));
            AppAuditLogger.Info(_db.Username, "BN.SaveInfo", $"mabn={_lblMABN.Text}");
            Toast.Show(this, "Đã cập nhật thông tin cá nhân", Toast.Kind.Success);
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: LỊCH SỬ BỆNH ÁN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildHSBATab()
    {
        var page = new TabPage("Lịch sử khám bệnh");

        _dgvHSBA = UiTheme.Grid();
        _dgvHSBA.Dock = DockStyle.Fill;

        var note = new Label
        {
            Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Text = "ℹ Chỉ hiển thị Mã HSBA, Ngày khám, Khoa và Kết luận. Thông tin chẩn đoán chi tiết do bác sĩ quản lý.",
            ForeColor = Color.DimGray, Font = UiTheme.Body(8f)
        };

        var btn = new Button
        {
            Dock = DockStyle.Top, Text = "Tải lịch sử khám bệnh", Height = 38,
            BackColor = UiTheme.HealthCyan, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Body(), Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        btn.Height = 38;
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => TryCatch(() =>
        {
            // BN_HSBA_View tự filter theo ORACLE_USER → chỉ thấy HSBA của mình
            // Ẩn CHANDOAN/DIEUTRI (chỉ BS/bác sĩ mới được xem chi tiết)
            _dgvHSBA.DataSource = _db.Query(
                "SELECT MAHSBA, TO_CHAR(NGAY,'DD/MM/YYYY') AS NGAY, MAKHOA, " +
                "SUBSTR(TO_NCHAR(KETLUAN),1,100) AS KETLUAN " +
                "FROM BVADMIN.BN_HSBA_View ORDER BY NGAY DESC");
        });

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btn.Dock = DockStyle.Fill;
        note.Dock = DockStyle.Fill;
        _dgvHSBA.Margin = Padding.Empty;
        layout.Controls.Add(btn, 0, 0);
        layout.Controls.Add(_dgvHSBA, 0, 1);
        layout.Controls.Add(note, 0, 2);
        page.Controls.Add(layout);
        return page;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: THÔNG BÁO
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildThongBaoTab()
    {
        var page = new TabPage("Thông báo");
        _dgvTB = UiTheme.Grid();
        _dgvTB.Dock = DockStyle.Fill;
        var btn = new Button
        {
            Dock = DockStyle.Top, Text = "Tải thông báo", Height = 38,
            BackColor = UiTheme.HealthCyan, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Body(),
            Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        btn.Height = 38;
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += (_, _) => TryCatch(() =>
        {
            // OLS tự filter nhãn → BN chỉ thấy thông báo phù hợp với label của mình
            _dgvTB.DataSource = _db.Query(
                "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG),1,120) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        });
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btn.Dock = DockStyle.Fill;
        _dgvTB.Margin = Padding.Empty;
        layout.Controls.Add(btn, 0, 0);
        layout.Controls.Add(_dgvTB, 0, 1);
        page.Controls.Add(layout);
        return page;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────
    private static Label SectionLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = UiTheme.Heading3(10.5f),
        ForeColor = UiTheme.Primary,
        Padding = new Padding(0, 10, 0, 6)
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text, AutoSize = true, Font = UiTheme.Body(9.5f),
        Padding = new Padding(0, 5, 6, 2)
    };

    private static Label ReadonlyValue(string val) => new()
    {
        Text = val, AutoSize = true, ForeColor = Color.DimGray,
        Font = UiTheme.Italic(),
        Padding = new Padding(0, 5, 0, 2)
    };

    private static TextBox EditBox(int width) => UiTheme.Pad(new()
    {
        Width = width, Height = 34, Font = UiTheme.Body(),
        BorderStyle = BorderStyle.FixedSingle
    });

    private static TextBox MemoBox(int width, int height) => UiTheme.Pad(new()
    {
        Width = width, Height = height, Multiline = true,
        ScrollBars = ScrollBars.Vertical, Font = UiTheme.Body(),
        BorderStyle = BorderStyle.FixedSingle
    });

    private void TryCatch(Action a, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        try { a(); }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, $"BN.{caller}", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void ShowSuccess(string m) =>
        MessageBox.Show(m, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(string m) =>
        MessageBox.Show(m, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
