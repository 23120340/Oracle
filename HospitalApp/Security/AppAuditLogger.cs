namespace HospitalApp.Security;

/// <summary>
/// Logging app-side (bổ sung cho DB audit của Yêu cầu 3).
/// File log nằm tại %APPDATA%/HospitalApp/logs/app-yyyyMMdd.log, rotate hàng ngày.
/// KHÔNG ghi password, KHÔNG ghi giá trị NCLOB (CHANDOAN, KETQUA).
/// </summary>
public static class AppAuditLogger
{
    private static readonly object _lock = new();
    private static readonly string _dir;

    static AppAuditLogger()
    {
        _dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HospitalApp", "logs");
        try { Directory.CreateDirectory(_dir); } catch { /* best-effort */ }
    }

    public enum Severity { Info, Warn, Error, Security }

    public static void Log(Severity sev, string user, string action,
                           string? detail = null, bool? success = null)
    {
        try
        {
            var path = Path.Combine(_dir, $"app-{DateTime.Now:yyyyMMdd}.log");
            var line = string.Join('\t',
                DateTime.Now.ToString("O"),
                sev.ToString(),
                user,
                action,
                success.HasValue ? (success.Value ? "OK" : "FAIL") : "",
                Sanitize(detail));
            lock (_lock) File.AppendAllText(path, line + Environment.NewLine);
        }
        catch
        {
            // best-effort: không bao giờ raise exception từ logger
        }
    }

    public static void Info (string user, string action, string? detail = null) => Log(Severity.Info,  user, action, detail);
    public static void Warn (string user, string action, string? detail = null) => Log(Severity.Warn,  user, action, detail);
    public static void Error(string user, string action, string? detail = null) => Log(Severity.Error, user, action, detail);
    public static void Security(string user, string action, string? detail = null) => Log(Severity.Security, user, action, detail);

    /// <summary>
    /// Loại bỏ ký tự xuống dòng / tab khỏi detail để log 1-line / 1-record.
    /// Cũng giới hạn 500 ký tự để tránh log quá dài.
    /// </summary>
    private static string Sanitize(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
        return t.Length > 500 ? t.Substring(0, 497) + "..." : t;
    }

    public static string LogDirectory => _dir;
}
