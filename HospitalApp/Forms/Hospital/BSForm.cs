using System.Data;
using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Bác sĩ / Y sĩ (BS_Role + VPD)
/// VPD tự động filter: chỉ thấy HSBA mình phụ trách.
/// </summary>
public class BSForm : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private TabControl _tabs = null!;

    // Tab HSBA
    private DataGridView _dgvHSBA  = null!, _dgvDV = null!, _dgvDT = null!;
    private TextBox _txtChandoan   = null!, _txtDieutri = null!, _txtKetluan = null!;
    private TextBox _txtTenthuoc   = null!, _txtLieudung = null!;
    private Button _btnSaveHSBA    = null!;
    private Label _lblHSBAId       = null!;

    // Tab Bệnh nhân
    private DataGridView _dgvBN    = null!;
    private TextBox _txtTSB        = null!, _txtTSBGD = null!, _txtDiung = null!;
    private Button _btnSaveBN      = null!;
    private Label _lblBNId         = null!;

    public BSForm(OracleHelper db)
    {
        _db = db;
        Text = $"Giao diện Bác sĩ – {db.Username}";
        Size = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        BackColor = Color.FromArgb(245, 250, 255);
        BuildUI();

        ShortcutHelper.WireStandard(this,
            onRefresh: LoadHSBA,
            onSave:    () => BtnSaveHSBA_Click(null, EventArgs.Empty));

        _session = new SessionManager(this, db.Username);
        FormClosed += (_, _) => _session.Dispose();
    }

    private void BuildUI()
    {
        Size = new Size(1280, 780);
        MinimumSize = new Size(1080, 650);
        BackColor = UiTheme.BgLight;

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill, Font = UiTheme.Body(),
            Appearance = TabAppearance.FlatButtons,
            SizeMode = TabSizeMode.Fixed,
            ItemSize = new Size(0, 1)
        };
        _tabs.TabPages.Add(BuildHSBATab());
        _tabs.TabPages.Add(BuildBNTab());
        _tabs.TabPages.Add(BuildThongBaoTab());
        _tabs.TabPages.Add(BuildMyProfileTab());

        var header = BuildAppHeader("Bác sĩ / Y sĩ", "BS", UiTheme.RoleBS);

        var sidebar = new Sidebar { AccentColor = UiTheme.HealthEmerald, Dock = DockStyle.Left };
        sidebar.AddBrand("HospitalApp", _db.Username);
        sidebar.AddSection("Khám chữa bệnh");
        sidebar.AddItem("hsba",    IconRegistry.Document, "Hồ sơ bệnh án");
        sidebar.AddItem("bn",      IconRegistry.People,   "Bệnh nhân");
        sidebar.AddSection("Thông tin");
        sidebar.AddItem("tb",      IconRegistry.Bell,     "Thông báo");
        sidebar.AddItem("profile", IconRegistry.Person,   "Thông tin của tôi");
        sidebar.ItemSelected += key =>
        {
            _tabs.SelectedIndex = key switch
            { "hsba" => 0, "bn" => 1, "tb" => 2, "profile" => 3, _ => 0 };
        };

        var status = new StatusBar
        {
            LeftText   = $"{_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  Bác sĩ  ·  VPD enforced"
        };

        Controls.Add(_tabs);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("hsba");
    }

    private TabPage BuildMyProfileTab()
    {
        var p = new TabPage("Thông tin của tôi");
        p.Controls.Add(new MyProfilePanel(_db));
        return p;
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
            BackColor = UiTheme.Surface, ForeColor = UiTheme.TextDark,
            GlyphColor = UiTheme.Danger,
            BorderThickness = 1, BorderTint = UiTheme.BorderStrong,
            CornerRadius = 0, Width = 152, Height = 38,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        btnLogout.Click += (_, _) =>
        {
            if (MessageBox.Show("Đăng xuất?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                Close();
        };
        var btnChangePw = new RoundedButton
        {
            Text = "Đổi mật khẩu", Glyph = IconRegistry.Key,
            BackColor = UiTheme.Surface, ForeColor = UiTheme.TextDark,
            GlyphColor = UiTheme.Primary,
            BorderThickness = 1, BorderTint = UiTheme.BorderStrong,
            CornerRadius = 0, Width = 168, Height = 38,
            Anchor = AnchorStyles.Right | AnchorStyles.Top
        };
        btnChangePw.Click += (_, _) =>
        {
            using var dlg = new ChangePasswordDialog(_db);
            if (dlg.ShowDialog(this) == DialogResult.OK) Close();
        };
        void layout()
        {
            btnLogout.Location   = new Point(header.Width - btnLogout.Width - 16, 11);
            btnChangePw.Location = new Point(btnLogout.Left - btnChangePw.Width - 8, 11);
            roleChip.Location    = new Point(btnChangePw.Left - roleChip.Width - 12, 16);
            lblTitle.Width = Math.Max(140, roleChip.Left - lblTitle.Left - 16);
        }
        header.Resize += (_, _) => layout();
        roleChip.HandleCreated += (_, _) => layout();
        header.Controls.Add(roleChip);
        header.Controls.Add(btnChangePw);
        header.Controls.Add(btnLogout);
        header.Controls.Add(lblTitle);
        layout();
        return header;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: HỒ SƠ BỆNH ÁN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildHSBATab()
    {
        var page = new TabPage("Hồ sơ bệnh án") { BackColor = UiTheme.BgLight };
        // FIX (Ảnh 3): thay SplitContainer (Panel2 bị ép ngắn → 2 ô textarea dưới co về 0,
        // gõ không hiện chữ) bằng TableLayoutPanel: danh sách trên co giãn (Percent), vùng chi tiết
        // dưới cao CỐ ĐỊNH 400px → 3 ô Chẩn đoán/Điều trị/Kết luận luôn đủ chỗ hiển thị & sửa.
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            Margin = Padding.Empty, Padding = Padding.Empty, BackColor = UiTheme.BgLight
        };
        outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 400));

        // Top: danh sách HSBA
        _dgvHSBA = MakeGrid();
        _dgvHSBA.Dock = DockStyle.Fill;
        _dgvHSBA.SelectionChanged += DgvHSBA_SelectionChanged;
        // (đưa vào topLayout ở dưới)

        // Bottom: chi tiết + HSBA_DV + ĐƠNTHUỐC
        var detail = new TabControl { Dock = DockStyle.Fill };

        // Sub-tab 1: Chỉnh sửa chẩn đoán
        // FIX (Ảnh 5): dùng TableLayoutPanel docked thay FlowLayoutPanel cố định 600px
        // -> 3 ô textarea trải hết bề ngang, chia đều chiều cao, không cắt "Kết luận".
        var tDetail = new TabPage("Cập nhật HSBA");
        var fl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(12, 10, 12, 10),
            AutoScroll = true
        };
        fl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 0: ID
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 1: label Chẩn đoán
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));            // 2: textarea Chẩn đoán
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 3: label Điều trị
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));            // 4: textarea Điều trị
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 5: label Kết luận
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));            // 6: textarea Kết luận
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 7: nút Lưu

        _lblHSBAId = new Label { AutoSize = true, Font = UiTheme.LabelBold(),
                                 ForeColor = UiTheme.TextDark, Margin = new Padding(0, 0, 0, 6) };
        fl.Controls.Add(_lblHSBAId, 0, 0);

        fl.Controls.Add(new Label { Text = "Chẩn đoán:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 1);
        _txtChandoan = TBFill(56);
        fl.Controls.Add(_txtChandoan, 0, 2);

        fl.Controls.Add(new Label { Text = "Điều trị:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 3);
        _txtDieutri = TBFill(56);
        fl.Controls.Add(_txtDieutri, 0, 4);

        fl.Controls.Add(new Label { Text = "Kết luận:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 5);
        _txtKetluan = TBFill(56);
        fl.Controls.Add(_txtKetluan, 0, 6);

        _btnSaveHSBA = Btn("Lưu HSBA", UiTheme.HealthGreen);
        _btnSaveHSBA.Margin = new Padding(0, 8, 0, 0);
        _btnSaveHSBA.Click += BtnSaveHSBA_Click;
        fl.Controls.Add(_btnSaveHSBA, 0, 7);
        tDetail.Controls.Add(fl);

        // Sub-tab 2: Dịch vụ (HSBA_DV)
        var tDV = new TabPage("Dịch vụ chẩn đoán");
        var dvPanel = new Panel { Dock = DockStyle.Fill };
        _dgvDV = MakeGrid(); _dgvDV.Dock = DockStyle.Fill;
        var dvBot = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(4),
                                          FlowDirection = FlowDirection.LeftToRight,
                                          WrapContents = false, AutoScroll = true };
        var btnAddDV   = Btn("Thêm DV", UiTheme.HealthCyan);
        var btnDelDV   = Btn("Xóa DV", UiTheme.Danger);
        btnAddDV.Click += BtnAddDV_Click;
        btnDelDV.Click += BtnDelDV_Click;
        dvBot.Controls.AddRange(new Control[] { btnAddDV, btnDelDV });
        var dvLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(UiTheme.Spacing3, UiTheme.Spacing3, UiTheme.Spacing3, UiTheme.Spacing2),
            BackColor = UiTheme.BgLight
        };
        dvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dvLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        dvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _dgvDV.Margin = Padding.Empty;
        dvBot.Dock = DockStyle.Fill;
        dvBot.Margin = Padding.Empty;
        dvLayout.Controls.Add(_dgvDV, 0, 0);
        dvLayout.Controls.Add(dvBot, 0, 1);
        dvPanel.Controls.Add(dvLayout);
        tDV.Controls.Add(dvPanel);

        // Sub-tab 3: Đơn thuốc
        var tDT = new TabPage("Đơn thuốc");
        var dtPanel = new Panel { Dock = DockStyle.Fill };
        _dgvDT = MakeGrid(); _dgvDT.Dock = DockStyle.Fill;
        // FIX: hàng nhập thuốc dùng TableLayoutPanel (không FlowLayoutPanel) — 2 ô nhập co giãn
        // theo cột Percent, nhãn ở ô AutoSize riêng (canh giữa bằng Anchor), 2 nút ở ô Absolute.
        var dtBot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            RowCount = 1,
            Padding = new Padding(UiTheme.Spacing2, UiTheme.Spacing2, UiTheme.Spacing2, UiTheme.Spacing2),
            Margin = Padding.Empty,
            BackColor = UiTheme.BgLight
        };
        dtBot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // 0: "Thuốc:"
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));    // 1: tên thuốc
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // 2: "Liều:"
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));    // 3: liều dùng
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // 4: nút Thêm
        dtBot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));        // 5: nút Xóa

        dtBot.Controls.Add(new Label { Text = "Thuốc:", AutoSize = true,
                                       Anchor = AnchorStyles.Left,
                                       Margin = new Padding(0, 0, UiTheme.Spacing2, 0) }, 0, 0);
        _txtTenthuoc = new TextBox { Dock = DockStyle.Fill, Height = 30, Font = UiTheme.Body(),
                                     Margin = new Padding(0, 4, UiTheme.Spacing3, 4) };
        dtBot.Controls.Add(_txtTenthuoc, 1, 0);
        dtBot.Controls.Add(new Label { Text = "Liều:", AutoSize = true,
                                       Anchor = AnchorStyles.Left,
                                       Margin = new Padding(0, 0, UiTheme.Spacing2, 0) }, 2, 0);
        _txtLieudung = new TextBox { Dock = DockStyle.Fill, Height = 30, Font = UiTheme.Body(),
                                     Margin = new Padding(0, 4, UiTheme.Spacing3, 4) };
        dtBot.Controls.Add(_txtLieudung, 3, 0);
        var btnAddDT = Btn("Thêm", UiTheme.HealthCyan, 110);
        var btnDelDT = Btn("Xóa", UiTheme.Danger, 110);
        btnAddDT.Anchor = AnchorStyles.None;
        btnDelDT.Anchor = AnchorStyles.None;
        btnAddDT.Margin = new Padding(0, 0, UiTheme.Spacing2, 0);
        btnDelDT.Margin = Padding.Empty;
        btnAddDT.Click += BtnAddDT_Click;
        btnDelDT.Click += BtnDelDT_Click;
        dtBot.Controls.Add(btnAddDT, 4, 0);
        dtBot.Controls.Add(btnDelDT, 5, 0);
        var dtLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = new Padding(UiTheme.Spacing3, UiTheme.Spacing3, UiTheme.Spacing3, 0),
            BackColor = UiTheme.BgLight
        };
        dtLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dtLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        dtLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _dgvDT.Margin = Padding.Empty;
        dtBot.Dock = DockStyle.Fill;
        dtBot.Margin = Padding.Empty;
        dtLayout.Controls.Add(_dgvDT, 0, 0);
        dtLayout.Controls.Add(dtBot, 0, 1);
        dtPanel.Controls.Add(dtLayout);
        tDT.Controls.Add(dtPanel);

        detail.TabPages.AddRange(new[] { tDetail, tDV, tDT });
        outer.Controls.Add(detail, 0, 1);

        var btnRefresh = Btn("Tải HSBA", UiTheme.HealthCyan);
        btnRefresh.Dock = DockStyle.Top;
        btnRefresh.Click += (_, _) => LoadHSBA();
        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        topLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        topLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btnRefresh.Dock = DockStyle.Fill;
        btnRefresh.Margin = Padding.Empty;
        _dgvHSBA.Margin = Padding.Empty;
        topLayout.Controls.Add(btnRefresh, 0, 0);
        topLayout.Controls.Add(_dgvHSBA, 0, 1);
        outer.Controls.Add(topLayout, 0, 0);

        page.Controls.Add(outer);
        page.Enter += (_, _) => LoadHSBA();
        return page;
    }

    private void LoadHSBA()
    {
        TryCatch(() =>
        {
            // VPD filter tự động: chỉ trả về HSBA của BS hiện tại (MABS = SESSION USER's MANV)
            var dt = _db.Query(
                "SELECT MAHSBA, MABN, TO_CHAR(NGAY,'DD/MM/YYYY') AS NGAY, " +
                "MAKHOA, MABS, " +
                "SUBSTR(TO_NCHAR(CHANDOAN),1,60) AS CHANDOAN, " +
                "SUBSTR(TO_NCHAR(KETLUAN),1,40) AS KETLUAN " +
                "FROM BVADMIN.HSBA ORDER BY NGAY DESC");
            _dgvHSBA.DataSource = dt;
        });
    }

    private void DgvHSBA_SelectionChanged(object? s, EventArgs e)
    {
        if (_dgvHSBA.CurrentRow is null) return;
        TryCatch(() =>
        {
            var mahsba = _dgvHSBA.CurrentRow.Cells["MAHSBA"].Value?.ToString() ?? "";
            _lblHSBAId.Text = $"MAHSBA: {mahsba}";

            // Load full CHANDOAN/DIEUTRI/KETLUAN
            var dt = _db.Query(
                "SELECT TO_NCHAR(CHANDOAN) AS C, TO_NCHAR(DIEUTRI) AS D, TO_NCHAR(KETLUAN) AS K " +
                "FROM BVADMIN.HSBA WHERE MAHSBA = :id",
                OracleHelper.Param("id", mahsba));
            if (dt.Rows.Count > 0)
            {
                _txtChandoan.Text = dt.Rows[0]["C"]?.ToString() ?? "";
                _txtDieutri.Text  = dt.Rows[0]["D"]?.ToString() ?? "";
                _txtKetluan.Text  = dt.Rows[0]["K"]?.ToString() ?? "";
            }

            // Load HSBA_DV
            _dgvDV.DataSource = _db.Query(
                "SELECT LOAIDV, TO_CHAR(NGAYDV,'DD/MM/YYYY') AS NGAYDV, MAKTV, " +
                "SUBSTR(TO_NCHAR(KETQUA),1,60) AS KETQUA " +
                "FROM BVADMIN.HSBA_DV WHERE MAHSBA = :id ORDER BY NGAYDV",
                OracleHelper.Param("id", mahsba));

            // Load DONTHUOC
            _dgvDT.DataSource = _db.Query(
                "SELECT TO_CHAR(NGAYDT,'DD/MM/YYYY') AS NGAYDT, TENTHUOC, LIEUDUNG " +
                "FROM BVADMIN.DONTHUOC WHERE MAHSBA = :id ORDER BY NGAYDT",
                OracleHelper.Param("id", mahsba));
        });
    }

    private void BtnSaveHSBA_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var mahsba = ExtractLabel(_lblHSBAId);
            if (string.IsNullOrEmpty(mahsba)) { ShowError("Chọn HSBA cần cập nhật."); return; }

            _db.Execute(
                "UPDATE BVADMIN.HSBA SET CHANDOAN = :c, DIEUTRI = :d, KETLUAN = :k WHERE MAHSBA = :id",
                OracleHelper.Param("c",  _txtChandoan.Text),
                OracleHelper.Param("d",  _txtDieutri.Text),
                OracleHelper.Param("k",  _txtKetluan.Text),
                OracleHelper.Param("id", mahsba));
            AppAuditLogger.Info(_db.Username, "BS.SaveHSBA", $"mahsba={mahsba}");
            Toast.Show(this, $"Đã cập nhật HSBA {mahsba}", Toast.Kind.Success);
        });
    }

    private void BtnAddDV_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var mahsba = ExtractLabel(_lblHSBAId);
            if (string.IsNullOrEmpty(mahsba)) { ShowError("Chọn HSBA."); return; }

            // Load danh sách KTV để dropdown chọn (UX: không phải nhớ MAKTV)
            var ktvList = _db.Query(
                "SELECT MANV, MANV||' - '||HOTEN AS DISPLAY " +
                "FROM BVADMIN.NV_LOOKUP_View WHERE VAITRO='KTV' ORDER BY HOTEN");

            using var dlg = new AddDVDialog(ktvList);
            if (dlg.ShowDialog() != DialogResult.OK) return;

            // Tránh PK collision (MAHSBA, LOAIDV, NGAYDV)
            var exists = Convert.ToInt32(_db.Scalar(
                "SELECT COUNT(*) FROM BVADMIN.HSBA_DV " +
                "WHERE MAHSBA=:h AND LOAIDV=:l AND TRUNC(NGAYDV)=TRUNC(SYSDATE)",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", dlg.LoaiDV)));
            if (exists > 0)
            { ShowError($"Dịch vụ '{dlg.LoaiDV}' đã được chỉ định cho HSBA này hôm nay."); return; }

            _db.Execute(
                "INSERT INTO BVADMIN.HSBA_DV(MAHSBA,LOAIDV,NGAYDV,MAKTV) VALUES(:h,:l,SYSDATE,:k)",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", dlg.LoaiDV),
                OracleHelper.Param("k", dlg.MaKTV));
            DgvHSBA_SelectionChanged(null, EventArgs.Empty);
        });
    }

    private void BtnDelDV_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvDV.CurrentRow is null) { ShowError("Chọn DV cần xóa."); return; }
            var mahsba = ExtractLabel(_lblHSBAId);
            var loaidv = _dgvDV.CurrentRow.Cells["LOAIDV"].Value?.ToString();

            if (!ConfirmDeleteDialog.Confirm(this,
                "Xoá dịch vụ chẩn đoán",
                $"Sẽ xoá dịch vụ \"{loaidv}\" khỏi HSBA {mahsba}.\n" +
                $"Thao tác này sẽ được ghi vào nhật ký kiểm toán."))
                return;

            _db.Execute(
                "DELETE FROM BVADMIN.HSBA_DV WHERE MAHSBA=:h AND LOAIDV=:l",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", loaidv));
            AppAuditLogger.Warn(_db.Username, "BS.DelDV", $"hsba={mahsba} loaidv={loaidv}");
            DgvHSBA_SelectionChanged(null, EventArgs.Empty);
        });
    }

    private void BtnAddDT_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var mahsba   = ExtractLabel(_lblHSBAId);
            var tenthuoc = _txtTenthuoc.Text.Trim();
            if (string.IsNullOrEmpty(mahsba) || string.IsNullOrEmpty(tenthuoc))
            { ShowError("Chọn HSBA và nhập tên thuốc."); return; }

            // PK DONTHUOC = (MAHSBA, NGAYDT, TENTHUOC) → check trùng trong ngày hôm nay
            var exists = Convert.ToInt32(_db.Scalar(
                "SELECT COUNT(*) FROM BVADMIN.DONTHUOC " +
                "WHERE MAHSBA=:h AND TENTHUOC=:t AND TRUNC(NGAYDT)=TRUNC(SYSDATE)",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("t", tenthuoc)));

            if (exists > 0)
            {
                // Đã có trong ngày → cập nhật liều thay vì insert (tránh PK collision)
                if (MessageBox.Show(
                    $"Thuốc '{tenthuoc}' đã được kê trong hôm nay.\nCập nhật liều dùng mới?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    != DialogResult.Yes) return;

                _db.Execute(
                    "UPDATE BVADMIN.DONTHUOC SET LIEUDUNG=:l " +
                    "WHERE MAHSBA=:h AND TENTHUOC=:t AND TRUNC(NGAYDT)=TRUNC(SYSDATE)",
                    OracleHelper.Param("l", _txtLieudung.Text),
                    OracleHelper.Param("h", mahsba),
                    OracleHelper.Param("t", tenthuoc));
            }
            else
            {
                _db.Execute(
                    "INSERT INTO BVADMIN.DONTHUOC(MAHSBA,NGAYDT,TENTHUOC,LIEUDUNG) " +
                    "VALUES(:h,SYSDATE,:t,:l)",
                    OracleHelper.Param("h", mahsba),
                    OracleHelper.Param("t", tenthuoc),
                    OracleHelper.Param("l", _txtLieudung.Text));
            }

            _txtTenthuoc.Clear(); _txtLieudung.Clear();
            DgvHSBA_SelectionChanged(null, EventArgs.Empty);
        });
    }

    private void BtnDelDT_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvDT.CurrentRow is null) { ShowError("Chọn thuốc cần xóa."); return; }
            var mahsba   = ExtractLabel(_lblHSBAId);
            var tenthuoc = _dgvDT.CurrentRow.Cells["TENTHUOC"].Value?.ToString();

            if (!ConfirmDeleteDialog.Confirm(this,
                "Xoá đơn thuốc",
                $"Sẽ xoá thuốc \"{tenthuoc}\" khỏi HSBA {mahsba}.\n" +
                $"FGA sẽ ghi nhận việc xoá đơn thuốc này."))
                return;

            _db.Execute(
                "DELETE FROM BVADMIN.DONTHUOC WHERE MAHSBA=:h AND TENTHUOC=:t",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("t", tenthuoc));
            AppAuditLogger.Warn(_db.Username, "BS.DelDT", $"hsba={mahsba} thuoc={tenthuoc}");
            DgvHSBA_SelectionChanged(null, EventArgs.Empty);
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: BỆNH NHÂN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildBNTab()
    {
        var page = new TabPage("Bệnh nhân");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 200 };

        _dgvBN = MakeGrid(); _dgvBN.Dock = DockStyle.Fill;
        _dgvBN.SelectionChanged += (_, _) => LoadBNDetail();

        var btnRefBN = Btn("Tải DS bệnh nhân", UiTheme.HealthCyan);
        btnRefBN.Dock = DockStyle.Top;
        btnRefBN.Click += (_, _) => LoadBN();
        var bnLeftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        bnLeftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        bnLeftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        bnLeftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        btnRefBN.Dock = DockStyle.Fill;
        btnRefBN.Margin = Padding.Empty;
        _dgvBN.Margin = Padding.Empty;
        bnLeftLayout.Controls.Add(btnRefBN, 0, 0);
        bnLeftLayout.Controls.Add(_dgvBN, 0, 1);
        split.Panel1.Controls.Add(bnLeftLayout);

        // FIX: thay FlowLayoutPanel + TextBox cố định 600px (bị cắt/không co giãn) bằng
        // TableLayoutPanel với RowStyles tường minh — 3 textarea Dock=Fill trải hết bề ngang,
        // chia đều phần dọc, nhãn nằm ô AutoSize riêng, nút Lưu ở hàng Absolute 56px dưới cùng.
        var fl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(UiTheme.Spacing4, UiTheme.Spacing3, UiTheme.Spacing4, UiTheme.Spacing3),
            BackColor = UiTheme.BgLight,
            AutoScroll = true
        };
        fl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 0: MABN id
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 1: label Tiền sử bệnh
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 40));             // 2: textarea Tiền sử bệnh
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 3: label Tiền sử bệnh GĐ
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 40));             // 4: textarea Tiền sử bệnh GĐ
        fl.RowStyles.Add(new RowStyle(SizeType.AutoSize));                 // 5: label Dị ứng thuốc
        fl.RowStyles.Add(new RowStyle(SizeType.Percent, 20));             // 6: textarea Dị ứng thuốc
        fl.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));            // 7: nút Lưu

        _lblBNId = new Label { AutoSize = true, Font = UiTheme.LabelBold(), ForeColor = UiTheme.TextDark,
                               Margin = new Padding(0, 0, 0, UiTheme.Spacing1) };
        fl.Controls.Add(_lblBNId, 0, 0);

        fl.Controls.Add(new Label { Text = "Tiền sử bệnh:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 1);
        _txtTSB = TBFill(60); fl.Controls.Add(_txtTSB, 0, 2);

        fl.Controls.Add(new Label { Text = "Tiền sử bệnh gia đình:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 3);
        _txtTSBGD = TBFill(60); fl.Controls.Add(_txtTSBGD, 0, 4);

        fl.Controls.Add(new Label { Text = "Dị ứng thuốc:", AutoSize = true,
                                    Margin = new Padding(0, 2, 0, 2) }, 0, 5);
        _txtDiung = TBFill(40); fl.Controls.Add(_txtDiung, 0, 6);

        _btnSaveBN = Btn("Cập nhật tiền sử", UiTheme.HealthGreen, 210);
        _btnSaveBN.Anchor = AnchorStyles.Left;
        _btnSaveBN.Margin = new Padding(0, UiTheme.Spacing2, 0, 0);
        _btnSaveBN.Click += BtnSaveBN_Click;
        fl.Controls.Add(_btnSaveBN, 0, 7);
        split.Panel2.Controls.Add(fl);

        page.Controls.Add(split);
        page.Enter += (_, _) => LoadBN();
        return page;
    }

    private void LoadBN()
    {
        TryCatch(() =>
        {
            // VPD filter: chỉ trả BN liên quan đến HSBA của BS này
            // Note: CCCD không hiển thị ở grid để hạn chế lộ thông tin nhạy cảm
            _dgvBN.DataSource = _db.Query(
                "SELECT MABN, TENBN, PHAI, TO_CHAR(NGAYSINH,'DD/MM/YYYY') AS NGAYSINH, " +
                "TINHTP FROM BVADMIN.BENHNHAN ORDER BY TENBN");
        });
    }

    private void LoadBNDetail()
    {
        if (_dgvBN.CurrentRow is null) return;
        TryCatch(() =>
        {
            var mabn = _dgvBN.CurrentRow.Cells["MABN"].Value?.ToString() ?? "";
            _lblBNId.Text = $"MABN: {mabn}";
            var dt = _db.Query(
                "SELECT TO_NCHAR(TIENSUBENH) AS TSB, TO_NCHAR(TIENSUBENHGD) AS TSBGD, DIUNGTHUOC " +
                "FROM BVADMIN.BENHNHAN WHERE MABN = :id",
                OracleHelper.Param("id", mabn));
            if (dt.Rows.Count > 0)
            {
                _txtTSB.Text   = dt.Rows[0]["TSB"]?.ToString() ?? "";
                _txtTSBGD.Text = dt.Rows[0]["TSBGD"]?.ToString() ?? "";
                _txtDiung.Text = dt.Rows[0]["DIUNGTHUOC"]?.ToString() ?? "";
            }
        });
    }

    private void BtnSaveBN_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var mabn = ExtractLabel(_lblBNId);
            if (string.IsNullOrEmpty(mabn)) { ShowError("Chọn bệnh nhân."); return; }
            _db.Execute(
                "UPDATE BVADMIN.BENHNHAN SET TIENSUBENH=:t,TIENSUBENHGD=:g,DIUNGTHUOC=:d WHERE MABN=:id",
                OracleHelper.Param("t",  _txtTSB.Text),
                OracleHelper.Param("g",  _txtTSBGD.Text),
                OracleHelper.Param("d",  _txtDiung.Text),
                OracleHelper.Param("id", mabn));
            ShowSuccess("Cập nhật tiền sử bệnh thành công.");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: THÔNG BÁO (OLS)
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildThongBaoTab()
    {
        var page = new TabPage("Thông báo");
        var lblLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 28,
            Padding = new Padding(8, 6, 0, 0),
            Font = UiTheme.LabelBold(),
            ForeColor = Color.FromArgb(0, 80, 60),
            Text = "Nhãn OLS: (chưa tải)"
        };
        var dgv  = MakeGrid(); dgv.Dock = DockStyle.Fill;
        var btn  = Btn("Tải thông báo", UiTheme.HealthCyan); btn.Dock = DockStyle.Top;
        btn.Click += (_, _) =>
        {
            TryCatch(() =>
            {
                lblLabel.Text = "Nhãn OLS: " + CurrentOlsLabel();
                // OLS tự filter: BS chỉ thấy thông báo phù hợp với nhãn của mình
                dgv.DataSource = _db.Query(
                    "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG),1,100) AS NOIDUNG, " +
                    "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                    "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
            });
        };
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

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static DataGridView MakeGrid() => UiTheme.Grid();

    private static TextBox TB(int width, int height = 30) =>
        UiTheme.Pad(new() { Multiline = height > 30, Width = width, Height = Math.Max(height, 30),
                Font = UiTheme.Body(), BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = height > 30 ? ScrollBars.Vertical : ScrollBars.None });

    // Ô textarea trải hết bề ngang ô chứa (Dock=Fill) + co giãn theo hàng Percent, có chiều cao tối thiểu.
    private static TextBox TBFill(int minHeight = 80) =>
        UiTheme.Pad(new()
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            MinimumSize = new Size(0, minHeight),
            Margin = new Padding(0, 0, 0, 8),
            Font = UiTheme.Body(),
            BorderStyle = BorderStyle.FixedSingle,
            ScrollBars = ScrollBars.Vertical
        });

    private static Button Btn(string text, Color color, int width = 140)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            MinimumSize = new Size(width, 38),
            BackColor = color,
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

    private static string ExtractLabel(Label lbl)
    {
        var parts = lbl.Text.Split(':');
        return parts.Length > 1 ? parts[1].Trim() : "";
    }

    private void TryCatch(Action a, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        try { a(); }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, $"BS.{caller}", ex.Message);
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
                "SELECT LBACSYS.fn_my_ols_label('BV_LABEL_POLICY') FROM DUAL")?.ToString() ?? "(chưa gán)";
        }
        catch { return "(không đọc được DBA_SA_USER_LABELS)"; }
    }
}

// ── Dialog thêm DV ────────────────────────────────────────────────────────────
internal class AddDVDialog : Form
{
    public string LoaiDV { get; private set; } = "";
    public string MaKTV  { get; private set; } = "";

    private static readonly string[] _commonLoaiDV =
    {
        "Xét nghiệm máu tổng quát", "Xét nghiệm nước tiểu", "Siêu âm tim",
        "Siêu âm bụng", "X-quang ngực", "CT scan", "MRI", "Điện não đồ",
        "Đo chức năng hô hấp", "Nội soi tiêu hóa", "Điện tim"
    };

    public AddDVDialog(System.Data.DataTable ktvList)
    {
        Text = "Thêm Dịch vụ chẩn đoán";
        // FIX (Ảnh 4): dùng ClientSize (không phải Size) để title bar/viền không ăn vào vùng dùng được,
        // và pin AutoScaleMode để khi màn hình scale (125%/150%) vùng client giãn theo control -> không cắt nút.
        AutoScaleMode = AutoScaleMode.Font;
        AutoScaleDimensions = new SizeF(96f, 96f);
        ClientSize = new Size(420, 200);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var cbLoai = new ComboBox
        {
            Location = new Point(120, 20), Width = 260,
            DropDownStyle = ComboBoxStyle.DropDown
        };
        cbLoai.Items.AddRange(_commonLoaiDV);

        var cbKtv = new ComboBox
        {
            Location = new Point(120, 60), Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList,
            DisplayMember = "DISPLAY", ValueMember = "MANV",
            DataSource = ktvList
        };

        var ok = new Button
        {
            Text = "Thêm", Width = 90, Height = 32,
            Location = new Point(ClientSize.Width - 90 - 90 - 20, ClientSize.Height - 32 - 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = "Hủy", Width = 90, Height = 32,
            Location = new Point(ClientSize.Width - 90 - 16, ClientSize.Height - 32 - 16),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[]
        {
            new Label { Text = "Loại DV:", Location = new Point(20, 23), AutoSize = true },
            cbLoai,
            new Label { Text = "Mã KTV:", Location = new Point(20, 63), AutoSize = true },
            cbKtv, ok, cancel
        });

        AcceptButton = ok;
        CancelButton = cancel;
        ok.Click += (_, _) =>
        {
            LoaiDV = cbLoai.Text.Trim();
            MaKTV  = cbKtv.SelectedValue?.ToString() ?? "";
        };
    }
}
