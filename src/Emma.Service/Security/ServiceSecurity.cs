using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace Emma.Service.Security;

public record ServiceKonfiguration(string ApiKey, string ZertifikatPasswort, int TimeoutMinuten = 30);

/// <summary>
/// Erzeugt beim ersten Start automatisch einen API-Key und ein selbstsigniertes
/// TLS-Zertifikat für den Service und legt beides lokal ab. Bei Neustarts wird
/// beides wiederverwendet, damit sich Client-Konfiguration und Zertifikats-Pinning
/// nicht bei jedem Neustart ändern.
/// </summary>
public static class ServiceSecurity
{
    private const string ConfigDatei = "emma-service-config.json";
    private const string PfxDatei = "emma-service-zertifikat.pfx";
    private const string CerDatei = "emma-service-zertifikat.cer";
    private const string ClientVorlageDatei = "fuer-clients.json";

    public static (ServiceKonfiguration Konfig, X509Certificate2 Zertifikat) LadeOderErstelle(ILogger logger)
    {
        var basis = KonfigurationsOrdner();
        var configPfad = Path.Combine(basis, ConfigDatei);
        var pfxPfad = Path.Combine(basis, PfxDatei);
        var cerPfad = Path.Combine(basis, CerDatei);

        MigriereAlteDateienFallsVorhanden(basis, [ConfigDatei, PfxDatei, CerDatei, ClientVorlageDatei]);

        ServiceKonfiguration konfig;
        var neuErstellt = !File.Exists(configPfad);

        if (!neuErstellt)
        {
            konfig = JsonSerializer.Deserialize<ServiceKonfiguration>(File.ReadAllText(configPfad))!;

            // Ältere Config-Dateien (vor Einführung des Timeout-Wächters) auf den Standardwert heben.
            if (konfig.TimeoutMinuten <= 0)
            {
                konfig = konfig with { TimeoutMinuten = 30 };
                File.WriteAllText(configPfad, JsonSerializer.Serialize(konfig, new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        else
        {
            konfig = new ServiceKonfiguration(
                ApiKey: Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                ZertifikatPasswort: Convert.ToHexString(RandomNumberGenerator.GetBytes(16)));
            File.WriteAllText(configPfad, JsonSerializer.Serialize(konfig, new JsonSerializerOptions { WriteIndented = true }));
        }

        X509Certificate2 zertifikat;
        if (File.Exists(pfxPfad))
        {
            zertifikat = X509CertificateLoader.LoadPkcs12FromFile(pfxPfad, konfig.ZertifikatPasswort, X509KeyStorageFlags.Exportable);
        }
        else
        {
            zertifikat = ErstelleSelbstsigniertesZertifikat();
            File.WriteAllBytes(pfxPfad, zertifikat.Export(X509ContentType.Pfx, konfig.ZertifikatPasswort));
            File.WriteAllBytes(cerPfad, zertifikat.Export(X509ContentType.Cert));
        }

        if (neuErstellt || !File.Exists(Path.Combine(basis, ClientVorlageDatei)))
        {
            var hostName = Dns.GetHostName();
            var vorlage = new
            {
                ServiceBaseUrl = $"https://{hostName}:5271/",
                konfig.ApiKey,
                ZertifikatThumbprint = zertifikat.Thumbprint
            };
            File.WriteAllText(
                Path.Combine(basis, ClientVorlageDatei),
                JsonSerializer.Serialize(vorlage, new JsonSerializerOptions { WriteIndented = true }));
        }

        logger.LogWarning(
            "EMMA-Service-Sicherheit: API-Key und Zertifikat bereit. Für jeden Client die Werte aus " +
            "'{Pfad}' in dessen emma-config.json übernehmen. Zertifikats-Thumbprint: {Thumbprint}",
            Path.Combine(basis, ClientVorlageDatei), zertifikat.Thumbprint);

        return (konfig, zertifikat);
    }

    /// <summary>
    /// %ProgramData%\EmmaAufgabenpool\Service\ - bewusst NICHT neben der .exe in "Program Files",
    /// da Windows Installer beim Upgrade eines MSI-Pakets unter Umständen auch nicht-versionierte
    /// Dateien im Installationsordner mit aufräumt. %ProgramData% wird von MSI nie angefasst.
    /// </summary>
    private static string KonfigurationsOrdner()
    {
        var basis = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var ordner = Path.Combine(basis, "EmmaAufgabenpool", "Service");

        try
        {
            Directory.CreateDirectory(ordner);
            return ordner;
        }
        catch
        {
            // Kein Zugriff auf ProgramData - Fallback neben die .exe, damit der Dienst trotzdem startet.
            return AppContext.BaseDirectory;
        }
    }

    /// <summary>
    /// Einmalige Übernahme von Config/Zertifikat/Vorlage aus einer älteren Version, die noch
    /// neben der .exe lagen - damit nach einem Update kein neues Zertifikat samt neuem API-Key
    /// entsteht (das würde alle Clients aussperren, bis sie neu konfiguriert werden).
    /// </summary>
    private static void MigriereAlteDateienFallsVorhanden(string neuerOrdner, string[] dateiNamen)
    {
        foreach (var name in dateiNamen)
        {
            var neuerPfad = Path.Combine(neuerOrdner, name);
            if (File.Exists(neuerPfad))
                continue;

            try
            {
                var alterPfad = Path.Combine(AppContext.BaseDirectory, name);
                if (File.Exists(alterPfad))
                    File.Copy(alterPfad, neuerPfad);
            }
            catch
            {
                // Keine alte Datei vorhanden oder kein Zugriff - kein Problem, wird ggf. neu erzeugt.
            }
        }
    }

    private static X509Certificate2 ErstelleSelbstsigniertesZertifikat()
    {
        using var rsa = RSA.Create(2048);
        var hostName = Dns.GetHostName();

        var request = new CertificateRequest(
            $"CN={hostName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(hostName);
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(sanBuilder.Build());

        using var selbstsigniert = request.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));

        // Export+Reimport ist der dokumentierte Workaround, damit der private Schlüssel
        // unter Windows/Kestrel zuverlässig verwendbar ist (statt des Ephemeral-Handles).
        var exportPasswort = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        var pfxBytes = selbstsigniert.Export(X509ContentType.Pfx, exportPasswort);
        return X509CertificateLoader.LoadPkcs12(pfxBytes, exportPasswort, X509KeyStorageFlags.Exportable);
    }
}
