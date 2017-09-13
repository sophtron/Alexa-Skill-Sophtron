using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Net;

namespace SophtronAlexaSkill
{
    public class SigningCertificate
    {
        /// <summary>
        /// Right now restricted to Amazon pem for Alexa verification
        /// A lot of assumptions of cert delimiter
        /// </summary>
        /// <param name="pemFile"></param>
        /// <param name="additionalCertificates"></param>
        /// <returns></returns>
        public static X509Certificate2 ReadCerts(string pemFileUri, out List<X509Certificate2> additionalCertificates)
        {
            string certStart = "-----BEGIN CERTIFICATE-----";
            string certEnd = "-----END CERTIFICATE-----";
            WebClient Client = new WebClient();
            byte[] pemFile = Client.DownloadData(pemFileUri);
            string pemStr = Encoding.ASCII.GetString(pemFile);
            pemStr = pemStr.Replace("\n", "");
            additionalCertificates = new List<X509Certificate2>();

            int firstEndIndex = pemStr.IndexOf(certEnd);
            string primaryCertStr = pemStr.Substring(0, firstEndIndex + certEnd.Length);
            primaryCertStr = primaryCertStr.Replace(certStart, string.Empty);
            primaryCertStr = primaryCertStr.Replace(certEnd, string.Empty);
            pemStr = pemStr.Substring(firstEndIndex + certEnd.Length);
            byte[] primaryBytes = Encoding.ASCII.GetBytes(primaryCertStr);
            X509Certificate2 primaryCert = new X509Certificate2(primaryBytes);

            while (pemStr.IndexOf(certEnd) > 0)
            {
                int endIndex = pemStr.IndexOf(certEnd);
                string additionalCertStr = pemStr.Substring(0, endIndex + certEnd.Length);
                additionalCertStr = additionalCertStr.Replace(certStart, string.Empty);
                additionalCertStr = additionalCertStr.Replace(certEnd, string.Empty);
                pemStr = pemStr.Substring(endIndex + certEnd.Length);
                byte[] additionalBytes = Encoding.ASCII.GetBytes(additionalCertStr);
                X509Certificate2 additionalCert = new X509Certificate2(additionalBytes);
                additionalCertificates.Add(additionalCert);
            }

            return primaryCert;
        }

        public static bool VerifyCertChain(X509Certificate2 primaryCertificate, List<X509Certificate2> additionalCertificates)
        {
            var chain = new X509Chain();
            foreach (var cert in additionalCertificates)
            {
                chain.ChainPolicy.ExtraStore.Add(cert);
            }

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage;

            // Do the preliminary validation.
            if (!chain.Build(primaryCertificate))
                return false;

            // Make sure all the thumbprints of the CAs match up.
            // The first one should be 'primaryCert', leading up to the root CA.
            for (var i = 1; i < chain.ChainPolicy.ExtraStore.Count + 1; i++)
            {
                if (chain.ChainElements[i].Certificate.Thumbprint != chain.ChainPolicy.ExtraStore[i - 1].Thumbprint)
                    return false;
            }
            return true;
        }
    }
}