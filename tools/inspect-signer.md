# Can the add-in signer run without ArcGIS Pro?

Signing CI's packages on a GitHub-hosted runner needs Esri's
`ArcGISSignAddIn.exe`, which only ships in Pro's bin folder. These steps find
the smallest set of Pro files it needs and check that set signs a package on a
clean Windows without Pro. Run them on a machine with Pro 3.7, in PowerShell 7.

Before anything leaves your machine, Esri's licence still has to allow keeping
a copy of these files in private storage for CI.

## 1. Record the signer's own help

```powershell
$bin = (Get-ItemProperty 'HKLM:\SOFTWARE\ESRI\ArcGISPro').InstallDir + 'bin'
& "$bin\ArcGISSignAddIn.exe" /? *> signer-help.txt
```

If that opens a window rather than printing, note it. A signer that shows UI
can't run in CI unless some switch (perhaps `/s`) suppresses it.

## 2. List the dependencies and copy them out

```powershell
pwsh tools/inspect-signer.ps1 -CopyTo C:\signer-test\signer -Report C:\signer-test\signer.json
```

The script follows assembly references, P/Invokes and native imports from the
signer, reports where each resolves, and copies the signer plus every
dependency that lives in Pro's bin. Rows marked `missing` are what to look at:
they're in a Pro subfolder, the GAC, or loaded another way.

## 3. Sign with Pro's own signer first

This is the reference result. Make a test certificate (self-signed, so Pro
reports the result as *Un-trusted* — what matters is that it isn't *Invalid*):

```powershell
New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=SoundCharts signing test' -CertStoreLocation Cert:\CurrentUser\My
```

Copy a CI-built `EncDotNet.ArcGIS.SoundCharts.esriAddinX` to
`C:\signer-test\baseline.esriAddinX` and sign it:

```powershell
& "$bin\ArcGISSignAddIn.exe" C:\signer-test\baseline.esriAddinX /n:"SoundCharts signing test" /s
$LASTEXITCODE
```

Double-click it, or open Pro's Add-In Manager after installing it: the
signature should show the test certificate and *Un-trusted*.

## 4. Sign with the copy, on a clean Windows

Windows Sandbox (Windows Pro or Enterprise) is a throwaway Windows with no Pro
on it. Enable it once, as administrator, then restart:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM
```

Save this as `C:\signer-test\signer.wsb` and open it:

```xml
<Configuration>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>C:\signer-test</HostFolder>
      <SandboxFolder>C:\signer-test</SandboxFolder>
      <ReadOnly>false</ReadOnly>
    </MappedFolder>
  </MappedFolders>
</Configuration>
```

In the sandbox, in Windows PowerShell:

1. If step 2 printed a `runtimeconfig.json`, install the runtime it names.
   `Microsoft.WindowsDesktop.App` is the Windows Desktop runtime;
   `Microsoft.NETCore.App` alone is the plain runtime. The runner image has
   both, so this only stands in for it:

   ```powershell
   Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1
   & $env:TEMP\dotnet-install.ps1 -Channel 10.0 -Runtime windowsdesktop
   $env:DOTNET_ROOT = "$env:LOCALAPPDATA\Microsoft\dotnet"
   ```

   With no `runtimeconfig.json` the signer is .NET Framework, which Windows
   already has.

2. Make the same test certificate (the sandbox has its own store), copy the
   unsigned package to `C:\signer-test\sandbox.esriAddinX`, and sign it with a
   time limit, so a hidden dialog shows up as a hang, not a stuck session:

   ```powershell
   New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=SoundCharts signing test' -CertStoreLocation Cert:\CurrentUser\My
   $p = Start-Process C:\signer-test\signer\ArcGISSignAddIn.exe -ArgumentList 'C:\signer-test\sandbox.esriAddinX','/n:"SoundCharts signing test"','/s' -PassThru -NoNewWindow
   if (-not $p.WaitForExit(120000)) { 'Timed out: probably waiting on a dialog.'; $p.Kill() } else { "Exit code $($p.ExitCode)" }
   ```

Back on the host, check `sandbox.esriAddinX` the same way as the baseline. It
passes if Pro shows the signature as *Un-trusted* with the test certificate,
like the baseline, and not *Invalid*.

## 5. If it fails

Run it again in the sandbox under Process Monitor (Sysinternals). Filter on
*Process Name is ArcGISSignAddIn.exe* and look for:

- `NAME NOT FOUND` / `PATH NOT FOUND` on `.dll`, `.json` or `.config` paths:
  a file to add to the copy.
- Registry reads under `HKLM\SOFTWARE\ESRI` or `HKCU\SOFTWARE\ESRI`: settings
  CI would have to create.

Add what's missing to `C:\signer-test\signer`, then repeat step 4 in a fresh
sandbox.

## What to send back

- `signer-help.txt`
- `signer.json` and the console output of step 2
- The exit codes and Pro's verdict for steps 3 and 4
- If step 5 was needed, what you added
