# Runs the RQ1 test phase from the saved models, hidden, and waits for it to finish.
# Without -Reproduce it writes the committed results (first test-phase attempt). With -Reproduce it re-runs into
# artifacts/final/reproduction and compares every committed output byte for byte.
# Usage, from the ml/ directory: powershell -NoProfile -File scripts/run_uplift_test_from_saved.ps1 [-Reproduce]
param([switch]$Reproduce)
$ErrorActionPreference = 'Stop'
$ml = Split-Path -Parent $PSScriptRoot
$logDir = Join-Path $ml 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$label = if ($Reproduce) { 'reproduction' } else { 'test' }
$stdout = Join-Path $logDir "uplift_$label-$stamp.out.log"
$stderr = Join-Path $logDir "uplift_$label-$stamp.err.log"
$arguments = @('run', '--directory', $ml, 'python', '-m', 'parley_ml.uplift.evaluate',
    '--config', 'configs/uplift_final.yaml', '--test-from-saved')
if ($Reproduce) { $arguments += '--reproduce' }
$uv = (Get-Command uv).Source
$process = Start-Process -FilePath $uv -ArgumentList $arguments `
    -WorkingDirectory $ml -WindowStyle Hidden -PassThru -Wait `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr
Write-Output "uplift $label phase exit code $($process.ExitCode); logs in $logDir"
exit $process.ExitCode
