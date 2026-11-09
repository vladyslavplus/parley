# Runs the final RQ1 evaluation once, with no console window, and waits for it to finish.
# The first run opens the test split once. Use --reproduce only for a re-run check; see the module docstring.
# Usage, from the ml/ directory: powershell -NoProfile -File scripts/run_uplift_final.ps1
$ErrorActionPreference = 'Stop'
$ml = Split-Path -Parent $PSScriptRoot
$logDir = Join-Path $ml 'artifacts/logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stdout = Join-Path $logDir "uplift_final-$stamp.out.log"
$stderr = Join-Path $logDir "uplift_final-$stamp.err.log"
$uv = (Get-Command uv).Source
$process = Start-Process -FilePath $uv `
    -ArgumentList @('run', '--directory', $ml, 'python', '-m', 'parley_ml.uplift.evaluate', '--config', 'configs/uplift_final.yaml') `
    -WorkingDirectory $ml -WindowStyle Hidden -PassThru -Wait `
    -RedirectStandardOutput $stdout -RedirectStandardError $stderr
Write-Output "uplift final evaluation exit code $($process.ExitCode); logs in $logDir"
exit $process.ExitCode
