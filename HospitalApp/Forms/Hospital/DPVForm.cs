using HospitalApp.Database;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Điều phối viên (DPV_Role + VPD).
/// Quản lý BỆNHNHÂN, tạo HSBA, điều phối BS và KTV.
/// </summary>
public class DPVForm : Form
{
    private readonly OracleHelper _db;
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
    }

    private void BuildUI()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(180, 100, 0) };
        header.Controls.Add(new Label
        {
            Text = "📋  Phân hệ 2 – Điều phối viên",
            Dock = DockStyle.Fill, ForeColor = Color.White,
            Font = new Font("Segoe UI", 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
        });
        Controls.Add(header);

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };
        _tabs.TabPages.Add(BuildBNTab());
        _tabs.TabPages.Add(BuildHSBATab());
        _tabs.TabPages.Add(BuildThongBaoTab());
        Controls.Add(_tabs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: QUẢN LÝ BỆNH NHÂN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildBNTab()
    {
        var page = new TabPage("🏥 Bệnh nhân");
        var split = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 350 };

        // Left: danh sách
        _dgvBN = MakeGrid(); _dgvBN.Dock = DockStyle.Fill;
        _dgvBN.SelectionChanged += (_, _) => LoadBNDetail();
        var toolBN = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 40, Padding = new Padding(4),
            FlowDirection = FlowDirection.LeftToRight
        };
        _btnNewBN = Btn("➕ Thêm BN", Color.Green);
        _btnNewBN.Click += (_, _) => { _isNewBN = true; ClearBNForm(); };
        toolBN.Controls.Add(_btnNewBN);
        toolBN.Controls.Add(Btn("🔄 Làm mới", Color.SteelBlue, onClick: (_, _) => LoadBN()));
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
        _btnSaveBN = Btn("💾 Lưu", Color.FromArgb(180, 100, 0));
        _btnSaveBN.Click += BtnSaveBN_Click;
        _btnDelBN  = Btn("🗑 Xóa BN", Color.Crimson);
        _btnDelBN.Click += BtnDelBN_Click;
        btnRow.Controls.AddRange(new Control[] { _btnSaveBN, _btnDelBN });
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
        });
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
                _db.Execute(
                    "INSERT INTO BVADMIN.BENHNHAN(MABN,TENBN,PHAI,NGAYSINH,CCCD,TINHTP) " +
                    "VALUES(:m,:t,:p,:n,:c,:tp)",
                    OracleHelper.Param("m",  _txtMABN.Text.Trim()),
                    OracleHelper.Param("t",  _txtTENBN.Text.Trim()),
                    OracleHelper.Param("p",  _cmbPhai.Text),
                    OracleHelper.Param("n",  _dtpNgaySinh.Value),
                    OracleHelper.Param("c",  _txtCCCD.Text.Trim()),
                    OracleHelper.Param("tp", _txtDiaChi.Text.Trim()));
                ShowSuccess("Thêm bệnh nhân thành công.");
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
                ShowSuccess("Cập nhật bệnh nhân thành công.");
            }
            LoadBN();
        });
    }

    private void BtnDelBN_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var mabn = _txtMABN.Text.Trim();
            if (string.IsNullOrEmpty(mabn)) { ShowError("Chọn BN cần xóa."); return; }
            if (MessageBox.Show($"Xóa bệnh nhân '{_txtTENBN.Text}'?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _db.Execute("DELETE FROM BVADMIN.BENHNHAN WHERE MABN=:id",
                OracleHelper.Param("id", mabn));
            ShowSuccess("Đã xóa bệnh nhân."); ClearBNForm(); LoadBN();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: QUẢN LÝ HSBA
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildHSBATab()
    {
        var page = new TabPage("📂 Hồ sơ Bệnh án");
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal,
                                          SplitterDistance = 220 };

        // Top: danh sách HSBA
        _dgvHSBA = MakeGrid(); _dgvHSBA.Dock = DockStyle.Fill;
        _dgvHSBA.SelectionChanged += (_, _) => LoadDV();

        var topBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(4),
                                            FlowDirection = FlowDirection.LeftToRight };
        topBar.Controls.Add(Btn("🔄 Tải HSBA", Color.SteelBlue, onClick: (_, _) => LoadHSBA()));
        split.Panel1.Controls.Add(_dgvHSBA);
        split.Panel1.Controls.Add(topBar);

        // Bottom: tạo HSBA mới + điều phối
        var botPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
                                              Padding = new Padding(10), AutoScroll = true };
        botPanel.Controls.Add(BoldLabel("Tạo HSBA mới / Điều phối"));

        var row1 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        row1.Controls.Add(Lbl("BN:"));
        _cmbBNForHSBA = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        row1.Controls.Add(_cmbBNForHSBA);
        row1.Controls.Add(Lbl("Khoa:"));
        _txtMakhoa = new TextBox { Width = 150, Font = new Font("Segoe UI", 9) };
        row1.Controls.Add(_txtMakhoa);
        _btnCreateHSBA = Btn("➕ Tạo HSBA", Color.Green);
        _btnCreateHSBA.Click += BtnCreateHSBA_Click;
        row1.Controls.Add(_btnCreateHSBA);
        botPanel.Controls.Add(row1);

        var row2 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        row2.Controls.Add(Lbl("Giao cho BS:"));
        _cmbBS = new ComboBox { Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        row2.Controls.Add(_cmbBS);
        _btnAssignBS = Btn("✅ Giao BS", Color.FromArgb(180, 100, 0));
        _btnAssignBS.Click += BtnAssignBS_Click;
        row2.Controls.Add(_btnAssignBS);
        botPanel.Controls.Add(row2);

        // HSBA_DV với điều phối KTV
        _lblHSBAInfo = new Label { AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Navy };
        botPanel.Controls.Add(_lblHSBAInfo);
        _dgvDV = MakeGrid();
        _dgvDV.Width = 800; _dgvDV.Height = 100;
        botPanel.Controls.Add(_dgvDV);

        var row3 = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _btnAssignKTV = Btn("👩‍⚕ Giao KTV cho DV", Color.Teal);
        _btnAssignKTV.Click += BtnAssignKTV_Click;
        row3.Controls.Add(_btnAssignKTV);
        botPanel.Controls.Add(row3);

        split.Panel2.Controls.Add(botPanel);
        page.Controls.Add(split);
        page.Enter += (_, _) => { LoadHSBA(); LoadBSList(); LoadBNList(); };
        return page;
    }

    private void LoadHSBA()
    {
        TryCatch(() =>
        {
            _dgvHSBA.DataSource = _db.Query(
                "SELECT MAHSBA, MABN, TO_CHAR(NGAY,'DD/MM/YYYY') AS NGAY, " +
                "MABS, MAKHOA, STATUS FROM BVADMIN.HSBA ORDER BY NGAY DESC");
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
            var dt = _db.Query("SELECT MANV||' - '||HOTEN AS BS FROM BVADMIN.NHANVIEN WHERE VAITRO='BS' ORDER BY HOTEN");
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
            var mabn   = _cmbBNForHSBA.Text.Split('-')[0].Trim();
            var khoa   = _txtMakhoa.Text.Trim();
            var mahsba = "HS" + DateTime.Now.ToString("yyyyMMddHHmm");
            _db.Execute(
                "INSERT INTO BVADMIN.HSBA(MAHSBA,MABN,NGAY,MAKHOA) VALUES(:h,:b,SYSDATE,:k)",
                OracleHelper.Param("h", mahsba),
                OracleHelper.Param("b", mabn),
                OracleHelper.Param("k", khoa));
            ShowSuccess($"Tạo HSBA '{mahsba}' thành công."); LoadHSBA();
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
            ShowSuccess($"Giao HSBA '{mahsba}' cho BS '{mabs}'."); LoadHSBA();
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
            ShowSuccess($"Giao dịch vụ '{loaidv}' cho KTV '{maktv}'."); LoadDV();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: THÔNG BÁO
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildThongBaoTab()
    {
        var page = new TabPage("📢 Thông báo");
        var dgv  = MakeGrid(); dgv.Dock = DockStyle.Fill;
        var btn  = Btn("🔄 Tải", Color.SteelBlue);
        btn.Dock = DockStyle.Top;
        btn.Click += (_, _) => TryCatch(() =>
        {
            dgv.DataSource = _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG),1,100) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        });
        page.Controls.Add(dgv); page.Controls.Add(btn);
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
        new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", 9),
                Padding = new Padding(0, 6, 4, 0) };

    private static Label BoldLabel(string text) =>
        new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 100, 0), Padding = new Padding(0, 4, 0, 4) };

    private static TextBox TB(int width) =>
        new() { Width = width, Height = 24, Font = new Font("Segoe UI", 9) };

    private static Button Btn(string text, Color color, int width = 130,
                              EventHandler? onClick = null)
    {
        var btn = new Button
        {
            Text = text, Width = width, Height = 32, BackColor = color,
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand
        };
        if (onClick is not null) btn.Click += onClick;
        return btn;
    }

    private static void TryCatch(Action a)
    {
        try { a(); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static void ShowSuccess(string m) =>
        MessageBox.Show(m, "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void ShowError(string m) =>
        MessageBox.Show(m, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
