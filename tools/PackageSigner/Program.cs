// Signs an ArcGIS Pro add-in (.esriAddinX) or plug-in (.esriPlugin) package
// the way ArcGISSignAddIn.exe does, without its UI:
//
//   1. Copies every entry of the (plain zip) package into a new OPC package,
//      in the same order, with the content type ArcGISSignAddIn uses: the
//      file extension twice ("dll/dll").
//   2. Signs every part with PackageDigitalSignatureManager: RSA-SHA256,
//      SHA-256 digests, the certificate in its own part.
//   3. Adds the KeyInfo ArcGISSignAddIn writes (outside what's signed).
//   4. Opens the result again and verifies the signature.
//
// Before signing, it refuses a certificate that's out of date or not for code
// signing, and warns when Windows wouldn't trust it (see CheckCertificate).
//
// Exit code 0 only when the output is signed and verifies.
//
// How this was worked out: by observing and comparing outputs, not from any
// Esri documentation or source. We signed the same packages with
// ArcGISSignAddIn.exe (ArcGIS Pro 3.7) and compared the results with the
// unsigned input and with this tool's output: zip entries and their order,
// compression, content types, relationship parts, and the XML signature
// (algorithms, references, properties, KeyInfo). Packages signed by this tool
// match Esri's apart from random IDs and one unused namespace declaration on
// the signature's <Manifest>. ArcGIS Pro's Add-In Manager shows the same
// signature verdict for both, rejects a package changed after signing, and
// loads and runs an add-in signed by this tool. A later Pro release could
// expect something different; compare again if it stops accepting these.
//
//   PackageSigner <input> <output> --thumbprint <thumbprint>
//   PackageSigner <input> <output> --subject <subject substring>
//   PackageSigner <input> <output> --pfx <file> [--password-env <variable>]

using System.IO; // Not implicit with UseWPF.
using System.IO.Compression;
using System.IO.Packaging;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

try
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Usage: PackageSigner <input> <output> (--thumbprint <tp> | --subject <name> | --pfx <file> [--password-env <var>])");
        return 2;
    }

    var input = Path.GetFullPath(args[0]);
    var output = Path.GetFullPath(args[1]);
    var options = ParseOptions(args.Skip(2).ToArray());

    using var certificate = LoadCertificate(options);
    if (!certificate.HasPrivateKey)
    {
        throw new InvalidOperationException($"The certificate {certificate.Subject} has no private key.");
    }

    CheckCertificate(certificate, DateTime.Now);

    Console.WriteLine($"Signing {input}");
    Console.WriteLine($"     as {certificate.Subject} ({certificate.Thumbprint}), valid until {certificate.NotAfter:yyyy-MM-dd}");

    var parts = CopyToPackage(input, output);
    Sign(output, certificate, parts);
    return Verify(output) ? 0 : 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: {ex.Message}");
    return 1;
}

// Refuses certificates that make packages ArcGIS Pro won't trust, and warns
// when Windows wouldn't trust the certificate on this machine.
//
// Tested with a private CA: Pro shows a signature as Trusted only when the
// certificate is in date, chains to a trusted root, and its revocation status
// can be checked. The package signature has no trusted timestamp, so Pro
// checks the certificate against the current date: a package signed with a
// certificate that has expired (or expires later) isn't trusted.
static void CheckCertificate(X509Certificate2 certificate, DateTime now)
{
    if (now < certificate.NotBefore)
    {
        throw new InvalidOperationException($"The certificate isn't valid until {certificate.NotBefore:yyyy-MM-dd HH:mm}.");
    }
    if (now > certificate.NotAfter)
    {
        throw new InvalidOperationException($"The certificate expired on {certificate.NotAfter:yyyy-MM-dd}. ArcGIS Pro doesn't trust packages signed with an expired certificate.");
    }

    const string codeSigning = "1.3.6.1.5.5.7.3.3";
    var usages = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().FirstOrDefault();
    if (usages is null || !usages.EnhancedKeyUsages.Cast<System.Security.Cryptography.Oid>().Any(oid => oid.Value == codeSigning))
    {
        throw new InvalidOperationException("The certificate isn't for code signing: its extended key usage doesn't include Code Signing (1.3.6.1.5.5.7.3.3).");
    }

    var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().FirstOrDefault();
    if (keyUsage is not null && !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
    {
        throw new InvalidOperationException("The certificate's key usage doesn't allow digital signatures.");
    }

    var daysLeft = (certificate.NotAfter - now).TotalDays;
    if (daysLeft < 30)
    {
        Console.Error.WriteLine($"warning: the certificate expires in {daysLeft:F0} days; packages signed with it stop being trusted then.");
    }

    // The same check Pro makes, as far as testing shows. A self-signed test
    // certificate fails it, so this is only a warning.
    using var chain = new X509Chain();
    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
    chain.ChainPolicy.ApplicationPolicy.Add(new System.Security.Cryptography.Oid(codeSigning));
    if (!chain.Build(certificate))
    {
        var problems = string.Join(", ", chain.ChainStatus.Select(s => s.Status).Distinct());
        Console.Error.WriteLine($"warning: Windows doesn't trust this certificate here ({problems}); ArcGIS Pro will show the signature as untrusted.");
    }
}

static Dictionary<string, string> ParseOptions(string[] args)
{
    var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    for (var i = 0; i + 1 < args.Length; i += 2)
    {
        options[args[i].TrimStart('-')] = args[i + 1];
    }
    return options;
}

static X509Certificate2 LoadCertificate(Dictionary<string, string> options)
{
    if (options.TryGetValue("pfx", out var pfx))
    {
        var password = options.TryGetValue("password-env", out var variable)
            ? Environment.GetEnvironmentVariable(variable)
            : null;
        return X509CertificateLoader.LoadPkcs12FromFile(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
    }

    using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
    store.Open(OpenFlags.ReadOnly);
    X509Certificate2Collection found;
    if (options.TryGetValue("thumbprint", out var thumbprint))
    {
        found = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
    }
    else if (options.TryGetValue("subject", out var subject))
    {
        // A substring of the subject, like ArcGISSignAddIn's /n:.
        found = store.Certificates.Find(X509FindType.FindBySubjectName, subject, validOnly: false);
    }
    else
    {
        throw new ArgumentException("Give --thumbprint, --subject or --pfx.");
    }

    return found.Count switch
    {
        0 => throw new InvalidOperationException("No matching certificate in CurrentUser\\My."),
        1 => found[0],
        _ => throw new InvalidOperationException($"{found.Count} certificates match; use --thumbprint."),
    };
}

static List<Uri> CopyToPackage(string input, string output)
{
    var parts = new List<Uri>();
    using var zip = ZipFile.OpenRead(input);
    using var package = Package.Open(output, FileMode.Create, FileAccess.ReadWrite);

    foreach (var entry in zip.Entries)
    {
        if (entry.FullName.EndsWith('/'))
        {
            continue; // A folder entry, not a file.
        }
        if (IsPackagingEntry(entry.FullName))
        {
            Console.WriteLine($"  dropping {entry.FullName} (from an earlier signing)");
            continue;
        }

        var uri = PackUriHelper.CreatePartUri(new Uri(
            string.Join('/', entry.FullName.Split('/').Select(Uri.EscapeDataString)), UriKind.Relative));
        var part = package.CreatePart(uri, ContentTypeFor(entry.FullName), CompressionOption.Maximum);
        using (var from = entry.Open())
        using (var to = part.GetStream(FileMode.Create, FileAccess.Write))
        {
            from.CopyTo(to);
        }
        parts.Add(uri);
    }

    Console.WriteLine($"  {parts.Count} parts");
    return parts;
}

// What an earlier signing (by this tool or ArcGISSignAddIn) added.
static bool IsPackagingEntry(string name) =>
    name.Equals("[Content_Types].xml", StringComparison.OrdinalIgnoreCase)
    || name.StartsWith("_rels/", StringComparison.OrdinalIgnoreCase)
    || name.StartsWith("package/services/digital-signature/", StringComparison.OrdinalIgnoreCase);

// As observed in ArcGISSignAddIn's output: "dll/dll", "daml/daml", "py/py".
static string ContentTypeFor(string name)
{
    var extension = Path.GetExtension(name).TrimStart('.');
    return extension.Length == 0 ? "application/octet-stream" : $"{extension}/{extension}";
}

static void Sign(string path, X509Certificate2 certificate, List<Uri> parts)
{
    using var package = Package.Open(path, FileMode.Open, FileAccess.ReadWrite);
    var manager = new PackageDigitalSignatureManager(package)
    {
        HashAlgorithm = "http://www.w3.org/2001/04/xmlenc#sha256",
        CertificateOption = CertificateEmbeddingOption.InCertificatePart,
    };
    var signature = manager.Sign(parts, certificate);
    AddKeyInfo(signature.SignaturePart, certificate);
}

// ArcGISSignAddIn's signatures (as observed in its output) also carry a KeyInfo (subject name, RSA public
// key, certificate) after SignatureValue; PackageDigitalSignatureManager
// writes none. SignedInfo covers only the package object, not KeyInfo, so
// adding it after signing leaves the signature valid.
static void AddKeyInfo(PackagePart signaturePart, X509Certificate2 certificate)
{
    var document = new XmlDocument { PreserveWhitespace = true };
    using (var stream = signaturePart.GetStream(FileMode.Open, FileAccess.Read))
    {
        document.Load(stream);
    }

    var namespaces = new XmlNamespaceManager(document.NameTable);
    namespaces.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);
    var signatureValue = document.SelectSingleNode("/ds:Signature/ds:SignatureValue", namespaces)
        ?? throw new InvalidOperationException("The signature has no SignatureValue.");
    if (document.SelectSingleNode("/ds:Signature/ds:KeyInfo", namespaces) is not null)
    {
        return;
    }

    using var rsa = certificate.GetRSAPublicKey()
        ?? throw new InvalidOperationException("Only RSA certificates are supported.");
    var keyInfo = new KeyInfo();
    keyInfo.AddClause(new KeyInfoName(certificate.Subject));
    keyInfo.AddClause(new RSAKeyValue(rsa));
    keyInfo.AddClause(new KeyInfoX509Data(certificate));
    signatureValue.ParentNode!.InsertAfter(document.ImportNode(keyInfo.GetXml(), deep: true), signatureValue);

    using var output = signaturePart.GetStream(FileMode.Create, FileAccess.Write);
    using var writer = XmlWriter.Create(output, new XmlWriterSettings { Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) });
    document.Save(writer);
}

static bool Verify(string path)
{
    using var package = Package.Open(path, FileMode.Open, FileAccess.Read);
    var manager = new PackageDigitalSignatureManager(package);
    if (!manager.IsSigned)
    {
        Console.Error.WriteLine("error: the output isn't signed.");
        return false;
    }

    var result = manager.VerifySignatures(exitOnFailure: false);
    var signature = manager.Signatures[0];
    var signedParts = signature.SignedParts.Count;
    var allParts = package.GetParts().Count(p => !IsPackagingEntry(p.Uri.OriginalString.TrimStart('/')));
    Console.WriteLine($"Verify: {result}; {signedParts} of {allParts} parts signed; signed {signature.SigningTime:o}");
    return result == VerifyResult.Success && signedParts == allParts;
}
