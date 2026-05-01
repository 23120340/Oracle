using HospitalApp.Database;
using Oracle.ManagedDataAccess.Client;

namespace HospitalApp.Forms.Admin;

/// <summary>
/// Phân hệ 1: Ứng dụng Quản trị CSDL Oracle
/// Tabs: Users | Roles | Grant | Revoke | View Privileges
/// </summary>
public class AdminDashboard : Form
{
    private readonly OracleHelper _db;
    private TabControl _tabs = null!;

    // ── User tab controls ──────────────────────────────────────────────────────
    private DataGridView _dgvUsers   = null!;
    private TextBox _txtNewUser      = null!, _txtNewPass = null!;
    private Button _btnCreateUser    = null!, _btnDropUser = null!,
                   _btnLockUser      = null!, _btnUnlockUser = null!,
                   _btnRefreshUsers  = null!;

    // ── Role tab controls ──────────────────────────────────────────────────────
    private DataGridView _dgvRoles   = null!;
    private TextBox _txtNewRole      = null!;
    private Button _btnCreateRole    = null!, _btnDropRole = null!,
                   _btnRefreshRoles  = null!;

    // ── Grant tab controls ─────────────────────────────────────────────────────
    private ComboBox _cmbGrantee     = null!, _cmbGrantType = null!,
                     _cmbObjType     = null!, _cmbPrivilege = null!,
                     _cmbObjSchema   = null!, _cmbObjName   = null!,
                     _cmbSysPriv     = null!, _cmbGrantRole = null!;
    private CheckedListBox _clbColumns = null!;
    private CheckBox _chkGrantOption = null!;
    private Button _btnGrant         = null!;
    private Label _lblColNote        = null!;
    private Panel _pnlObjectPriv     = null!, _pnlSysPriv = null!, _pnlRole = null!;

    // ── Revoke tab controls ────────────────────────────────────────────────────
    private ComboBox _cmbRevokeFrom  = null!;
    private DataGridView _dgvGranted = null!;
    private Button _btnRevoke        = null!, _btnLoadGranted = null!;

    // ── View Privileges tab ────────────────────────────────────────────────────
    private ComboBox _cmbViewTarget  = null!;
    private TabControl _tabPrivDetail= null!;
    private DataGridView _dgvSysPrivs= null!, _dgvObjPrivs = null!,
                         _dgvColPrivs= null!, _dgvRolePrivs= null!;
    private Button _btnViewRefresh   = null!;

    public AdminDashboard(OracleHelper db)
    {
        _db   = db;
        Text  = $"Quản trị CSDL Oracle – {db.Username}@{db.Host}/{db.Sid}";
        Size  = new Size(1050, 720);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize   = new Size(900, 600);
        BackColor     = Color.FromArgb(245, 248, 255);

        BuildUI();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // UI BUILDER
    // ═══════════════════════════════════════════════════════════════════════════
    private void BuildUI()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 50,
            BackColor = Color.FromArgb(30, 90, 160)
        };
        header.Controls.Add(new Label
        {
            Text      = "⚙  Phân hệ 1 – Quản trị CSDL Oracle",
            Dock      = DockStyle.Fill,
            ForeColor = Color.White,
            Font      = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });
        Controls.Add(header);

        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9)
        };
        _tabs.TabPages.Add(BuildUserTab());
        _tabs.TabPages.Add(BuildRoleTab());
        _tabs.TabPages.Add(BuildGrantTab());
        _tabs.TabPages.Add(BuildRevokeTab());
        _tabs.TabPages.Add(BuildViewPrivTab());
        Controls.Add(_tabs);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 1: USER MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildUserTab()
    {
        var page = new TabPage("👤 Users");

        _dgvUsers = MakeGrid(new[]
        {
            "USERNAME", "ACCOUNT_STATUS", "DEFAULT_TABLESPACE",
            "CREATED", "EXPIRY_DATE"
        });
        _dgvUsers.Dock = DockStyle.Fill;

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 110, Padding = new Padding(8) };

        var row1 = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Top,
            Height        = 40,
            Padding       = new Padding(0, 4, 0, 0)
        };
        row1.Controls.Add(Label("Username mới:"));
        _txtNewUser = TB(150); row1.Controls.Add(_txtNewUser);
        row1.Controls.Add(Label("Password:"));
        _txtNewPass = TB(120, isPass: true); row1.Controls.Add(_txtNewPass);
        _btnCreateUser = Btn("➕ Tạo User", Color.Green);
        _btnCreateUser.Click += BtnCreateUser_Click;
        row1.Controls.Add(_btnCreateUser);

        var row2 = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Fill,
            Padding       = new Padding(0, 4, 0, 0)
        };
        _btnDropUser    = Btn("🗑 Drop User",  Color.Crimson);   _btnDropUser.Click    += BtnDropUser_Click;
        _btnLockUser    = Btn("🔒 Lock",       Color.DarkOrange); _btnLockUser.Click    += (_, _) => LockUser(true);
        _btnUnlockUser  = Btn("🔓 Unlock",     Color.Teal);       _btnUnlockUser.Click  += (_, _) => LockUser(false);
        _btnRefreshUsers= Btn("🔄 Refresh",    Color.SteelBlue);  _btnRefreshUsers.Click+= (_, _) => LoadUsers();
        row2.Controls.AddRange(new Control[] { _btnDropUser, _btnLockUser, _btnUnlockUser, _btnRefreshUsers });

        panel.Controls.AddRange(new Control[] { row2, row1 });
        page.Controls.Add(_dgvUsers);
        page.Controls.Add(panel);

        page.Enter += (_, _) => LoadUsers();
        return page;
    }

    private void LoadUsers()
    {
        TryCatch(() =>
        {
            var dt = _db.Query(
                "SELECT USERNAME, ACCOUNT_STATUS, DEFAULT_TABLESPACE, " +
                "TO_CHAR(CREATED,'DD/MM/YYYY') AS CREATED, " +
                "TO_CHAR(EXPIRY_DATE,'DD/MM/YYYY') AS EXPIRY_DATE " +
                "FROM DBA_USERS " +
                "WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','DIP','ORACLE_OCM'," +
                "'XDB','ANONYMOUS','APEX_040000','DBSNMP','WMSYS','EXFSYS') " +
                "ORDER BY USERNAME");
            _dgvUsers.DataSource = dt;
        });
    }

    private void BtnCreateUser_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = OracleHelper.SafeIdentifier(_txtNewUser.Text);
            var pass = _txtNewPass.Text;
            if (string.IsNullOrEmpty(pass)) { Error("Nhập password."); return; }

            _db.Execute($"CREATE USER {name} IDENTIFIED BY \"{pass}\" " +
                        $"DEFAULT TABLESPACE USERS QUOTA UNLIMITED ON USERS");
            _db.Execute($"GRANT CREATE SESSION TO {name}");
            Success($"Đã tạo user '{name}' và cấp CREATE SESSION.");
            _txtNewUser.Clear(); _txtNewPass.Clear();
            LoadUsers();
        });
    }

    private void BtnDropUser_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvUsers, "USERNAME"); if (name is null) return;
            if (MessageBox.Show($"Xóa user '{name}'? (CASCADE)", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _db.Execute($"DROP USER {name} CASCADE");
            Success($"Đã xóa user '{name}'."); LoadUsers();
        });
    }

    private void LockUser(bool doLock)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvUsers, "USERNAME"); if (name is null) return;
            var action = doLock ? "ACCOUNT LOCK" : "ACCOUNT UNLOCK";
            _db.Execute($"ALTER USER {name} {action}");
            Success($"Đã {(doLock ? "khóa" : "mở khóa")} user '{name}'."); LoadUsers();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 2: ROLE MANAGEMENT
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildRoleTab()
    {
        var page = new TabPage("🎭 Roles");

        _dgvRoles = MakeGrid(new[] { "ROLE", "AUTHENTICATION_TYPE", "COMMON" });
        _dgvRoles.Dock = DockStyle.Fill;

        var panel = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(8) };
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            Dock          = DockStyle.Fill
        };
        row.Controls.Add(Label("Tên role mới:"));
        _txtNewRole = TB(150); row.Controls.Add(_txtNewRole);
        _btnCreateRole  = Btn("➕ Tạo Role",   Color.Green);
        _btnDropRole    = Btn("🗑 Drop Role",   Color.Crimson);
        _btnRefreshRoles= Btn("🔄 Refresh",     Color.SteelBlue);
        _btnCreateRole.Click   += BtnCreateRole_Click;
        _btnDropRole.Click     += BtnDropRole_Click;
        _btnRefreshRoles.Click += (_, _) => LoadRoles();
        row.Controls.AddRange(new Control[] { _btnCreateRole, _btnDropRole, _btnRefreshRoles });
        panel.Controls.Add(row);

        page.Controls.Add(_dgvRoles);
        page.Controls.Add(panel);
        page.Enter += (_, _) => LoadRoles();
        return page;
    }

    private void LoadRoles()
    {
        TryCatch(() =>
        {
            _dgvRoles.DataSource = _db.Query(
                "SELECT ROLE, AUTHENTICATION_TYPE, COMMON " +
                "FROM DBA_ROLES ORDER BY ROLE");
        });
    }

    private void BtnCreateRole_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = OracleHelper.SafeIdentifier(_txtNewRole.Text);
            _db.Execute($"CREATE ROLE {name}");
            Success($"Đã tạo role '{name}'."); _txtNewRole.Clear(); LoadRoles();
        });
    }

    private void BtnDropRole_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var name = GetSelectedCell(_dgvRoles, "ROLE"); if (name is null) return;
            if (MessageBox.Show($"Xóa role '{name}'?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _db.Execute($"DROP ROLE {name}");
            Success($"Đã xóa role '{name}'."); LoadRoles();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 3: GRANT PRIVILEGES
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildGrantTab()
    {
        var page = new TabPage("✅ Grant");
        page.AutoScroll = true;

        int y = 10;
        // Grantee
        page.Controls.Add(Lbl("Cấp cho (User/Role):", 10, y));
        _cmbGrantee = Cmb(160, y, 200); page.Controls.Add(_cmbGrantee);

        // Loại grant
        page.Controls.Add(Lbl("Loại:", 375, y));
        _cmbGrantType = new ComboBox { Location = new Point(420, y), Size = new Size(150, 24) };
        _cmbGrantType.Items.AddRange(new[] { "Object Privilege", "System Privilege", "Role" });
        _cmbGrantType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbGrantType.SelectedIndex = 0;
        _cmbGrantType.SelectedIndexChanged += CmbGrantType_Changed;
        page.Controls.Add(_cmbGrantType);

        y += 40;
        // ── Object Privilege panel ──────────────────────────────────────────
        _pnlObjectPriv = new Panel
        {
            Location = new Point(10, y), Size = new Size(980, 260),
            BorderStyle = BorderStyle.FixedSingle
        };
        _pnlObjectPriv.Controls.Add(new Label
        {
            Text = "OBJECT PRIVILEGE", Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(10, 5), AutoSize = true, ForeColor = Color.Navy
        });
        int py = 30;
        _pnlObjectPriv.Controls.Add(Lbl("Loại đối tượng:", 10, py));
        _cmbObjType = new ComboBox { Location = new Point(130, py), Size = new Size(130, 24) };
        _cmbObjType.Items.AddRange(new[] { "TABLE", "VIEW", "PROCEDURE", "FUNCTION" });
        _cmbObjType.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbObjType.SelectedIndex = 0;
        _cmbObjType.SelectedIndexChanged += CmbObjType_Changed;
        _pnlObjectPriv.Controls.Add(_cmbObjType);

        _pnlObjectPriv.Controls.Add(Lbl("Schema:", 280, py));
        _cmbObjSchema = Cmb(330, py, 150); _cmbObjSchema.SelectedIndexChanged += LoadObjectNames;
        _pnlObjectPriv.Controls.Add(_cmbObjSchema);

        _pnlObjectPriv.Controls.Add(Lbl("Đối tượng:", 500, py));
        _cmbObjName = Cmb(570, py, 180); _cmbObjName.SelectedIndexChanged += LoadColumns;
        _pnlObjectPriv.Controls.Add(_cmbObjName);

        py += 35;
        _pnlObjectPriv.Controls.Add(Lbl("Quyền:", 10, py));
        _cmbPrivilege = new ComboBox { Location = new Point(80, py), Size = new Size(150, 24) };
        _cmbPrivilege.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbPrivilege.SelectedIndexChanged += CmbPrivilege_Changed;
        _pnlObjectPriv.Controls.Add(_cmbPrivilege);
        UpdatePrivilegesForObjectType();

        _chkGrantOption = new CheckBox
        {
            Text     = "WITH GRANT OPTION",
            Location = new Point(250, py),
            AutoSize = true,
            Font     = new Font("Segoe UI", 9)
        };
        _pnlObjectPriv.Controls.Add(_chkGrantOption);

        py += 35;
        _lblColNote = new Label
        {
            Text      = "Cột (SELECT/UPDATE cho phép chọn cột – bỏ trống = tất cả):",
            Location  = new Point(10, py),
            AutoSize  = true,
            ForeColor = Color.DimGray
        };
        _pnlObjectPriv.Controls.Add(_lblColNote);

        py += 25;
        _clbColumns = new CheckedListBox
        {
            Location      = new Point(10, py),
            Size          = new Size(820, 90),
            CheckOnClick  = true,
            ScrollAlwaysVisible = false
        };
        _pnlObjectPriv.Controls.Add(_clbColumns);

        py += 100;
        _btnGrant = Btn("✅ Thực hiện GRANT", Color.DarkGreen, width: 180);
        _btnGrant.Location = new Point(10, py);
        _btnGrant.Click += BtnGrant_Click;
        _pnlObjectPriv.Controls.Add(_btnGrant);

        page.Controls.Add(_pnlObjectPriv);

        // ── System Privilege panel ─────────────────────────────────────────
        _pnlSysPriv = new Panel
        {
            Location = new Point(10, y), Size = new Size(980, 120),
            BorderStyle = BorderStyle.FixedSingle, Visible = false
        };
        _pnlSysPriv.Controls.Add(new Label
        {
            Text = "SYSTEM PRIVILEGE", Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(10, 5), AutoSize = true, ForeColor = Color.Navy
        });
        _pnlSysPriv.Controls.Add(Lbl("Quyền hệ thống:", 10, 35));
        _cmbSysPriv = new ComboBox { Location = new Point(130, 35), Size = new Size(250, 24) };
        _cmbSysPriv.DropDownStyle = ComboBoxStyle.DropDownList;
        _cmbSysPriv.Items.AddRange(new[]
        {
            "CREATE SESSION","CREATE TABLE","CREATE VIEW","CREATE PROCEDURE",
            "CREATE TRIGGER","CREATE SEQUENCE","CREATE USER","DROP USER",
            "ALTER USER","GRANT ANY PRIVILEGE","DBA","CONNECT","RESOURCE"
        });
        _pnlSysPriv.Controls.Add(_cmbSysPriv);
        var chkAdminOpt = new CheckBox { Text = "WITH ADMIN OPTION", Location = new Point(400, 35), AutoSize = true };
        _pnlSysPriv.Controls.Add(chkAdminOpt);
        var btnGrantSys = Btn("✅ GRANT", Color.DarkGreen);
        btnGrantSys.Location = new Point(10, 70);
        btnGrantSys.Click += (_, _) =>
        {
            TryCatch(() =>
            {
                var grantee  = _cmbGrantee.Text.Trim().ToUpper();
                var priv     = _cmbSysPriv.Text;
                var adminOpt = chkAdminOpt.Checked ? " WITH ADMIN OPTION" : "";
                _db.Execute($"GRANT {priv} TO {grantee}{adminOpt}");
                Success($"GRANT {priv} TO {grantee} thành công.");
            });
        };
        _pnlSysPriv.Controls.Add(btnGrantSys);
        page.Controls.Add(_pnlSysPriv);

        // ── Role grant panel ───────────────────────────────────────────────
        _pnlRole = new Panel
        {
            Location = new Point(10, y), Size = new Size(980, 90),
            BorderStyle = BorderStyle.FixedSingle, Visible = false
        };
        _pnlRole.Controls.Add(new Label
        {
            Text = "GRANT ROLE", Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(10, 5), AutoSize = true, ForeColor = Color.Navy
        });
        _pnlRole.Controls.Add(Lbl("Role:", 10, 35));
        _cmbGrantRole = Cmb(70, 35, 200);
        _pnlRole.Controls.Add(_cmbGrantRole);
        var btnGrantRole = Btn("✅ GRANT ROLE", Color.DarkGreen);
        btnGrantRole.Location = new Point(290, 30);
        btnGrantRole.Click += (_, _) =>
        {
            TryCatch(() =>
            {
                var grantee = _cmbGrantee.Text.Trim().ToUpper();
                var role    = _cmbGrantRole.Text.Trim().ToUpper();
                _db.Execute($"GRANT {role} TO {grantee}");
                Success($"GRANT {role} TO {grantee} thành công.");
            });
        };
        _pnlRole.Controls.Add(btnGrantRole);
        page.Controls.Add(_pnlRole);

        page.Enter += (_, _) =>
        {
            RefreshGrantees();
            LoadSchemas();
            LoadRolesForGrant();
        };
        return page;
    }

    private void CmbGrantType_Changed(object? s, EventArgs e)
    {
        _pnlObjectPriv.Visible = _cmbGrantType.SelectedIndex == 0;
        _pnlSysPriv.Visible    = _cmbGrantType.SelectedIndex == 1;
        _pnlRole.Visible       = _cmbGrantType.SelectedIndex == 2;
    }

    private void CmbObjType_Changed(object? s, EventArgs e)
    {
        UpdatePrivilegesForObjectType();
        LoadObjectNames(s, e);
    }

    private void UpdatePrivilegesForObjectType()
    {
        _cmbPrivilege.Items.Clear();
        var type = _cmbObjType?.Text ?? "TABLE";
        if (type is "TABLE" or "VIEW")
            _cmbPrivilege.Items.AddRange(new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "REFERENCES" });
        else
            _cmbPrivilege.Items.AddRange(new[] { "EXECUTE" });
        if (_cmbPrivilege.Items.Count > 0) _cmbPrivilege.SelectedIndex = 0;
    }

    private void CmbPrivilege_Changed(object? s, EventArgs e)
    {
        var priv = _cmbPrivilege.Text;
        bool canCol = priv is "SELECT" or "UPDATE";
        _clbColumns.Enabled = canCol;
        _lblColNote.ForeColor = canCol ? Color.DimGray : Color.LightGray;
    }

    private void LoadObjectNames(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            _cmbObjName.Items.Clear();
            _clbColumns.Items.Clear();
            var schema = _cmbObjSchema.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(schema)) return;

            var objType = _cmbObjType.Text switch
            {
                "TABLE"     => "'TABLE'",
                "VIEW"      => "'VIEW'",
                "PROCEDURE" => "'PROCEDURE'",
                "FUNCTION"  => "'FUNCTION'",
                _           => "'TABLE'"
            };

            var dt = _db.Query(
                $"SELECT OBJECT_NAME FROM DBA_OBJECTS " +
                $"WHERE OWNER = '{schema}' AND OBJECT_TYPE = {objType} " +
                $"ORDER BY OBJECT_NAME");
            foreach (DataRow row in dt.Rows)
                _cmbObjName.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadColumns(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            _clbColumns.Items.Clear();
            var schema = _cmbObjSchema.Text.Trim().ToUpper();
            var obj    = _cmbObjName.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(schema) || string.IsNullOrEmpty(obj)) return;

            var dt = _db.Query(
                "SELECT COLUMN_NAME FROM DBA_TAB_COLUMNS " +
                $"WHERE OWNER = '{schema}' AND TABLE_NAME = '{obj}' " +
                "ORDER BY COLUMN_ID");
            foreach (DataRow row in dt.Rows)
                _clbColumns.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadSchemas()
    {
        TryCatch(() =>
        {
            _cmbObjSchema.Items.Clear();
            var dt = _db.Query(
                "SELECT DISTINCT OWNER FROM DBA_OBJECTS " +
                "WHERE OWNER NOT IN ('SYS','SYSTEM','OUTLN','XDB') " +
                "ORDER BY OWNER");
            foreach (DataRow row in dt.Rows)
                _cmbObjSchema.Items.Add(row[0].ToString()!);
        });
    }

    private void RefreshGrantees()
    {
        TryCatch(() =>
        {
            _cmbGrantee.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME AS NAME FROM DBA_USERS " +
                "WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB') " +
                "UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows)
                _cmbGrantee.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadRolesForGrant()
    {
        TryCatch(() =>
        {
            _cmbGrantRole.Items.Clear();
            var dt = _db.Query("SELECT ROLE FROM DBA_ROLES ORDER BY ROLE");
            foreach (DataRow row in dt.Rows)
                _cmbGrantRole.Items.Add(row[0].ToString()!);
        });
    }

    private void BtnGrant_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            var grantee   = _cmbGrantee.Text.Trim().ToUpper();
            var schema    = _cmbObjSchema.Text.Trim().ToUpper();
            var obj       = _cmbObjName.Text.Trim().ToUpper();
            var priv      = _cmbPrivilege.Text.Trim().ToUpper();
            var withGrant = _chkGrantOption.Checked ? " WITH GRANT OPTION" : "";

            if (string.IsNullOrEmpty(grantee) || string.IsNullOrEmpty(obj) || string.IsNullOrEmpty(priv))
            { Error("Vui lòng chọn đủ Grantee, Object và Privilege."); return; }

            // Xây chuỗi cột (nếu có chọn và privilege hỗ trợ column-level)
            var cols = new List<string>();
            if (priv is "SELECT" or "UPDATE")
            {
                foreach (int i in _clbColumns.CheckedIndices)
                    cols.Add(_clbColumns.Items[i].ToString()!);
            }

            string sql;
            if (cols.Count > 0)
                sql = $"GRANT {priv}({string.Join(",", cols)}) ON {schema}.{obj} TO {grantee}{withGrant}";
            else
                sql = $"GRANT {priv} ON {schema}.{obj} TO {grantee}{withGrant}";

            _db.Execute(sql);
            Success($"Thực hiện thành công:\n{sql}");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 4: REVOKE
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildRevokeTab()
    {
        var page = new TabPage("❌ Revoke");

        var topPanel = new FlowLayoutPanel
        {
            Dock      = DockStyle.Top,
            Height    = 45,
            Padding   = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };
        topPanel.Controls.Add(Lbl("User/Role:"));
        _cmbRevokeFrom = Cmb(0, 0, 200);
        topPanel.Controls.Add(_cmbRevokeFrom);
        _btnLoadGranted = Btn("🔍 Tải quyền", Color.SteelBlue);
        _btnLoadGranted.Click += (_, _) => LoadGrantedPrivileges();
        topPanel.Controls.Add(_btnLoadGranted);

        _dgvGranted = MakeGrid(new[]
        {
            "TYPE", "PRIVILEGE", "OBJECT", "GRANTABLE", "COLUMNS"
        });
        _dgvGranted.Dock = DockStyle.Fill;

        var botPanel = new Panel { Dock = DockStyle.Bottom, Height = 45, Padding = new Padding(8) };
        _btnRevoke = Btn("❌ Revoke đã chọn", Color.Crimson, width: 160);
        _btnRevoke.Click += BtnRevoke_Click;
        botPanel.Controls.Add(_btnRevoke);

        page.Controls.Add(_dgvGranted);
        page.Controls.Add(botPanel);
        page.Controls.Add(topPanel);

        page.Enter += (_, _) => RefreshRevokeGrantees();
        return page;
    }

    private void RefreshRevokeGrantees()
    {
        TryCatch(() =>
        {
            _cmbRevokeFrom.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME FROM DBA_USERS WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB')" +
                " UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows) _cmbRevokeFrom.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadGrantedPrivileges()
    {
        TryCatch(() =>
        {
            var target = _cmbRevokeFrom.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(target)) return;

            var dt = _db.Query(
                "SELECT 'Object' AS TYPE, PRIVILEGE, OWNER||'.'||TABLE_NAME AS OBJECT, " +
                "GRANTABLE, NULL AS COLUMNS " +
                $"FROM DBA_TAB_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'Column', PRIVILEGE, OWNER||'.'||TABLE_NAME||'.'||COLUMN_NAME, 'NO', NULL " +
                $"FROM DBA_COL_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'Role', GRANTED_ROLE, NULL, ADMIN_OPTION, NULL " +
                $"FROM DBA_ROLE_PRIVS WHERE GRANTEE = '{target}' " +
                "UNION ALL " +
                "SELECT 'System', PRIVILEGE, NULL, ADMIN_OPTION, NULL " +
                $"FROM DBA_SYS_PRIVS WHERE GRANTEE = '{target}' " +
                "ORDER BY 1,2");
            _dgvGranted.DataSource = dt;
        });
    }

    private void BtnRevoke_Click(object? s, EventArgs e)
    {
        TryCatch(() =>
        {
            if (_dgvGranted.CurrentRow is null) { Error("Chọn quyền cần thu hồi."); return; }
            var row    = _dgvGranted.CurrentRow;
            var type   = row.Cells["TYPE"].Value?.ToString() ?? "";
            var priv   = row.Cells["PRIVILEGE"].Value?.ToString() ?? "";
            var obj    = row.Cells["OBJECT"].Value?.ToString() ?? "";
            var target = _cmbRevokeFrom.Text.Trim().ToUpper();

            string sql = type switch
            {
                "Object" => $"REVOKE {priv} ON {obj} FROM {target}",
                "Column" => $"REVOKE {priv}({obj.Split('.')[2]}) ON {obj.Split('.')[0]}.{obj.Split('.')[1]} FROM {target}",
                "Role"   => $"REVOKE {priv} FROM {target}",
                "System" => $"REVOKE {priv} FROM {target}",
                _        => throw new InvalidOperationException("Loại quyền không xác định.")
            };

            if (MessageBox.Show($"Thực hiện:\n{sql}", "Xác nhận Revoke",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _db.Execute(sql);
            Success($"Revoke thành công."); LoadGrantedPrivileges();
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TAB 5: VIEW PRIVILEGES
    // ═══════════════════════════════════════════════════════════════════════════
    private TabPage BuildViewPrivTab()
    {
        var page = new TabPage("🔍 View Privileges");

        var top = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, Height = 45, Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };
        top.Controls.Add(Lbl("User/Role:"));
        _cmbViewTarget = Cmb(0, 0, 200); top.Controls.Add(_cmbViewTarget);
        _btnViewRefresh = Btn("🔍 Xem quyền", Color.SteelBlue);
        _btnViewRefresh.Click += (_, _) => LoadPrivilegeDetail();
        top.Controls.Add(_btnViewRefresh);

        _tabPrivDetail = new TabControl { Dock = DockStyle.Fill };
        _dgvSysPrivs = MakeGrid(new[] { "PRIVILEGE", "ADMIN_OPTION" });
        _dgvObjPrivs = MakeGrid(new[] { "OWNER", "TABLE_NAME", "PRIVILEGE", "GRANTABLE", "GRANTOR" });
        _dgvColPrivs = MakeGrid(new[] { "OWNER", "TABLE_NAME", "COLUMN_NAME", "PRIVILEGE", "GRANTABLE" });
        _dgvRolePrivs = MakeGrid(new[] { "GRANTED_ROLE", "ADMIN_OPTION", "DEFAULT_ROLE" });

        var tSys  = new TabPage("System Privs");  tSys.Controls.Add(_dgvSysPrivs);  _dgvSysPrivs.Dock  = DockStyle.Fill;
        var tObj  = new TabPage("Object Privs");  tObj.Controls.Add(_dgvObjPrivs);  _dgvObjPrivs.Dock  = DockStyle.Fill;
        var tCol  = new TabPage("Column Privs");  tCol.Controls.Add(_dgvColPrivs);  _dgvColPrivs.Dock  = DockStyle.Fill;
        var tRole = new TabPage("Roles Granted"); tRole.Controls.Add(_dgvRolePrivs); _dgvRolePrivs.Dock = DockStyle.Fill;
        _tabPrivDetail.TabPages.AddRange(new[] { tSys, tObj, tCol, tRole });

        page.Controls.Add(_tabPrivDetail);
        page.Controls.Add(top);
        page.Enter += (_, _) => RefreshViewTargets();
        return page;
    }

    private void RefreshViewTargets()
    {
        TryCatch(() =>
        {
            _cmbViewTarget.Items.Clear();
            var dt = _db.Query(
                "SELECT USERNAME FROM DBA_USERS WHERE USERNAME NOT IN ('SYS','SYSTEM','OUTLN','XDB')" +
                " UNION SELECT ROLE FROM DBA_ROLES ORDER BY 1");
            foreach (DataRow row in dt.Rows) _cmbViewTarget.Items.Add(row[0].ToString()!);
        });
    }

    private void LoadPrivilegeDetail()
    {
        TryCatch(() =>
        {
            var target = _cmbViewTarget.Text.Trim().ToUpper();
            if (string.IsNullOrEmpty(target)) return;

            _dgvSysPrivs.DataSource = _db.Query(
                $"SELECT PRIVILEGE, ADMIN_OPTION FROM DBA_SYS_PRIVS WHERE GRANTEE='{target}' ORDER BY PRIVILEGE");
            _dgvObjPrivs.DataSource = _db.Query(
                $"SELECT OWNER,TABLE_NAME,PRIVILEGE,GRANTABLE,GRANTOR FROM DBA_TAB_PRIVS WHERE GRANTEE='{target}' ORDER BY TABLE_NAME,PRIVILEGE");
            _dgvColPrivs.DataSource = _db.Query(
                $"SELECT OWNER,TABLE_NAME,COLUMN_NAME,PRIVILEGE,GRANTABLE FROM DBA_COL_PRIVS WHERE GRANTEE='{target}' ORDER BY TABLE_NAME,COLUMN_NAME");
            _dgvRolePrivs.DataSource = _db.Query(
                $"SELECT GRANTED_ROLE,ADMIN_OPTION,DEFAULT_ROLE FROM DBA_ROLE_PRIVS WHERE GRANTEE='{target}' ORDER BY GRANTED_ROLE");
        });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    private static DataGridView MakeGrid(string[] columns)
    {
        var dgv = new DataGridView
        {
            ReadOnly          = true,
            AllowUserToAddRows= false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
            SelectionMode     = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect       = false,
            BackgroundColor   = Color.White,
            BorderStyle       = BorderStyle.None,
            RowHeadersVisible = false
        };
        return dgv;
    }

    private static string? GetSelectedCell(DataGridView dgv, string col)
    {
        if (dgv.CurrentRow is null) { MessageBox.Show("Chọn 1 dòng."); return null; }
        return dgv.CurrentRow.Cells[col].Value?.ToString();
    }

    private static Label Lbl(string text, int x = 0, int y = 0) =>
        new() { Text = text, Location = new Point(x, y), AutoSize = true,
                Font = new Font("Segoe UI", 9) };

    private static Label Label(string text) =>
        new() { Text = text, AutoSize = true, Font = new Font("Segoe UI", 9),
                Padding = new Padding(0, 5, 0, 0) };

    private static TextBox TB(int width, bool isPass = false) =>
        new() { Width = width, Height = 24, Font = new Font("Segoe UI", 9),
                PasswordChar = isPass ? '●' : '\0', BorderStyle = BorderStyle.FixedSingle };

    private static Button Btn(string text, Color backColor, int width = 120) =>
        new() { Text = text, Width = width, Height = 30, BackColor = backColor,
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand,
                Padding = new Padding(2) };

    private static ComboBox Cmb(int x, int y, int width) =>
        new() { Location = new Point(x, y), Size = new Size(width, 24),
                DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9) };

    private static void TryCatch(Action action)
    {
        try { action(); }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Success(string msg) =>
        MessageBox.Show(msg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private static void Error(string msg) =>
        MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
