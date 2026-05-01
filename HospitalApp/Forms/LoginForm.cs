using HospitalApp.Database;
using HospitalApp.Forms.Admin;
using HospitalApp.Forms.Hospital;

namespace HospitalApp.Forms;

public class LoginForm : Form
{
    private TextBox txtHost, txtPort, txtSid, txtUser, txtPass;
    private Button  btnLogin;
    private Label   lblStatus;

    public LoginForm()
    {
        Text            = "Đăng nhập – Hệ thống Quản lý Bệnh viện";
        Size            = new Size(420, 400);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = Color.FromArgb(245, 248, 255);

        BuildUI();
    }

    private void BuildUI()
    {
        // ── Header ────────────────────────────────────────────────────────────
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 80,
            BackColor = Color.FromArgb(30, 90, 160)
        };
        var lblTitle = new Label
        {
            Text      = "🏥  Quản lý Bệnh viện",
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 16, FontStyle.Bold),
            AutoSize  = false,
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };
        header.Controls.Add(lblTitle);
        Controls.Add(header);

        // ── Form panel ────────────────────────────────────────────────────────
        var panel = new Panel { Padding = new Padding(30, 20, 30, 20) };
        panel.AutoSize = true;

        int y = 100;
        txtHost = AddField("Host",     "localhost", ref y);
        txtPort = AddField("Port",     "1521",      ref y);
        txtSid  = AddField("SID",      "ORCL",      ref y);
        txtUser = AddField("Username", "",          ref y);
        txtPass = AddField("Password", "",          ref y, isPassword: true);

        btnLogin = new Button
        {
            Text      = "Đăng nhập",
            Location  = new Point(120, y + 10),
            Size      = new Size(160, 38),
            BackColor = Color.FromArgb(30, 90, 160),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor    = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.Click += BtnLogin_Click;
        Controls.Add(btnLogin);

        lblStatus = new Label
        {
            Location  = new Point(30, y + 60),
            Size      = new Size(350, 30),
            ForeColor = Color.Red,
            Font      = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleCenter
        };
        Controls.Add(lblStatus);
    }

    private TextBox AddField(string label, string defaultVal, ref int y,
                             bool isPassword = false)
    {
        Controls.Add(new Label
        {
            Text     = label + ":",
            Location = new Point(30, y),
            Size     = new Size(90, 24),
            Font     = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleRight
        });
        var tb = new TextBox
        {
            Text          = defaultVal,
            Location      = new Point(130, y),
            Size          = new Size(230, 24),
            Font          = new Font("Segoe UI", 10),
            PasswordChar  = isPassword ? '●' : '\0',
            BorderStyle   = BorderStyle.FixedSingle
        };
        Controls.Add(tb);
        y += 38;
        return tb;
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        btnLogin.Enabled = false;
        lblStatus.Text   = "Đang kết nối...";
        lblStatus.ForeColor = Color.DimGray;

        var host = txtHost.Text.Trim();
        var port = txtPort.Text.Trim();
        var sid  = txtSid.Text.Trim();
        var user = txtUser.Text.Trim();
        var pass = txtPass.Text;

        if (string.IsNullOrEmpty(user))
        {
            lblStatus.Text = "Vui lòng nhập Username.";
            lblStatus.ForeColor = Color.Red;
            btnLogin.Enabled = true;
            return;
        }

        await Task.Run(() =>
        {
            try
            {
                var db   = new OracleHelper(host, port, sid, user, pass);
                db.TestConnection();
                var role = db.GetHospitalRole();

                Invoke(() =>
                {
                    Form? next = role switch
                    {
                        "DBA"     => new AdminDashboard(db),
                        "DPV"     => new DPVForm(db),
                        "BS"      => new BSForm(db),
                        "KTV"     => new KTVForm(db),
                        "BN"      => new BNForm(db),
                        _         => null
                    };

                    if (next is null)
                    {
                        lblStatus.Text = $"Không xác định được vai trò (role='{role}').";
                        lblStatus.ForeColor = Color.Red;
                        btnLogin.Enabled = true;
                        return;
                    }

                    Hide();
                    next.FormClosed += (_, _) => Close();
                    next.Show();
                });
            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    lblStatus.Text = "Lỗi kết nối: " + ex.Message;
                    lblStatus.ForeColor = Color.Red;
                    btnLogin.Enabled = true;
                });
            }
        });
    }
}
