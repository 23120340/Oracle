using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// TextBox tìm kiếm có icon, debounce 300ms.
/// Bind vào DataGridView qua <see cref="AttachTo"/> để filter realtime.
/// </summary>
public sealed class SearchBox : UserControl
{
    private readonly TextBox _txt;
    private readonly Label _icon;
    private readonly System.Windows.Forms.Timer _debounce;
    private DataGridView? _target;
    private string[] _searchColumns = Array.Empty<string>();

    public event Action<string>? TextChangedDebounced;

    public SearchBox()
    {
        Width = 300; Height = 40;
        BackColor = Color.White;
        Padding = new Padding(8, 0, 8, 0);
        BorderStyle = BorderStyle.FixedSingle;

        _icon = new Label
        {
            Text = "🔍",
            Dock = DockStyle.Left,
            Width = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = UiTheme.Body(11f),
            ForeColor = UiTheme.TextMuted
        };

        _txt = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = UiTheme.Body(10.5f),
            ForeColor = UiTheme.TextDark,
            PlaceholderText = "Tìm kiếm..."
        };

        _debounce = new System.Windows.Forms.Timer { Interval = 300 };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            DoFilter();
            TextChangedDebounced?.Invoke(_txt.Text);
        };
        _txt.TextChanged += (_, _) => { _debounce.Stop(); _debounce.Start(); };

        Controls.Add(_txt);
        Controls.Add(_icon);

        // FIX: TextBox 1 dòng KHÔNG tự giãn cao theo Dock=Fill → canh giữa theo chiều dọc THỦ CÔNG
        // để chữ (dấu tiếng Việt) không bị viền trên/dưới cắt mất.
        void LayoutTxt()
        {
            int left = _icon.Right + 2;
            int top = Math.Max(0, (ClientSize.Height - _txt.PreferredHeight) / 2);
            _txt.SetBounds(left, top, Math.Max(10, ClientSize.Width - left - 6), _txt.PreferredHeight);
        }
        Resize += (_, _) => LayoutTxt();
        HandleCreated += (_, _) => LayoutTxt();
        LayoutTxt();
    }

    public new string Text
    {
        get => _txt.Text;
        set => _txt.Text = value;
    }

    public string Placeholder
    {
        get => _txt.PlaceholderText;
        set => _txt.PlaceholderText = value;
    }

    /// <summary>
    /// Gắn vào DataGridView để filter tự động theo các cột được chỉ định.
    /// Yêu cầu DataGridView.DataSource là DataTable.
    /// </summary>
    public void AttachTo(DataGridView grid, params string[] columns)
    {
        _target = grid;
        _searchColumns = columns;
    }

    private void DoFilter()
    {
        if (_target?.DataSource is not System.Data.DataTable dt) return;

        var q = _txt.Text.Trim().Replace("'", "''");
        if (string.IsNullOrEmpty(q))
        {
            dt.DefaultView.RowFilter = "";
            return;
        }

        if (_searchColumns.Length == 0) return;

        var parts = _searchColumns.Select(c => $"CONVERT([{c}], 'System.String') LIKE '%{q}%'");
        dt.DefaultView.RowFilter = string.Join(" OR ", parts);
    }

    public void Clear() => _txt.Clear();
}
