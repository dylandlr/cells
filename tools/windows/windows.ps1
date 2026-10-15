param(
    [string]$UnityEditor,
    [switch]$SkipTests
)
$ErrorActionPreference = 'Stop'
$project = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$versionLine = Get-Content (Join-Path $project 'ProjectSettings/ProjectVersion.txt') | Select-Object -First 1
$version = ($versionLine -split ': ')[1]
if (-not $UnityEditor) {
    $UnityEditor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$version/Editor/Unity.exe"
}
if (-not (Test-Path $UnityEditor)) { throw "Unity $version not found. Pass -UnityEditor with its full path." }
$artifacts = Join-Path $project ('Artifacts/Windows-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
function Invoke-Editor([string[]]$EditorArguments) {
    & $UnityEditor @EditorArguments
    if ($LASTEXITCODE -ne 0) { throw "Unity failed ($LASTEXITCODE). See $artifacts" }
}
if (-not $SkipTests) {
    foreach ($mode in @('EditMode', 'PlayMode')) {
        $results = Join-Path $artifacts "$mode.xml"
        Invoke-Editor @('-batchmode', '-projectPath', $project, '-runTests', '-testPlatform', $mode,
            '-testResults', $results, '-logFile', (Join-Path $artifacts "$mode.log"))
        if (-not (Test-Path $results)) { throw "Missing $mode test results." }
        [xml]$report = Get-Content $results
        if ($report.'test-run'.result -ne 'Passed' -or [int]$report.'test-run'.total -le 0) {
            throw "$mode tests did not pass. See $results"
        }
    }
}
$oldOutput = $env:CELLS_BUILD_OUTPUT
try {
    $player = Join-Path $artifacts 'Player/Cells.exe'
    $env:CELLS_BUILD_OUTPUT = $player
    Invoke-Editor @('-batchmode', '-quit', '-projectPath', $project, '-buildTarget', 'Win64',
        '-executeMethod', 'Cells.Editor.DemoProjectSetup.BuildWindows', '-logFile', (Join-Path $artifacts 'build.log'))
    foreach ($required in @($player, (Join-Path $artifacts 'Player/UnityPlayer.dll'), (Join-Path $artifacts 'Player/Cells_Data'))) {
        if (-not (Test-Path $required)) { throw "Incomplete Windows player: $required" }
    }
    $archive = Join-Path $artifacts 'cells-windows-x64.zip'
    Compress-Archive -Path (Join-Path $artifacts 'Player/*') -DestinationPath $archive
    Write-Host "Windows package: $archive"
    Write-Host 'Extract the entire ZIP and launch Cells.exe. Runtime testing on Windows is still required.'
} finally { $env:CELLS_BUILD_OUTPUT = $oldOutput }
