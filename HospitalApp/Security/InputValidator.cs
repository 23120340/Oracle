using System.Text.RegularExpressions;

namespace HospitalApp.Security;

/// <summary>
/// Validate input ở App layer trước khi gửi xuống DB.
/// "Defense in depth" — DB cũng có CHECK constraints, app layer chặn sớm hơn.
/// </summary>
public static class InputValidator
{
    private static readonly Regex CCCD_RX     = new(@"^\d{12}$",            RegexOptions.Compiled);
    private static readonly Regex CMND_RX     = new(@"^\d{9,12}$",          RegexOptions.Compiled);
    private static readonly Regex PHONE_RX    = new(@"^0\d{9,10}$",         RegexOptions.Compiled);
    private static readonly Regex MA_RX       = new(@"^[A-Z]{1,4}\d{1,8}$", RegexOptions.Compiled);
    private static readonly Regex SAFE_ID_RX  = new(@"^[A-Za-z][A-Za-z0-9_]{0,29}$", RegexOptions.Compiled);
    // Chặn các ký tự có thể dùng SQL injection trong trường text tự do
    private static readonly Regex SQL_DANGER  = new(@"(--|;|/\*|\*/|xp_)", RegexOptions.Compiled);

    public static bool IsValidCccd(string? s)  => s != null && CCCD_RX.IsMatch(s.Trim());
    public static bool IsValidCmnd(string? s)  => s != null && CMND_RX.IsMatch(s.Trim());
    public static bool IsValidPhone(string? s) => s != null && PHONE_RX.IsMatch(s.Trim());
    public static bool IsValidMa(string? s)    => s != null && MA_RX.IsMatch(s.Trim());
    public static bool IsSafeIdentifier(string? s) => s != null && SAFE_ID_RX.IsMatch(s.Trim());

    /// <summary>
    /// Kiểm tra text tự do (TIENSUBENH, CHANDOAN, KETQUA, ...) có ký tự nguy hiểm.
    /// Lưu ý: tham số hoá luôn là tuyến phòng thủ chính, đây là lớp phụ.
    /// </summary>
    public static bool HasSqlDanger(string? s)
        => s != null && SQL_DANGER.IsMatch(s);

    /// <summary>
    /// Giới hạn độ dài chuỗi (chống DoS / buffer overflow ở DB).
    /// </summary>
    public static string Truncate(string? s, int max)
        => string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s.Substring(0, max));

    // ═════════════════════════════════════════════════════════════════════════
    // MASK — Che dữ liệu nhạy cảm khi hiển thị ngoài form chi tiết
    // ═════════════════════════════════════════════════════════════════════════
    public static string MaskCccd(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Trim();
        return t.Length >= 4 ? new string('•', t.Length - 4) + t[^4..] : t;
    }

    public static string MaskPhone(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var t = s.Trim();
        return t.Length >= 3 ? t[..3] + new string('•', Math.Max(0, t.Length - 3)) : t;
    }

    /// <summary>
    /// Kiểm tra mật khẩu mạnh: ≥8 ký tự, có chữ hoa, chữ thường, số, ký tự đặc biệt.
    /// </summary>
    public static (bool ok, string msg) CheckPasswordStrength(string? pw)
    {
        if (string.IsNullOrEmpty(pw))        return (false, "Mật khẩu rỗng.");
        if (pw.Length < 8)                   return (false, "Mật khẩu phải có ít nhất 8 ký tự.");
        if (!pw.Any(char.IsUpper))           return (false, "Mật khẩu phải có chữ HOA.");
        if (!pw.Any(char.IsLower))           return (false, "Mật khẩu phải có chữ thường.");
        if (!pw.Any(char.IsDigit))           return (false, "Mật khẩu phải có chữ số.");
        if (!pw.Any(c => !char.IsLetterOrDigit(c))) return (false, "Mật khẩu phải có ký tự đặc biệt.");
        return (true, "OK");
    }
}
