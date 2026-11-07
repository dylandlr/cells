[CmdletBinding()]
param(
    [string]$RunId = 'cells-motor-v1',
    [switch]$SmokeOnly
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$trainingExe = Join-Path $projectRoot 'Build/MLTraining/CellsTraining.exe'
if (-not (Test-Path -LiteralPath $trainingExe -PathType Leaf)) {
    throw 'Training player is missing. Run Cells > ML Agents > 4 Build Windows training player in Unity.'
}
$pythonCommand = Get-Command python -ErrorAction Stop
Push-Location $projectRoot
try {
    & $pythonCommand.Source (Join-Path $PSScriptRoot 'check_training_environment.py')
    if ($LASTEXITCODE -ne 0) {
        throw 'Python training environment is incompatible. Unity was not launched.'
    }
    & $pythonCommand.Source (Join-Path $PSScriptRoot 'smoke_motor_environment.py') --build $trainingExe --episodes 3
    if ($LASTEXITCODE -ne 0) {
        throw 'Unity environment smoke test failed. PPO was not started.'
    }
    if (-not $SmokeOnly) {
        & $pythonCommand.Source -m mlagents.trainers.learn 'config/cells-motor-ppo.yaml' --env $trainingExe --run-id $RunId --seed 631
        if ($LASTEXITCODE -ne 0) {
            throw 'ML-Agents trainer exited with an error. Inspect the trainer output.'
        }
    }
}
finally {
    Pop-Location
}
