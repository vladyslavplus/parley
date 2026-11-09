<#
.SYNOPSIS
Recreates a local demo database, applies the migrations and seeds the demo workspace.

.DESCRIPTION
Destructive by design: the target database is dropped and created again. The script refuses any target that is not a
loopback host (localhost, 127.0.0.1 or ::1), whose database name is not a demo name (parley_demo, optionally with a
suffix, case-sensitive), whose port is protected, or whose connection string has unknown or repeated keys.
With -DockerContainer the container must publish the connection string's port on loopback (checked with docker port).
It also requires -ConfirmReset. The database is recreated with psql on the PATH, or with psql inside -DockerContainer.
The seed process starts the host in Development with migrations enabled for the identity and CRM modules, then runs
the Api `seed-demo` command.

Run it from anywhere; paths are resolved from the repository root. Use -CheckOnly to validate the target and exit.

.EXAMPLE
$env:PARLEY_DEMO_CONNECTION_STRING = "Host=127.0.0.1;Port=5440;Database=parley_demo;Username=postgres;Password=..."
.\scripts\reset-demo.ps1 -ConfirmReset

The connection string reaches the seed process through the Database__ConnectionString environment variable, not its command line.
#>
[CmdletBinding()]
param(
    # Preferred: set PARLEY_DEMO_CONNECTION_STRING instead, so the password stays off the command line.
    [string] $ConnectionString,

    [string] $InputDirectory = 'ml/artifacts/demo_export',

    [string] $AccountsFile = 'artifacts/demo-accounts.txt',

    [string] $DockerContainer,

    [switch] $ConfirmReset,

    [switch] $CheckOnly
)

$ErrorActionPreference = 'Stop'

# Port of the manual-testing compose database. Its data must never be reset by this script.
$ProtectedPorts = @(5433)
# Case-sensitive full match. \z rather than $, which would also accept a trailing newline.
$DatabaseNamePattern = '^parley_demo(_[a-z0-9_]+)?\z'
$LoopbackHosts = @('localhost', '127.0.0.1', '::1')
$MaintenanceDatabase = 'postgres'
$DefaultPort = '5432'
# Port PostgreSQL listens on inside the container; docker port maps it to the host.
$ContainerDatabasePort = 5432

# Keys the script understands, mapped to the field they set. Any other key, or a key given twice (under any spelling),
# is refused: the database this script drops must be the one the seed connects to.
$KeyFields = @{
    'host'          = 'Host'
    'server'        = 'Host'
    'data source'   = 'Host'
    'port'          = 'Port'
    'database'      = 'Database'
    'initial catalog' = 'Database'
    'username'      = 'Username'
    'user id'       = 'Username'
    'user name'     = 'Username'
    'password'      = 'Password'
}

function Read-ConnectionString {
    param([string] $Text)

    $fields = @{}
    $seenFields = @{}
    $refusals = @()
    foreach ($part in $Text.Split(';')) {
        if ([string]::IsNullOrWhiteSpace($part)) {
            continue
        }

        $pair = $part.Split('=', 2)
        if ($pair.Count -ne 2) {
            $refusals += 'The connection string has a part without a value.'
            continue
        }

        $key = $pair[0].Trim().ToLowerInvariant()
        $value = $pair[1].Trim()
        if (-not $KeyFields.ContainsKey($key)) {
            $refusals += "The connection string key '$key' is not supported by this script."
            continue
        }

        if ($value.Contains("'") -or $value.Contains('"')) {
            $refusals += "The value of '$key' is quoted. Quoted values are not supported by this script."
            continue
        }

        $field = $KeyFields[$key]
        if ($seenFields.ContainsKey($field)) {
            $refusals += "The connection string names $field more than once."
            continue
        }

        $seenFields[$field] = $true
        $fields[$field] = $value
    }

    [pscustomobject]@{
        Host     = $fields['Host']
        Port     = if ($fields.ContainsKey('Port')) { $fields['Port'] } else { $DefaultPort }
        Database = $fields['Database']
        Username = $fields['Username']
        Password = $fields['Password']
        Refusals = $refusals
    }
}

function Get-RefusalReasons {
    param($Target)

    $reasons = @()
    $reasons += $Target.Refusals

    if ([string]::IsNullOrWhiteSpace($Target.Host)) {
        $reasons += 'The host is missing. Name a loopback host explicitly (localhost, 127.0.0.1 or ::1).'
    }
    elseif (-not ($LoopbackHosts -contains $Target.Host)) {
        # A single-label name can resolve to another machine, and a host with a colon can carry a port
        # that overrides Port. Only the loopback names are accepted.
        $reasons += "The host $($Target.Host) is not loopback. Only localhost, 127.0.0.1 or ::1 is allowed."
    }

    $port = 0
    if (-not [int]::TryParse($Target.Port, [ref] $port) -or $port -lt 1 -or $port -gt 65535) {
        $reasons += 'The port is not a valid number.'
    }
    elseif ($ProtectedPorts -contains $port) {
        $reasons += "The port $port belongs to the protected manual-testing database. Use another local port."
    }

    if (-not $Target.Database -or $Target.Database -cnotmatch $DatabaseNamePattern) {
        $reasons += "The database name must be parley_demo or parley_demo_<suffix>; got '$($Target.Database)'."
    }

    if (-not $Target.Username) {
        $reasons += 'The user name is missing.'
    }

    return , $reasons
}

# True when the container publishes its database port on a loopback address at the same port as the connection string.
# A publication on any other address is refused too, because it would expose the database beyond this machine.
function Test-DockerPublishesLoopbackPort {
    param([string] $Container, [int] $Port)

    $lines = & docker port $Container "$ContainerDatabasePort/tcp" 2>$null
    if ($LASTEXITCODE -ne 0 -or -not $lines) {
        return $false
    }

    $loopbackMatch = $false
    foreach ($line in @($lines)) {
        if ($line -match '^\s*(\S+):(\d+)\s*$') {
            $address = $Matches[1]
            $published = [int] $Matches[2]
            if ($address -notin @('127.0.0.1', '[::1]')) {
                return $false
            }

            if ($published -eq $Port) {
                $loopbackMatch = $true
            }
        }
    }

    return $loopbackMatch
}

function Invoke-Sql {
    param([string] $Sql, $Target)

    if ($DockerContainer) {
        # The container's own psql connects over its local socket, so no password is passed on the command line.
        $Sql | & docker exec -i $DockerContainer psql -U $Target.Username -d $MaintenanceDatabase -v ON_ERROR_STOP=1 -q
    }
    else {
        $env:PGPASSWORD = $Target.Password
        try {
            & psql -h $Target.Host -p $Target.Port -U $Target.Username -d $MaintenanceDatabase -v ON_ERROR_STOP=1 -q -c $Sql
        }
        finally {
            Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        }
    }

    if ($LASTEXITCODE -ne 0) {
        throw "psql failed with exit code $LASTEXITCODE."
    }
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = $env:PARLEY_DEMO_CONNECTION_STRING
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    [Console]::Error.WriteLine('Refused: no connection string. Set PARLEY_DEMO_CONNECTION_STRING (or pass -ConnectionString).')
    exit 3
}

$target = Read-ConnectionString -Text $ConnectionString

Write-Output "Target: host $($target.Host), port $($target.Port), database $($target.Database)."

$reasons = Get-RefusalReasons -Target $target
if ($reasons.Count -gt 0) {
    foreach ($reason in $reasons) {
        [Console]::Error.WriteLine("Refused: $reason")
    }

    exit 3
}

if ($DockerContainer -and -not (Test-DockerPublishesLoopbackPort -Container $DockerContainer -Port ([int] $target.Port))) {
    [Console]::Error.WriteLine(
        "Refused: container $DockerContainer does not publish port $($target.Port) on loopback (docker port). " +
        'The drop would run in that container while the seed connects to the connection string, so the two must match.')
    exit 3
}

if ($CheckOnly) {
    Write-Output 'Target accepted. Nothing was changed (-CheckOnly).'
    exit 0
}

if (-not $ConfirmReset) {
    [Console]::Error.WriteLine('Refused: the database is dropped and recreated. Pass -ConfirmReset to proceed.')
    exit 3
}

function Resolve-RepositoryPath {
    param([string] $Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $Path))
}

$inputPath = Resolve-RepositoryPath -Path $InputDirectory
if (-not (Test-Path -LiteralPath (Join-Path $inputPath 'contacts.csv.gz'))) {
    [Console]::Error.WriteLine("The demo export is missing in $inputPath. Run the ml demo_export step first.")
    exit 2
}

$accountsPath = Resolve-RepositoryPath -Path $AccountsFile

Push-Location $repositoryRoot
try {
    Invoke-Sql -Sql "DROP DATABASE IF EXISTS `"$($target.Database)`" WITH (FORCE);" -Target $target
    Invoke-Sql -Sql "CREATE DATABASE `"$($target.Database)`";" -Target $target
    Write-Output "Database $($target.Database) recreated."

    # The seed process applies the identity and CRM migrations on start (both are off by default outside this script).
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:Database__ConnectionString = $ConnectionString
    $env:IdentityStorage__ApplyMigrationsOnStartup = 'true'
    $env:CrmStorage__ApplyMigrationsOnStartup = 'true'
    & dotnet run --project (Join-Path $repositoryRoot 'backend/src/Parley.Api/Parley.Api.csproj') --no-launch-profile -- `
        seed-demo --input $inputPath --accounts-file $accountsPath --confirm-target-database
    if ($LASTEXITCODE -ne 0) {
        throw "seed-demo failed with exit code $LASTEXITCODE."
    }

    Write-Output 'Demo workspace seeded.'
}
finally {
    Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    Remove-Item Env:Database__ConnectionString -ErrorAction SilentlyContinue
    Remove-Item Env:IdentityStorage__ApplyMigrationsOnStartup -ErrorAction SilentlyContinue
    Remove-Item Env:CrmStorage__ApplyMigrationsOnStartup -ErrorAction SilentlyContinue
    Pop-Location
}
