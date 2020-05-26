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

        /// <summary>
        /// Get pfx Certificate from filePath
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static X509Certificate2 GetX509Certificate(string certificatePath, string password = "")
        {
            using FileStream fs = File.OpenRead(certificatePath);
            byte[] blob = new byte[fs.Length];
            fs.Read(blob, 0, blob.Length);
            X509Certificate2 certificate = new X509Certificate2(blob, password, X509KeyStorageFlags.MachineKeySet);
            return certificate;
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
        /// decrypt string from Base64String with RSAPrivateKey
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="cipherB64str"></param>
        /// <returns></returns>
        public static string DecryptFromBase64String(this X509Certificate2 certificate, string cipherB64str) => certificate.DecryptFromUTF8bytes(Convert.FromBase64String(cipherB64str));

        /// <summary>
        /// decrypt string from ReadOnlySpan[byte] with RSAPrivateKey
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="cipherBytes"></param>
        /// <returns></returns>
        public static string DecryptFromUTF8bytes(this X509Certificate2 certificate, ReadOnlySpan<byte> cipherBytes)
        {
            var _r = certificate.Decrypt(cipherBytes);
            if (_r.IsEmpty)
            {
                return string.Empty;
            }
            else
            {
                return Encoding.UTF8.GetString(_r);
            }
        }

        /// <summary>
        /// decrypt data with RSAPrivateKey
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="cipherBytes"></param>
        /// <returns></returns>
        public static ReadOnlySpan<byte> Decrypt(this X509Certificate2 certificate, ReadOnlySpan<byte> cipherBytes)
        {
            // PKCS#1 padding 11字节
            // 明文长度 <= 密钥长度 - padding
            var privateKey = certificate.GetRSAPrivateKey();
            // 密文长度 = 密钥的长度 = Modulus.Length * 8
            var ciphertextLength = privateKey.ExportParameters(false).Modulus.Length;
            var ciphertextStd = new byte[ciphertextLength];
            // 值:258 Hex:0x01 0x02  小字节序
            // [0] 低位内存地址  0x01
            // [1] 高位内存地址  0x02
            var ciphertextAct = cipherBytes.ToArray();

            if (ciphertextAct.Length > ciphertextStd.Length)
            {
                return Span<byte>.Empty;
            }
            else if (ciphertextAct.Length < ciphertextStd.Length)
            {
                // 短密文处理, 高位字节补 0x00
                if (BitConverter.IsLittleEndian)
                {
                    // 偏移量从低位内存地址开始计算
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
                return span;
            }
            return Span<byte>.Empty;
        }
    }
}
