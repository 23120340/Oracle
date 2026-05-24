namespace HospitalApp.Security;

/// <summary>
/// Theo dõi idle time. Sau N phút không có thao tác (chuột/phím) → đóng form
/// (form chính đã wire-up: Close() = logout, quay về LoginForm).
/// Áp dụng cho mọi form chính bằng cách gọi <see cref="Attach"/>.
/// </summary>
public sealed class SessionManager : IDisposable
{
    private readonly Form _form;
    private readonly System.Windows.Forms.Timer _ticker;
    private readonly TimeSpan _idleLimit;
    private readonly string _username;
    private DateTime _lastActivity;
    private bool _warned;

    public SessionManager(Form form, string username, TimeSpan? idleLimit = null)
    {
        _form       = form;
        _username   = username;
        _idleLimit  = idleLimit ?? TimeSpan.FromMinutes(10);
        _lastActivity = DateTime.Now;

        // Bắt mọi sự kiện chuột/phím trong form (đệ quy qua mọi control con)
        WireActivityHandlers(_form);

        _ticker = new System.Windows.Forms.Timer { Interval = 5000 }; // tick 5s
        _ticker.Tick += OnTick;
        _ticker.Start();

        AppAuditLogger.Info(_username, "session_start",
            $"idle_limit={_idleLimit.TotalMinutes}m");
    }

    private void OnTick(object? s, EventArgs e)
    {
        var idle = DateTime.Now - _lastActivity;

        // Cảnh báo trước 1 phút khi sắp hết hạn
        if (!_warned && idle >= _idleLimit - TimeSpan.FromMinutes(1)
                     && idle < _idleLimit)
        {
            _warned = true;
            _form.Invoke(() =>
            {
                MessageBox.Show(_form,
                    $"Phiên làm việc sẽ tự đăng xuất trong 1 phút nữa do không có thao tác.\n" +
                    $"Di chuyển chuột để tiếp tục.",
                    "Cảnh báo phiên làm việc",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            });
        }

        if (idle >= _idleLimit)
        {
            _ticker.Stop();
            AppAuditLogger.Security(_username, "session_timeout",
                $"idle={idle.TotalMinutes:F1}m");

            _form.Invoke(() =>
            {
                MessageBox.Show(_form,
                    "Phiên làm việc đã hết hạn do không có thao tác.\nVui lòng đăng nhập lại.",
                    "Hết phiên", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _form.Close();
            });
        }
    }

    private void Touch(object? sender, EventArgs e)
    {
        _lastActivity = DateTime.Now;
        _warned = false;
    }

    private void WireActivityHandlers(Control root)
    {
        root.MouseMove += Touch;
        root.MouseClick += Touch;
        root.KeyDown += Touch;
        foreach (Control c in root.Controls)
            WireActivityHandlers(c);
    }

    public void Dispose()
    {
        _ticker.Stop();
        _ticker.Dispose();
        AppAuditLogger.Info(_username, "session_end");
    }
}
