namespace HospitalApp.Controls;

/// <summary>
/// Helper gắn keyboard shortcuts chuẩn cho mọi form chính.
/// </summary>
public static class ShortcutHelper
{
    public static void WireStandard(Form form,
        Action? onRefresh = null,
        Action? onSave    = null,
        Action? onNew     = null)
    {
        form.KeyPreview = true;
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F5 && onRefresh != null)
            { onRefresh(); e.Handled = true; return; }

            if (e.Control && e.KeyCode == Keys.S && onSave != null)
            { onSave();    e.Handled = true; return; }

            if (e.Control && e.KeyCode == Keys.N && onNew != null)
            { onNew();     e.Handled = true; return; }

            if (e.Control && e.KeyCode == Keys.L)
            {
                if (MessageBox.Show(form, "Đăng xuất?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    form.Close();
                e.Handled = true;
            }
        };
    }
}
