using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KaneBlake.Basis.Extensions.Cryptography
{
    public class CertificateExtensions
    {
        public static X509Certificate2 GetX509Certificate(string certificatePath, string password = "")
        {
            using (FileStream fs = File.OpenRead(certificatePath))
            {
                byte[] blob = new byte[fs.Length];
                fs.Read(blob, 0, blob.Length);
                X509Certificate2 certificate = new X509Certificate2(blob, password, X509KeyStorageFlags.MachineKeySet);
                return certificate;
            }

        }
    }
}
