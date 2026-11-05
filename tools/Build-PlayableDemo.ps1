[CmdletBinding()]
param([string]$UnityExe, [switch]$Launch)
$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$versionText = Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') -Raw
$version = [regex]::Match($versionText, 'm_EditorVersion: (\S+)').Groups[1].Value
if (-not $UnityExe) {
    $UnityExe = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
}
if (-not (Test-Path $UnityExe)) { throw "Unity $version not found. Supply -UnityExe with its full path." }
# Use a new directory so old test reports or builds cannot masquerade as this run.
$runId = (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$run = Join-Path $project "Build/PlayableDemo/$runId"
New-Item -ItemType Directory -Path $run -Force | Out-Null
function Run-Unity([string]$Label, [string[]]$Extra) {
    $log = Join-Path $run "$Label.log"
    $arguments = @('-batchmode', '-projectPath', ('"' + $project + '"'), '-logFile', ('"' + $log + '"')) + $Extra
    Write-Host "Unity: $Label (log: $log)"
    $process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -PassThru
    if (-not $process.WaitForExit(1800000)) {
        $process.Kill()
        throw "$Label timed out after 30 minutes. Review $log"
    }
    $process.WaitForExit()
    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        if (Test-Path $log) {
            Write-Host "Unity failure details (last 100 log lines):"
            Get-Content $log -Tail 100 | ForEach-Object { Write-Host $_ }
        }
        $reportPath = Join-Path $run "$Label.xml"
        if (Test-Path $reportPath) {
            try {
                [xml]$report = Get-Content $reportPath -Raw
                foreach ($test in $report.SelectNodes("//test-case[@result='Failed']")) {
                    Write-Host "`nFAILED TEST: $($test.fullname)"
                    Write-Host $test.failure.message.InnerText
                    Write-Host $test.failure.'stack-trace'.InnerText
                }
            } catch { Write-Host "Could not read test failure report: $_" }
        }
        throw "$Label failed with exit $($process.ExitCode). Review $log"
    }
}
$results = @()
foreach ($mode in @('EditMode', 'PlayMode')) {
    $xmlPath = Join-Path $run "$mode.xml"
    # Test Runner owns shutdown; -quit can terminate tests prematurely.
    Run-Unity $mode @('-runTests', '-testPlatform', $mode, '-testResults', ('"' + $xmlPath + '"'))
    if (-not (Test-Path $xmlPath)) { throw "$mode produced no test report." }
    [xml]$xml = Get-Content $xmlPath -Raw
    $root = $xml.'test-run'
    if (-not $root -or $root.result -ne 'Passed' -or [int]$root.passed -le 0 -or [int]$root.failed -ne 0 -or [int]$root.skipped -ne 0) {
        throw "$mode did not pass completely. Review $xmlPath"
    }
    $results += @{mode=$mode; passed=[int]$root.passed; failed=[int]$root.failed; skipped=[int]$root.skipped}
}
$previousOutput = $env:CELLS_DEMO_OUTPUT
$exe = Join-Path $run 'Player/Cells.exe'
try {
    $env:CELLS_DEMO_OUTPUT = $exe
    Run-Unity 'build' @('-quit', '-executeMethod', 'Cells.Editor.DemoProjectSetup.BuildPlayableDemo')
} finally { $env:CELLS_DEMO_OUTPUT = $previousOutput }
if (-not (Test-Path $exe)) { throw 'Build returned without producing the player.' }
if (-not (Select-String -Path (Join-Path $run 'build.log') -Pattern 'CELLS_PLAYABLE_BUILD_OK:' -Quiet)) {
    throw 'Build success marker missing.'
}
$commit = $null
if (Get-Command git -ErrorAction SilentlyContinue) { $commit = git -C $project rev-parse HEAD }
@{schema='cells.playable-demo-build.v1'; requestedEditor=$version; sourceCommit=$commit;
  tests=$results; executable=$exe; sha256=(Get-FileHash $exe -Algorithm SHA256).Hash;
  runtimeVerified=$false; visualReviewComplete=$false} |
    ConvertTo-Json -Depth 6 | Set-Content (Join-Path $run 'build-manifest.json')
Write-Host "Player built: $exe"
Write-Host 'Build and tests passed. Live behavior and appearance still require review.'
if ($Launch) { Start-Process -FilePath $exe -ArgumentList @('-logFile', ('"' + (Join-Path $run 'player.log') + '"')) }
