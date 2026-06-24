using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.RegularExpressions;

namespace HospitalApp.Database;

/// <summary>
/// Oracle DB helper – mỗi instance giữ 1 connection string (1 Oracle user).
/// VPD/RBAC áp dụng tự động theo user đang kết nối.
/// </summary>
public class OracleHelper
{
    private readonly string _connStr;

    public string Username { get; }
    public string Host     { get; }
    public string Port     { get; }
    public string Sid      { get; }

    public OracleHelper(string host, string port, string serviceName, string username, string password)
    {
        Host     = host;
        Port     = port;
        Sid      = serviceName;
        Username = username.ToUpper();

        // Connection string thuần, password được quote bằng `"..."` nếu chứa ký tự đặc biệt
        // (Oracle managed driver chấp nhận quoted password để chứa ; = @ ! ' v.v.)
        var safePass = password.Contains('"') || password.Contains(';') ||
                       password.Contains('=') || password.Contains('@')
            ? "\"" + password.Replace("\"", "\"\"") + "\""
            : password;

        _connStr = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                   $"(HOST={host})(PORT={port}))(CONNECT_DATA=(SERVICE_NAME={serviceName})));" +
                   $"User Id={username};Password={safePass};Connection Timeout=10;";
    }

    // ── Kết nối + test ────────────────────────────────────────────────────────
    public void TestConnection()
    {
        using var conn = new OracleConnection(_connStr);
        conn.Open();
    }

    public OracleConnection OpenConnection()
    {
        var conn = new OracleConnection(_connStr);
        conn.Open();
        return conn;
    }

    // ── Query trả DataTable ───────────────────────────────────────────────────
    public DataTable Query(string sql, params OracleParameter[] parms)
    {
        using var conn = new OracleConnection(_connStr);
        conn.Open();
        using var cmd  = new OracleCommand(sql, conn);
        if (parms.Length > 0) cmd.Parameters.AddRange(parms);
        using var da = new OracleDataAdapter(cmd);
        var dt = new DataTable();
        da.Fill(dt);
        return dt;
    }

    // ── Query trả scalar ──────────────────────────────────────────────────────
    public object? Scalar(string sql, params OracleParameter[] parms)
    {
        using var conn = new OracleConnection(_connStr);
        conn.Open();
        using var cmd  = new OracleCommand(sql, conn);
        if (parms.Length > 0) cmd.Parameters.AddRange(parms);
        return cmd.ExecuteScalar();
    }

    // ── Thực thi DML/DDL ─────────────────────────────────────────────────────
    public int Execute(string sql, params OracleParameter[] parms)
    {
        using var conn = new OracleConnection(_connStr);
        conn.Open();
        using var cmd  = new OracleCommand(sql, conn);
        if (parms.Length > 0) cmd.Parameters.AddRange(parms);
        return cmd.ExecuteNonQuery();
    }

    // ── Thực thi nhiều lệnh DDL trong 1 transaction ───────────────────────────
    public void ExecuteBatch(IEnumerable<string> statements)
    {
        using var conn = new OracleConnection(_connStr);
        conn.Open();
        using var tx  = conn.BeginTransaction();
        try
        {
            foreach (var sql in statements)
            {
                if (string.IsNullOrWhiteSpace(sql)) continue;
                using var cmd = new OracleCommand(sql, conn) { Transaction = tx };
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    // ── Kiểm tra DBA privilege ────────────────────────────────────────────────
    public bool IsDba()
    {
        // SYS / SYSTEM luôn là DBA
        if (Username.Equals("SYS", StringComparison.OrdinalIgnoreCase) ||
            Username.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check user có DBA role được grant
        try
        {
            var result = Scalar(
                "SELECT COUNT(*) FROM USER_ROLE_PRIVS WHERE GRANTED_ROLE = 'DBA'");
            if (Convert.ToInt32(result) > 0) return true;
        }
        catch { /* ignore */ }

        // Fallback: check một số DBA-typical privileges qua SESSION_PRIVS
        try
        {
            var result = Scalar(
                "SELECT COUNT(*) FROM SESSION_PRIVS " +
                "WHERE PRIVILEGE IN ('CREATE ANY TABLE', 'ALTER SYSTEM', 'DROP USER', 'GRANT ANY ROLE')");
            return Convert.ToInt32(result) >= 2;
        }
        catch { return false; }
    }

    // ── Lấy vai trò của user hiện tại trong hệ thống bệnh viện ───────────────
    public string GetHospitalRole()
    {
        try
        {
            var dt = Query(
                "SELECT VAITRO FROM BVADMIN.NHANVIEN " +
                "WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER')");
            if (dt.Rows.Count > 0) return dt.Rows[0][0].ToString()!;

            // Kiểm tra bệnh nhân
            var bnCount = Convert.ToInt32(
                Scalar("SELECT COUNT(*) FROM BVADMIN.BENHNHAN " +
                       "WHERE ORACLE_USER = SYS_CONTEXT('USERENV','SESSION_USER')"));
            if (bnCount > 0) return "BN";

            // Kiểm tra OLS user (u1-u8): có nhãn trong DBA_SA_USER_LABELS
            try
            {
                var olsCount = Convert.ToInt32(Scalar(
                    "SELECT COUNT(*) FROM DBA_SA_USER_LABELS " +
                    "WHERE POLICY_NAME = 'BV_LABEL_POLICY' AND USER_NAME = USER"));
                if (olsCount > 0) return "OLS";
            }
            catch { /* DBA_SA_* có thể không tồn tại nếu chưa cài OLS */ }
        }
        catch { /* ignore */ }

        // Demo fallback: Oracle XE setup may restrict direct role-detection queries.
        // The DB policies still enforce data access after the form opens.
        if (Username.StartsWith("DPV_", StringComparison.OrdinalIgnoreCase)) return "DPV";
        if (Username.StartsWith("BS_",  StringComparison.OrdinalIgnoreCase)) return "BS";
        if (Username.StartsWith("KTV_", StringComparison.OrdinalIgnoreCase)) return "KTV";
        if (Username.StartsWith("BN_",  StringComparison.OrdinalIgnoreCase)) return "BN";
        if (Username.StartsWith("U",   StringComparison.OrdinalIgnoreCase)) return "OLS";

        return IsDba() ? "DBA" : "UNKNOWN";
    }

    // ── Validate identifier an toàn (chống SQL injection trong DDL) ───────────
    public static string SafeIdentifier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên không được để trống.");
        if (!Regex.IsMatch(name, @"^[A-Za-z][A-Za-z0-9_$#]{0,29}$"))
            throw new ArgumentException($"Tên '{name}' không hợp lệ (chỉ chữ/số/_ , bắt đầu bằng chữ, tối đa 30 ký tự).");
        return name.ToUpper();
    }

    // ── OracleParameter helpers ────────────────────────────────────────────────
    // Bind chuỗi dưới dạng NVarchar2 (national charset = Unicode/AL16UTF16) để tiếng Việt
    // LUÔN round-trip đúng với cột NVARCHAR2/NCLOB, kể cả khi DB charset không phải AL32UTF8.
    // (Mặc định OracleParameter suy ra Varchar2 từ string → có thể hỏng dấu khi ghi.)
    public static OracleParameter Param(string name, object? value)
    {
        var p = new OracleParameter(name, value ?? DBNull.Value);
        if (value is string) p.OracleDbType = OracleDbType.NVarchar2;
        return p;
    }
}
