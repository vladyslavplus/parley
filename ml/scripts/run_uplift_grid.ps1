# Runs the uplift validation grid once, with no console window, and waits for it to finish.
# Re-running resumes: finished runs (metrics.json present, same config hash) are skipped.
# Usage, from the ml/ directory:  powershell -NoProfile -File scripts/run_uplift_grid.ps1
$ErrorActionPreference = 'Stop'
$ml = Split-Path -Parent $PSScriptRoot
$logDir = Join-Path $ml 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stdout = Join-Path $logDir "uplift_grid-$stamp.out.log"
$stderr = Join-Path $logDir "uplift_grid-$stamp.err.log"
$uv = (Get-Command uv).Source
$process = Start-Process -FilePath $uv `
    -ArgumentList @('run', '--directory', $ml, 'python', '-m', 'parley_ml.uplift.train_grid', '--config', 'configs/uplift_grid.yaml') `
    -WorkingDirectory $ml -WindowStyle Hidden -PassThru -Wait `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr
Write-Output "uplift grid exit code $($process.ExitCode); logs in $logDir"
exit $process.ExitCode
