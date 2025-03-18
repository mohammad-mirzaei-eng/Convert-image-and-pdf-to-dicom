using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Convert_to_dcom.Helper
{
    public static class EncryptionHelper
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes(@"PNcgXnaj|NgCXTme4hJwN.x{?/.YhNxF"); // باید 32 کاراکتر باشد
        private static readonly byte[] IV = Encoding.UTF8.GetBytes(@"xO^T^_aUuN!bX^F4"); // باید 16 کاراکتر باشد

        public static string Encrypt(string plainText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                if (Key == null || (Key.Length != 16 && Key.Length != 24 && Key.Length != 32))
                {
                    throw new ArgumentException("Key size must be 128, 192, or 256 bits.");
                }

                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}
