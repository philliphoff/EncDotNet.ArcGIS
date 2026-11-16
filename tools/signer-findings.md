# Findings: running ArcGISSignAddIn.exe without ArcGIS Pro

Run on 2026-11-15 on a Windows 11 Pro VM (VMware, ARM64) with ArcGIS Pro 3.7
(3.7.3.1904), following `tools/inspect-signer.md`. No Esri file is in this
repository; names, versions and sizes only.

**Short answer: CI doesn't need Esri's signer.** The signature it makes is a
standard OPC package signature, and our own `tools/PackageSigner` makes the
same one with .NET's `System.IO.Packaging`. In Pro's Add-In Manager, an
add-in signed by our tool and one signed by Esri's look identical. A tampered
copy correctly shows *Verified* unchecked. Ours installs, loads and works in
Pro, with the signature shown as *Untrusted* (self-signed test certificate). Our tool has no UI, takes half the
time, and exits non-zero on failure.

Esri's signer does run from three copied files (249 KB) without Pro's folder.
But it can't run unattended as it is. It reports every result, success or
failure, in a modal message box, exits only after someone presses OK, and
its exit code is always 0.

## The signer

| | |
|---|---|
| Kind | .NET (not .NET Framework): an app host `.exe` plus `ArcGISSignAddIn.dll` (IL only) |
| Target framework | `.NETCoreApp,Version=v10.0` (`net10.0`) |
| Architecture | x64 (`Amd64`) |
| Runtimes needed | `Microsoft.NETCore.App` 10.0 and `Microsoft.WindowsDesktop.App` 10.0 (from `ArcGISSignAddIn.runtimeconfig.json`; it's a WPF app) |
| Version | 13.7.0.1901 |
| Signature format | OPC package digital signature (`System.IO.Packaging`): adds `package/services/digital-signature/...` with the signing certificate, `_rels/.rels` and `[Content_Types].xml` |

The `windows-latest` runner image has both .NET 10 runtimes, so nothing needs
installing there.

## The files

What `tools/inspect-signer.ps1` finds statically, from Pro's bin:

| File | Version | Size (bytes) | Needed for |
|---|---|---:|---|
| ArcGISSignAddIn.exe | 13.7.0.1901 | 172,952 | always |
| ArcGISSignAddIn.dll | 13.7.0.1901 | 75,672 | always |
| ArcGISSignAddIn.runtimeconfig.json | | 519 | always |
| Azure.Core.dll | 1.5000.0.0 | 439,192 | `/kvu:` only |
| Azure.Identity.dll | 1.1400.225.36004 | 356,912 | `/kvu:` only |
| Azure.Security.KeyVault.Certificates.dll | 4.800.25.31702 | 303,136 | `/kvu:` only |
| Azure.Security.KeyVault.Keys.dll | 4.800.25.32705 | 366,152 | `/kvu:` only |
| Microsoft.Identity.Client.dll | 4.73.1.0 | 1,028,656 | `/kvu:` only |
| Microsoft.IdentityModel.Abstractions.dll | 6.35.0.41201 | 18,968 | `/kvu:` only |
| System.ClientModel.dll | 1.800.25.55302 | 232,520 | `/kvu:` only |

Total: 2,994,679 bytes (10 files) for everything; **249,143 bytes (the first
three) for signing with a certificate from the store** (`/n:` or
`/cert-thumbprint:`). Signing both packages with only those three files, in
a folder of their own, worked. The process loaded nothing from Pro's folder
and none of the Azure libraries. Everything else comes from the two .NET
runtimes and Windows: crypt32, cryptui, user32, advapi32, kernel32, shell32.
`System.IO.Packaging` and `System.Security.Cryptography.Xml` are in
`Microsoft.WindowsDesktop.App`.

The `missing` rows, and where each actually comes from:

| Name | Referenced by | Where it is |
|---|---|---|
| System.Memory.Data | Azure.Core and the other Azure libraries | Not on this machine: not in Pro (any folder), the GAC, or any shared framework. It's a NuGet package. |
| Microsoft.Identity.Client.Extensions.Msal | Azure.Identity | Not on this machine (NuGet package: MSAL token-cache persistence). |
| Microsoft.Extensions.Logging.Abstractions | System.ClientModel | Only in `Microsoft.AspNetCore.App`, which the signer doesn't use, so it can't load it. |
| `/System/Library/...`, `/usr/lib/libobjc.dylib`, `libsecret-1.so.0` | Azure.Identity, Microsoft.Identity.Client | macOS and Linux P/Invokes; never loaded on Windows. |

Pro has no `ArcGISSignAddIn.deps.json` or `.config`, so there are no extra
probing paths: the signer looks only in its own folder and its two
frameworks. The first two rows are therefore missing for Pro's own signer
too. They only matter on the `/kvu:` (Azure Key Vault) path. Whether that
path works with Pro's files was **not tested**: it needs a real Key Vault. If
it fails with a missing-assembly error, adding `System.Memory.Data` from NuGet
is the likely fix.

## The help output

`ArcGISSignAddIn.exe /?` prints nothing to the console. It opens a message box
titled *Esri Digital Signature Wizard* and waits for OK. The text in the box:

```
Syntax: ArcGISSignAddIn.exe <add-in file name> [Options]

Options:
 /r:<output file name> --  Output add-in file name
 /n:<subject name> --  Digital Certificate's subject name. This value can be a substring of the entire subject name.
 /c:<certificate file name> -- Digital certificate file path.
 /p:<password> --  Digital Certificate's password.
 /rm -- Remove all digital signatures.
 /localmachine -- Get Digital Certificate from the local machine certificate store. This option requires administrator privileges.
 /cert-thumbprint:<thumbprint> -- Digital Certificate's thumbprint
 /kvu:<key vault url> -- The URL to an Azure Key Vault
 /kvc:<certificate name> -- Azure Key Vault certificate name
```

- **`/s` doesn't exist.** It's not in the help, and the string isn't anywhere
  in the signer's code (the switches the code knows are exactly the ones
  above). The procedure's `/s` came from a guess; it's removed.
- **No verify option.** The code has signature-verification routines
  internally, but no switch exposes them. Verify with `System.IO.Packaging`
  instead (`PackageDigitalSignatureManager.VerifySignatures`).
- **No timestamp option.** OPC signatures carry the signing time from the
  signer's clock, not a trusted timestamp. Once the certificate expires,
  Pro's verdict on old packages depends on how Pro treats expired
  certificates (not tested).
- `-?` and `/help` aren't recognised; they're taken as a file name ("File "-?"
  doesn't exist.", in a message box).
- `/r:` writes the signed package to a new file and leaves the input
  unsigned. Without it, the input is signed in place.
- Azure Key Vault signing (`/kvu:` + `/kvc:`) uses `DefaultAzureCredential`.
  In CI that means the `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` /
  `AZURE_CLIENT_SECRET` environment variables or a federated (OIDC) login
  from `azure/login`. No certificate's private key would be on the runner.

## Results

| Run | Package | Exit code | Time | Message box | Signature check | Pro's Add-In Manager |
|---|---|---:|---:|---|---|---|
| Baseline, Pro's signer | add-in | 0 | 46 s | *The add-in was signed successfully.* | Success, signer `CN=SoundCharts signing test`, chain `UntrustedRoot` | Verified ☑, Trusted ☐, *"WARNING: This Add-In file contains invalid digital signatures"* (checked on `signed.esriAddinX`, a second signing the same way) |
| Baseline, Pro's signer, `/r:` | plug-in | 0 | 18 s | same | same | *pending* |
| Copied signer (all 10 files), host | add-in | 0 | 42 s | same | same | |
| Copied signer (all 10 files), host | plug-in | 0 | 23 s | same | same | |
| Copied signer (3 files), host | plug-in | 0 | 21 s | same | same | |
| Wrong `/n:` subject | plug-in | **0** | 1 s | *No certificates were found in the current user's My store that has a subject name of ...* | not signed | |
| Missing input file | | **0** | 1 s | *File "..." doesn't exist.* | | |
| Sandbox, copied signer | add-in | *pending* | | | | |
| Sandbox, copied signer | plug-in | *pending* | | | | |

The exit code is the code after the message box is dismissed (here by UI
Automation). Until then the process doesn't exit at all. The host runs used
the copy in `C:\signer-test\signer`, which has no Pro folder on its probing
path, on a machine that still has Pro installed. The sandbox run is the
clean-machine test.

## What signing changes in the package

I compared the v0.1.0 `EncDotNet.ArcGIS.SoundCharts.esriAddinX` with a copy
signed in place with `/n:"SoundCharts signing test"`.

**Contents are untouched; the zip is rewritten.** All 84 original entries
keep their names, order, sizes and CRCs. But the signer rewrites the whole
archive:
- Every entry is recompressed. 80 of 84 compressed sizes change, and the
  deflate flag changes from `0x0000` to `0x0002` (maximum compression).
- Every entry's timestamp is set to the signing time.
- The file goes from 166,238,018 to 165,925,061 bytes. So signing isn't
  reproducible byte for byte, and any hash of the unsigned package is lost.

**Seven entries are added** at the end:

| Entry | Size | What it is |
|---|---:|---|
| `[Content_Types].xml` | 1,013 | OPC content types. The unsigned package has none, so it's only a plain zip until it's signed. One `Default` per file extension, in first-seen order, with an invented type `ext/ext` (`dll/dll`, `daml/daml`, `py/py`...), plus the four signature types. |
| `_rels/.rels` | 332 | A package relationship of type `digital-signature/origin` to the origin part. |
| `package/services/digital-signature/origin.psdsor` | 0 | The signature origin. |
| `.../_rels/origin.psdsor.rels` | 375 | Origin → signature relationship. |
| `.../xml-signature/<guid>.psdsxs` | 22,042 | The XML signature. |
| `.../xml-signature/_rels/<guid>.psdsxs.rels` | 372 | Signature → certificate relationship. |
| `.../certificate/<serial>.cer` | 794 | The signing certificate (DER), named by its serial number. |

**The XML signature** is XML-DSig in the OPC form:
- **Algorithms:** C14N 1.0 canonicalization, RSA-SHA256, and SHA-256 digests
  throughout.
- **`SignedInfo`:** one reference, to `#idPackageObject`.
- **`Object`:** holds a `Manifest` with one reference per original part (84),
  each URI carrying its content type (`/Config.daml?ContentType=daml/daml`).
  It has no transforms.
- **What's not signed:** no relationship parts are referenced, so the `.rels`
  files and `[Content_Types].xml` are outside the signature. The content
  types are still protected, through the `?ContentType=` in each reference.
- **`SignatureProperties`:** a `SignatureTime` from the signing machine's
  clock (`YYYY-MM-DDThh:mm:ss.sTZD`, for example `2026-11-15T21:41:03.5-08:00`).
  It's not a trusted timestamp.
- **`KeyInfo`:** has the `KeyName` (subject), the RSA public key and the
  certificate in `X509Data`, as well as the separate certificate part.
- **IDs:** `SignatureIdValue`, `idPackageObject` and `idSignatureTime` are
  `System.IO.Packaging`'s own defaults.

So this looks like plain `PackageDigitalSignatureManager.Sign` over every part,
with the hash set to SHA-256.

## Signing with our own code

`tools/PackageSigner` reproduces it:
1. It copies each entry into a new `System.IO.Packaging` package, in order,
   with ArcGIS's `ext/ext` content types and maximum compression.
2. It signs every part with `PackageDigitalSignatureManager`, using SHA-256
   and the certificate in its own part.
3. It adds the same `KeyInfo` after `SignatureValue`. That's outside
   `SignedInfo`, so it doesn't affect the signature.
4. It opens the output again and verifies it. The exit code is 0 only if
   every part is signed and the signature verifies.

```powershell
dotnet run --project tools/PackageSigner -c Release -- <in> <out> --thumbprint <tp>
#   or --subject <substring>, or --pfx <file> --password-env <variable>
```

Compared with Esri's output for the same add-in:
- **Zip:** the same 91 entries in the same order. All 84 content entries have
  the same compressed bytes and flags, and `[Content_Types].xml` is
  identical.
- **Random IDs:** the two `.rels` files differ only in their random
  relationship IDs. The signature part's name is a random GUID in both.
- **Signature XML:** identical tag for tag (values aside), except that
  Esri's `<Manifest>` also declares an unused
  `xmlns:opc="http://schemas.openxmlformats.org/package/2006/digital-signature"`.
  It's inside the signed object, so a verifier recomputes the digest from
  whatever is there.
- **Checks:** both packages verify with .NET 10 and .NET Framework
  `System.IO.Packaging`.
- **Speed and failures:** it takes 23 s for the add-in (Esri: 49 s) and 12 s
  for the plug-in. There's no UI, and it exits 1 on failure (for example,
  no matching certificate).

| Package | Ours | Esri's | Pro's Add-In Manager |
|---|---|---|---|
| add-in | `C:\signer-test\ours.esriAddinX` | `C:\signer-test\signed.esriAddinX` | **The same for both:** Verified ☑, Trusted ☐, *"WARNING: This Add-In file contains invalid digital signatures"* |
| plug-in | `C:\signer-test\ours.esriPlugin` | `C:\signer-test\baseline.esriPlugin` | *not checked in Pro* |
| add-in, ours with `Config.daml` changed after signing (control) | `C:\signer-test\tampered.esriAddinX` | | Verified ☐, Trusted ☐, same banner. .NET: `InvalidSignature` |

The control shows that *Verified* really checks the contents, so a checked
*Verified* box means Pro accepted our signature.

Installed in Pro, the add-in signed by our tool (`ours.esriAddinX`) loads and
works. Its signature property in Pro reads *Untrusted*, as expected for the
self-signed test certificate.

Pro's Add-In Manager has no single *Un-trusted* or *Invalid* verdict. It
shows two checkboxes, *Verified* and *Trusted*. With the self-signed test
certificate, Esri's signature and ours both get Verified ☑ and Trusted ☐.
Both also get the same banner, which says "invalid" although the signature
verifies. It seems to mean "not trusted": a self-signed root isn't trusted,
and .NET reports the chain as `UntrustedRoot`. A certificate from a trusted
CA should check *Trusted* and clear the banner, but that hasn't been tested.

Since Pro accepts ours, CI doesn't need Esri's files, the storage account,
the message-box workaround or Windows Sandbox. It needs only the certificate
(a `.pfx` from a secret, or later Key Vault). The sandbox run (step 4) wasn't
done; with our own signer it no longer decides anything.

The test machine is Windows 11 on **ARM64** (a VMware VM on Apple silicon).
Pro and its signer are x64 and ran under emulation, on the x64 .NET in
`C:\Program Files\dotnet\x64`. Timings on an x64 runner will differ. An
ARM64 process can't load Esri's x64 DLL to call its internal verifier, so
that check wasn't made.

## Run-time extras

*Pending the sandbox run.* On the host, the copied signer's process loaded no
module from Pro's folder (the module list is in the sandbox script's log). The
signer's code contains no `SOFTWARE\ESRI` registry path or licensing call. It
doesn't start ArcGIS Pro or talk to its licence manager.

## Blockers for CI

1. **Modal message box on every run.** No switch suppresses it. On a
   GitHub-hosted runner, whether WPF can
   show the box there, and whether anything can dismiss it, is untested. (I'm
   not certain how the hosted runner's session is set up; it may have no
   interactive desktop.) If
   the box fails to show, the signer may also throw. If it shows on an
   invisible desktop, the step hangs. Workarounds, in order of preference:
   - Run the signer and dismiss its window with UI Automation, as
     `tools/inspect-signer-sandbox.ps1` does, and give up after a time limit.
     This needs a test on a real runner.
   - Sign with our own code instead: the format is a standard OPC signature
     (`PackageDigitalSignatureManager.Sign`). That works only if Pro accepts
     it the same way. Comparing it with Esri's output (which parts are signed,
     the signature-origin relationship, the certificate part) would show what
     to match.
2. **Exit code is always 0.** CI must verify the package's signature itself
   after signing (`IsSigned` and `VerifySignatures` returning `Success`).
3. **Licence.** Esri's licence has to allow keeping a copy of the three files
   in private storage for CI. Not checked here.

No prompts other than the message box, no UAC, and no licensing check were
seen.
