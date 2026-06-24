# ============================================================
# Wrapper PowerShell chạy TOÀN BỘ migration cho Phân hệ 2 theo đúng thứ tự
# ============================================================
# Cách dùng:
#   cd D:\repos\Oracle\PhanHe2
#   .\run_migrations.ps1
#
# Script sẽ:
#  1) Set NLS_LANG = .AL32UTF8 (đảm bảo UTF-8)
#  2) Hỏi mật khẩu SYSTEM (kết nối khởi đầu) và SYS (cho các bước SYSDBA)
#  3) Chạy lần lượt 01 → 13 + setup_all, mỗi file dừng ngay nếu gặp lỗi SQL
#
# LƯU Ý QUAN TRỌNG (xem REVIEW_LOI_PHANHE2.md mục B10):
#   Các file .sql có lệnh CONNECT nội bộ với mật khẩu CỐ ĐỊNH:
#     - CONNECT SYSTEM/oracle
#     - CONNECT LBACSYS/lbacsys
#     - CONNECT BVADMIN/"BVAdmin@2025"
#     - CONNECT SYS/&&sys_pwd AS SYSDBA   (sẽ lấy từ giá trị bạn nhập bên dưới)
#   Hãy chỉnh các mật khẩu này cho khớp môi trường của bạn TRƯỚC khi chạy,
#   hoặc chạy thủ công từng file bằng SQL*Plus/SQLcl (cách an toàn nhất).
# ============================================================

param(
    [string]$Host_   = "localhost",
    [int]   $Port    = 1521,
    [string]$Service = "XEPDB1",
    [string]$User    = "SYSTEM"      # file 01 cần quyền tạo user BVADMIN
)

$env:NLS_LANG = ".AL32UTF8"
Write-Host "NLS_LANG = $env:NLS_LANG" -ForegroundColor Cyan

function Read-Plain([string]$prompt) {
    $sec = Read-Host $prompt -AsSecureString
    $bstr = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
    try   { return [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr) }
    finally { [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null }
}

$systemPass = Read-Plain "Mat khau cho $User (ket noi khoi dau)"
$sysPass    = Read-Plain "Mat khau cho SYS (dung cho cac buoc SYSDBA: file 06/07/10)"

$conn      = "$User/$systemPass@//${Host_}:$Port/$Service"
$scriptDir = $PSScriptRoot

# Thứ tự đầy đủ — các file tự CONNECT sang user phù hợp bằng mật khẩu trong file
$migrations = @(
    "01_schema_data.sql",            # schema + dữ liệu mẫu (chạy bởi SYSTEM → tạo BVADMIN)
    "02_TC1_accounts.sql",           # Oracle accounts cho NV/BN (TC#1)
    "03_YC1_C2_RBAC_KTV_BN.sql",     # RBAC + view + INSTEAD OF trigger
    "04_YC1_C3_VPD_DPV_BS.sql",      # VPD policy + audit trigger
    "05_YC2_OLS_ThongBao.sql",       # OLS labels (cần LBACSYS)
    "06_YC3_Audit.sql",              # Standard + FGA audit
    "07_YC4_Backup_Recovery.sql",    # RMAN/DataPump/Flashback config
    "08_App_Migrations.sql",         # SEQ, app log, proc
    "09_OLS_NhanVien_Unified.sql",   # NV_NHANVIEN_View 12 cột + nhãn OLS nhân viên
    "10_XE_App_Demo_Fix.sql",        # fix account mapping cho XE
    "11_NV_Lookup_Grants.sql",       # NV_LOOKUP_View + grants
    "12_Fix_UTF8_Data.sql",          # sửa dữ liệu Việt nếu lệch encoding
    "13_Audit_Grants.sql",           # grant SELECT bảng log
    "setup_all.sql"                  # tổng hợp views + grants (chạy sau cùng)
)

foreach ($f in $migrations) {
    $path = Join-Path $scriptDir $f
    if (-not (Test-Path $path)) {
        Write-Host "Bo qua (khong tim thay): $f" -ForegroundColor Yellow
        continue
    }
    Write-Host "`n-> Chay $f ..." -ForegroundColor Green

    # Prepend:
    #   DEFINE sys_pwd  -> cung cap gia tri cho cac lenh CONNECT SYS/&&sys_pwd trong file (B10)
    #   WHENEVER SQLERROR EXIT -> dung ngay khi gap loi SQL that su (H8)
    # (Cac file 03/04/06 tu dat WHENEVER SQLERROR CONTINUE truoc phan KIEM THU co chu y gay loi.)
    $preamble = "DEFINE sys_pwd=$sysPass`nWHENEVER SQLERROR EXIT SQL.SQLCODE`n@""$path""`n"
    $preamble | sqlplus -L -S $conn

    if ($LASTEXITCODE -ne 0) {
        Write-Host "LOI khi chay $f (exit=$LASTEXITCODE) - dung." -ForegroundColor Red
        break
    }
}

# Clear passwords from memory (best effort)
$systemPass = $null; $sysPass = $null
[GC]::Collect()

Write-Host "`nXong." -ForegroundColor Green
