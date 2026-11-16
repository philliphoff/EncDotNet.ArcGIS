<#
.SYNOPSIS
    Step 4 of tools/inspect-signer.md, unattended: signs copies of the unsigned
    packages with the copied signer inside Windows Sandbox.

.DESCRIPTION
    Runs from signer.wsb's LogonCommand, in Windows PowerShell 5.1. Installs
    the .NET runtimes the signer's runtimeconfig.json names, makes the test
    certificate, then signs each package with the copy in <Root>\signer.

    ArcGISSignAddIn.exe reports every outcome, success or failure, in a modal
    "Esri Digital Signature Wizard" box and exits 0 either way, so this
    script closes that box through UI Automation, records its text, and checks
    the package's OPC signature itself. Each signing is given up after
    -TimeoutSec. Everything goes to <Root>\sandbox-log.txt.
#>
param(
    [string] $Root = 'C:\signer-test',
    [int] $TimeoutSec = 120,
    [string] $Subject = 'SoundCharts signing test',
    # For a smoke test on the host, which already has the runtime.
    [switch] $SkipRuntime
)

$ErrorActionPreference = 'Continue'
$log = Join-Path $Root 'sandbox-log.txt'
function Log([string] $text) { $line = "[$(Get-Date -Format HH:mm:ss)] $text"; Add-Content -Path $log -Value $line -Encoding UTF8; Write-Host $line }

Set-Content -Path $log -Value "Sandbox run on $env:COMPUTERNAME, $(Get-Date -Format o)" -Encoding UTF8
$signer = Join-Path $Root 'signer\ArcGISSignAddIn.exe'
Log "Signer copy: $signer (exists: $(Test-Path $signer))"
Log "ArcGIS Pro registry key present: $(Test-Path 'HKLM:\SOFTWARE\ESRI\ArcGISPro')"

# 1. The runtime the signer's runtimeconfig.json names.
$runtimeConfig = Join-Path $Root 'signer\ArcGISSignAddIn.runtimeconfig.json'
if ($SkipRuntime) {
    Log 'Skipping the runtime install (-SkipRuntime).'
} elseif (Test-Path $runtimeConfig) {
    $config = Get-Content $runtimeConfig -Raw | ConvertFrom-Json
    $frameworks = @($config.runtimeOptions.frameworks) + @($config.runtimeOptions.framework) | Where-Object { $_ }
    $channel = ($frameworks[0].version -split '\.')[0..1] -join '.'
    Log "runtimeconfig.json: $(($frameworks | ForEach-Object { "$($_.name) $($_.version)" }) -join ', ')"
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $installer = Join-Path $env:TEMP 'dotnet-install.ps1'
        Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $installer -UseBasicParsing
        $dotnetRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
        foreach ($fw in $frameworks) {
            $runtime = switch ($fw.name) {
                'Microsoft.NETCore.App' { 'dotnet' }
                'Microsoft.WindowsDesktop.App' { 'windowsdesktop' }
                'Microsoft.AspNetCore.App' { 'aspnetcore' }
            }
            Log "Installing $($fw.name) (channel $channel, -Runtime $runtime)"
            & $installer -Channel $channel -Runtime $runtime -InstallDir $dotnetRoot -NoPath *>&1 | Out-String | ForEach-Object { Add-Content $log $_ }
        }
        $env:DOTNET_ROOT = $dotnetRoot
        $env:PATH = "$dotnetRoot;$env:PATH"
        Log "Runtimes: $((& (Join-Path $dotnetRoot 'dotnet.exe') --list-runtimes) -join '; ')"
    } catch { Log "Runtime install failed: $_" }
} else {
    Log 'No runtimeconfig.json: .NET Framework signer, Windows provides the runtime.'
}

# 2. The test certificate (the sandbox has its own store).
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object Subject -eq "CN=$Subject" | Select-Object -First 1
if (-not $cert) { $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=$Subject" -CertStoreLocation Cert:\CurrentUser\My }
Log "Test certificate: $($cert.Subject) $($cert.Thumbprint)"

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, WindowsBase

function Invoke-Signer([string] $package) {
    $psi = New-Object System.Diagnostics.ProcessStartInfo $signer
    $psi.Arguments = "`"$package`" `"/n:$Subject`""
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    Log "Running: $signer $($psi.Arguments)"
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $p = [Diagnostics.Process]::Start($psi)
    $stdout = $p.StandardOutput.ReadToEndAsync(); $stderr = $p.StandardError.ReadToEndAsync()
    $modules = @{}
    $dialogs = @()
    while (-not $p.HasExited -and $sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        Start-Sleep -Milliseconds 500
        # What it loads, as a stand-in for Process Monitor's file view.
        try { $p.Refresh(); foreach ($m in $p.Modules) { $modules[$m.FileName] = $true } } catch {}
        $cond = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ProcessIdProperty, $p.Id)
        try {
            foreach ($w in [System.Windows.Automation.AutomationElement]::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $cond)) {
                $texts = foreach ($e in $w.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
                    if ($e.Current.Name -and $e.Current.Name -ne 'OK') { $e.Current.Name }
                }
                $dialogs += "$($w.Current.Name): $($texts -join ' | ')"
                Log "Dialog at $([math]::Round($sw.Elapsed.TotalSeconds, 1)) s: $($dialogs[-1])"
                $w.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close()
            }
        } catch { Log "Dialog handling error: $_" }
    }
    if (-not $p.HasExited) {
        Log "TIMED OUT after $TimeoutSec s; killing."
        $p.Kill(); $p.WaitForExit()
    } else {
        Log "Exit code: $($p.ExitCode) after $([math]::Round($sw.Elapsed.TotalSeconds, 1)) s"
    }
    if ($stdout.Result) { Log "stdout: $($stdout.Result)" }
    if ($stderr.Result) { Log "stderr: $($stderr.Result)" }
    Log "Modules loaded ($($modules.Count)):"
    $modules.Keys | Sort-Object | ForEach-Object { Add-Content $log "    $_" }

    # The signer's exit code doesn't say whether it worked: check the package.
    try {
        $pkg = [System.IO.Packaging.Package]::Open($package, 'Open', 'Read')
        $mgr = New-Object System.IO.Packaging.PackageDigitalSignatureManager $pkg
        if ($mgr.IsSigned) {
            $c = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 $mgr.Signatures[0].Signer
            Log "Signature: $($mgr.VerifySignatures($false)); signer $($c.Subject); chain $([System.IO.Packaging.PackageDigitalSignatureManager]::VerifyCertificate($c))"
        } else {
            Log 'Signature: NOT SIGNED'
        }
        $pkg.Close()
    } catch { Log "Signature check failed: $_" }
}

# 3. Sign copies of the unsigned packages.
$packages = @(
    @{ From = 'pkg\EncDotNet.ArcGIS.SoundCharts.esriAddinX'; To = 'sandbox.esriAddinX' }
    @{ From = 'pkg\EncDotNet.ArcGIS.SoundCharts.Features.esriPlugin'; To = 'sandbox.esriPlugin' }
)
foreach ($item in $packages) {
    $from = Join-Path $Root $item.From
    $to = Join-Path $Root $item.To
    if (-not (Test-Path $from)) { Log "Skipping: $from doesn't exist."; continue }
    Copy-Item $from $to -Force
    Invoke-Signer $to
}

Log 'Done.'
