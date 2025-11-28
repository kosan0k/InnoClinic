using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Services.Shared.Utils;

public static class DevCertificateTrust
{
    public static HttpClientHandler CreateTrustingHttpClientHandler(string caPath)
    {
        // Find the mkcert root CA file path.
        // You can find this by running 'mkcert -CAROOT' in your terminal.
        // This path must be accessible to your.NET app.                

        if (!File.Exists(caPath))
        {
            throw new FileNotFoundException("mkcert root CA not found.", caPath);
        }

        // Load the root CA
        var rootCaCert = X509CertificateLoader.LoadCertificateFromFile(caPath);
        var customTrustStore = new X509Certificate2Collection { rootCaCert };

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
            {
                // If there are no errors, the cert is valid (e.g., a public CA)
                if (errors == SslPolicyErrors.None)
                    return true;

                //Build a new chain with our custom trust store
                var chainPolicy = new X509ChainPolicy
                {
                    TrustMode = X509ChainTrustMode.CustomRootTrust,
                    RevocationMode = X509RevocationMode.NoCheck // No revocation for local dev
                };
                chainPolicy.CustomTrustStore.Add(rootCaCert);

                var customChain = new X509Chain { ChainPolicy = chainPolicy };

                // Validate the server's certificate (cert) against our custom root
                // This is the "real" validation, just with a custom root.
                return customChain.Build(cert!);
            }
        };

        return handler;
    }
}
