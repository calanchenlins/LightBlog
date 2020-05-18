using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace KaneBlake.Basis.Extensions.Cryptography
{
    public static class CertificateExtensions
    {
        private static readonly string RsaPrefix = "-----BEGIN RSA PUBLIC KEY-----";
        private static readonly string RsaEndfix = "-----END RSA PUBLIC KEY-----";
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

        /// <summary>
        /// Attempts to export the current key in the PKCS#1 RSAPublicKey format into string
        /// </summary>
        /// <param name="certificate">current key</param>
        /// <returns></returns>
        public static string ExportRSAPublicKey(this X509Certificate2 certificate) 
        {
            var temp = new StringBuilder();
            temp.Append(RsaPrefix.AsSpan());
            Span<byte> cache = new byte[4096];
            if (certificate.GetRSAPublicKey().TryExportRSAPublicKey(cache, out int bolbRawLength))
            {
                Base64.EncodeToUtf8InPlace(cache, bolbRawLength, out int bolbBase64Length);
                var span = cache.Slice(0, bolbBase64Length);
                temp.Append(Encoding.UTF8.GetString(span).AsSpan());
                temp.Append(RsaEndfix.AsSpan());
                return temp.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ciphertext"></param>
        /// <returns></returns>
        public static string Decrypt(this X509Certificate2 certificate,string ciphertext)
        {
            // PKCS#1 padding 11字节
            // 明文长度 <= 密钥长度 - padding
            var privateKey = certificate.GetRSAPrivateKey();
            // 密文长度 = 密钥的长度 = Modulus.Length * 8
            var ciphertextLength = privateKey.ExportParameters(false).Modulus.Length;
            var ciphertextStd = new byte[ciphertextLength];
            // [0] 低位内存地址
            // [1] 高位内存地址
            var ciphertextAct = Convert.FromBase64String(ciphertext);

            if (ciphertextAct.Length > ciphertextStd.Length)
            {
                return string.Empty;
            }
            else if (ciphertextAct.Length < ciphertextStd.Length)
            {
                // 短密文处理, 高位字节补 0x00
                if (BitConverter.IsLittleEndian)
                {
                    Buffer.BlockCopy(ciphertextAct, 0, ciphertextStd, ciphertextStd.Length - ciphertextAct.Length, ciphertextAct.Length);
                }
                else
                {
                    Buffer.BlockCopy(ciphertextAct, 0, ciphertextStd, 0, ciphertextAct.Length);
                }
            }
            else
            {
                ciphertextStd = ciphertextAct;
            }

            Span<byte> plaintext = new byte[ciphertextLength];
            if (privateKey.TryDecrypt(ciphertextStd, plaintext, RSAEncryptionPadding.Pkcs1, out int messageLength))
            {
                var span = plaintext.Slice(0, messageLength);
                return Encoding.UTF8.GetString(span);
            }
            return string.Empty;
        }
    }
}
