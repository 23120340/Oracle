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
            },
            (_, _) =>
            {
                using var dlg = new ChangePasswordDialog(_db);
                if (dlg.ShowDialog(this) == DialogResult.OK) Close();  // đổi xong → đăng nhập lại
            });
        // ── Body: 4 hàng dọc, mỗi block nằm trong CELL riêng (không Dock chồng nhau) ──
        //   [0] info Card (nhãn OLS + tổng + ghi chú)   — Absolute, cao đủ 3 dòng
        //   [1] toolbar (1 nút Tải lại)                 — Absolute
        //   [2] grid trong Card (Fill)                  — Percent(100) hút phần dư
        //   [3] StatusBar                               — Absolute 30 (đúng chiều cao thật)
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.BgLight
            // KHÔNG AutoScroll: hàng grid Percent đã hút hết phần dư; bật AutoScroll khiến
            // thanh trạng thái (hàng cuối) bị đẩy xuống dưới mép → "thời gian bị che".
        };
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 132)); // info Card
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));  // toolbar
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // grid Card (slack)
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        // FIX: thêm body (Fill) TRƯỚC, header (Top) SAU → header chiếm phần trên, body nằm dưới
        // (trước đây body bị vẽ đè dưới header làm dòng "Nhãn/Tổng N thông báo" lệch lên/khuất).
        Controls.Add(body);
        Controls.Add(header);

        // ── [0] Info Card: nhãn OLS hiện tại ───────────────────────────────────
        var infoCard = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(UiTheme.Spacing4, UiTheme.Spacing4, UiTheme.Spacing4, UiTheme.Spacing2),
            Padding = new Padding(UiTheme.Spacing5, UiTheme.Spacing3, UiTheme.Spacing5, UiTheme.Spacing3),
            CornerRadius = UiTheme.RadiusLg,
            ShadowDepth = 4,
            BorderWidth = 0
        };
        var infoGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = UiTheme.Surface
        };
        infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // nhãn (icon + tên nhãn)
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 22)); // tổng số
        infoGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // ghi chú (hút phần dư)

        var lblLabelIcon = new IconLabel
        {
            Dock = DockStyle.Fill,
            Glyph = IconRegistry.Bell,
            GlyphSize = 13f,
            Font = UiTheme.BodyBold(11f),
            ForeColor = UiTheme.Primary,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        _lblLabel = lblLabelIcon;            // GIỮ field _lblLabel (LoadData gán .Text)
        infoGrid.Controls.Add(_lblLabel, 0, 0);

        _lblCount = new Label
        {
            Dock = DockStyle.Fill, AutoSize = false,
            Font = UiTheme.Body(),
            ForeColor = UiTheme.TextMuted,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        infoGrid.Controls.Add(_lblCount, 0, 1);

        var infoNote = new IconLabel
        {
            Dock = DockStyle.Fill,
            Glyph = IconRegistry.Info,
            GlyphSize = 12f,
            Text = "Bạn chỉ thấy những thông báo phù hợp với nhãn bảo mật được cấp.",
            Font = UiTheme.Italic(),
            ForeColor = UiTheme.TextMuted,
            AutoEllipsis = true,
            Margin = Padding.Empty
        };
        infoGrid.Controls.Add(infoNote, 0, 2);
        infoCard.Controls.Add(infoGrid);
        body.Controls.Add(infoCard, 0, 0);

        // ── [1] Toolbar (1 hàng nút — FlowLayoutPanel hợp lệ cho dãy nút) ──────
        var tool = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiTheme.Spacing4, UiTheme.Spacing2, UiTheme.Spacing4, UiTheme.Spacing2),
            Margin = Padding.Empty,
            BackColor = UiTheme.BgLight,
            WrapContents = false
        };
        var btnRefresh = new RoundedButton
        {
            Text = "Tải lại (F5)", Glyph = IconRegistry.Refresh,
            BackColor = UiTheme.Primary, Width = 150, Height = 36,
            Margin = new Padding(0, 0, UiTheme.Spacing2, 0)
        };
        btnRefresh.Click += (_, _) => LoadData();
        tool.Controls.Add(btnRefresh);
        body.Controls.Add(tool, 0, 1);

        // ── [2] Grid trong Card (Fill) ─────────────────────────────────────────
        var gridCard = new Card
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(UiTheme.Spacing4, UiTheme.Spacing2, UiTheme.Spacing4, UiTheme.Spacing2),
            Padding = new Padding(UiTheme.Spacing2),
            CornerRadius = UiTheme.RadiusLg,
            ShadowDepth = 4,
            BorderWidth = 0
        };
        _dgv = UiTheme.Grid();
        _dgv.Dock = DockStyle.Fill;
        _dgv.Margin = Padding.Empty;
        gridCard.Controls.Add(_dgv);
        body.Controls.Add(gridCard, 0, 2);

        // ── Status bar: dock ĐÁY FORM (giữ Dock=Bottom mặc định) → luôn hiện, không bị body cắt ──
        var status = new StatusBar
        {
            LeftText   = $"{_db.Host}:{_db.Port}/{_db.Sid}",
            CenterText = $"{_db.Username}  ·  OLS Viewer"
        };
        Controls.Add(status);   // thêm cuối → dock trước → chiếm đáy; header (Top) + body (Fill) phân giải đúng
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
                "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
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
                "SELECT LBACSYS.fn_my_ols_label('BV_LABEL_POLICY') FROM DUAL")?.ToString()
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
                "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO, 'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM BVADMIN.THONGBAO ORDER BY NGAYGIO DESC");
        }
        catch
        {
            return _db.Query(
                "SELECT MATB, SUBSTR(TO_NCHAR(NOIDUNG), 1, 150) AS NOIDUNG, " +
                "TO_CHAR(NGAYGIO, 'DD/MM/YYYY HH24:MI') AS NGAYGIO, DIADIEM " +
                "FROM THONGBAO ORDER BY NGAYGIO DESC");
        }
    }
}
