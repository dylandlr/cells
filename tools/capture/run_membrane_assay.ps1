# Run the real Unity membrane experiment on a locally activated Windows Editor.
# Usage: powershell -ExecutionPolicy Bypass -File tools/capture/run_membrane_assay.ps1
param(
    [string]$UnityExe = "",
    [string]$PythonExe = "python"
)
$ErrorActionPreference = "Stop"
$project = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
if (-not $UnityExe) {
    $candidates = @(
        "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe",
        "C:\Program Files\Unity Hub\Editor\6000.6.5f1\Editor\Unity.exe"
    )
    $UnityExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $UnityExe -or -not (Test-Path $UnityExe)) {
    throw "Unity 6000.6.5f1 Editor not found. Specify -UnityExe '<full path to Unity.exe>'."
}
$artifacts = Join-Path $project "artifacts"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$log = Join-Path $artifacts "membrane-unity.log"
$env:CELLS_ASSAY_OUTPUT = "artifacts/membrane-assay.json"
$reports = @("membrane-assay.json", "membrane-convergence.json", "membrane-comparison.json")
# Prevent stale reports from being passed off as newly generated ones.
foreach ($report in $reports) {
    $path = Join-Path $artifacts $report
    if (Test-Path $path) { Remove-Item $path -Force }
}
Write-Host "Running Unity 6000.6.5f1 assay using: $UnityExe"
$unityArgs = @("-batchmode", "-nographics", "-quit", "-projectPath", $project,
    "-executeMethod", "Cells.EditorTools.MembraneAssayBatchExport.Export",
    "-logFile", $log)
& $UnityExe @unityArgs
if ($LASTEXITCODE -ne 0) {
    throw "Unity assay exited $LASTEXITCODE. Inspect $log."
}
foreach ($report in $reports) {
    $path = Join-Path $artifacts $report
    if (-not (Test-Path $path) -or (Get-Item $path).Length -eq 0) {
        throw "Unity did not generate $report. Inspect $log."
    }
}
& $PythonExe (Join-Path $project "tools/capture/analyze_membrane_assay.py") (Join-Path $artifacts $reports[0]) $artifacts
if ($LASTEXITCODE -ne 0) { throw "Failed constriction report validation" }
& $PythonExe (Join-Path $project "tools/capture/analyze_membrane_convergence.py") (Join-Path $artifacts $reports[1]) $artifacts
if ($LASTEXITCODE -ne 0) { throw "Failed convergence report validation" }
& $PythonExe (Join-Path $project "tools/capture/analyze_membrane_comparison.py") (Join-Path $artifacts $reports[2]) $artifacts
if ($LASTEXITCODE -ne 0) { throw "Failed solver comparison report validation" }
Write-Host "Assay complete; validated files at $artifacts"
