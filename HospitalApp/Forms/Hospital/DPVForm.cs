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
    private ComboBox _cmbKhoa = null!;
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
        // Audit là chức năng của DBA (Yêu cầu 3 spec) — không cấp cho DPV

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
            LeftText   = $"{_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  Vai trò: Điều phối viên"
        };

        Controls.Add(_tabs);
        Controls.Add(header);
        Controls.Add(sidebar);
        Controls.Add(status);

        sidebar.SelectByKey("bn");

        // Force load lookup data ngay khi form hiện ra (không chờ tab.Enter)
        Shown += (_, _) =>
        {
            LoadBN();
            LoadHSBA();
            LoadBSList();
            LoadBNList();
        };
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
            lblTitle.Width = Math.Max(140, roleChip.Left - lblTitle.Left - 16);
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
        var page = new TabPage("Bệnh nhân") { BackColor = UiTheme.BgLight };

        // Shell 2 cột: danh sách (trái) | form chi tiết (phải).
        // Dùng TableLayoutPanel thay SplitContainer → không bao giờ có overlap; cột phải
        // luôn rộng cố định, cột trái Percent(100) nuốt phần dư. Bọc trong padding BgLight.
        var split = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = UiTheme.BgLight,
            Padding = new Padding(UiTheme.Spacing4),
            Margin = Padding.Empty
        };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        split.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        // ── Trái: toolbar + danh sách (mỗi phần một ô riêng) ──────────────────
        _dgvBN = MakeGrid(); _dgvBN.Dock = DockStyle.Fill; _dgvBN.Margin = Padding.Empty;
        _dgvBN.SelectionChanged += (_, _) => LoadBNDetail();

        // Toolbar: FlowLayoutPanel CHỈ chứa một hàng ngang nút + search (hợp lệ theo rule 3)
        var toolBN = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Margin = Padding.Empty,
            Padding = new Padding(0, 0, 0, UiTheme.Spacing2),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false
        };
        _btnNewBN = Btn("Thêm BN", UiTheme.HealthGreen);
        _btnNewBN.Margin = new Padding(0, 0, UiTheme.Spacing2, 0);
        _btnNewBN.Click += (_, _) => { _isNewBN = true; ClearBNForm(); };
        toolBN.Controls.Add(_btnNewBN);
        var btnReloadBN = Btn("Làm mới", UiTheme.HealthCyan, onClick: (_, _) => LoadBN());
        btnReloadBN.Margin = new Padding(0, 0, UiTheme.Spacing3, 0);
        toolBN.Controls.Add(btnReloadBN);

        var search = new SearchBox { Width = 300, Placeholder = "Tìm theo Họ tên / Mã BN...", Margin = new Padding(0) };
        toolBN.Controls.Add(search);
        // Attach sau khi LoadBN có DataTable
        _dgvBN.DataBindingComplete += (_, _) => search.AttachTo(_dgvBN, "TENBN", "MABN", "CCCD");

        // Grid bọc trong Card để đồng bộ với các tab khác
        var listCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(UiTheme.Spacing2), Margin = Padding.Empty };
        listCard.Controls.Add(_dgvBN);

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, UiTheme.Spacing3, 0),
            Padding = Padding.Empty,
            BackColor = UiTheme.BgLight
        };
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // toolbar (SearchBox 38px + đệm) — ô riêng
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // danh sách nuốt phần dư
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        leftLayout.Controls.Add(toolBN, 0, 0);
        leftLayout.Controls.Add(listCard, 0, 1);
        split.Controls.Add(leftLayout, 0, 0);

        // ── Phải: form chi tiết trong Card + TableLayoutPanel (KHÔNG FlowLayoutPanel) ──
        // Mỗi nhãn ở ô AutoSize riêng, mỗi input ở ô Absolute riêng → không đè nhau,
        // không bị ép chiều cao. Hàng spacer Percent(100) nuốt slack, hàng nút riêng 56px.
        var detailCard = new Card { Dock = DockStyle.Fill, Padding = new Padding(UiTheme.Spacing4), Margin = Padding.Empty };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1,
            BackColor = UiTheme.Surface,
            Margin = Padding.Empty, Padding = Padding.Empty,
            AutoScroll = true   // safety net nếu cao không đủ
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        int r = 0;
        void AddLabelRow(Control c)
        {
            form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            c.Margin = new Padding(0, UiTheme.Spacing2, 0, 2);
            form.Controls.Add(c, 0, r++);
        }
        void AddInputRow(Control c, int height = 30)
        {
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, height + 8));
            c.Dock = DockStyle.Top;
            c.Margin = new Padding(0, 0, 0, UiTheme.Spacing2);
            form.Controls.Add(c, 0, r++);
        }

        var titleBN = BoldLabel("Thông tin Bệnh nhân");
        titleBN.Margin = new Padding(0, 0, 0, UiTheme.Spacing2);
        form.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        form.Controls.Add(titleBN, 0, r++);

        AddLabelRow(Lbl("Mã BN:"));
        _txtMABN  = TB(200); AddInputRow(_txtMABN);
        AddLabelRow(Lbl("Họ tên:"));
        _txtTENBN = TB(260); AddInputRow(_txtTENBN);

        AddLabelRow(Lbl("Phái:"));
        _cmbPhai = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = UiTheme.Body(), Height = 28 };
        _cmbPhai.Items.AddRange(new[] { "M", "F" }); _cmbPhai.SelectedIndex = 0;
        AddInputRow(_cmbPhai, 28);

        AddLabelRow(Lbl("Ngày sinh:"));
        _dtpNgaySinh = new DateTimePicker { Format = DateTimePickerFormat.Short, Font = UiTheme.Body() };
        AddInputRow(_dtpNgaySinh, 28);

        AddLabelRow(Lbl("CCCD:"));
        _txtCCCD  = TB(200); AddInputRow(_txtCCCD);
        AddLabelRow(Lbl("Địa chỉ:"));
        _txtDiaChi = TB(300); AddInputRow(_txtDiaChi);

        // Hàng spacer nuốt slack — đẩy nút xuống đáy, các input ở trên không bị giãn
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        form.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface, Margin = Padding.Empty }, 0, r++);

        // Hàng nút riêng 56px — không bao giờ đè nội dung phía trên
        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, AutoScroll = false,
            BackColor = UiTheme.Surface, Margin = new Padding(0, UiTheme.Spacing2, 0, 0)
        };
        _btnSaveBN = Btn("Lưu", UiTheme.RoleDPV);
        _btnSaveBN.Click += BtnSaveBN_Click;
        // Lưu ý: DPV không có quyền DELETE (TC#2). Không thêm nút xoá vào UI.
        // _btnDelBN giữ lại để tương thích nhưng không hiển thị.
        _btnDelBN  = Btn("Xóa BN", UiTheme.Danger);
        _btnDelBN.Visible = false;
        _btnDelBN.Click += BtnDelBN_Click;
        btnRow.Controls.Add(_btnSaveBN);
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        form.Controls.Add(btnRow, 0, r++);

        detailCard.Controls.Add(form);
        split.Controls.Add(detailCard, 1, 0);

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
                    CommandType = System.Data.CommandType.StoredProcedure,
                    BindByName  = true   // bind theo tên cho chắc đúng tham số
                };
                // Dùng OracleHelper.Param → chuỗi bind NVarchar2 (tiếng Việt round-trip đúng)
                cmd.Parameters.Add(OracleHelper.Param("p_mabn",      _txtMABN.Text.Trim()));
                cmd.Parameters.Add(OracleHelper.Param("p_tenbn",     _txtTENBN.Text.Trim()));
                cmd.Parameters.Add(OracleHelper.Param("p_phai",      _cmbPhai.Text));
                cmd.Parameters.Add(OracleHelper.Param("p_ngaysinh",  _dtpNgaySinh.Value));
                cmd.Parameters.Add(OracleHelper.Param("p_cccd",      _txtCCCD.Text.Trim()));
                cmd.Parameters.Add(OracleHelper.Param("p_sonha",     DBNull.Value));
                cmd.Parameters.Add(OracleHelper.Param("p_tenduong",  DBNull.Value));
                cmd.Parameters.Add(OracleHelper.Param("p_quanhuyen", DBNull.Value));
                cmd.Parameters.Add(OracleHelper.Param("p_tinhtp",    _txtDiaChi.Text.Trim()));
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
        var btnReload = Btn("Tải lại danh sách", UiTheme.HealthCyan, width: 220);
        btnReload.Click += (_, _) => LoadHSBA();
        btnReload.Dock = DockStyle.Left;
        topBar.Controls.Add(btnReload);
        var hsbaTopLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        hsbaTopLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        hsbaTopLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        hsbaTopLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.Dock = DockStyle.Fill;
        topBar.Margin = Padding.Empty;
        _dgvHSBA.Margin = Padding.Empty;
        hsbaTopLayout.Controls.Add(topBar, 0, 0);
        hsbaTopLayout.Controls.Add(_dgvHSBA, 0, 1);
        split.Panel1.Controls.Add(hsbaTopLayout);

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
            Dock = DockStyle.Fill, Height = 32,
            Font = UiTheme.LabelBold(11f),
            ForeColor = UiTheme.TextDark,
            TextAlign = ContentAlignment.MiddleLeft
        };
        var coordLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.Surface
        };
        coordLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));   // tiêu đề
        coordLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // FIX: 3 combo — AutoSize để KHÔNG bị ép nhỏ / cắt hàng Bác sĩ
        coordLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // FIX: hàng nút riêng, đủ cao cho nút + padding
        coordLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        coordLayout.Controls.Add(coordTitle, 0, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0, 4, 0, 0),
            Margin = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 3; i++)
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // FIX: đủ cao cho combo 28px + margin

        _cmbBNForHSBA = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 10, 0, 4),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UiTheme.Body(10f),
            Height = 28
        };
        _cmbKhoa = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 10, 0, 4),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UiTheme.Body(10f),
            Height = 28
        };
        _cmbKhoa.Items.AddRange(new object[]
        {
            "Tim mạch", "Thần kinh", "Tiêu hóa", "Hô hấp",
            "Nội tiết", "Cơ xương khớp", "Sản phụ khoa", "Nhi khoa",
            "Da liễu", "Tai mũi họng", "Mắt", "Răng hàm mặt",
            "Truyền nhiễm", "Cấp cứu", "Hồi sức tích cực"
        });
        _cmbBS = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 10, 0, 4),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = UiTheme.Body(10f),
            Height = 28
        };

        grid.Controls.Add(GridLabel("Bệnh nhân"), 0, 0);
        grid.Controls.Add(_cmbBNForHSBA,          1, 0);
        grid.Controls.Add(GridLabel("Khoa"),      0, 1);
        grid.Controls.Add(_cmbKhoa,               1, 1);
        grid.Controls.Add(GridLabel("Bác sĩ"),    0, 2);
        grid.Controls.Add(_cmbBS,                 1, 2);

        var btnRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            BackColor = UiTheme.Surface, Padding = new Padding(0, 6, 0, 0)
        };
        _btnAssignBS = Btn("Giao cho bác sĩ", UiTheme.RoleDPV, width: 150);
        _btnAssignBS.Width = 190;
        _btnAssignBS.MinimumSize = new Size(190, _btnAssignBS.Height);
        _btnAssignBS.Click += BtnAssignBS_Click;
        _btnCreateHSBA = Btn("Tạo HSBA mới", UiTheme.HealthGreen, width: 140);
        _btnCreateHSBA.Width = 160;
        _btnCreateHSBA.MinimumSize = new Size(160, _btnCreateHSBA.Height);
        _btnCreateHSBA.Click += BtnCreateHSBA_Click;
        btnRow.Controls.Add(_btnAssignBS);
        btnRow.Controls.Add(_btnCreateHSBA);

        coordLayout.Controls.Add(grid, 0, 1);
        coordLayout.Controls.Add(btnRow, 0, 2);   // FIX: hàng nút nằm dưới cùng, luôn hiển thị đủ
        coordCard.Controls.Add(coordLayout);
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
        _dgvDV = MakeGrid();
        _dgvDV.Dock = DockStyle.Fill;
        _dgvDV.Margin = Padding.Empty;

        var dvLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        dvLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        dvLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        dvLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dvBottom.Dock = DockStyle.Fill;
        dvBottom.Margin = Padding.Empty;
        dvLayout.Controls.Add(_dgvDV, 0, 0);
        dvLayout.Controls.Add(dvBottom, 0, 1);
        dvCard.Controls.Add(dvLayout);

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
                "SELECT MAHSBA, LOAIDV, NGAYDV, MAKTV, SUBSTR(TO_NCHAR(KETQUA),1,50) AS KETQUA " +
                "FROM BVADMIN.HSBA_DV WHERE MAHSBA=:id",
                OracleHelper.Param("id", mahsba));
        });
    }

    private void LoadBSList()
    {
        try
        {
            _cmbBS.Items.Clear();
            var dt = _db.Query("SELECT MANV||' - '||HOTEN AS BS FROM BVADMIN.NV_LOOKUP_View WHERE VAITRO='BS' ORDER BY HOTEN");
            foreach (DataRow r in dt.Rows) _cmbBS.Items.Add(r[0].ToString()!);
            if (_cmbBS.Items.Count == 0)
                _cmbBS.Items.Add("(không có bác sĩ — chạy 11_NV_Lookup_Grants.sql)");
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "DPV.LoadBSList", ex.Message);
            _cmbBS.Items.Clear();
            _cmbBS.Items.Add($"(lỗi: {OracleErrorMapper.Short(ex)})");
        }
    }

    private void LoadBNList()
    {
        try
        {
            _cmbBNForHSBA.Items.Clear();
            var dt = _db.Query("SELECT MABN||' - '||TENBN FROM BVADMIN.BENHNHAN ORDER BY TENBN");
            foreach (DataRow r in dt.Rows) _cmbBNForHSBA.Items.Add(r[0].ToString()!);
            if (_cmbBNForHSBA.Items.Count == 0)
                _cmbBNForHSBA.Items.Add("(chưa có bệnh nhân nào)");
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "DPV.LoadBNList", ex.Message);
            _cmbBNForHSBA.Items.Clear();
            _cmbBNForHSBA.Items.Add($"(lỗi tải BN: {OracleErrorMapper.Short(ex)})");
        }
    }

    private void BtnCreateHSBA_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_cmbBNForHSBA.SelectedIndex < 0) { ShowError("Chọn bệnh nhân."); return; }
            var mabn = _cmbBNForHSBA.Text.Split('-')[0].Trim();
            var khoa = _cmbKhoa.SelectedItem?.ToString() ?? "";

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
        page.BackColor = UiTheme.BgLight;

        var lblLabel = new Label
        {
            Dock = DockStyle.Top, Height = 36,
            Padding = new Padding(12, 8, 0, 0),
            Font = UiTheme.LabelBold(10f),
            ForeColor = UiTheme.HealthCyan,
            Text = "Nhãn OLS của bạn: (bấm Tải để xem)",
            BackColor = UiTheme.Surface
        };

        var lblHint = new Label
        {
            Dock = DockStyle.Top, Height = 28,
            Padding = new Padding(12, 4, 12, 4),
            Font = UiTheme.Italic(9f),
            ForeColor = UiTheme.TextMuted,
            BackColor = UiTheme.Surface,
            Text = "Hệ thống tự lọc thông báo theo nhãn OLS. Nếu trống, cần chạy migration OLS labels."
        };

        var dgv = MakeGrid(); dgv.Dock = DockStyle.Fill;
        var btn = Btn("Tải thông báo", UiTheme.HealthCyan, width: 150);
        btn.Dock = DockStyle.Top;
        btn.Click += (_, _) =>
        {
            try
            {
                try { lblLabel.Text = "Nhãn OLS của bạn: " + CurrentOlsLabel(); }
                catch { lblLabel.Text = "Nhãn OLS của bạn: (chưa có — chạy migration 09 hoặc setup_all.sql)"; }

                dgv.DataSource = _db.Query(
                    "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG),1,100) AS NOIDUNG, " +
                    "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                    "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
            }
            catch (Exception ex)
            {
                AppAuditLogger.Error(_db.Username, "DPV.LoadTB", ex.Message);
                MessageBox.Show(this,
                    $"{OracleErrorMapper.Friendly(ex)}\n\n" +
                    "Để xem thông báo, cần chạy migration:\n" +
                    "  @PhanHe2/setup_all.sql\n\n" +
                    "Migration sẽ:\n" +
                    "  • Grant SELECT trên THONGBAO cho DPV/BS/KTV role\n" +
                    "  • Gán nhãn OLS cho nhân viên (CAPBAC, COSO, KHOA)",
                    "Cần cấp quyền OLS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        lblLabel.Dock = DockStyle.Fill;
        lblHint.Dock = DockStyle.Fill;
        btn.Dock = DockStyle.Fill;
        dgv.Margin = Padding.Empty;
        layout.Controls.Add(lblLabel, 0, 0);
        layout.Controls.Add(lblHint, 0, 1);
        layout.Controls.Add(btn, 0, 2);
        layout.Controls.Add(dgv, 0, 3);
        page.Controls.Add(layout);
        return page;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static DataGridView MakeGrid() => UiTheme.Grid();

    private static Label Lbl(string text) =>
        new() { Text = text, AutoSize = true, Font = UiTheme.Body(),
                Margin = new Padding(0, 8, 0, 2),       // FIX: tách rõ label với input phía trên
                Padding = new Padding(0, 0, 4, 0) };

    private static Label BoldLabel(string text) =>
        new() { Text = text, AutoSize = true, Font = UiTheme.Heading3(),
                ForeColor = Color.FromArgb(180, 100, 0), Padding = new Padding(0, 4, 0, 4) };

    private static TextBox TB(int width) =>
        new() { Width = width, Height = 28, Font = UiTheme.Body(),
                Margin = new Padding(0, 0, 0, 10) };     // FIX: khoảng cách xuống label kế tiếp

    private static Button Btn(string text, Color color, int width = 130,
                              EventHandler? onClick = null)
    {
        var btn = new Button
        {
            Text = text, Width = width, Height = 38, MinimumSize = new Size(width, 38), BackColor = color,
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Body(), Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleCenter,
            UseCompatibleTextRendering = false
        };
        btn.FlatAppearance.BorderSize = 0;
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
                "SELECT LBACSYS.fn_my_ols_label('BV_LABEL_POLICY') FROM DUAL")?.ToString() ?? "(chưa gán)";
        }
        catch { return "(không đọc được DBA_SA_USER_LABELS)"; }
    }
}
