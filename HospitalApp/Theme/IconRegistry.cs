namespace HospitalApp.Theme;

/// <summary>
/// Segoe Fluent Icons (Windows 10/11 built-in) codepoint constants.
/// Dùng FontFamily "Segoe Fluent Icons" hoặc fallback "Segoe MDL2 Assets".
/// Tất cả icons đều monochrome — màu được điều khiển qua ForeColor.
/// Reference: https://learn.microsoft.com/en-us/windows/apps/design/style/segoe-fluent-icons-font
/// </summary>
public static class IconRegistry
{
    public static readonly FontFamily IconFontFamily;

    static IconRegistry()
    {
        try
        {
            IconFontFamily = new FontFamily("Segoe Fluent Icons");
        }
        catch
        {
            try { IconFontFamily = new FontFamily("Segoe MDL2 Assets"); }
            catch { IconFontFamily = SystemFonts.DefaultFont.FontFamily; }
        }
    }

    public static Font Icon(float size = 14f, FontStyle style = FontStyle.Regular)
        => new(IconFontFamily, size, style, GraphicsUnit.Point);

    // ─── Navigation ──────────────────────────────────────────────────────────
    public const string Menu          = "";
    public const string Back          = "";
    public const string Forward       = "";
    public const string ChevronDown   = "";
    public const string ChevronUp     = "";
    public const string ChevronLeft   = "";
    public const string ChevronRight  = "";

    // ─── Actions ─────────────────────────────────────────────────────────────
    public const string Add           = "";
    public const string Cancel        = "";
    public const string Close         = "";
    public const string Save          = "";
    public const string Delete        = "";
    public const string Edit          = "";
    public const string Refresh       = "";
    public const string Search        = "";
    public const string Filter        = "";
    public const string Sort          = "";
    public const string Settings      = "";
    public const string More          = "";
    public const string Check         = "";
    public const string Accept        = "";

    // ─── Status ──────────────────────────────────────────────────────────────
    public const string Info          = "";
    public const string Warning       = "";
    public const string Error         = "";
    public const string Lock          = "";
    public const string Unlock        = "";
    public const string Shield        = "";

    // ─── User / People ───────────────────────────────────────────────────────
    public const string Person        = "";
    public const string People        = "";
    public const string AddUser       = "";
    public const string Contact       = "";
    public const string SignOut       = "";

    // ─── Content ─────────────────────────────────────────────────────────────
    public const string Document      = "";
    public const string Folder        = "";
    public const string Calendar      = "";
    public const string Clock         = "";
    public const string Mail          = "";
    public const string Notify        = "";
    public const string Bell          = "";
    public const string Tag           = "";

    // ─── Healthcare context (Segoe có một số) ────────────────────────────────
    public const string Health        = "";  // HealthRose
    public const string Lab           = "";  // Lab
    public const string Pill          = "";  // Prescription pill
    public const string Pulse         = "";  // Pulse line

    // ─── Data ────────────────────────────────────────────────────────────────
    public const string Database      = "";
    public const string Table         = "";
    public const string Chart         = "";
    public const string ChartBar      = "";

    // ─── Auth / Crypto ───────────────────────────────────────────────────────
    public const string Key           = "";
    public const string Eye           = "";
    public const string EyeHide       = "";
    public const string Permission    = "";

    // ─── Misc ────────────────────────────────────────────────────────────────
    public const string Home          = "";
    public const string Globe         = "";
    public const string Heart         = "";
    public const string Star          = "";
    public const string Pin           = "";
    public const string MapPin        = "";
}
