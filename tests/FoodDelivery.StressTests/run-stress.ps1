param(
    [int]$Duration = 45,
    [int]$ReadRps = 40,
    [int]$WriteRps = 8,
    [int]$Copies = 20
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $root

$env:FD_STRESS_DURATION = "$Duration"
$env:FD_STRESS_READ_RPS = "$ReadRps"
$env:FD_STRESS_WRITE_RPS = "$WriteRps"
$env:FD_STRESS_COPIES = "$Copies"

Write-Host "Checking APIs..."
foreach ($port in 5100, 5200, 5300, 5400) {
    try {
        Invoke-WebRequest "http://127.0.0.1:$port/" -UseBasicParsing -TimeoutSec 3 | Out-Null
        Write-Host "  $port OK"
    }
    catch {
        throw "API on port $port is down. Start microservices first."
    }
}

Write-Host "Running NBomber stress (Duration=${Duration}s ReadRPS=$ReadRps WriteRPS=$WriteRps Copies=$Copies)..."
dotnet run --project tests/FoodDelivery.StressTests -c Release --no-launch-profile
