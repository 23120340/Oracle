param(
    [string]$HostName = "localhost",
    [string]$Port = "1521",
    [string]$Sid = "XEPDB1",
    [string]$SysPass = "oracle",
    [string]$BvAdminPass = "BVAdmin@2025",
    [string]$LbacsysPass = "lbacsys",
    [switch]$Reset,
    [switch]$AppOnly,
    [switch]$SkipRecoveryDemo
)

$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

function Invoke-SqlScript {
    param(
        [string]$User,
        [string]$Password,
        [string]$ScriptPath,
        [switch]$AsSysDba
    )

    $safePassword = $Password.Replace('"', '\"')
    $connect = "$User/`"$safePassword`"@$HostName`:$Port/$Sid"
    if ($AsSysDba) {
        $connect = "$connect AS SYSDBA"
    }

    $fullPath = Join-Path $RepoRoot $ScriptPath
    $connectId = "$HostName`:$Port/$Sid"
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) ("oracle_setup_" + [System.IO.Path]::GetFileName($ScriptPath))
    $scriptText = Get-Content -LiteralPath $fullPath -Raw
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
    $scriptText = $scriptText + [Environment]::NewLine + "EXIT" + [Environment]::NewLine
    Set-Content -LiteralPath $tempPath -Value $scriptText -NoNewline

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
EXIT
"@
    $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) "oracle_setup_reset.sql"
    Set-Content -LiteralPath $tempPath -Value $resetSql -NoNewline
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

if (-not $SkipRecoveryDemo) {
    Invoke-SqlScript -User "sys" -Password $SysPass -ScriptPath "PhanHe2\09_Recovery_Demo.sql" -AsSysDba
}

Write-Host "Done." -ForegroundColor Green
