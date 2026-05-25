# build.ps1 — Сборка и установка CoalRespawnMod
# Использование:
#   .\build.ps1          — собрать Release, скопировать в Mods/
#   .\build.ps1 -Debug   — собрать Debug
#   .\build.ps1 -Clean   — очистить артефакты

param(
    [switch]$Debug,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$ProjectDir  = $PSScriptRoot
$ProjectFile = Join-Path $ProjectDir "CoalRespawnMod.csproj"
$Config      = if ($Debug) { "Debug" } else { "Release" }
$OutputDir   = Join-Path $ProjectDir "bin\$Config\net6.0"
$ModsDir     = Join-Path $ProjectDir "..\Mods"
$ModName     = "CoalRespawnMod"

Write-Host "===============================================" -ForegroundColor DarkCyan
Write-Host "  TLD Mod Builder — $ModName" -ForegroundColor DarkCyan
Write-Host "  Конфигурация: $Config" -ForegroundColor DarkCyan
Write-Host "===============================================" -ForegroundColor DarkCyan

# ── Очистка ──────────────────────────────────────────────────────────────────
if ($Clean) {
    Write-Host "`n[*] Очистка..." -ForegroundColor Cyan
    dotnet clean $ProjectFile --configuration $Config
    Remove-Item -Recurse -Force (Join-Path $ProjectDir "bin"), (Join-Path $ProjectDir "obj") -ErrorAction SilentlyContinue
    Write-Host "[✓] Очищено." -ForegroundColor Green
    exit 0
}

# ── Восстановление пакетов ────────────────────────────────────────────────────
Write-Host "`n[*] Восстановление NuGet-пакетов..." -ForegroundColor Cyan
dotnet restore $ProjectFile --nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[✗] Восстановление пакетов не удалось!" -ForegroundColor Red
    exit 1
}

# ── Сборка ───────────────────────────────────────────────────────────────────
Write-Host "`n[*] Сборка..." -ForegroundColor Cyan
dotnet build $ProjectFile --configuration $Config --nologo --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[✗] Сборка завершилась с ошибкой!" -ForegroundColor Red
    exit 1
}

# ── Копирование DLL в Mods/ ───────────────────────────────────────────────────
$SrcDll  = Join-Path $OutputDir "$ModName.dll"
$DestDll = Join-Path $ModsDir   "$ModName.dll"

if (!(Test-Path $SrcDll)) {
    Write-Host "[✗] Файл не найден: $SrcDll" -ForegroundColor Red
    exit 1
}

Copy-Item -Force $SrcDll $DestDll
Write-Host "`n[✓] Скопировано в Mods/" -ForegroundColor Green

$info = Get-Item $DestDll
Write-Host ""
Write-Host "  Файл  : $($info.FullName)" -ForegroundColor White
Write-Host "  Размер: $([math]::Round($info.Length / 1KB, 1)) KB" -ForegroundColor White
Write-Host "  Время : $($info.LastWriteTime)" -ForegroundColor White
Write-Host ""
Write-Host "  Готово! Запустите игру для проверки." -ForegroundColor Green
