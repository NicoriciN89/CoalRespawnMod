# build.ps1 — Build and install WildernessRenewableMod
# Usage:
#   .\build.ps1          — build Release, copy to Mods/
#   .\build.ps1 -Debug   — build Debug
#   .\build.ps1 -Clean   — clean build artifacts

param(
    [switch]$Debug,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ProjectDir  = $PSScriptRoot
$ProjectFile = Join-Path $ProjectDir "CoalRespawnMod.csproj"
$Config      = if ($Debug) { "Debug" } else { "Release" }
$OutputDir   = Join-Path $ProjectDir "bin\$Config\net6.0"
$ModsDir     = Join-Path $ProjectDir "..\..\Mods"
$ModName     = "WildernessRenewableMod"

Write-Host "===============================================" -ForegroundColor DarkCyan
Write-Host "  TLD Mod Builder — $ModName" -ForegroundColor DarkCyan
Write-Host "  Configuration: $Config" -ForegroundColor DarkCyan
Write-Host "===============================================" -ForegroundColor DarkCyan

# ── Clean ─────────────────────────────────────────────────────────────────────
if ($Clean) {
    Write-Host "`n[*] Cleaning..." -ForegroundColor Cyan
    dotnet clean $ProjectFile --configuration $Config
    Remove-Item -Recurse -Force (Join-Path $ProjectDir "bin"), (Join-Path $ProjectDir "obj") -ErrorAction SilentlyContinue
    Write-Host "[✓] Done." -ForegroundColor Green
    exit 0
}

# ── Restore packages ──────────────────────────────────────────────────────────
Write-Host "`n[*] Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore $ProjectFile --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[✗] Package restore failed!" -ForegroundColor Red
    exit 1
}

# ── Build ─────────────────────────────────────────────────────────────────────
Write-Host "`n[*] Building..." -ForegroundColor Cyan
dotnet build $ProjectFile --configuration $Config --nologo --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[✗] Build failed!" -ForegroundColor Red
    exit 1
}

# ── Copy DLL to Mods/ ─────────────────────────────────────────────────────────
$SrcDll  = Join-Path $OutputDir "$ModName.dll"
$DestDll = Join-Path $ModsDir   "$ModName.dll"

if (!(Test-Path $SrcDll)) {
    Write-Host "[✗] File not found: $SrcDll" -ForegroundColor Red
    exit 1
}

Copy-Item -Force $SrcDll $DestDll
Write-Host "`n[✓] Copied to Mods/" -ForegroundColor Green

$info = Get-Item $DestDll
Write-Host ""
Write-Host "  File : $($info.FullName)" -ForegroundColor White
Write-Host "  Size : $([math]::Round($info.Length / 1KB, 1)) KB" -ForegroundColor White
Write-Host "  Time : $($info.LastWriteTime)" -ForegroundColor White
Write-Host ""
Write-Host "  Done! Launch the game to test." -ForegroundColor Green
