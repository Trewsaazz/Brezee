<#
.SYNOPSIS
    Starts a Firebird server from the downloaded kit for the network integration tests.

.DESCRIPTION
    Sets the SYSDBA password in the kit's security database (using the embedded engine, before the
    server starts), launches firebird.exe as an application on port 3050 and waits until it accepts
    connections. Tests find it through BREZEE_TEST_SERVER / BREZEE_TEST_PASSWORD, which this script
    exports to later GitHub Actions steps.

.EXAMPLE
    ./eng/start-test-server.ps1
#>
param(
    [string] $Password = 'masterkey'
)

$root = Split-Path -Parent $PSScriptRoot
$kit = Get-ChildItem (Join-Path $root 'third_party/firebird') -Directory -ErrorAction SilentlyContinue |
    Where-Object { Test-Path (Join-Path $_.FullName 'firebird.exe') } |
    Sort-Object Name -Descending | Select-Object -First 1
if (-not $kit) {
    Write-Host '::error::Firebird kit not found in third_party/firebird. Build the solution first.'
    exit 1
}

# Create (or reset) SYSDBA while no server holds the security database.
$script = Join-Path ([IO.Path]::GetTempPath()) 'brezee-init-security.sql'
"create or alter user SYSDBA password '$Password' using plugin Srp;`ncommit;`nexit;" | Set-Content $script -Encoding ascii
& (Join-Path $kit.FullName 'isql.exe') -user SYSDBA -q -i $script security.db
if ($LASTEXITCODE -ne 0) {
    Write-Host '::error::Could not initialize the Firebird security database.'
    exit 1
}

Start-Process -FilePath (Join-Path $kit.FullName 'firebird.exe') -ArgumentList '-a' -WindowStyle Hidden

$deadline = (Get-Date).AddSeconds(30)
$ready = $false
while (-not $ready -and (Get-Date) -lt $deadline) {
    try {
        $client = [Net.Sockets.TcpClient]::new('127.0.0.1', 3050)
        $client.Dispose()
        $ready = $true
    }
    catch {
        Start-Sleep -Milliseconds 500
    }
}
if (-not $ready) {
    Write-Host '::error::Firebird server did not start listening on port 3050 within 30 seconds.'
    exit 1
}

Write-Host "Firebird server from $($kit.Name) is listening on port 3050."
if ($env:GITHUB_ENV) {
    "BREZEE_TEST_SERVER=localhost" | Add-Content $env:GITHUB_ENV
    "BREZEE_TEST_PASSWORD=$Password" | Add-Content $env:GITHUB_ENV
}
else {
    $env:BREZEE_TEST_SERVER = 'localhost'
    $env:BREZEE_TEST_PASSWORD = $Password
}
