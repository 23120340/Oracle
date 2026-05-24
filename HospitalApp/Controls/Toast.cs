using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Toast notification top-right, tự ẩn sau 3 giây. Không yêu cầu click.
/// Dùng thay MessageBox cho thông báo success/info.
/// </summary>
public sealed class Toast : Form
{
    public enum Kind { Success, Info, Warning, Error }

    private readonly System.Windows.Forms.Timer _autoClose;

    public static void Show(Form owner, string message, Kind kind = Kind.Success,
                            int durationMs = 3000)
    {
        if (owner.InvokeRequired)
        {
            owner.Invoke(() => Show(owner, message, kind, durationMs));
            return;
        }

        var toast = new Toast(message, kind, durationMs);
        toast.PositionRelativeTo(owner);
        toast.Show(owner);
    }

    private Toast(string message, Kind kind, int durationMs)
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0.95;

        var (bg, icon) = kind switch
        {
            Kind.Success => (UiTheme.Accent,  "✓"),
            Kind.Warning => (UiTheme.Warning, "⚠"),
            Kind.Error   => (UiTheme.Danger,  "✕"),
            _            => (UiTheme.Primary, "ⓘ")
        };

        BackColor = bg;

        var lblIcon = new Label
        {
            Text = icon, Dock = DockStyle.Left, Width = 40,
            Font = new Font(UiTheme.Family, 16, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblMsg = new Label
        {
            Text = message, Dock = DockStyle.Fill,
            Font = UiTheme.Body(10f),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(6, 4, 12, 4)
        };

        Controls.Add(lblMsg);
        Controls.Add(lblIcon);

        // Tự đo width dựa theo message
        using var g = CreateGraphics();
        var sz = g.MeasureString(message, lblMsg.Font);
        Size = new Size(Math.Min(420, (int)sz.Width + 80), 50);

        _autoClose = new System.Windows.Forms.Timer { Interval = durationMs };
        _autoClose.Tick += (_, _) => { _autoClose.Stop(); Close(); };
        _autoClose.Start();

        // Click vào toast cũng đóng
        foreach (Control c in new Control[] { this, lblIcon, lblMsg })
            c.Click += (_, _) => Close();
    }

    private void PositionRelativeTo(Form owner)
    {
        var screen = owner.Bounds;
        Location = new Point(
            screen.Right - Width - 24,
            screen.Top + 80);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE;   // Không cướp focus
            return cp;
        }
    }
}
