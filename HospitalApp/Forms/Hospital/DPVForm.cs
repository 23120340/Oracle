using System.Data;
using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Điều phối viên (DPV_Role + VPD).
/// Quản lý BỆNHNHÂN, tạo HSBA, điều phối BS và KTV.
/// </summary>
public class DPVForm : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private TabControl _tabs = null!;

    // BENHNHAN tab
    private DataGridView _dgvBN = null!;
    private TextBox _txtMABN, _txtTENBN, _txtCCCD, _txtDiaChi = null!;
    private ComboBox _cmbPhai = null!;
    private DateTimePicker _dtpNgaySinh = null!;
    private Button _btnSaveBN = null!, _btnNewBN = null!, _btnDelBN = null!;
    private bool _isNewBN;

    // HSBA tab
    private DataGridView _dgvHSBA = null!;
    private Label _lblHSBAInfo = null!;
    private ComboBox _cmbBNForHSBA = null!, _cmbBS = null!;
    private TextBox _txtMakhoa = null!;
    private Button _btnCreateHSBA = null!, _btnAssignBS = null!,
                   _btnAssignKTV  = null!;
    private DataGridView _dgvDV = null!;

    public DPVForm(OracleHelper db)
    {
        _db = db;
        Text = $"Giao diện Điều phối viên – {db.Username}";
        Size = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        BackColor = Color.FromArgb(255, 250, 240);

        // silence nullable warnings for fields initialized in BuildUI
        _txtMABN = _txtTENBN = _txtCCCD = _txtDiaChi = new TextBox();
        _cmbPhai = new ComboBox();
        _dtpNgaySinh = new DateTimePicker();

        BuildUI();
        WireShortcuts();

        _session = new SessionManager(this, db.Username);
        FormClosed += (_, _) => _session.Dispose();
    }

    private void WireShortcuts()
    {
        KeyPreview = true;
        KeyDown += (_, e) =>
        {
            // F5: Refresh tab hiện tại
            if (e.KeyCode == Keys.F5)
            {
                if (_tabs.SelectedIndex == 0) LoadBN();
                else if (_tabs.SelectedIndex == 1) { LoadHSBA(); LoadBSList(); LoadBNList(); }
                e.Handled = true;
            }
            // Ctrl+L: Đăng xuất
            else if (e.Control && e.KeyCode == Keys.L)
            {
                if (MessageBox.Show("Đăng xuất?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Close();
                e.Handled = true;
            }
            // Ctrl+N: Thêm BN (chỉ ở tab Bệnh nhân)
            else if (e.Control && e.KeyCode == Keys.N && _tabs.SelectedIndex == 0)
            {
                _isNewBN = true; ClearBNForm();
                e.Handled = true;
            }
            // Ctrl+S: Lưu BN (chỉ ở tab Bệnh nhân)
            else if (e.Control && e.KeyCode == Keys.S && _tabs.SelectedIndex == 0)
            {
                BtnSaveBN_Click(null, EventArgs.Empty);
                e.Handled = true;
            }
        };
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
        _tabs.TabPages.Add(BuildBNTab());
        _tabs.TabPages.Add(BuildHSBATab());
        _tabs.TabPages.Add(BuildThongBaoTab());
        _tabs.TabPages.Add(BuildMyProfileTab());

        var header = BuildAppHeader("Điều phối viên", "ĐPV", UiTheme.RoleDPV);

        var sidebar = new Sidebar { AccentColor = UiTheme.RoleDPV, Dock = DockStyle.Left };
        sidebar.AddBrand("HospitalApp", _db.Username);
        sidebar.AddSection("Làm việc");
        sidebar.AddItem("bn",      IconRegistry.People,   "Bệnh nhân");
        sidebar.AddItem("hsba",    IconRegistry.Document, "Hồ sơ bệnh án");
        sidebar.AddSection("Thông tin");
        sidebar.AddItem("tb",      IconRegistry.Bell,     "Thông báo");
        sidebar.AddItem("profile", IconRegistry.Person,   "Thông tin của tôi");
        sidebar.ItemSelected += key =>
        {
            _tabs.SelectedIndex = key switch
            { "bn" => 0, "hsba" => 1, "tb" => 2, "profile" => 3, _ => 0 };
        };

        var status = new StatusBar
        {
            LeftText   = $"{IconRegistry.Database}  {_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  Vai trò: Điều phối viên"
        };

        Controls.Add(_tabs);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("bn");
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
            TextAlign = ContentAlignment.MiddleLeft
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
            // Logout button luôn ở phải, chip nằm bên trái logout — không overlap
            btnLogout.Location = new Point(header.Width - btnLogout.Width - 16, 12);
            roleChip.Location  = new Point(btnLogout.Left - roleChip.Width - 12, 17);
        }
        header.Resize += (_, _) => layout();
        // Chip width tính lại sau khi đo text
        roleChip.HandleCreated += (_, _) => layout();
        header.Controls.Add(roleChip);
        header.Controls.Add(btnLogout);
        header.Controls.Add(lblTitle);
        layout();
        return header;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: QUẢN LÝ BỆNH NHÂN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildBNTab()
    {
        var page = new TabPage("Bệnh nhân");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 350 };

        // Left: danh sách
        _dgvBN = MakeGrid(); _dgvBN.Dock = DockStyle.Fill;
        _dgvBN.SelectionChanged += (_, _) => LoadBNDetail();
        var toolBN = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 44, Padding = new Padding(4),
            FlowDirection = FlowDirection.LeftToRight
        };
        _btnNewBN = Btn("Thêm BN", UiTheme.HealthGreen);
        _btnNewBN.Click += (_, _) => { _isNewBN = true; ClearBNForm(); };
        toolBN.Controls.Add(_btnNewBN);
        toolBN.Controls.Add(Btn("Làm mới", UiTheme.HealthCyan, onClick: (_, _) => LoadBN()));

        var search = new SearchBox { Width = 280, Placeholder = "Tìm theo Họ tên / Mã BN..." };
        toolBN.Controls.Add(search);
        // Attach sau khi LoadBN có DataTable
        _dgvBN.DataBindingComplete += (_, _) => search.AttachTo(_dgvBN, "TENBN", "MABN", "CCCD");

        split.Panel1.Controls.Add(_dgvBN);
        split.Panel1.Controls.Add(toolBN);

        // Right: form chi tiết
        var fl = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(10), AutoScroll = true
        };

        fl.Controls.Add(BoldLabel("Thông tin Bệnh nhân"));
        fl.Controls.Add(Lbl("Mã BN:"));     _txtMABN  = TB(200); fl.Controls.Add(_txtMABN);
        fl.Controls.Add(Lbl("Họ tên:"));    _txtTENBN = TB(260); fl.Controls.Add(_txtTENBN);
        fl.Controls.Add(Lbl("Phái:"));
        _cmbPhai = new ComboBox { Width = 80, DropDownStyle = ComboBoxStyle.DropDownList };
        _cmbPhai.Items.AddRange(new[] { "M", "F" }); _cmbPhai.SelectedIndex = 0;
        fl.Controls.Add(_cmbPhai);
        fl.Controls.Add(Lbl("Ngày sinh:"));
        _dtpNgaySinh = new DateTimePicker { Width = 200, Format = DateTimePickerFormat.Short };
        fl.Controls.Add(_dtpNgaySinh);
        fl.Controls.Add(Lbl("CCCD:"));    _txtCCCD   = TB(200); fl.Controls.Add(_txtCCCD);
        fl.Controls.Add(Lbl("Địa chỉ:")); _txtDiaChi = TB(300); fl.Controls.Add(_txtDiaChi);

        var btnRow = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _btnSaveBN = Btn("Lưu", UiTheme.RoleDPV);
        _btnSaveBN.Click += BtnSaveBN_Click;
        // Lưu ý: DPV không có quyền DELETE (TC#2). Không thêm nút xoá vào UI.
        // _btnDelBN giữ lại để tương thích nhưng không hiển thị.
        _btnDelBN  = Btn("Xóa BN", UiTheme.Danger);
        _btnDelBN.Visible = false;
        _btnDelBN.Click += BtnDelBN_Click;
        btnRow.Controls.Add(_btnSaveBN);
        fl.Controls.Add(btnRow);
        split.Panel2.Controls.Add(fl);

        page.Controls.Add(split);
        page.Enter += (_, _) => LoadBN();
        return page;
    }

    private void LoadBN()
    {
        TryCatch(() =>
        {
            _dgvBN.DataSource = _db.Query(
                "SELECT MABN, TENBN, PHAI, TO_CHAR(NGAYSINH,'DD/MM/YYYY') AS NGAYSINH, " +
                "CCCD, TINHTP FROM BVADMIN.BENHNHAN ORDER BY TENBN");

            // Bảo mật: mask CCCD ở danh sách (chỉ form chi tiết mới hiện đầy đủ)
            _dgvBN.CellFormatting -= MaskCccdHandler;
            _dgvBN.CellFormatting += MaskCccdHandler;
        });
    }

    private void MaskCccdHandler(object? s, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || _dgvBN.Columns[e.ColumnIndex].Name != "CCCD") return;
        e.Value = InputValidator.MaskCccd(e.Value?.ToString());
        e.FormattingApplied = true;
    }

    private void LoadBNDetail()
    {
        if (_dgvBN.CurrentRow is null || _isNewBN) return;
        TryCatch(() =>
        {
            var mabn = _dgvBN.CurrentRow.Cells["MABN"].Value?.ToString() ?? "";
            var dt = _db.Query(
                "SELECT MABN,TENBN,PHAI,NGAYSINH,CCCD," +
                "SONHA||' '||TENDUONG||', '||QUANHUYEN||', '||TINHTP AS DIACHI " +
                "FROM BVADMIN.BENHNHAN WHERE MABN=:id",
                OracleHelper.Param("id", mabn));
            if (dt.Rows.Count == 0) return;
            var r = dt.Rows[0];
            _isNewBN         = false;
            _txtMABN.Text    = r["MABN"]?.ToString() ?? "";
            _txtTENBN.Text   = r["TENBN"]?.ToString() ?? "";
            _cmbPhai.Text    = r["PHAI"]?.ToString() ?? "M";
            if (r["NGAYSINH"] != DBNull.Value)
                _dtpNgaySinh.Value = Convert.ToDateTime(r["NGAYSINH"]);
            _txtCCCD.Text    = r["CCCD"]?.ToString() ?? "";
            _txtDiaChi.Text  = r["DIACHI"]?.ToString() ?? "";
            _txtMABN.ReadOnly = true;
        });
    }

    private void ClearBNForm()
    {
        _txtMABN.ReadOnly = false;
        _txtMABN.Clear(); _txtTENBN.Clear(); _txtCCCD.Clear(); _txtDiaChi.Clear();
        _dtpNgaySinh.Value = DateTime.Today.AddYears(-30);
        _cmbPhai.SelectedIndex = 0;
    }

    private void BtnSaveBN_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (string.IsNullOrWhiteSpace(_txtMABN.Text) || string.IsNullOrWhiteSpace(_txtTENBN.Text))
            { ShowError("Nhập đủ Mã BN và Họ tên."); return; }

            if (_isNewBN)
            {
                // Validate CCCD ở app layer (chống dữ liệu xấu trước khi tới DB)
                if (!System.Text.RegularExpressions.Regex.IsMatch(_txtCCCD.Text.Trim(), @"^\d{12}$"))
                { ShowError("CCCD phải đúng 12 chữ số."); return; }

                // Gọi procedure tạo BN + Oracle account (TC#1)
                using var conn = _db.OpenConnection();
                using var cmd  = new OracleCommand("BVADMIN.sp_create_benhnhan_full", conn)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };
                cmd.Parameters.Add(new OracleParameter("p_mabn",      _txtMABN.Text.Trim()));
                cmd.Parameters.Add(new OracleParameter("p_tenbn",     _txtTENBN.Text.Trim()));
                cmd.Parameters.Add(new OracleParameter("p_phai",      _cmbPhai.Text));
                cmd.Parameters.Add(new OracleParameter("p_ngaysinh",  _dtpNgaySinh.Value));
                cmd.Parameters.Add(new OracleParameter("p_cccd",      _txtCCCD.Text.Trim()));
                cmd.Parameters.Add(new OracleParameter("p_sonha",     DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_tenduong",  DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_quanhuyen", DBNull.Value));
                cmd.Parameters.Add(new OracleParameter("p_tinhtp",    _txtDiaChi.Text.Trim()));
                cmd.ExecuteNonQuery();

                AppAuditLogger.Info(_db.Username, "DPV.NewBN", $"mabn={_txtMABN.Text.Trim()}");
                Toast.Show(this, $"Đã tạo BN {_txtMABN.Text.Trim()} + tài khoản BN_{_txtMABN.Text.Trim()}", Toast.Kind.Success);
                MessageBox.Show($"Mật khẩu mặc định cho tài khoản BN_{_txtMABN.Text.Trim()}: BV@2025!\n\n" +
                                "Vui lòng hướng dẫn bệnh nhân đổi mật khẩu khi đăng nhập lần đầu.",
                                "Thông tin tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _isNewBN = false;
            }
            else
            {
                _db.Execute(
                    "UPDATE BVADMIN.BENHNHAN SET TENBN=:t,PHAI=:p,NGAYSINH=:n,CCCD=:c,TINHTP=:tp WHERE MABN=:m",
                    OracleHelper.Param("t",  _txtTENBN.Text.Trim()),
                    OracleHelper.Param("p",  _cmbPhai.Text),
                    OracleHelper.Param("n",  _dtpNgaySinh.Value),
                    OracleHelper.Param("c",  _txtCCCD.Text.Trim()),
                    OracleHelper.Param("tp", _txtDiaChi.Text.Trim()),
                    OracleHelper.Param("m",  _txtMABN.Text.Trim()));
                Toast.Show(this, "Đã cập nhật bệnh nhân", Toast.Kind.Success);
            }
            LoadBN();
        });
    }

    private void BtnDelBN_Click(object? s, EventArgs e)
    {
        // Điều phối viên KHÔNG có quyền DELETE bệnh nhân (chính sách bảo mật TC#2).
        // Việc xoá hồ sơ bệnh nhân phải do DBA thực hiện sau khi có quyết định nghiệp vụ.
        ShowError("Điều phối viên không có quyền xoá bệnh nhân.\nLiên hệ DBA nếu cần huỷ hồ sơ.");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: QUẢN LÝ HSBA
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildHSBATab()
    {
        var page = new TabPage("Hồ sơ bệnh án");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
            SplitterDistance = 260, IsSplitterFixed = false
        };

        // ── Top: danh sách HSBA + toolbar ─────────────────────────────────────
        _dgvHSBA = MakeGrid();
        _dgvHSBA.Dock = DockStyle.Fill;
        _dgvHSBA.SelectionChanged += (_, _) => LoadDV();

        var topBar = new Panel
        {
            Dock = DockStyle.Top, Height = 48,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = UiTheme.Surface
        };
        var btnReload = Btn("Tải lại danh sách", UiTheme.HealthCyan, width: 160);
        btnReload.Click += (_, _) => LoadHSBA();
        btnReload.Dock = DockStyle.Left;
        topBar.Controls.Add(btnReload);
        split.Panel1.Controls.Add(_dgvHSBA);
        split.Panel1.Controls.Add(topBar);

        // ── Bottom: 2 cột — tạo/điều phối (trái) + dịch vụ (phải) ────────────
        var bot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            Padding = new Padding(12), BackColor = UiTheme.BgLight
        };
        bot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        bot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

        // ── Card trái: Tạo HSBA mới / Điều phối ──────────────────────────────
        var coordCard = new Panel
        {
            Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(0, 0, 8, 0)
        };
        var coordTitle = new Label
        {
            Text = "Tạo HSBA mới / Điều phối",
            Dock = DockStyle.Top, Height = 28,
            Font = UiTheme.LabelBold(11f),
            ForeColor = UiTheme.TextDark
        };
        coordCard.Controls.Add(coordTitle);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0, 12, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 4; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _cmbBNForHSBA = new ComboBox
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4),
            DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body(10f)
        };
        _txtMakhoa = new TextBox
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4),
            Font = UiTheme.Body(10f)
        };
        _cmbBS = new ComboBox
        {
            Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4),
            DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body(10f)
        };

        grid.Controls.Add(GridLabel("Bệnh nhân"), 0, 0);
        grid.Controls.Add(_cmbBNForHSBA,          1, 0);
        grid.Controls.Add(GridLabel("Khoa"),      0, 1);
        grid.Controls.Add(_txtMakhoa,             1, 1);
        grid.Controls.Add(GridLabel("Bác sĩ"),    0, 2);
        grid.Controls.Add(_cmbBS,                 1, 2);

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            BackColor = UiTheme.Surface, Padding = new Padding(0, 6, 0, 0)
        };
        _btnAssignBS = Btn("Giao cho bác sĩ", UiTheme.RoleDPV, width: 150);
        _btnAssignBS.Click += BtnAssignBS_Click;
        _btnCreateHSBA = Btn("Tạo HSBA mới", UiTheme.HealthGreen, width: 140);
        _btnCreateHSBA.Click += BtnCreateHSBA_Click;
        btnRow.Controls.Add(_btnAssignBS);
        btnRow.Controls.Add(_btnCreateHSBA);
        grid.Controls.Add(btnRow, 1, 3);

        coordCard.Controls.Add(grid);
        bot.Controls.Add(coordCard, 0, 0);

        // ── Card phải: Dịch vụ chẩn đoán ─────────────────────────────────────
        var dvCard = new Panel
        {
            Dock = DockStyle.Fill, BackColor = UiTheme.Surface,
            Padding = new Padding(18, 14, 18, 14),
            Margin = new Padding(8, 0, 0, 0)
        };
        _lblHSBAInfo = new Label
        {
            Dock = DockStyle.Top, Height = 28,
            Font = UiTheme.LabelBold(11f),
            ForeColor = UiTheme.TextDark,
            Text = "Dịch vụ chẩn đoán (chọn HSBA bên trên)"
        };
        dvCard.Controls.Add(_lblHSBAInfo);

        var dvBottom = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = UiTheme.Surface };
        _btnAssignKTV = Btn("Giao KTV cho dịch vụ", UiTheme.HealthGreen, width: 180);
        _btnAssignKTV.Dock = DockStyle.Right;
        _btnAssignKTV.Click += BtnAssignKTV_Click;
        dvBottom.Controls.Add(_btnAssignKTV);
        dvCard.Controls.Add(dvBottom);

        _dgvDV = MakeGrid();
        _dgvDV.Dock = DockStyle.Fill;
        dvCard.Controls.Add(_dgvDV);

        bot.Controls.Add(dvCard, 1, 0);

        split.Panel2.Controls.Add(bot);
        page.Controls.Add(split);
        page.Enter += (_, _) => { LoadHSBA(); LoadBSList(); LoadBNList(); };
        return page;
    }

    private static Label GridLabel(string text) => new()
    {
        Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
        Font = UiTheme.LabelBold(9.5f), ForeColor = UiTheme.TextDark,
        Margin = new Padding(0, 8, 8, 0)
    };

    private void LoadHSBA()
    {
        TryCatch(() =>
        {
            _dgvHSBA.DataSource = _db.Query(
                "SELECT MAHSBA, MABN, TO_CHAR(NGAY,'DD/MM/YYYY') AS NGAY, " +
                "NVL(MABS,'(chưa giao)') AS MABS, " +
                "NVL(MAKHOA,'(chưa giao)') AS MAKHOA, " +
                "CASE WHEN KETLUAN IS NULL THEN 'Đang điều trị' ELSE 'Đã kết luận' END AS TRANGTHAI " +
                "FROM BVADMIN.HSBA ORDER BY NGAY DESC");
        });
    }

    private void LoadDV()
    {
        if (_dgvHSBA.CurrentRow is null) return;
        TryCatch(() =>
        {
            var mahsba = _dgvHSBA.CurrentRow.Cells["MAHSBA"].Value?.ToString() ?? "";
            _lblHSBAInfo.Text = $"HSBA: {mahsba}";
            _dgvDV.DataSource = _db.Query(
                "SELECT MAHSBA, LOAIDV, NGAYDV, MAKTV, SUBSTR(TO_CHAR(KETQUA),1,50) AS KETQUA " +
                "FROM BVADMIN.HSBA_DV WHERE MAHSBA=:id",
                OracleHelper.Param("id", mahsba));
        });
    }

    private void LoadBSList()
    {
        TryCatch(() =>
        {
            _cmbBS.Items.Clear();
            var dt = _db.Query("SELECT MANV||' - '||HOTEN AS BS FROM BVADMIN.NV_LOOKUP_View WHERE VAITRO='BS' ORDER BY HOTEN");
            foreach (DataRow r in dt.Rows) _cmbBS.Items.Add(r[0].ToString()!);
        });
    }

    private void LoadBNList()
    {
        TryCatch(() =>
        {
            _cmbBNForHSBA.Items.Clear();
            var dt = _db.Query("SELECT MABN||' - '||TENBN FROM BVADMIN.BENHNHAN ORDER BY TENBN");
            foreach (DataRow r in dt.Rows) _cmbBNForHSBA.Items.Add(r[0].ToString()!);
        });
    }

    private void BtnCreateHSBA_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_cmbBNForHSBA.SelectedIndex < 0) { ShowError("Chọn bệnh nhân."); return; }
            var mabn = _cmbBNForHSBA.Text.Split('-')[0].Trim();
            var khoa = _txtMakhoa.Text.Trim();

            // Lấy MAHSBA từ SEQUENCE (không collision dù tạo nhanh)
            var mahsba = _db.Scalar(
                "SELECT BVADMIN.fn_next_mahsba() FROM DUAL")?.ToString() ?? "";

            _db.Execute(
                "INSERT INTO BVADMIN.HSBA(MAHSBA,MABN,NGAY,MAKHOA) VALUES(:h,:b,SYSDATE,:k)",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("b", mabn),
                OracleHelper.Param("k", khoa));
            AppAuditLogger.Info(_db.Username, "DPV.NewHSBA", $"hsba={mahsba}");
            Toast.Show(this, $"Đã tạo HSBA {mahsba}", Toast.Kind.Success);
            LoadHSBA();
        });
    }

    private void BtnAssignBS_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvHSBA.CurrentRow is null || _cmbBS.SelectedIndex < 0)
            { ShowError("Chọn HSBA và Bác sĩ."); return; }
            var mahsba = _dgvHSBA.CurrentRow.Cells["MAHSBA"].Value?.ToString();
            var mabs   = _cmbBS.Text.Split('-')[0].Trim();
            _db.Execute("UPDATE BVADMIN.HSBA SET MABS=:b WHERE MAHSBA=:h",
                OracleHelper.Param("b", mabs),
                OracleHelper.Param("h", mahsba));
            AppAuditLogger.Info(_db.Username, "DPV.AssignBS", $"hsba={mahsba} bs={mabs}");
            Toast.Show(this, $"Đã giao HSBA {mahsba} → BS {mabs}", Toast.Kind.Success);
            LoadHSBA();
        });
    }

    private void BtnAssignKTV_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvDV.CurrentRow is null) { ShowError("Chọn dòng dịch vụ."); return; }
            var mahsba = _dgvDV.CurrentRow.Cells["MAHSBA"].Value?.ToString();
            var loaidv = _dgvDV.CurrentRow.Cells["LOAIDV"].Value?.ToString();

            var maktv = Microsoft.VisualBasic.Interaction.InputBox(
                "Nhập Mã KTV:", "Giao KTV", "");
            if (string.IsNullOrEmpty(maktv)) return;

            _db.Execute(
                "UPDATE BVADMIN.HSBA_DV SET MAKTV=:k WHERE MAHSBA=:h AND LOAIDV=:l",
                OracleHelper.Param("k", maktv),
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("l", loaidv));
            AppAuditLogger.Info(_db.Username, "DPV.AssignKTV", $"hsba={mahsba} dv={loaidv} ktv={maktv}");
            Toast.Show(this, $"Đã giao DV {loaidv} → KTV {maktv}", Toast.Kind.Success);
            LoadDV();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: THÔNG BÁO
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
            ForeColor = Color.FromArgb(80, 60, 30),
            Text = "Nhãn OLS: (chưa tải)"
        };
        var dgv  = MakeGrid(); dgv.Dock = DockStyle.Fill;
        var btn  = Btn("Tải lại", UiTheme.HealthCyan);
        btn.Dock = DockStyle.Top;
        btn.Click += (_, _) => TryCatch(() =>
        {
            lblLabel.Text = "Nhãn OLS: " + CurrentOlsLabel();
            dgv.DataSource = _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG),1,100) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        });
        page.Controls.Add(dgv); page.Controls.Add(lblLabel); page.Controls.Add(btn);
        return page;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static DataGridView MakeGrid() => new()
    {
        ReadOnly = true, AllowUserToAddRows = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        BackgroundColor = Color.White, RowHeadersVisible = false
    };

    private static Label Lbl(string text) =>
        new() { Text = text, AutoSize = true, Font = UiTheme.Body(),
                Padding = new Padding(0, 6, 4, 0) };

    private static Label BoldLabel(string text) =>
        new() { Text = text, AutoSize = true, Font = UiTheme.Heading3(),
                ForeColor = Color.FromArgb(180, 100, 0), Padding = new Padding(0, 4, 0, 4) };

    private static TextBox TB(int width) =>
        new() { Width = width, Height = 24, Font = UiTheme.Body() };

    private static Button Btn(string text, Color color, int width = 130,
                              EventHandler? onClick = null)
    {
        var btn = new Button
        {
            Text = text, Width = width, Height = 32, BackColor = color,
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Body(), Cursor = Cursors.Hand
        };
        if (onClick is not null) btn.Click += onClick;
        return btn;
    }

    private void TryCatch(Action a, [System.Runtime.CompilerServices.CallerMemberName] string caller = "")
    {
        try { a(); }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, $"DPV.{caller}", ex.Message);
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
                "SELECT NVL(MAX(LABEL), '(chưa gán)') " +
                "FROM DBA_SA_USER_LABELS " +
                "WHERE POLICY_NAME='BV_LABEL_POLICY' AND USER_NAME=USER")?.ToString() ?? "(chưa gán)";
        }
        catch { return "(không đọc được DBA_SA_USER_LABELS)"; }
    }
}
