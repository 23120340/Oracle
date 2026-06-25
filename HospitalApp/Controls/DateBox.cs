using System.Globalization;
using HospitalApp.Theme;

namespace HospitalApp.Controls;

/// <summary>
/// Ô chọn ngày thay cho DateTimePicker gốc (trên Win11 themed, DTP cắt mất số đầu vì
/// không nhận padding/khoảng trắng đầu). DateBox = TextField có lề trái thật + nút lịch
/// thả xuống. Cho phép GÕ ngày (dd / MM / yyyy) hoặc chọn từ lịch. API tương thích:
/// dùng thuộc tính <see cref="Value"/> (DateTime) y như DateTimePicker.
/// </summary>
public sealed class DateBox : UserControl
{
    private const string Display = "dd / MM / yyyy";
    private static readonly string[] ParseFmts =
    {
        "dd / MM / yyyy", "dd/MM/yyyy", "d/M/yyyy",
        "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy"
    };

    private readonly TextBox _txt;
    private readonly Label _btn;
    private ToolStripDropDown? _dd;
    private MonthCalendar? _cal;
    private DateTime _value = DateTime.Today.AddYears(-30);

    public DateBox()
    {
        Height = 30;
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        _btn = new Label
        {
            Text = IconRegistry.Calendar,
            Dock = DockStyle.Right, Width = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = IconRegistry.Icon(11f),
            ForeColor = UiTheme.TextMuted,
            Cursor = Cursors.Hand,
            BackColor = Color.White
        };
        _btn.Click += (_, _) => ToggleCalendar();

        _txt = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = UiTheme.Body(10.5f),
            ForeColor = UiTheme.TextDark,
            // Multiline để tự đặt chiều cao > line-height → dấu/đậm số không bị cắt trên/dưới.
            Multiline = true,
            WordWrap = false
        };
        UiTheme.Pad(_txt, 2, 2);
        _txt.Leave += (_, _) => Commit();
        _txt.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { Commit(); e.SuppressKeyPress = true; }
            else if (e.KeyCode == Keys.Down && e.Alt) { ToggleCalendar(); e.SuppressKeyPress = true; }
        };

        Controls.Add(_txt);
        Controls.Add(_btn);

        Resize += (_, _) => LayoutInner();
        HandleCreated += (_, _) => { Render(); LayoutInner(); };
        Render();
    }

    /// <summary>Giá trị ngày — tương thích DateTimePicker.Value.</summary>
    public DateTime Value
    {
        get => _value;
        set { _value = value.Date; Render(); }
    }

    private void Render() => _txt.Text = _value.ToString(Display, CultureInfo.InvariantCulture);

    private void LayoutInner()
    {
        const int padLeft = 8;
        int boxH = _txt.Font.Height + 6;            // dư chỗ cho số/dấu, canh giữa dọc
        int top  = Math.Max(0, (ClientSize.Height - boxH) / 2);
        int w    = Math.Max(10, _btn.Left - padLeft - 2);
        _txt.SetBounds(padLeft, top, w, boxH);
    }

    private void Commit()
    {
        var raw = _txt.Text.Trim();
        var compact = raw.Replace(" ", "");
        if (DateTime.TryParseExact(raw, ParseFmts, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d) ||
            DateTime.TryParseExact(compact, ParseFmts, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out d) ||
            DateTime.TryParse(raw, new CultureInfo("vi-VN"), DateTimeStyles.None, out d))
        {
            _value = d.Date;
        }
        Render();   // luôn hiển thị lại chuẩn → nhập sai thì tự quay về giá trị hợp lệ
    }

    private void ToggleCalendar()
    {
        if (_dd is { Visible: true }) { _dd.Close(); return; }

        if (_dd == null)
        {
            _cal = new MonthCalendar { MaxSelectionCount = 1 };
            _cal.DateSelected += (_, e) => { Value = e.Start; _dd?.Close(); };
            var host = new ToolStripControlHost(_cal)
            {
                Margin = Padding.Empty, Padding = Padding.Empty, AutoSize = true
            };
            _dd = new ToolStripDropDown
            {
                Padding = Padding.Empty, AutoClose = true,
                DropShadowEnabled = true
            };
            _dd.Items.Add(host);
        }

        try { _cal!.SelectionStart = _value; } catch { /* ngoài MinDate/MaxDate */ }
        _dd!.Show(this, new Point(0, Height));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _dd?.Dispose(); _cal?.Dispose(); }
        base.Dispose(disposing);
    }
}
