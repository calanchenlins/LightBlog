using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace K.Extensions
{
    /// <summary>
    /// Extensions to RSA Certificate
    /// </summary>
    public static class CertificateExtensions
    {
        private static readonly string RsaPublicPrefix = "-----BEGIN RSA PUBLIC KEY-----";
        private static readonly string RsaPublicEndfix = "-----END RSA PUBLIC KEY-----";
        private static readonly string PublicPrefix = "-----BEGIN PUBLIC KEY-----";
        private static readonly string PublicEndfix = "-----END PUBLIC KEY-----";

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
        /// Attempts to export the current key in the PKCS#1 PublicKey format into byte[] without Prefix and Endfix
        /// https://lapo.it/asn1js/
        /// https://etherhack.co.uk/asymmetric/docs/rsa_key_breakdown.html
        /// https://blog.csdn.net/pcjustin/article/details/79084232
        /// https://tools.ietf.org/html/rfc3447#appendix-A.1.1
        /// RSAPublicKey::= SEQUENCE {
        ///     modulus INTEGER,  --n
        ///     publicExponent INTEGER   --e
        /// }
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        private static byte[] GetPKCS1PublicKeyASN1Body(this X509Certificate2 certificate)
        {
            var rsaParms = certificate.GetRSAPublicKey().ExportParameters(false);

            var Modulus_Node = GetASN1_Data(new byte[1] { 0x02 }, rsaParms.Modulus);

            var Exponent_Node = GetASN1_Data(new byte[1] { 0x02 }, rsaParms.Exponent);

            var root_Node = GetASN1_Data(new byte[1] { 0x30 }, Modulus_Node.Concat(Exponent_Node).ToArray());

            return root_Node;
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#1 RSAPublicKey format into string(Microsoft)
        /// </summary>
        /// <param name="certificate">current key</param>
        /// <returns></returns>
        public static string ExportRSAPKCS1PublicKeyMS(this X509Certificate2 certificate) 
        {
            var temp = new StringBuilder();
            temp.Append(RsaPublicPrefix.AsSpan());
            Span<byte> cache = new byte[4096];
            if (certificate.GetRSAPublicKey().TryExportRSAPublicKey(cache, out int bolbRawLength))
            {
                Base64.EncodeToUtf8InPlace(cache, bolbRawLength, out int bolbBase64Length);
                var span = cache.Slice(0, bolbBase64Length);
                temp.Append(Encoding.UTF8.GetString(span).AsSpan());
                temp.Append(RsaPublicEndfix.AsSpan());
                return temp.ToString();
            }
            return string.Empty;
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#1 PublicKey format into string
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string ExportRSAPKCS1PublicKey(this X509Certificate2 certificate)
        {
            var body = certificate.GetPKCS1PublicKeyASN1Body();

            var root = Convert.ToBase64String(body);

            var temp = new StringBuilder();
            temp.Append(RsaPublicPrefix.AsSpan());
            temp.Append(root.AsSpan());
            temp.Append(RsaPublicEndfix.AsSpan());
            return temp.ToString();
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#8 PublicKey format into string
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public static string ExportRSAPKCS8PublicKey(this X509Certificate2 certificate)
        {
            // OBJECT IDENTIFIER 1.2.840.113549.1.1.1 rsaEncryption (PKCS #1)
            var ObjectIdentifier_Node = GetASN1_Data(new byte[1] { 0x06 }, new byte[] { 0x2A,0x86 ,0x48 ,0x86 ,0xF7, 0x0D ,0x01 ,0x01 ,0x01  });

            var Null_Node = new byte[] { 0x05, 0x00 };

            var Node_0 = GetASN1_Data(new byte[1] { 0x30 }, CombineByteArray(ObjectIdentifier_Node, Null_Node));

            var pkcs1 = certificate.GetPKCS1PublicKeyASN1Body();

            var Node_1 = GetASN1_Data(new byte[1] { 0x03 }, pkcs1);

            var body = GetASN1_Data(new byte[1] { 0x30 }, CombineByteArray(Node_0, Node_1));

            var root = Convert.ToBase64String(body);

            var temp = new StringBuilder();
            temp.Append(PublicPrefix.AsSpan());
            temp.Append(root.AsSpan());
            temp.Append(PublicEndfix.AsSpan());
            return temp.ToString();
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
            // 短密文处理, 高位字节补 0x00? 还是数组高位补 0x00?
            else if (ciphertextAct.Length < ciphertextStd.Length)
            {
                // 高位字节补 0x00
                if (BitConverter.IsLittleEndian)
                {
                    Buffer.BlockCopy(ciphertextAct, 0, ciphertextStd, 0, ciphertextAct.Length);
                }
                else
                {
                    Buffer.BlockCopy(ciphertextAct, 0, ciphertextStd, ciphertextStd.Length - ciphertextAct.Length, ciphertextAct.Length);
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

        #region 
        /// <summary>
        /// 数组高位补0x00
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] BytesPadLeft(byte[] data, int length)
        {
            if (data.Length >= length)
            {
                return data;
            }
            var objData = new byte[length];
            Buffer.BlockCopy(data, 0, objData, objData.Length - data.Length, data.Length);
            return objData;
        }

        /// <summary>
        /// 高位字节补0x00
        /// </summary>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] BytesPadHigher(byte[] data, int length)
        {
            if (data.Length >= length)
            {
                return data;
            }
            var objData = new byte[length];
            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(data, 0, objData, 0, data.Length);
            }
            else
            {
                Buffer.BlockCopy(data, 0, objData, objData.Length - data.Length, data.Length);
            }
            return objData;
        }

        /// <summary>
        /// 去除int整数数值 byte[] 中的高位0x00字节
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Span<byte> GetBytesTrim(int value)
        {
            var dataLength = BitConverter.GetBytes(value).AsSpan();
            var dataValidLength = 0;
            for (int i = 0; i < dataLength.Length; i++)
            {
                var point = i;
                if (BitConverter.IsLittleEndian)
                {
                    point = dataLength.Length - 1 - i;
                }
                if (!dataLength[point].Equals(0x00))
                {
                    dataValidLength = dataLength.Length - i;
                    break;
                }
            }
            if (BitConverter.IsLittleEndian)
            {
                return dataLength.Slice(0, dataValidLength);
            }
            return dataLength.Slice(dataLength.Length - dataValidLength, dataValidLength);
        }

        /// <summary>
        /// 合并字节数组
        /// </summary>
        /// <param name="arrays"></param>
        /// <returns></returns>
        public static byte[] CombineByteArray(params byte[][] arrays)
        {
            byte[] ret = new byte[arrays.Sum(x => x.Length)];
            int offset = 0;
            foreach (byte[] data in arrays)
            {
                Buffer.BlockCopy(data, 0, ret, offset, data.Length);
                offset += data.Length;
            }
            return ret;
        }

        /// <summary>
        /// 构建ASN1数据节点
        /// [0x02,0x03,0x01,0x00,0x01]
        /// [标识符(1 byte),数据长度(1 byte),{数据(n byte)}]
        /// [标识符(1 byte),数据长度的长度(1 byte),{数据长度(n byte)},{数据(n byte)}]
        /// https://www.cnblogs.com/NathanYang/p/9951282.html
        /// 不支持构建 bit流长度 %8 != 0 的 BIT STRING,比如 BIT STRING (4 bit) 1111
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] GetASN1_Data(byte[] tag, byte[] data)
        {
            // Integer: bit流的前4位16进制值大于8,那么需要在bit流前端补0x00
            if (tag[0]== 0x02 && (data[0] >> 4) > 0x08)
            {
                data = BytesPadLeft(data, data.Length + 1);
            }

            if (tag[0] == 0x03)
            {
                // 对于 BIT STRING 需要在bit流前段补充一个字节，用于表示末尾字节未填充位数
                data = BytesPadLeft(data, data.Length + 1);
            }

            var dataLengthBytes = GetBytesTrim(data.Length);
            //数据长度大于等于128,需要增加数据长度的长度
            if (dataLengthBytes.Length > 1 || dataLengthBytes[0] >= 0x80)
            {
                if (BitConverter.IsLittleEndian)
                {
                    dataLengthBytes.Reverse();
                }
                var dataLengthBytesLengthBytes = GetBytesTrim(dataLengthBytes.Length);
                if (dataLengthBytesLengthBytes.Length != 1)
                {
                    return Array.Empty<byte>();
                }
                if (dataLengthBytesLengthBytes[0] < 0x80)
                {
                    dataLengthBytesLengthBytes[0] = (byte)(dataLengthBytesLengthBytes[0] | 0x80);
                }

                return CombineByteArray(tag, dataLengthBytesLengthBytes.ToArray(), dataLengthBytes.ToArray(), data);
            }
            else
            {
                return CombineByteArray(tag, dataLengthBytes.ToArray(), data);
            }
        }
        #endregion
    }
}
