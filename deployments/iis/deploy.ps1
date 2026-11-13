#Requires -RunAsAdministrator
<#
.SYNOPSIS
  Deploys Food Delivery publish output to IIS under C:\FoodDeveryServer (public port 5050).
#>
param(
    [string]$SourceRoot = "",
    [string]$DeployRoot = "C:\FoodDeveryServer",
    [int]$PublicPort = 5050
)

$ErrorActionPreference = "Stop"
$AppCmd = Join-Path $env:windir "system32\inetsrv\appcmd.exe"

if (-not (Test-Path $AppCmd)) {
    throw "IIS appcmd not found. Install IIS (Web Server) first."
}

if ([string]::IsNullOrWhiteSpace($SourceRoot)) {
    $SourceRoot = Join-Path $PSScriptRoot "publish"
}

if (-not (Test-Path $SourceRoot)) {
    throw "Publish folder not found: $SourceRoot"
}

$services = @(
    @{ Name = "Gateway";   Folder = "Gateway";   Site = "FoodDelivery";           Bindings = @("http/*:${PublicPort}:") }
    @{ Name = "Catalogs";  Folder = "Catalogs";  Site = "FoodDelivery-Catalogs";  Bindings = @("http/127.0.0.1:5100:") }
    @{ Name = "Customers"; Folder = "Customers"; Site = "FoodDelivery-Customers"; Bindings = @("http/127.0.0.1:5200:") }
    @{ Name = "Orders";    Folder = "Orders";    Site = "FoodDelivery-Orders";    Bindings = @("http/127.0.0.1:5300:") }
    @{ Name = "Identity";  Folder = "Identity";  Site = "FoodDelivery-Identity";  Bindings = @("http/127.0.0.1:5400:") }
)

function Invoke-AppCmd {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)
    & $AppCmd @Args
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 183) {
        # 183 / duplicate object is handled by callers when needed
    }
}

function Ensure-AppPool([string]$PoolName) {
    $existing = & $AppCmd list apppool /name:"$PoolName" 2>$null
    if (-not $existing) {
        Write-Host "Creating app pool $PoolName"
        & $AppCmd add apppool /name:"$PoolName" /managedRuntimeVersion:"" /startMode:AlwaysRunning | Out-Null
    }
    & $AppCmd set apppool /apppool.name:"$PoolName" /managedRuntimeVersion:"" | Out-Null
    & $AppCmd set config /section:applicationPools "/[name='$PoolName'].processModel.identityType:ApplicationPoolIdentity" | Out-Null
}

function Ensure-Site {
    param(
        [string]$SiteName,
        [string]$PhysicalPath,
        [string]$PoolName,
        [string[]]$Bindings
    )

    New-Item -ItemType Directory -Force -Path $PhysicalPath | Out-Null
    $binding = ($Bindings -join ",")

    $existing = & $AppCmd list site /name:"$SiteName" 2>$null
    if (-not $existing) {
        Write-Host "Creating site $SiteName -> $PhysicalPath ($binding)"
        & $AppCmd add site /name:"$SiteName" /physicalPath:"$PhysicalPath" /bindings:"$binding" | Out-Null
    }
    else {
        Write-Host "Updating site $SiteName -> $PhysicalPath ($binding)"
        & $AppCmd set site /site.name:"$SiteName" "/bindings:$binding" | Out-Null
        & $AppCmd set vdir /vdir.name:"$SiteName/" /physicalPath:"$PhysicalPath" | Out-Null
    }

    & $AppCmd set app /app.name:"$SiteName/" /applicationPool:"$PoolName" | Out-Null
}

Write-Host "=== Food Delivery IIS deploy ==="
Write-Host "Source : $SourceRoot"
Write-Host "Target : $DeployRoot"
Write-Host "Public : http://localhost:$PublicPort"

# Stop pools before copy
foreach ($svc in $services) {
    $pool = "FoodDelivery-$($svc.Name)"
    $exists = & $AppCmd list apppool /name:"$pool" 2>$null
    if ($exists) {
        Write-Host "Stopping app pool $pool"
        & $AppCmd stop apppool /apppool.name:"$pool" 2>$null | Out-Null
    }
}

Start-Sleep -Seconds 2

# Copy publish output
foreach ($svc in $services) {
    $src = Join-Path $SourceRoot $svc.Folder
    $dst = Join-Path $DeployRoot $svc.Folder
    if (-not (Test-Path $src)) {
        throw "Missing publish output: $src"
    }

    Write-Host "Sync $($svc.Folder) -> $dst"
    New-Item -ItemType Directory -Force -Path $dst | Out-Null
    robocopy $src $dst /MIR /NFL /NDL /NJH /NJS /nc /ns /np | Out-Null
    if ($LASTEXITCODE -ge 8) {
        throw "robocopy failed for $($svc.Folder) with exit $LASTEXITCODE"
    }
}

# Gateway production reverse-proxy config (fixed local backends)
$gatewayProdSettings = Join-Path $PSScriptRoot "appsettings.Production.json"
if (Test-Path $gatewayProdSettings) {
    Copy-Item $gatewayProdSettings (Join-Path $DeployRoot "Gateway\appsettings.Production.json") -Force
}

# Ensure IIS objects
foreach ($svc in $services) {
    $pool = "FoodDelivery-$($svc.Name)"
    $path = Join-Path $DeployRoot $svc.Folder
    Ensure-AppPool $pool
    Ensure-Site -SiteName $svc.Site -PhysicalPath $path -PoolName $pool -Bindings $svc.Bindings

    # Environment for ASP.NET Core under IIS
    $webConfig = Join-Path $path "web.config"
    if (Test-Path $webConfig) {
        [xml]$xml = Get-Content $webConfig
        $aspNetCore = $xml.configuration.location.system.webServer.aspNetCore
        if (-not $aspNetCore) {
            $aspNetCore = $xml.SelectSingleNode("//aspNetCore")
        }
        if ($aspNetCore) {
            $aspNetCore.SetAttribute("hostingModel", "outofprocess")
            $envNode = $aspNetCore.SelectSingleNode("environmentVariables")
            if (-not $envNode) {
                $envNode = $xml.CreateElement("environmentVariables")
                [void]$aspNetCore.AppendChild($envNode)
            }
            $existingEnv = $envNode.SelectSingleNode("environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")
            if (-not $existingEnv) {
                $ev = $xml.CreateElement("environmentVariable")
                $ev.SetAttribute("name", "ASPNETCORE_ENVIRONMENT")
                $ev.SetAttribute("value", "Production")
                [void]$envNode.AppendChild($ev)
            }
            else {
                $existingEnv.SetAttribute("value", "Production")
            }
            $xml.Save($webConfig)
        }
    }
}

# Start everything
foreach ($svc in $services) {
    $pool = "FoodDelivery-$($svc.Name)"
    Write-Host "Starting app pool $pool"
    & $AppCmd start apppool /apppool.name:"$pool" | Out-Null
    & $AppCmd start site /site.name:"$($svc.Site)" 2>$null | Out-Null
}

Write-Host ""
Write-Host "Deploy finished."
Write-Host "Gateway : http://localhost:$PublicPort"
Write-Host "Demo    : http://localhost:$PublicPort/demo"
Write-Host "Catalogs path via gateway: http://localhost:$PublicPort/catalogs/api/v1/products"
