#requires -Version 5.1
<#
.SYNOPSIS
Runs cells Unity tests using an existing licensed Windows editor installation.
.EXAMPLE
powershell -ExecutionPolicy Bypass -File .\Tools\Run-UnityTests.ps1
.EXAMPLE
.\Tools\Run-UnityTests.ps1 -UnityEditor "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" -Mode EditMode
.NOTES
Does not bypass Unity license activation. Requires the project imported and
a licensed editor. Writes result XML and diagnostic logs under UnityTestResults.
#>
[CmdletBinding()]
param(
    [string] $UnityEditor = "",
    [ValidateSet("EditMode", "PlayMode", "Both")]
    [string] $Mode = "Both",
    [string] $ProjectPath = ""
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
        throw "Unity Hub editor directory missing. Supply -UnityEditor pointing to Unity.exe."
    }
    $candidates = @(Get-ChildItem -Path $hub -Filter Unity.exe -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\Editor\\Unity\.exe$' } |
        Sort-Object FullName -Descending)
    if ($candidates.Count -eq 0) {
        throw "No Unity.exe found in Unity Hub installations. Supply -UnityEditor."
    }
    $UnityEditor = $candidates[0].FullName
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity executable not found: $UnityEditor"
}

$output = Join-Path $ProjectPath "UnityTestResults"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$modes = if ($Mode -eq "Both") { @("EditMode", "PlayMode") } else { @($Mode) }
$errors = 0
foreach ($current in $modes) {
    $xml = Join-Path $output ($current + ".xml")
    $log = Join-Path $output ($current + ".log")
    Remove-Item -LiteralPath $xml -ErrorAction SilentlyContinue

    Write-Host "Running $current Unity tests with $UnityEditor"
    # Passing an argument array avoids shell injection and permits spaces in paths.
    $arguments = @(
        "-batchmode", "-nographics", "-quit",
        "-projectPath", $ProjectPath,
        "-runTests", "-testPlatform", $current,
        "-testResults", $xml,
        "-logFile", $log
    )
    & $UnityEditor @arguments
    $exitCode = $LASTEXITCODE

    if (-not (Test-Path -LiteralPath $xml)) {
        Write-Error "$current produced no XML result (editor exit=$exitCode). Inspect $log." -ErrorAction Continue
        $errors++
        continue
    }
    try {
        [xml] $report = Get-Content -LiteralPath $xml -Raw
        $root = $report.DocumentElement
        $failures = [int] $root.GetAttribute("failed")
        $count = [int] $root.GetAttribute("total")
        $inconclusive = [int] $root.GetAttribute("inconclusive")
        Write-Host "$current total=$count failed=$failures inconclusive=$inconclusive editor_exit=$exitCode"
        if ($count -eq 0 -or $failures -gt 0 -or $inconclusive -gt 0 -or $exitCode -ne 0) {
            Write-Error "$current did not pass. Results: $xml; log: $log" -ErrorAction Continue
            $errors++
        }
    }
    catch {
        Write-Error "Unable to read $current NUnit XML: $_" -ErrorAction Continue
        $errors++
    }
}

if ($errors -gt 0) {
    throw "$errors Unity test mode(s) failed or did not produce valid results."
}
Write-Host "Unity tests passed. Reports: $output"
