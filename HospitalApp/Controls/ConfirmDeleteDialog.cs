using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Modal xác nhận xoá. Nút "Xoá" chỉ enable khi user gõ đúng cụm xác nhận
/// (mặc định "XOA"). Tránh xoá nhầm do click nhanh.
/// </summary>
public sealed class ConfirmDeleteDialog : Form
{
    public static bool Confirm(IWin32Window owner, string title, string detail,
                               string confirmWord = "XOA")
    {
        using var dlg = new ConfirmDeleteDialog(title, detail, confirmWord);
        return dlg.ShowDialog(owner) == DialogResult.OK;
    }

    private ConfirmDeleteDialog(string title, string detail, string confirmWord)
    {
        Text = title;
        Size = new Size(440, 260);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        BackColor = UiTheme.Surface;
        Font = UiTheme.Body();

        var lblIcon = new Label
        {
            Text = "⚠",
            Location = new Point(20, 20),
            Size = new Size(40, 40),
            Font = new Font(UiTheme.Family, 22, FontStyle.Bold),
            ForeColor = UiTheme.Danger,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblTitle = new Label
        {
            Text = "Hành động không thể hoàn tác",
            Location = new Point(70, 20),
            Size = new Size(340, 24),
            Font = UiTheme.Heading3(11f),
            ForeColor = UiTheme.TextDark
        };

        var lblDetail = new Label
        {
            Text = detail,
            Location = new Point(70, 50),
            Size = new Size(340, 60),
            Font = UiTheme.Body(),
            ForeColor = UiTheme.TextMuted
        };

        var lblPrompt = new Label
        {
            Text = $"Nhập \"{confirmWord}\" để xác nhận:",
            Location = new Point(20, 125),
            Size = new Size(400, 22),
            Font = UiTheme.LabelBold(),
            ForeColor = UiTheme.TextDark
        };

        var txt = new TextBox
        {
            Location = new Point(20, 150),
            Size = new Size(390, 28),
            Font = UiTheme.Body(11f),
            BorderStyle = BorderStyle.FixedSingle
        };

        var btnDelete = new Button
        {
            Text = "Xoá",
            Location = new Point(220, 190),
            Size = new Size(90, 32),
            BackColor = UiTheme.Danger, ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Button(), Enabled = false,
            DialogResult = DialogResult.OK
        };
        btnDelete.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text = "Huỷ",
            Location = new Point(320, 190),
            Size = new Size(90, 32),
            BackColor = UiTheme.BgLight, ForeColor = UiTheme.TextDark,
            FlatStyle = FlatStyle.Flat,
            Font = UiTheme.Button(),
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderColor = UiTheme.Border;

        txt.TextChanged += (_, _) =>
            btnDelete.Enabled = txt.Text.Trim().Equals(confirmWord, StringComparison.Ordinal);

        Controls.AddRange(new Control[] { lblIcon, lblTitle, lblDetail, lblPrompt, txt, btnDelete, btnCancel });

        AcceptButton = btnDelete;
        CancelButton = btnCancel;
        Shown += (_, _) => txt.Focus();
    }
}
