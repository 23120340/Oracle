using HospitalApp.Controls;
using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Giao diện minh hoạ OLS (Yêu cầu 2) cho các user u1–u8.
/// Họ KHÔNG nằm trong NHANVIEN/BENHNHAN, chỉ có quyền SELECT THONGBAO + nhãn OLS.
/// Form chỉ hiển thị thông báo (OLS tự filter theo nhãn user).
/// </summary>
public class OLSViewerForm : Form
{
    private readonly OracleHelper _db;
    private readonly SessionManager _session;
    private DataGridView _dgv = null!;
    private Label _lblLabel  = null!;
    private Label _lblCount  = null!;

    public OLSViewerForm(OracleHelper db)
    {
        _db = db;
        Text = $"Hộp thư thông báo OLS – {db.Username}";
        Size = new Size(920, 600);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = UiTheme.BgLight;
        Font = UiTheme.Body();

        BuildUI();
        LoadData();

        ShortcutHelper.WireStandard(this, onRefresh: LoadData);
        _session = new SessionManager(this, db.Username, TimeSpan.FromMinutes(15));
        FormClosed += (_, _) => _session.Dispose();
    }

    private void BuildUI()
    {
        // Header
        var header = UiTheme.Header($"Thông báo OLS - {_db.Username}",
            UiTheme.Primary, UiTheme.PrimaryDark, (_, _) =>
            {
                if (MessageBox.Show("Đăng xuất?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Close();
            });
        Controls.Add(header);
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.BgLight
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(body);
        header.BringToFront();

        // Info bar: nhãn OLS hiện tại
        var info = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            Padding = new Padding(20, 10, 20, 10)
        };
        _lblLabel = new Label
        {
            Dock = DockStyle.Top, AutoSize = false, Height = 30,
            Font = UiTheme.BodyBold(11f),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true
        };
        _lblCount = new Label
        {
            Dock = DockStyle.Top, AutoSize = false, Height = 22,
            Font = UiTheme.Body(),
            ForeColor = UiTheme.TextMuted,
            AutoEllipsis = true
        };
        var infoNote = new Label
        {
            Dock = DockStyle.Top, AutoSize = false, Height = 36,
            Text = "ℹ Bạn chỉ thấy những thông báo phù hợp với nhãn bảo mật được cấp.",
            Font = UiTheme.Italic(),
            ForeColor = UiTheme.TextMuted,
            AutoEllipsis = true
        };
        info.Controls.Add(infoNote);
        info.Controls.Add(_lblCount);
        info.Controls.Add(_lblLabel);
        body.Controls.Add(info, 0, 0);

        // Toolbar
        var tool = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, Padding = new Padding(10, 8, 10, 8),
            BackColor = UiTheme.BgLight
        };
        var btnRefresh = new RoundedButton
        {
            Text = "Tải lại (F5)", Glyph = IconRegistry.Refresh,
            BackColor = UiTheme.Primary, Width = 150, Height = 36
        };
        btnRefresh.Click += (_, _) => LoadData();
        tool.Controls.Add(btnRefresh);
        body.Controls.Add(tool, 0, 1);

        // Grid (Fill)
        _dgv = UiTheme.Grid();
        _dgv.Dock = DockStyle.Fill;
        _dgv.Margin = Padding.Empty;
        body.Controls.Add(_dgv, 0, 2);

        // Status bar
        var status = new StatusBar
        {
            LeftText   = $"{IconRegistry.Database}  {_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  OLS Viewer"
        };
        status.Dock = DockStyle.Fill;
        body.Controls.Add(status, 0, 3);
    }

    private void LoadData()
    {
        try
        {
            // Lấy nhãn OLS của user hiện tại
            var lbl = CurrentOlsLabel();
            /*
            var oldLbl = _db.Scalar(
                "SELECT MAX_READ_LABEL FROM DBA_SA_USER_LABELS " +
                "WHERE POLICY_NAME = 'BV_LABEL_POLICY' AND USER_NAME = USER")?.ToString()
                ?? "(chưa được cấp nhãn)";
            */
            _lblLabel.Text = $"Nhãn bảo mật: {lbl}";

            // OLS tự filter
            var dt = QueryThongBao();
            /*
            var oldDt = _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO, 'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
            */
            _dgv.DataSource = dt;
            _lblCount.Text = $"Tổng {dt.Rows.Count} thông báo hiển thị cho bạn.";

            AppAuditLogger.Info(_db.Username, "OLS.LoadTB", $"count={dt.Rows.Count}");
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "OLS.LoadTB", ex.Message);
            MessageBox.Show(OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string CurrentOlsLabel()
    {
        try
        {
            return _db.Scalar(
                "SELECT MAX_READ_LABEL FROM DBA_SA_USER_LABELS " +
                "WHERE POLICY_NAME = 'BV_LABEL_POLICY' AND USER_NAME = USER")?.ToString()
                ?? "(chưa được cấp nhãn)";
        }
        catch
        {
            return "(không đọc được nhãn, vẫn áp dụng OLS khi xem thông báo)";
        }
    }

    private System.Data.DataTable QueryThongBao()
    {
        try
        {
            return _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO, 'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        }
        catch
        {
            return _db.Query(
                "SELECT MATB, SUBSTR(TO_CHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO, 'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM THONGBAO ORDER BY NGAYGIO DESC");
        }
    }
}
