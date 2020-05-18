using KaneBlake.Basis.Extensions.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace KaneBlake.STS.Identity.Common
{
    public class AppInfo
    {
        private AppInfo() { }

        public static AppInfo Instance { get; private set; }

        static AppInfo()
        {
            Instance = new AppInfo
            {
                Certificate = CertificateExtensions.GetX509Certificate(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Certs", "IdentityServerCredential.pfx"))
            };

        }

        public X509Certificate2 Certificate { get; private set; }
    }
}
