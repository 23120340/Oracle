using HospitalApp.Database;
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

    // Tab thông báo
    private DataGridView _dgvTB = null!;

    public BNForm(OracleHelper db)
    {
        _db = db;
        Text = $"Thông tin Bệnh nhân – {db.Username}";
        Size = new Size(900, 660);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(255, 252, 248);
        BuildUI();
    }

    private void BuildUI()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(140, 60, 140) };
        header.Controls.Add(new Label
        {
            Text = "🧑‍⚕  Phân hệ 2 – Bệnh nhân",
            Dock = DockStyle.Fill, ForeColor = Color.White,
            Font = new Font("Segoe UI", 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
        });
        Controls.Add(header);

        _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9) };
        _tabs.TabPages.Add(BuildInfoTab());
        _tabs.TabPages.Add(BuildHSBATab());
        _tabs.TabPages.Add(BuildThongBaoTab());
        Controls.Add(_tabs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: THÔNG TIN CÁ NHÂN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildInfoTab()
    {
        var page = new TabPage("👤 Thông tin của tôi");

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var fl = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(20, 15, 20, 10), AutoSize = true
        };

        // Thông tin chỉ đọc (không được sửa)
        fl.Controls.Add(SectionLabel("📌 Thông tin định danh (chỉ đọc)"));

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
        fl.Controls.Add(SectionLabel("✏ Thông tin có thể cập nhật"));
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

        fl.Controls.Add(SectionLabel("🏥 Thông tin y tế"));
        fl.Controls.Add(FieldLabel("Tiền sử bệnh:"));
        _txtTSB   = MemoBox(700, 70); fl.Controls.Add(_txtTSB);
        fl.Controls.Add(FieldLabel("Tiền sử gia đình:"));
        _txtTSBGD = MemoBox(700, 70); fl.Controls.Add(_txtTSBGD);
        fl.Controls.Add(FieldLabel("Dị ứng thuốc:"));
        _txtDiung = EditBox(500); fl.Controls.Add(_txtDiung);

        _btnSaveInfo = new Button
        {
            Text = "💾  Lưu thông tin", Width = 180, Height = 36,
            BackColor = Color.FromArgb(140, 60, 140), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand, Margin = new Padding(0, 10, 0, 0)
        };
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
                "TO_CHAR(TIENSUBENH) AS TSB, TO_CHAR(TIENSUBENHGD) AS TSBGD, DIUNGTHUOC " +
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
            ShowSuccess("Cập nhật thông tin thành công.");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: LỊCH SỬ BỆNH ÁN
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildHSBATab()
    {
        var page = new TabPage("📋 Lịch sử khám bệnh");

        _dgvHSBA = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White, RowHeadersVisible = false
        };

        var note = new Label
        {
            Dock = DockStyle.Bottom, Height = 30, TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Text = "ℹ Chỉ hiển thị Mã HSBA, Ngày khám, Khoa và Kết luận. Thông tin chẩn đoán chi tiết do bác sĩ quản lý.",
            ForeColor = Color.DimGray, Font = new Font("Segoe UI", 8)
        };

        var btn = new Button
        {
            Dock = DockStyle.Top, Text = "🔄 Tải lịch sử khám bệnh", Height = 36,
            BackColor = Color.SteelBlue, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand
        };
        btn.Click += (_, _) => TryCatch(() =>
        {
            // BN_HSBA_View tự filter theo ORACLE_USER → chỉ thấy HSBA của mình
            // Ẩn CHANDOAN/DIEUTRI (chỉ BS/bác sĩ mới được xem chi tiết)
            _dgvHSBA.DataSource = _db.Query(
                "SELECT MAHSBA, TO_CHAR(NGAY,'DD/MM/YYYY') AS NGAY, MAKHOA, " +
                "SUBSTR(TO_CHAR(KETLUAN),1,100) AS KETLUAN " +
                "FROM BVADMIN.BN_HSBA_View ORDER BY NGAY DESC");
        });

        page.Controls.Add(_dgvHSBA);
        page.Controls.Add(note);
        page.Controls.Add(btn);
        return page;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: THÔNG BÁO
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildThongBaoTab()
    {
        var page = new TabPage("📢 Thông báo");
        _dgvTB = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            BackgroundColor = Color.White, RowHeadersVisible = false
        };
        var btn = new Button
        {
            Dock = DockStyle.Top, Text = "🔄 Tải thông báo", Height = 36,
            BackColor = Color.SteelBlue, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9)
        };
        btn.Click += (_, _) => TryCatch(() =>
        {
            // OLS tự filter nhãn → BN chỉ thấy thông báo phù hợp với label của mình
            _dgvTB.DataSource = _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG),1,120) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO,'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        });
        page.Controls.Add(_dgvTB);
        page.Controls.Add(btn);
        return page;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────
    private static Label SectionLabel(string text) => new()
    {
        Text = text, AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = Color.FromArgb(100, 40, 100),
        Padding = new Padding(0, 8, 0, 4)
    };

    private static Label FieldLabel(string text) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", 9),
        Padding = new Padding(0, 5, 6, 2)
    };

    private static Label ReadonlyValue(string val) => new()
    {
        Text = val, AutoSize = true, ForeColor = Color.DimGray,
        Font = new Font("Segoe UI", 9, FontStyle.Italic),
        Padding = new Padding(0, 5, 0, 2)
    };

    private static TextBox EditBox(int width) => new()
    {
        Width = width, Height = 24, Font = new Font("Segoe UI", 9),
        BorderStyle = BorderStyle.FixedSingle
    };

    private static TextBox MemoBox(int width, int height) => new()
    {
        Width = width, Height = height, Multiline = true,
        ScrollBars = ScrollBars.Vertical, Font = new Font("Segoe UI", 9),
        BorderStyle = BorderStyle.FixedSingle
    };

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
