using System;
using System.IO;
using System.Security.Cryptography;

namespace DataForge {
    public class AES : IDataSecurity {
        private readonly byte[] _key;

        public AES(byte[] key) {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("Key length must be 16, 24, or 32 bytes.", nameof(key));

            _key = key;
        }

        // Encrypts and prepends IV to ciphertext
        public byte[] Encrypt(byte[] plainBytes) {
            if (plainBytes == null) throw new ArgumentNullException(nameof(plainBytes));

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using MemoryStream ms = new();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write)) {
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }

        // Extracts IV from first 16 bytes and decrypts the rest
        public byte[] Decrypt(byte[] cipherBytes) {
            if (cipherBytes == null) throw new ArgumentNullException(nameof(cipherBytes));
            if (cipherBytes.Length < 16)
                throw new ArgumentException("Ciphertext too short - missing IV.", nameof(cipherBytes));

            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[16];
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            int cipherTextLength = cipherBytes.Length - iv.Length;

            using MemoryStream ms = new();
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Write);
            cs.Write(cipherBytes, iv.Length, cipherTextLength);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
    }
}
