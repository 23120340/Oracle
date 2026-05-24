using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Security;

/// <summary>
/// Map Oracle exception → thông điệp thân thiện cho người dùng.
/// Mục đích bảo mật: KHÔNG để raw Oracle error rò rỉ ra UI
/// (vd. "ORA-00942: table or view does not exist" tiết lộ schema).
/// Vẫn ghi chi tiết vào AppAuditLogger để debug.
/// </summary>
public static class OracleErrorMapper
{
    public static string Friendly(Exception ex)
    {
        if (ex is OracleException oe) return FromOraNumber(oe.Number, oe.Message);
        // Fallback: parse ORA-xxxxx từ message
        var msg = ex.Message ?? "";
        var idx = msg.IndexOf("ORA-", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && msg.Length >= idx + 9 &&
            int.TryParse(msg.Substring(idx + 4, 5), out var code))
            return FromOraNumber(code, msg);
        return "Đã xảy ra lỗi không xác định. Vui lòng thử lại.";
    }

    private static string FromOraNumber(int n, string original) => n switch
    {
        // ── Authentication / Session ────────────────────────────────────────
         1017 => "Tên đăng nhập hoặc mật khẩu không đúng.",
        28000 => "Tài khoản đã bị khoá. Vui lòng liên hệ DBA.",
        28001 => "Mật khẩu đã hết hạn. Vui lòng đổi mật khẩu.",
        28002 => "Mật khẩu sẽ hết hạn sớm. Hãy đổi mật khẩu.",
        12154 => "Tên kết nối (SID/Service) không tìm thấy trong tnsnames.ora.",
        12170 => "Hết thời gian chờ kết nối. Kiểm tra Host/Port.",
        12514 => "Service không hợp lệ. Kiểm tra SID.",
        12541 => "Không kết nối được tới Oracle Listener (Host/Port sai hoặc Oracle chưa bật).",

        // ── Quyền truy cập ──────────────────────────────────────────────────
         1031 => "Bạn không có quyền thực hiện thao tác này.",
          942 => "Đối tượng dữ liệu không tồn tại hoặc bạn không có quyền truy cập.",
         1748 => "Tên đối tượng không hợp lệ.",
        28150 => "Không xác thực được proxy. Liên hệ DBA.",

        // ── Constraint / dữ liệu ────────────────────────────────────────────
            1 => "Trùng dữ liệu khoá chính (giá trị đã tồn tại).",
         1400 => "Thiếu giá trị bắt buộc (NOT NULL).",
         1407 => "Không được cập nhật cột này thành NULL.",
         2291 => "Giá trị tham chiếu không tồn tại (vi phạm khoá ngoại).",
         2292 => "Không thể xoá vì còn dữ liệu phụ thuộc (khoá ngoại).",
         2290 => "Dữ liệu vi phạm ràng buộc CHECK của hệ thống.",
        12899 => "Giá trị quá dài so với độ dài cho phép của cột.",
         1438 => "Giá trị số vượt độ chính xác cho phép.",

        // ── Business rule (RAISE_APPLICATION_ERROR trong DB) ────────────────
        20001 => "Kỹ thuật viên chỉ được cập nhật cột KẾT QUẢ.",
        20002 => "Không được phép thay đổi MÃ BN, HỌ TÊN, PHÁI, NGÀY SINH, CCCD.",
        20003 => "Không được phép thay đổi thông tin định danh của nhân viên.",

        // ── Network / Hệ thống ──────────────────────────────────────────────
         3113 => "Mất kết nối tới cơ sở dữ liệu.",
         3114 => "Chưa kết nối tới cơ sở dữ liệu.",
        12537 => "Kết nối bị đóng do timeout. Đăng nhập lại.",
        12560 => "Lỗi giao thức kết nối TCP. Liên hệ DBA.",
        24338 => "Lỗi định dạng câu lệnh SQL.",

        // ── VPD / OLS / FGA ─────────────────────────────────────────────────
        28113 => "Chính sách bảo mật phát hiện vi phạm. Liên hệ DBA.",
        28115 => "Không đủ nhãn bảo mật để xem thông tin này (OLS).",
        28117 => "Cập nhật vi phạm chính sách bảo mật (VPD).",
        28133 => "Bạn chưa được gán nhãn bảo mật phù hợp.",

        _ => $"Hệ thống từ chối thao tác (mã: ORA-{n:D5})."
    };

    /// <summary>
    /// Tóm tắt ngắn (1 dòng) để hiển thị trong status bar / toast.
    /// </summary>
    public static string Short(Exception ex)
    {
        var full = Friendly(ex);
        if (full.Length > 80) return full.Substring(0, 77) + "...";
        return full;
    }

    /// <summary>
    /// Lấy mã ORA-xxxxx (nếu có) để hiển thị cùng message thân thiện khi debug.
    /// </summary>
    public static int? ExtractOraNumber(Exception ex)
    {
        if (ex is OracleException oe && oe.Number != 0) return oe.Number;
        var msg = ex.Message ?? "";
        var idx = msg.IndexOf("ORA-", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0 && msg.Length >= idx + 9 &&
            int.TryParse(msg.Substring(idx + 4, 5), out var code))
            return code;
        return null;
    }

    /// <summary>
    /// Message dài cho debug: friendly + mã ORA hoặc type exception + 1 dòng message gốc.
    /// </summary>
    public static string Verbose(Exception ex)
    {
        var ora = ExtractOraNumber(ex);
        if (ora.HasValue)
            return $"{Friendly(ex)}  (ORA-{ora.Value:D5})";

        // Không có ORA code → exception ngoài Oracle (network, parsing, ...)
        var raw = (ex.Message ?? "").Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (raw.Length > 140) raw = raw.Substring(0, 137) + "...";
        var typeName = ex.GetType().Name;
        return $"[{typeName}] {raw}";
    }
}
