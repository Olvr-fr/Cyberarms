using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace Cyberarms.IntrusionDetection {

    /// <summary>
    /// Crypto library for encrypting/decrypting configuration files
    /// </summary>
    internal class CryptoLib {
        const string YYHSK_DNRTF = "62Hd_cn$a0)8_fnS";
        internal static string Encrypt(string toEncrypt, bool useHashing) {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            string key = YYHSK_DNRTF;
            if (useHashing) {
                using MD5 hashmd5 = MD5.Create();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            } else
                keyArray = UTF8Encoding.UTF8.GetBytes(key);

            using TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        internal static string Decrypt(string cipherString, bool useHashing) {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            string key = YYHSK_DNRTF;

            if (useHashing) {
                using MD5 hashmd5 = MD5.Create();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            } else {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            using TripleDES tdes = TripleDES.Create();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
    }
}
