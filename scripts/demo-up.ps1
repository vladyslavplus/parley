#Requires -Version 7.0
<#
.SYNOPSIS
Brings up the whole system in containers in the Development demo mode: API, web, PostgreSQL, Mailpit and (with the `ml`
profile) the scoring service. Waits for health and for the demo seed, then prints the addresses and the demo accounts.

.DESCRIPTION
Idempotent: it creates `.env` from `.env.example` when it is missing and generates any secret that is still a
`<placeholder>`; it never overwrites a real value. The Development seed is switched on only for this Compose call, so
`docker compose --profile app up` on its own stays production-like. Running the script twice is safe: the seed skips a
fresh workspace and the containers stay up.

.PARAMETER Project
Compose project name. Use a distinct name (with a distinct env file and shifted ports) to run a second isolated stack.

.PARAMETER EnvFile
Compose environment file. Defaults to `.env` in the repository root.

.PARAMETER ComposeFile
Explicit Compose files, passed with `-f`. When set, the git-ignored `docker-compose.override.yml` is not merged, which is
what an isolated verification stack needs. When empty, Compose discovers the base file and the local override itself.

.PARAMETER Profiles
Compose profiles to start. Defaults to `app` and `ml`.

.PARAMETER NoBuild
Reuse the existing images instead of rebuilding them.

.PARAMETER SkipArtifacts
Do not create scoring stub artifacts when the model directory is empty.
#>
[CmdletBinding()]
param(
    [string]$Project = "parley",
    [string]$EnvFile = ".env",
    [string[]]$ComposeFile = @(),
    [string[]]$Profiles = @("app", "ml"),
    [switch]$NoBuild,
    [switch]$SkipArtifacts,
    [int]$HealthTimeoutSeconds = 600,
    [int]$SeedTimeoutSeconds = 1200
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$started = [DateTime]::UtcNow
$repoRoot = Split-Path -Parent $PSScriptRoot
$envPath = if ([System.IO.Path]::IsPathRooted($EnvFile)) { $EnvFile } else { Join-Path $repoRoot $EnvFile }
$examplePath = Join-Path $repoRoot ".env.example"

# The demo organization and the seed's default refresh window; both are fixed values of the Development seed.
$demoOrganizationId = "0192f5a0-7b1c-7000-8000-00000000de30"
$seedRefreshAfterDays = 7

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Write-Detail([string]$Message) {
    Write-Host "    $Message"
}

function Write-WarningLine([string]$Message) {
    Write-Host "    ! $Message" -ForegroundColor Yellow
}

function New-RandomHex([int]$Bytes) {
    return [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes($Bytes)).ToLowerInvariant()
}

function Test-Placeholder([string]$Value) {
    return [string]::IsNullOrWhiteSpace($Value) -or ($Value.StartsWith("<") -and $Value.EndsWith(">"))
}

function Read-EnvFile([string]$Path) {
    $values = [ordered]@{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) { continue }
        $separator = $trimmed.IndexOf("=")
        if ($separator -lt 1) { continue }
        $key = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim()
        if ($value.Length -ge 2 -and $value.StartsWith('"') -and $value.EndsWith('"')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        $values[$key] = $value
    }
    return $values
}

function Set-EnvValue([string]$Path, [string]$Key, [string]$Value) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([string[]](Get-Content -LiteralPath $Path))
    $pattern = "^\s*" + [regex]::Escape($Key) + "\s*="
    $replaced = $false
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -match $pattern) {
            $lines[$index] = "$Key=$Value"
            $replaced = $true
        }
    }
    if (-not $replaced) { $lines.Add("$Key=$Value") }
    Set-Content -LiteralPath $Path -Value $lines -Encoding utf8NoBOM
}

function Invoke-Compose([string[]]$Arguments) {
    & docker compose @composeArguments @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Wait-ForHttp([string]$Url, [int]$TimeoutSeconds, [string]$Name) {
    Write-Step "Waiting for $Name at $Url"
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    $lastError = "no response yet"
    $nextReport = [DateTime]::UtcNow
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -SkipHttpErrorCheck -TimeoutSec 5
            if ($response.StatusCode -eq 200) {
                Write-Detail "$Name is ready."
                return
            }
            $lastError = "HTTP $($response.StatusCode)"
        }
        catch {
            $lastError = $_.Exception.Message
        }
        if ([DateTime]::UtcNow -ge $nextReport) {
            $elapsed = [int]([DateTime]::UtcNow - $started).TotalSeconds
            Write-Detail "Still waiting after $elapsed s ($lastError)."
            $nextReport = [DateTime]::UtcNow.AddSeconds(15)
        }
        Start-Sleep -Seconds 5
    }
    throw "$Name did not become ready within $TimeoutSeconds s. Last error: $lastError"
}

# --- Prerequisites ----------------------------------------------------------------------------------------------------
Write-Step "Checking prerequisites"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker is not installed or not on PATH. Install Docker Desktop and start it."
}

& docker version --format "{{.Server.Version}}" *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker is not running. Start Docker Desktop and run the script again."
}

& docker compose version *> $null
if ($LASTEXITCODE -ne 0) {
    throw "Docker Compose v2 is not available. Update Docker Desktop."
}

Write-Detail "Docker and Compose are available."

# --- Environment file and local secrets -------------------------------------------------------------------------------
if (-not (Test-Path -LiteralPath $envPath)) {
    if (-not (Test-Path -LiteralPath $examplePath)) {
        throw "Neither $envPath nor $examplePath exists. Restore the repository files."
    }
    Copy-Item -LiteralPath $examplePath -Destination $envPath
    Write-Detail "Created $envPath from .env.example."
}

$envValues = Read-EnvFile $envPath
$webPort = if ($envValues["WEB_PORT"]) { $envValues["WEB_PORT"] } else { "3000" }
$apiPort = if ($envValues["API_PORT"]) { $envValues["API_PORT"] } else { "5000" }
$mailpitPort = if ($envValues["MAILPIT_HTTP_PORT"]) { $envValues["MAILPIT_HTTP_PORT"] } else { "8025" }

$generated = @(
    @{ Key = "POSTGRES_USER"; Value = "parley" },
    @{ Key = "POSTGRES_PASSWORD"; Value = (New-RandomHex 24) },
    @{ Key = "POSTGRES_DB"; Value = "parley" },
    @{ Key = "PARLEY_ML_KEY"; Value = (New-RandomHex 32) },
    @{ Key = "AUTH_SIGNING_KEY"; Value = (New-RandomHex 32) },
    @{ Key = "UNSUBSCRIBE_TOKEN_KEY"; Value = (New-RandomHex 32) },
    @{ Key = "DEMO_SEED_ACCOUNT_PASSWORD"; Value = (New-RandomHex 8) },
    @{ Key = "PUBLIC_WEB_BASE_URL"; Value = "http://127.0.0.1:$webPort" },
    @{ Key = "PUBLIC_API_BASE_URL"; Value = "http://127.0.0.1:$apiPort" }
)

$createdSecrets = @()
foreach ($entry in $generated) {
    $current = $envValues[$entry.Key]
    if (Test-Placeholder $current) {
        Set-EnvValue -Path $envPath -Key $entry.Key -Value $entry.Value
        $createdSecrets += $entry.Key
    }
}

if ($createdSecrets.Count -gt 0) {
    Write-Detail "Generated local values in ${envPath}: $($createdSecrets -join ', ')."
}
else {
    Write-Detail "All local values in $envPath are set; nothing was overwritten."
}

$envValues = Read-EnvFile $envPath
$seedPassword = $envValues["DEMO_SEED_ACCOUNT_PASSWORD"]
$dbUser = $envValues["POSTGRES_USER"]
$dbName = $envValues["POSTGRES_DB"]

if ([string]::IsNullOrWhiteSpace($seedPassword)) {
    throw "DEMO_SEED_ACCOUNT_PASSWORD is empty in $envPath."
}

# --- Scoring artifacts --------------------------------------------------------------------------------------------------
if (-not $SkipArtifacts) {
    $modelRoot = Join-Path $repoRoot "ml/artifacts/models"
    $upliftModels = @()
    if (Test-Path -LiteralPath $modelRoot) {
        $upliftModels = @(Get-ChildItem -LiteralPath $modelRoot -Directory -Filter "crm_uplift-*" -ErrorAction SilentlyContinue)
    }
    if ($upliftModels.Count -eq 0) {
        if (Get-Command uv -ErrorAction SilentlyContinue) {
            Write-Step "No uplift artifact is present; writing the scoring stub artifacts"
            Push-Location $repoRoot
            try {
                & uv run --directory ml python scripts/write_stub_artifacts.py
                if ($LASTEXITCODE -ne 0) {
                    Write-WarningLine "Writing the stub artifacts failed. The seed continues in RFM mode."
                }
            }
            finally {
                Pop-Location
            }
        }
        else {
            Write-WarningLine "No uplift artifact and no uv on PATH: the seed runs in RFM mode. Train a model or run scripts/write_stub_artifacts.py."
        }
    }
}

# --- Compose settings ---------------------------------------------------------------------------------------------------
$composeArguments = @("-p", $Project, "--env-file", $envPath)
foreach ($file in $ComposeFile) {
    $resolved = if ([System.IO.Path]::IsPathRooted($file)) { $file } else { Join-Path $repoRoot $file }
    $composeArguments += @("-f", $resolved)
}
foreach ($composeProfile in $Profiles) {
    $composeArguments += @("--profile", $composeProfile)
}

# The Development demo mode of this call only. Compose gives the process environment precedence over `.env`, so the
# plain `app` profile keeps its production-like defaults. The values are restored in the `finally` below, so a
# dot-sourced run does not change the caller's session.
$previousSeedEnvironment = @{
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    DEMO_SEED_ENABLED = $env:DEMO_SEED_ENABLED
    DEMO_SEED_ACCOUNT_PASSWORD = $env:DEMO_SEED_ACCOUNT_PASSWORD
    APPLY_MIGRATIONS_ON_STARTUP = $env:APPLY_MIGRATIONS_ON_STARTUP
}
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DEMO_SEED_ENABLED = "true"
$env:DEMO_SEED_ACCOUNT_PASSWORD = $seedPassword
$env:APPLY_MIGRATIONS_ON_STARTUP = "true"

Push-Location $repoRoot
try {
    Write-Step "Starting the stack (project '$Project', profiles: $($Profiles -join ', '))"
    $upArguments = @("up", "-d")
    if (-not $NoBuild) { $upArguments += "--build" }
    Invoke-Compose $upArguments

    Wait-ForHttp -Url "http://127.0.0.1:$apiPort/health" -TimeoutSeconds $HealthTimeoutSeconds -Name "API health"
    Wait-ForHttp -Url "http://127.0.0.1:$webPort/en/login" -TimeoutSeconds $HealthTimeoutSeconds -Name "web"

    # --- Demo seed ----------------------------------------------------------------------------------------------------
    Write-Step "Waiting for the demo workspace (first run generates about 5,000 contacts, orders, campaigns and scores)"
    $markerSql = "SELECT (extract(epoch FROM seeded_at) * 1000)::bigint FROM identity.demo_seed_states WHERE organization_id = '$demoOrganizationId'"
    $seedDeadline = [DateTime]::UtcNow.AddSeconds($SeedTimeoutSeconds)
    $nextSeedReport = [DateTime]::UtcNow
    $seededAt = $null
    while ([DateTime]::UtcNow -lt $seedDeadline) {
        $raw = & docker compose @composeArguments exec -T db psql -U $dbUser -d $dbName -tAc $markerSql 2>$null
        $value = ($raw | Out-String).Trim()
        if ($value -match "^\d+$") {
            $candidate = [DateTimeOffset]::FromUnixTimeMilliseconds([long]$value)
            # A marker older than the refresh window means a stale workspace that is being regenerated right now.
            if ($candidate -gt [DateTimeOffset]::UtcNow.AddDays(-$seedRefreshAfterDays)) {
                $seededAt = $candidate
                break
            }
        }
        if ([DateTime]::UtcNow -ge $nextSeedReport) {
            $elapsed = [int]([DateTime]::UtcNow - $started).TotalSeconds
            Write-Detail "Seeding is still running (elapsed $([TimeSpan]::FromSeconds($elapsed).ToString('mm\:ss')))."
            $nextSeedReport = [DateTime]::UtcNow.AddSeconds(20)
        }
        Start-Sleep -Seconds 10
    }

    if ($null -eq $seededAt) {
        Write-Detail "Last API log lines:"
        & docker compose @composeArguments logs --no-color --tail=40 api
        throw "The demo seed did not finish within $SeedTimeoutSeconds s. Look at the API log above."
    }

    # The marker proves the workspace is complete; the insight rows prove that RFM, the priority list and (with the
    # scoring service) the ML runs are there too. Reading the database avoids pulling the API's full log volume.
    $insightsSql = "SELECT (SELECT count(*) FROM insights.priority_items)::text || '|' || (SELECT coalesce(bool_or(ml_enabled), false)::text FROM insights.ml_settings) || '|' || (SELECT count(*) FROM insights.scores)::text"
    $insightsRaw = & docker compose @composeArguments exec -T db psql -U $dbUser -d $dbName -tAc $insightsSql 2>$null
    $insights = ($insightsRaw | Out-String).Trim()
    if ($insights -match "^(?<priority>\d+)\|(?<ml>true|false)\|(?<scores>\d+)$") {
        $mlMode = if ($Matches["ml"] -eq "true") { "on" } else { "off" }
        Write-Detail "Demo insights: $($Matches["priority"]) priority items, $($Matches["scores"]) scores, ML $mlMode."
        if ($Matches["ml"] -ne "true" -and $Profiles -contains "ml") {
            Write-WarningLine "The seed completed without ML scores (RFM mode). Check the scoring service and the model artifacts."
        }
    }

    $total = [int]([DateTime]::UtcNow - $started).TotalSeconds
    Write-Host ""
    Write-Host "Parley is ready in container mode." -ForegroundColor Green
    Write-Host "  Web app     http://127.0.0.1:$webPort/en/app"
    Write-Host "  API health  http://127.0.0.1:$apiPort/health"
    Write-Host "  Mailpit     http://127.0.0.1:$mailpitPort"
    Write-Host "  Total time  $([TimeSpan]::FromSeconds($total).ToString('mm\:ss'))"
    Write-Host ""
    Write-Host "Demo accounts (password: $seedPassword):" -ForegroundColor Green
    foreach ($account in @(
            "test@gmail.com            admin",
            "admin@demo.parley.test    admin",
            "marketer@demo.parley.test marketer",
            "viewer@demo.parley.test   viewer",
            "manager.north@demo.parley.test  sales manager, North team",
            "manager.south@demo.parley.test  sales manager, South team",
            "rep.north.1@demo.parley.test to rep.north.3  sales rep, North team",
            "rep.south.1@demo.parley.test to rep.south.3  sales rep, South team"
        )) {
        Write-Host "  $account"
    }
    Write-Host ""
    if ($ComposeFile.Count -eq 0) {
        Write-Detail "The API and web containers stay up. The Aspire dashboard and PostgreSQL are published by docker-compose.override.yml."
    }
    else {
        Write-Detail "The API and web containers stay up. This run skipped docker-compose.override.yml; only the app and Mailpit ports are published."
    }
}
finally {
    Pop-Location
    # A dot-sourced run must leave the caller's environment exactly as it found it.
    foreach ($name in $previousSeedEnvironment.Keys) {
        if ($null -eq $previousSeedEnvironment[$name]) {
            Remove-Item -Path "Env:$name" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -Path "Env:$name" -Value $previousSeedEnvironment[$name]
        }
    }
}

