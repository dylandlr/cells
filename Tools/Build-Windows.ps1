#requires -Version 5.1
<#
.SYNOPSIS
Builds the cells Windows x64 development player with a locally activated Unity editor.
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\Tools\Build-Windows.ps1
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\Tools\Build-Windows.ps1 -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
.NOTES
Requires a Windows 64-bit Unity editor and its Windows Build Support module.
This does not circumvent Unity activation or Windows Smart App Control signing.
#>
[CmdletBinding()]
param(
    [string]$UnityEditor = "",
    [string]$ProjectPath = ""
)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($ProjectPath)) { $ProjectPath = $repo }
$ProjectPath = (Resolve-Path $ProjectPath).Path
if (-not (Test-Path (Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"))) {
    throw "Not a Unity project: $ProjectPath"
}

if ([string]::IsNullOrWhiteSpace($UnityEditor)) {
    $hub = "C:\Program Files\Unity\Hub\Editor"
    if (-not (Test-Path $hub)) {
        throw "Unity Hub editor directory missing. Supply -UnityEditor."
    }
    $candidates = @(Get-ChildItem -Path $hub -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\Editor\\Unity\.exe$' } |
        Sort-Object FullName -Descending)
    if ($candidates.Count -eq 0) { throw "No Unity.exe found. Supply -UnityEditor." }
    $UnityEditor = $candidates[0].FullName
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity editor not found: $UnityEditor"
}
$logFolder = Join-Path $ProjectPath "UnityTestResults"
New-Item -ItemType Directory -Force -Path $logFolder | Out-Null
$log = Join-Path $logFolder "WindowsBuild.log"
$exe = Join-Path $ProjectPath "Build\Cells.exe"
$data = Join-Path $ProjectPath "Build\Cells_Data"
Remove-Item -LiteralPath $exe -ErrorAction SilentlyContinue

Write-Host "Building cells Windows player with $UnityEditor"
$arguments = @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $ProjectPath,
    "-executeMethod", "Cells.Editor.DemoProjectSetup.BuildWindows",
    "-logFile", $log
)
& $UnityEditor @arguments
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0 -or -not (Test-Path $exe -PathType Leaf) -or
    -not (Test-Path $data -PathType Container)) {
    throw "Windows build did not complete. Exit=$exitCode, exe=$exe; review $log"
}
$buildSize = (Get-Item -LiteralPath $exe).Length
if ($buildSize -le 0) { throw "Unity generated an empty executable. Inspect $log." }
Write-Host "SUCCESS: Windows player at $exe ($buildSize bytes)."
Write-Host "Distribute the entire Build directory including Cells_Data and required DLLs."
Write-Host "This is an unsigned development build; Smart App Control may block it."
