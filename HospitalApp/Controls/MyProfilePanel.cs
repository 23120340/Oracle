using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Panel "Thông tin của tôi" cho nhân viên (DPV/BS/KTV).
/// Dùng NV_NHANVIEN_View (file 08_App_Migrations.sql).
/// View tự filter ORACLE_USER = SESSION_USER → chỉ thấy 1 dòng.
/// INSTEAD OF trigger chặn UPDATE các trường định danh.
/// </summary>
public sealed class MyProfilePanel : UserControl
{
    private readonly OracleHelper _db;
    private Label _lblMANV = null!, _lblHoTen = null!, _lblPhai = null!,
                  _lblNgaySinh = null!, _lblCMND = null!, _lblVaiTro = null!,
                  _lblChuyenKhoa = null!, _lblCapBac = null!, _lblCoSo = null!,
                  _lblKhoaNhan = null!;
    private TextBox _txtQueQuan = null!, _txtSoDT = null!;

    public MyProfilePanel(OracleHelper db)
    {
        _db = db;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Surface;
        Padding = new Padding(20);
        Font = UiTheme.Body();
        Build();
        Load += (_, _) => LoadProfile();
    }

    private void Build()
    {
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UiTheme.Surface
        };

        var fl = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown,
            AutoSize = true, Padding = new Padding(0),
            WrapContents = false
        };

        fl.Controls.Add(UiTheme.SectionLabel("Thông tin định danh (chỉ đọc)"));

        var roGrid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 2, 0, 12) };
        roGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        roGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblMANV       = RoValue();
        _lblHoTen      = RoValue();
        _lblPhai       = RoValue();
        _lblNgaySinh   = RoValue();
        _lblCMND       = RoValue();
        _lblVaiTro     = RoValue();
        _lblChuyenKhoa = RoValue();
        _lblCapBac     = RoValue();
        _lblCoSo       = RoValue();
        _lblKhoaNhan   = RoValue();

        roGrid.Controls.Add(UiTheme.FieldLabel("Mã nhân viên:"));  roGrid.Controls.Add(_lblMANV);
        roGrid.Controls.Add(UiTheme.FieldLabel("Họ tên:"));         roGrid.Controls.Add(_lblHoTen);
        roGrid.Controls.Add(UiTheme.FieldLabel("Phái:"));           roGrid.Controls.Add(_lblPhai);
        roGrid.Controls.Add(UiTheme.FieldLabel("Ngày sinh:"));      roGrid.Controls.Add(_lblNgaySinh);
        roGrid.Controls.Add(UiTheme.FieldLabel("CMND:"));           roGrid.Controls.Add(_lblCMND);
        roGrid.Controls.Add(UiTheme.FieldLabel("Vai trò:"));        roGrid.Controls.Add(_lblVaiTro);
        roGrid.Controls.Add(UiTheme.FieldLabel("Chuyên khoa:"));    roGrid.Controls.Add(_lblChuyenKhoa);
        roGrid.Controls.Add(UiTheme.FieldLabel("Cấp bậc OLS:"));     roGrid.Controls.Add(_lblCapBac);
        roGrid.Controls.Add(UiTheme.FieldLabel("Cơ sở OLS:"));       roGrid.Controls.Add(_lblCoSo);
        roGrid.Controls.Add(UiTheme.FieldLabel("Khoa OLS:"));        roGrid.Controls.Add(_lblKhoaNhan);
        fl.Controls.Add(roGrid);

        fl.Controls.Add(UiTheme.SectionLabel("Thông tin có thể cập nhật"));

        var rwGrid = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 2, 0, 0) };
        rwGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        rwGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _txtQueQuan = UiTheme.TextField(300);
        _txtSoDT    = UiTheme.TextField(180);

        rwGrid.Controls.Add(UiTheme.FieldLabel("Quê quán:"));        rwGrid.Controls.Add(_txtQueQuan);
        rwGrid.Controls.Add(UiTheme.FieldLabel("Số điện thoại:"));   rwGrid.Controls.Add(_txtSoDT);
        fl.Controls.Add(rwGrid);

        var btnSave = UiTheme.AccentButton("Lưu thay đổi", (_, _) => SaveProfile());
        btnSave.Margin = new Padding(0, 14, 0, 0);
        fl.Controls.Add(btnSave);

        var info = new Label
        {
            Text = "Trường họ tên, mã NV, vai trò, chuyên khoa do hệ thống quản lý — " +
                   "liên hệ DBA nếu cần điều chỉnh.",
            Font = UiTheme.Italic(),
            ForeColor = UiTheme.TextMuted,
            AutoSize = true, MaximumSize = new Size(700, 0),
            Margin = new Padding(0, 16, 0, 0)
        };
        fl.Controls.Add(info);

        scroll.Controls.Add(fl);
        Controls.Add(scroll);
    }

    private static Label RoValue() => new()
    {
        AutoSize = true,
        Font = UiTheme.BodyBold(),
        ForeColor = UiTheme.TextDark,
        Padding = new Padding(0, 5, 0, 2)
    };

    private void LoadProfile()
    {
        try
        {
            var dt = _db.Query(
                "SELECT MANV, HOTEN, PHAI, TO_CHAR(NGAYSINH,'DD/MM/YYYY') AS NGAYSINH, " +
                "CMND, QUEQUAN, SODT, VAITRO, CHUYENKHOA, CAPBAC, COSO, KHOA_NHAN " +
                "FROM BVADMIN.NV_NHANVIEN_View");

            if (dt.Rows.Count == 0)
            {
                AppAuditLogger.Warn(_db.Username, "MyProfile.NotFound");
                return;
            }

            var r = dt.Rows[0];
            _lblMANV.Text       = r["MANV"]?.ToString()       ?? "";
            _lblHoTen.Text      = r["HOTEN"]?.ToString()      ?? "";
            _lblPhai.Text       = r["PHAI"]?.ToString() == "M" ? "Nam" : "Nữ";
            _lblNgaySinh.Text   = r["NGAYSINH"]?.ToString()   ?? "";
            _lblCMND.Text       = InputValidator.MaskCccd(r["CMND"]?.ToString());
            _lblVaiTro.Text     = MapVaiTro(r["VAITRO"]?.ToString());
            _lblChuyenKhoa.Text = r["CHUYENKHOA"]?.ToString() ?? "";
            _lblCapBac.Text     = MapCapBac(r["CAPBAC"]?.ToString());
            _lblCoSo.Text       = MapCoSo(r["COSO"]?.ToString());
            _lblKhoaNhan.Text   = MapKhoaNhan(r["KHOA_NHAN"]?.ToString());
            _txtQueQuan.Text    = r["QUEQUAN"]?.ToString()    ?? "";
            _txtSoDT.Text       = r["SODT"]?.ToString()       ?? "";
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "MyProfile.Load", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveProfile()
    {
        try
        {
            if (!string.IsNullOrEmpty(_txtSoDT.Text) &&
                !InputValidator.IsValidPhone(_txtSoDT.Text))
            {
                MessageBox.Show("Số điện thoại không hợp lệ (10–11 số, bắt đầu bằng 0).",
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _db.Execute(
                "UPDATE BVADMIN.NV_NHANVIEN_View SET QUEQUAN=:q, SODT=:s WHERE MANV=:m",
                OracleHelper.Param("q", InputValidator.Truncate(_txtQueQuan.Text, 200)),
                OracleHelper.Param("s", _txtSoDT.Text.Trim()),
                OracleHelper.Param("m", _lblMANV.Text));

            AppAuditLogger.Info(_db.Username, "MyProfile.Save", $"manv={_lblMANV.Text}");
            Toast.Show(FindForm() ?? Form.ActiveForm!, "Đã lưu thông tin cá nhân", Toast.Kind.Success);
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "MyProfile.Save", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string MapVaiTro(string? v) => v switch
    {
        "DPV" => "Điều phối viên",
        "BS"  => "Bác sĩ / Y sĩ",
        "KTV" => "Kỹ thuật viên",
        _     => v ?? ""
    };

    private static string MapCapBac(string? v) => v switch
    {
        "NV"  => "Nhân viên",
        "LDK" => "Lãnh đạo khoa",
        "BGD" => "Ban giám đốc",
        _     => string.IsNullOrWhiteSpace(v) ? "(chưa gán)" : v
    };

    private static string MapCoSo(string? v) => v switch
    {
        "HCM" => "Hồ Chí Minh",
        "HPN" => "Hải Phòng",
        "HNI" => "Hà Nội",
        _     => string.IsNullOrWhiteSpace(v) ? "(toàn viện)" : v
    };

    private static string MapKhoaNhan(string? v) => v switch
    {
        "TH"  => "Tiêu hóa",
        "TK"  => "Thần kinh",
        "TM"  => "Tim mạch",
        "ALL" => "Tất cả khoa",
        _     => string.IsNullOrWhiteSpace(v) ? "(toàn viện)" : v
    };
}
