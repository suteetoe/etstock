#!/usr/bin/env pwsh
# ETStock — ตั้งค่า goose ระดับ project (Windows / PowerShell)
# ใช้: powershell -ExecutionPolicy Bypass -File .\scripts\setup-goose.ps1
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot

# 1) หา config dir ของ goose บน Windows
#    ตาม source ของ goose: %APPDATA%\Block\goose\config\config.yaml
#    (ยืนยันของจริงด้วย `goose info` เผื่อต่างเวอร์ชัน)
$ConfigDir = Join-Path $env:APPDATA "Block\goose\config"
New-Item -ItemType Directory -Force -Path $ConfigDir | Out-Null

# 2) สำรอง config เดิม แล้ววาง team config
$ConfigFile = Join-Path $ConfigDir "config.yaml"
if (Test-Path $ConfigFile) {
    $stamp = [int][double]::Parse((Get-Date -UFormat %s))
    Copy-Item $ConfigFile "$ConfigFile.bak.$stamp"
    Write-Host "สำรอง config.yaml เดิมไว้แล้ว"
}
Copy-Item (Join-Path $RepoRoot ".goose\config.yaml") $ConfigFile -Force
Write-Host "วาง team config ไปที่ $ConfigFile แล้ว"

# 3) ชี้ให้ goose เห็น recipe ในโปรเจกต์
$RecipePath = Join-Path $RepoRoot "recipes"
Write-Host ""
Write-Host "ตั้ง GOOSE_RECIPE_PATH สำหรับ session ปัจจุบัน:"
Write-Host "  `$env:GOOSE_RECIPE_PATH = `"$RecipePath`""
Write-Host "ตั้งแบบถาวร (ระดับ user):"
Write-Host "  setx GOOSE_RECIPE_PATH `"$RecipePath`""
Write-Host ""
Write-Host "อย่าลืม: Copy-Item .env.example .env (เติมคีย์จริง),"
Write-Host "         . .\scripts\load-env.ps1   แล้ว   gh auth login"
