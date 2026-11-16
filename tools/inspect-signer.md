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
& "$bin\ArcGISSignAddIn.exe" /?
```

On Pro 3.7 this prints nothing: it opens an *Esri Digital Signature Wizard*
message box with the help text, and waits for OK. Copy the text out of the box
(Ctrl+C in a message box, or UI Automation). There is no switch that turns
the box off; see `tools/signer-findings.md`.

## 2. List the dependencies and copy them out

```powershell
pwsh tools/inspect-signer.ps1 -CopyTo C:\signer-test\signer -Report C:\signer-test\signer.json
```

The script follows assembly references, P/Invokes and native imports from the
signer, reports where each resolves, and copies the signer plus every
dependency that lives in Pro's bin. Rows marked `missing` are what to look at:
the `Elsewhere` column says whether each is in a Pro subfolder, another
framework or the GAC, or not on the machine at all.

The static list is the most the signer could need. With `/n:` or
`/cert-thumbprint:` it loads only its own three files (`.exe`, `.dll`,
`.runtimeconfig.json`); the Azure libraries are for `/kvu:` alone. The
sandbox script's log lists what was actually loaded.

## 3. Sign with Pro's own signer first

This is the reference result. Make a test certificate (self-signed, so Pro
reports it as verified but not trusted):

```powershell
New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=SoundCharts signing test' -CertStoreLocation Cert:\CurrentUser\My
```

Copy a CI-built `EncDotNet.ArcGIS.SoundCharts.esriAddinX` to
`C:\signer-test\baseline.esriAddinX` and sign it:

```powershell
& "$bin\ArcGISSignAddIn.exe" C:\signer-test\baseline.esriAddinX /n:"SoundCharts signing test"
```

(There's no `/s`.) The signer reports the result in a message box (*The
add-in was signed successfully.*, or the error) and exits only after OK, with
exit code 0 whether it worked or not. So check the package itself: it's an
OPC package, and `System.IO.Packaging` can verify it:

```powershell
Add-Type -AssemblyName WindowsBase
$pkg = [System.IO.Packaging.Package]::Open('C:\signer-test\baseline.esriAddinX', 'Open', 'Read')
$mgr = [System.IO.Packaging.PackageDigitalSignatureManager]::new($pkg)
$mgr.IsSigned; $mgr.VerifySignatures($false)   # True, Success
$pkg.Close()
```

Double-click it, or open Pro's Add-In Manager after installing it: the
signature should show *Verified* checked and *Trusted* unchecked. With a
self-signed certificate Pro also shows "WARNING: This Add-In file contains
invalid digital signatures", even though the signature verifies.

## 4. Sign with the copy, on a clean Windows

Windows Sandbox (Windows Pro or Enterprise) is a throwaway Windows with no Pro
on it. Enable it once, as administrator, then restart:

```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Containers-DisposableClientVM
```

In a virtual machine, Sandbox also needs nested virtualization (in VMware,
*Virtualize Intel VT-x/EPT or AMD-V/RVI* in the VM's processor settings).

Put the unsigned packages in `C:\signer-test\pkg`, copy
`tools/inspect-signer-sandbox.ps1` to `C:\signer-test\sandbox-run.ps1`, save
this as `C:\signer-test\signer.wsb` and open it:

```xml
<Configuration>
  <Networking>Enable</Networking>
  <MappedFolders>
    <MappedFolder>
      <HostFolder>C:\signer-test</HostFolder>
      <SandboxFolder>C:\signer-test</SandboxFolder>
      <ReadOnly>false</ReadOnly>
    </MappedFolder>
  </MappedFolders>
  <LogonCommand>
    <Command>powershell.exe -NoProfile -ExecutionPolicy Bypass -File C:\signer-test\sandbox-run.ps1</Command>
  </LogonCommand>
</Configuration>
```

The script does the steps below on its own (installs the runtimes, makes the
certificate, signs `sandbox.esriAddinX` and `sandbox.esriPlugin`, closes the
signer's message boxes, checks each signature) and writes everything to
`C:\signer-test\sandbox-log.txt`. Wait for `Done.` in the log, then close the
sandbox. By hand, the steps are, in Windows PowerShell:

1. If step 2 printed a `runtimeconfig.json`, install the runtime it names.
   `Microsoft.WindowsDesktop.App` is the Windows Desktop runtime;
   `Microsoft.NETCore.App` alone is the plain runtime. The runner image has
   both, so this only stands in for it:

   ```powershell
   Invoke-WebRequest https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1
   & $env:TEMP\dotnet-install.ps1 -Channel 10.0 -Runtime dotnet
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
   $p = Start-Process C:\signer-test\signer\ArcGISSignAddIn.exe -ArgumentList 'C:\signer-test\sandbox.esriAddinX','/n:"SoundCharts signing test"' -PassThru
   if (-not $p.WaitForExit(120000)) { 'Timed out: is the message box still up?'; $p.Kill() } else { "Exit code $($p.ExitCode)" }
   ```

   Press OK on the signer's message box (signing the add-in takes about 45
   seconds), and check the signature as in step 3: the exit code is 0 either
   way.

Back on the host, check `sandbox.esriAddinX` the same way as the baseline. It
passes if Pro shows it the same way as the baseline: *Verified* checked,
*Trusted* unchecked.

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
