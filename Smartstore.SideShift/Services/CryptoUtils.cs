using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Smartstore.SideShift.Services
{
    public static class CryptoUtils
    {
        private const int SaltSize = 16;
        private const int KeyMaterialSize = 48;
        private const int Iterations = 200_000;
        private static readonly HashAlgorithmName Algo = HashAlgorithmName.SHA256;

        public static string Encrypt(string plainText, string password)
        {
            // sel aléatoire
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            // dérivation PBKDF2 (remplit keyMaterial)
            byte[] keyMaterial = new byte[KeyMaterialSize];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, keyMaterial, Iterations, Algo);

            var key = new byte[32];
            var iv = new byte[16];
            Array.Copy(keyMaterial, 0, key, 0, 32);
            Array.Copy(keyMaterial, 32, iv, 0, 16);

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;
                using (var ms = new MemoryStream())
                {
                    // préfixe : sel (pour pouvoir dériver la clé au déchiffrement)
                    ms.Write(salt, 0, salt.Length);
                    using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs, Encoding.UTF8))
                    {
                        sw.Write(plainText);
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static string Decrypt(string cipherTextBase64, string password)
        {
            byte[] full = Convert.FromBase64String(cipherTextBase64);

            // extraire sel
            byte[] salt = new byte[SaltSize];
            Array.Copy(full, 0, salt, 0, SaltSize);

            // dériver même keyMaterial
            byte[] keyMaterial = new byte[KeyMaterialSize];
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, keyMaterial, Iterations, Algo);

            var key = new byte[32];
            var iv = new byte[16];
            Array.Copy(keyMaterial, 0, key, 0, 32);
            Array.Copy(keyMaterial, 32, iv, 0, 16);

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Key = key;
                aes.IV = iv;

                using (var ms = new MemoryStream(full, SaltSize, full.Length - SaltSize))
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (var sr = new StreamReader(cs, Encoding.UTF8))
                {
                    return sr.ReadToEnd();
                }
            }
        }
    }
}



