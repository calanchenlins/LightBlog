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
            // [] 数组表示了在内存中连续的一块地址
            // 索引 [0] [1] [2] [3] [4] [5]
            // 地址  a  a+1 a+2 a+3 a+4 a+5
            /*******************************************************/
            // 值:258 Hex:0x00 0x00 0x01 0x02
            // 小字节序:低位字节优先于高位字节(低位字节存放在数组的高位)
            // byte[0] 数组高位 == 低位内存地址  0x02  低位字节
            // byte[1]                         0x01
            // byte[2]                         0x00
            // byte[3] 数组低位 == 高位内存地址  0x00  高位字节
            /*******************************************************/
            // 值:"abcd" Hex:0x61 0x62 0x63 0x64
            // 字符串在内存中以byte[]的形式存在,数组高位存储了字符串的高位字符
            // byte[0] 数组高位 == 低位内存地址  0x61  字符串高位
            // byte[1]                         0x62
            // byte[2]                         0x63
            // byte[3] 数组低位 == 高位内存地址  0x64  字符串低位

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
