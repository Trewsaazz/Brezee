<#
.SYNOPSIS
    Runs every Brezee test suite against an existing build.

.EXAMPLE
    ./eng/run-tests.ps1 -Configuration Debug

    Build first (msbuild Brezee.sln -p:Configuration=Debug -p:Platform=x64). Exits non-zero if any
    suite fails. On GitHub Actions, failures are also reported as annotations.
#>
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

$root = Split-Path -Parent $PSScriptRoot
$onCi = $env:GITHUB_ACTIONS -eq 'true'

$suites = @(
    @{ Name = 'Core'; Path = "x64/$Configuration/Brezee.Core.Tests.exe" }
    @{ Name = 'App';  Path = "tests/Brezee.App.Tests/bin/x64/$Configuration/net10.0-windows/Brezee.App.Tests.exe" }
)

$failed = @()
foreach ($suite in $suites) {
    $exe = Join-Path $root $suite.Path
    if (-not (Test-Path $exe)) {
        Write-Host "$($suite.Name) tests not found at $exe. Build the solution first."
        if ($onCi) { Write-Host "::error title=$($suite.Name) tests::Test executable not found: $($suite.Path)" }
        exit 1
    }

    if ($onCi) { Write-Host "::group::$($suite.Name) tests" } else { Write-Host "== $($suite.Name) tests" }
    $output = & $exe 2>&1 | ForEach-Object { "$_" }
    $exitCode = $LASTEXITCODE
    $output | Write-Host
    if ($onCi) { Write-Host '::endgroup::' }

    if ($exitCode -ne 0) {
        $failed += $suite.Name
        if ($onCi) {
            $output | Where-Object { $_.Trim() } | Select-Object -Last 40 |
                ForEach-Object { Write-Host "::error title=$($suite.Name) tests::$_" }
        }
    }
}

if ($failed) {
    Write-Host "Failed suites: $($failed -join ', ')"
    exit 1
}

Write-Host 'All test suites passed.'
