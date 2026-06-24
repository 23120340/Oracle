using HospitalApp.Database;
using HospitalApp.Security;
using HospitalApp.Theme;

namespace HospitalApp.Forms;

/// <summary>
/// Hộp thoại tự đổi mật khẩu Oracle của chính người dùng đang đăng nhập.
/// Dùng:  ALTER USER "&lt;user&gt;" IDENTIFIED BY "&lt;new&gt;" REPLACE "&lt;old&gt;"
/// — mệnh đề REPLACE cho phép user đổi mật khẩu CHÍNH MÌNH mà KHÔNG cần quyền
///   ALTER USER (Oracle tự kiểm tra mật khẩu cũ đúng mới cho đổi).
/// Sau khi đổi thành công, connection cũ (đang giữ mật khẩu cũ) không dùng lại được
/// → caller nên đăng xuất để người dùng đăng nhập lại bằng mật khẩu mới
/// (DialogResult.OK báo hiệu điều đó).
/// </summary>
public sealed class ChangePasswordDialog : Form
{
    private readonly OracleHelper _db;
    private TextBox _txtOld = null!, _txtNew = null!, _txtConfirm = null!;
    private CheckBox _chkShow = null!;

    public ChangePasswordDialog(OracleHelper db)
    {
        _db = db;
        Text = "Đổi mật khẩu";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(464, 432);
        BackColor = UiTheme.Surface;
        Font = UiTheme.Body();
        Build();
    }

    private void Build()
    {
        const int fieldW = 396;

        // Thân: xếp dòng dọc → các nhãn/ô không bao giờ chồng (tránh che chữ).
        var content = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown,
            WrapContents = false, BackColor = UiTheme.Surface,
            Padding = new Padding(28, 22, 28, 6)
        };

        var heading = UiTheme.SectionLabel("Đổi mật khẩu");
        heading.Margin = new Padding(0, 0, 0, 2);

        var sub = new Label
        {
            Text = $"Tài khoản: {_db.Username}",
            AutoSize = true, Font = UiTheme.Body(), ForeColor = UiTheme.TextMuted,
            Margin = new Padding(2, 0, 0, 14)
        };

        _txtOld     = MakePwField(fieldW);
        _txtNew     = MakePwField(fieldW);
        _txtConfirm = MakePwField(fieldW);

        _chkShow = new CheckBox
        {
            Text = "Hiện mật khẩu", AutoSize = true,
            Font = UiTheme.Body(), ForeColor = UiTheme.TextMuted,
            Margin = new Padding(2, 2, 0, 4)
        };
        _chkShow.CheckedChanged += (_, _) =>
        {
            bool show = _chkShow.Checked;
            _txtOld.UseSystemPasswordChar = _txtNew.UseSystemPasswordChar =
                _txtConfirm.UseSystemPasswordChar = !show;
        };

        var helper = new Label
        {
            Text = "Tối thiểu 8 ký tự và khác mật khẩu hiện tại.",
            AutoSize = true, Font = UiTheme.Italic(), ForeColor = UiTheme.TextMuted,
            Margin = new Padding(2, 0, 0, 0)
        };

        content.Controls.Add(heading);
        content.Controls.Add(sub);
        content.Controls.Add(FieldLabel("Mật khẩu hiện tại"));
        content.Controls.Add(_txtOld);
        content.Controls.Add(FieldLabel("Mật khẩu mới"));
        content.Controls.Add(_txtNew);
        content.Controls.Add(FieldLabel("Xác nhận mật khẩu mới"));
        content.Controls.Add(_txtConfirm);
        content.Controls.Add(_chkShow);
        content.Controls.Add(helper);

        // Hàng nút dưới đáy
        var btnOk = UiTheme.AccentButton("Đổi mật khẩu", OnSubmit);
        var btnCancel = new Button
        {
            Text = "Huỷ", Height = 38, MinimumSize = new Size(96, 38),
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14, 0, 14, 0),
            BackColor = UiTheme.Surface, ForeColor = UiTheme.TextMuted,
            FlatStyle = FlatStyle.Flat, Font = UiTheme.Button(),
            Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = UiTheme.BorderStrong;
        btnCancel.FlatAppearance.BorderSize = 1;
        btnCancel.FlatAppearance.MouseOverBackColor = UiTheme.BgLight;

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom, Height = 64,
            FlowDirection = FlowDirection.RightToLeft, WrapContents = false,
            Padding = new Padding(0, 13, 24, 13), BackColor = UiTheme.Surface
        };
        bottom.Controls.Add(btnOk);      // phải nhất
        bottom.Controls.Add(btnCancel);  // bên trái OK

        // Thêm cụm đáy TRƯỚC, thân Fill SAU → Fill nhận phần còn lại phía trên.
        Controls.Add(bottom);
        Controls.Add(content);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
        ActiveControl = _txtOld;
    }

    private static Label FieldLabel(string text) => new()
    {
        Text = text, AutoSize = true, Font = UiTheme.Label(),
        ForeColor = UiTheme.TextMuted, Margin = new Padding(2, 0, 0, 3)
    };

    private static TextBox MakePwField(int w)
    {
        var tb = UiTheme.TextField(w);
        tb.Margin = new Padding(2, 0, 0, 12);
        tb.UseSystemPasswordChar = true;
        return tb;
    }

    private void OnSubmit(object? sender, EventArgs e)
    {
        string oldPw = _txtOld.Text, newPw = _txtNew.Text, confirm = _txtConfirm.Text;

        if (string.IsNullOrEmpty(oldPw) || string.IsNullOrEmpty(newPw))
        { Warn("Vui lòng nhập đầy đủ mật khẩu."); return; }
        if (newPw.Length < 8)
        { Warn("Mật khẩu mới phải có ít nhất 8 ký tự."); return; }
        if (newPw != confirm)
        { Warn("Xác nhận mật khẩu mới không khớp."); return; }
        if (newPw == oldPw)
        { Warn("Mật khẩu mới phải khác mật khẩu hiện tại."); return; }
        // Mật khẩu đặt trong "..." (quoted literal) để chứa được @ ! * ; ... → cấm dấu " để
        // không phá cú pháp/né SQL-injection (username đã được OracleHelper chuẩn hoá HOA).
        if (newPw.Contains('"') || oldPw.Contains('"'))
        { Warn("Mật khẩu không được chứa dấu nháy kép (\")."); return; }

        try
        {
            var sql = $"ALTER USER \"{_db.Username}\" IDENTIFIED BY \"{newPw}\" REPLACE \"{oldPw}\"";
            _db.Execute(sql);
            AppAuditLogger.Info(_db.Username, "ChangePassword.Success");
            MessageBox.Show(this,
                "Đổi mật khẩu thành công.\nVui lòng đăng nhập lại bằng mật khẩu mới.",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;   // báo caller: đăng xuất để đăng nhập lại
            Close();
        }
        catch (Exception ex)
        {
            AppAuditLogger.Error(_db.Username, "ChangePassword", ex.Message);
            MessageBox.Show(this, OracleErrorMapper.Friendly(ex), "Lỗi",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Warn(string msg) =>
        MessageBox.Show(this, msg, "Kiểm tra lại",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
