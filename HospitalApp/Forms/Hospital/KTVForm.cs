using HospitalApp.Database;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Hospital;

/// <summary>
/// Phân hệ 2 – Giao diện Kỹ thuật viên (KTV_Role + View filter).
/// RBAC View tự động filter: chỉ thấy HSBA_DV do mình thực hiện.
/// </summary>
public class KTVForm : Form
{
    private readonly OracleHelper _db;
    private DataGridView _dgvDV   = null!;
    private TextBox      _txtKQ   = null!;
    private Label        _lblInfo = null!;
    private Button       _btnSave = null!, _btnRefresh = null!;

    public KTVForm(OracleHelper db)
    {
        _db = db;
        Text = $"Giao diện Kỹ thuật viên – {db.Username}";
        Size = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 252, 245);
        BuildUI();
    }

    private void BuildUI()
    {
        // Header
        var header = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(0, 140, 60) };
        header.Controls.Add(new Label
        {
            Text = "🔬  Phân hệ 2 – Kỹ thuật viên",
            Dock = DockStyle.Fill, ForeColor = Color.White,
            Font = new Font("Segoe UI", 13, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter
        });
        Controls.Add(header);

        // Toolbar
        var tool = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 44, Padding = new Padding(6),
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = Color.FromArgb(235, 250, 235)
        };
        _btnRefresh = new Button
        {
            Text = "🔄 Tải danh sách DV của tôi", Width = 220, Height = 32,
            BackColor = Color.SteelBlue, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand
        };
        _btnRefresh.Click += (_, _) => LoadMyDV();
        tool.Controls.Add(_btnRefresh);

        _lblInfo = new Label
        {
            AutoSize = true, ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9), Padding = new Padding(10, 8, 0, 0)
        };
        tool.Controls.Add(_lblInfo);
        Controls.Add(tool);

        // Grid
        _dgvDV = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            BackgroundColor = Color.White, RowHeadersVisible = false,
            Font = new Font("Segoe UI", 9)
        };
        _dgvDV.SelectionChanged += DgvDV_SelectionChanged;
        Controls.Add(_dgvDV);

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
            Font = new Font("Segoe UI", 9, FontStyle.Bold), Height = 22
        });
        _txtKQ = new TextBox
        {
            Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical,
            Font = new Font("Segoe UI", 9), BorderStyle = BorderStyle.FixedSingle
        };
        bottom.Controls.Add(_txtKQ);

        _btnSave = new Button
        {
            Dock = DockStyle.Bottom, Text = "💾  Lưu Kết quả",
            Height = 36, BackColor = Color.FromArgb(0, 140, 60),
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand
        };
        _btnSave.Click += BtnSave_Click;
        bottom.Controls.Add(_btnSave);
        Controls.Add(bottom);

        LoadMyDV();
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

            ShowSuccess(
                $"Đã lưu kết quả cho:\n" +
                $"HSBA: {mahsba}\n" +
                $"Dịch vụ: {loaidv}\n\n" +
                $"(Trigger LOG_KTV_KETQUA đã ghi vết thay đổi này)");
            LoadMyDV();
        });
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
