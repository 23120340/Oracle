param(
    [string]$HostName = "localhost",
    [string]$Port = "1521",
    [string]$Sid = "XEPDB1",
    [string]$SysPass = "oracle",
    [string]$BvAdminPass = "BVAdmin@2025",
    [string]$LbacsysPass = "lbacsys",
    [switch]$Reset,
    [switch]$AppOnly
)

$ErrorActionPreference = "Stop"
$env:NLS_LANG = ".AL32UTF8"   # FIX: đảm bảo sqlplus đọc/gửi UTF-8 (tiếng Việt không bị hỏng dấu)
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Invoke-SqlScript {
    param(
        [string]$User,
        [string]$Password,
        [string]$ScriptPath,
        [switch]$AsSysDba,
        [string]$Schema          # nếu set: prepend ALTER SESSION SET CURRENT_SCHEMA (cho file không có CONNECT)
    )

    $safePassword = $Password.Replace('"', '\"')
    $connect = "$User/`"$safePassword`"@$HostName`:$Port/$Sid"
    if ($AsSysDba) {
        $connect = "$connect AS SYSDBA"
    }

    $fullPath = Join-Path $RepoRoot $ScriptPath
    $connectId = "$HostName`:$Port/$Sid"
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("oracle_setup_" + [System.IO.Path]::GetFileName($ScriptPath))
    # FIX: đọc UTF-8 (tự nhận BOM) — tránh Get-Content (PS5.1) đọc nhầm ANSI làm hỏng tiếng Việt
    $scriptText = [System.IO.File]::ReadAllText($fullPath)
    $lines = $scriptText -split "\r?\n"
    $scriptText = ($lines | ForEach-Object {
        if ($_ -match '^(?<prefix>\s*CONNECT\s+)(?<credential>\S+)(?<as>\s+AS\s+SYSDBA)?(?<semi>;?)\s*$') {
            $credential = $Matches['credential']
            $user = ($credential -split '/', 2)[0].Trim('"').ToUpperInvariant()

            if ($user -in @('SYS', 'SYSTEM')) {
                "ALTER SESSION SET CURRENT_SCHEMA = SYS;"
            }
            elseif ($user -eq 'LBACSYS') {
                "ALTER SESSION SET CURRENT_SCHEMA = LBACSYS;"
            }
            elseif ($user -eq 'BVADMIN') {
                "ALTER SESSION SET CURRENT_SCHEMA = BVADMIN;"
            }
            else {
                "-- Skipped demo CONNECT for $user during automated setup."
            }
        }
        else {
            $_
        }
    }) -join [Environment]::NewLine
    if ($Schema) {
        # Các file 11/13/setup_all không có CONNECT nội bộ → cần đặt schema BVADMIN tường minh
        $scriptText = "ALTER SESSION SET CURRENT_SCHEMA = $Schema;" + [Environment]::NewLine + $scriptText
    }
    # FIX: tắt thay thế biến '&' cho MỌI script → tránh prompt "Enter value for ..." khi comment/chuỗi có '&'
    $scriptText = "SET DEFINE OFF" + [Environment]::NewLine + $scriptText
    $scriptText = $scriptText + [Environment]::NewLine + "EXIT" + [Environment]::NewLine
    # FIX: ghi UTF-8 KHÔNG BOM — Set-Content (PS5.1) mặc định ANSI/Windows-1252 sẽ làm hỏng tiếng Việt
    [System.IO.File]::WriteAllText($tempPath, $scriptText, (New-Object System.Text.UTF8Encoding($false)))

    Write-Host "Running $ScriptPath ..." -ForegroundColor Cyan
    sqlplus -L $connect "@$tempPath"
    if ($LASTEXITCODE -ne 0) {
        throw "Failed: $ScriptPath"
    }
}

function Invoke-ResetDemoUsers {
    $safePassword = $SysPass.Replace('"', '\"')
    $connect = "sys/`"$safePassword`"@$HostName`:$Port/$Sid AS SYSDBA"
    $resetSql = @"
WHENEVER SQLERROR CONTINUE
SET SERVEROUTPUT ON
BEGIN
    FOR r IN (
        SELECT username
        FROM dba_users
        WHERE username IN (
            'BVADMIN',
            'DPV_NV001','DPV_NV002',
            'BS_NV003','BS_NV004','BS_NV005',
            'KTV_NV006','KTV_NV007',
            'BN_BN001','BN_BN002','BN_BN003',
            'U1_GIAMDOC','U2_LDTM_HCM','U3_LDTK_HNI','U4_NVTK_HCM',
            'U5_NVTM_HCM','U6_LDP_TM_HCM','U7_LDP_ALL','U8_NVTH_HNI'
        )
    ) LOOP
        EXECUTE IMMEDIATE 'DROP USER ' || r.username || ' CASCADE';
        DBMS_OUTPUT.PUT_LINE('Dropped user ' || r.username);
    END LOOP;
END;
/
BEGIN
    FOR r IN (
        SELECT role
        FROM dba_roles
        WHERE role IN ('DPV_ROLE','BS_ROLE','KTV_ROLE','BENHNHAN_ROLE')
    ) LOOP
        EXECUTE IMMEDIATE 'DROP ROLE ' || r.role;
        DBMS_OUTPUT.PUT_LINE('Dropped role ' || r.role);
    END LOOP;
END;
/
-- Drop OLS policy (kem components/labels/user-labels + cot OLS_LABEL) de chay lai file 05
-- khong bao "already exists" (ORA-12447/12453/00001). Bo qua neu chua co policy.
BEGIN
    SA_SYSDBA.DROP_POLICY('BV_LABEL_POLICY', TRUE);
    DBMS_OUTPUT.PUT_LINE('Dropped OLS policy BV_LABEL_POLICY');
EXCEPTION WHEN OTHERS THEN
    DBMS_OUTPUT.PUT_LINE('OLS policy drop skipped: ' || SQLERRM);
END;
/
EXIT
"@
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) "oracle_setup_reset.sql"
    [System.IO.File]::WriteAllText($tempPath, $resetSql, (New-Object System.Text.UTF8Encoding($false)))
    Write-Host "Resetting demo users/roles ..." -ForegroundColor Yellow
    sqlplus -L $connect "@$tempPath"
    if ($LASTEXITCODE -ne 0) {
        throw "Reset failed."
    }
}

if ($Reset) {
    Invoke-ResetDemoUsers
}

$scripts = if ($AppOnly) {
    @(
        "PhanHe2\01_schema_data.sql",
        "PhanHe2\02_TC1_accounts.sql",
        "PhanHe2\03_YC1_C2_RBAC_KTV_BN.sql",
        "PhanHe2\04_YC1_C3_VPD_DPV_BS.sql",
        "PhanHe2\05_YC2_OLS_ThongBao.sql",
        "PhanHe2\08_App_Migrations.sql",
        "PhanHe2\09_OLS_NhanVien_Unified.sql",
        "PhanHe2\10_XE_App_Demo_Fix.sql"
    )
}
else {
    @(
        "PhanHe2\01_schema_data.sql",
        "PhanHe2\02_TC1_accounts.sql",
        "PhanHe2\03_YC1_C2_RBAC_KTV_BN.sql",
        "PhanHe2\04_YC1_C3_VPD_DPV_BS.sql",
        "PhanHe2\05_YC2_OLS_ThongBao.sql",
        "PhanHe2\06_YC3_Audit.sql",
        "PhanHe2\07_YC4_Backup_Recovery.sql",
        "PhanHe2\08_App_Migrations.sql",
        "PhanHe2\09_OLS_NhanVien_Unified.sql",
        "PhanHe2\10_XE_App_Demo_Fix.sql"
    )
}

foreach ($script in $scripts) {
    Invoke-SqlScript -User "sys" -Password $SysPass -ScriptPath $script -AsSysDba
}

# View tra cứu + grant tổng hợp — chạy trong schema BVADMIN (các file này không có CONNECT nội bộ)
foreach ($g in @(
        "PhanHe2\11_NV_Lookup_Grants.sql",
        "PhanHe2\12_Audit_Grants.sql",
        "PhanHe2\setup_all.sql"
    )) {
    Invoke-SqlScript -User "sys" -Password $SysPass -ScriptPath $g -AsSysDba -Schema "BVADMIN"
}

# Tài khoản DBA cho AdminDashboard (Phân hệ 1)
Invoke-SqlScript -User "sys" -Password $SysPass -ScriptPath "PhanHe2\setup_admin_user.sql" -AsSysDba

Write-Host "Done." -ForegroundColor Green
