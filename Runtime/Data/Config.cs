using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace DataForge {
    public abstract class Config {
        protected abstract byte[] Key { get; }
        public virtual IDataSecurity Security => new XORCipher(XORWithDeviceId(Key));
        public virtual IDataSerializer Serializer => new JsonSerializer();

        protected byte[] XORWithDeviceId(byte[] key) {
            var deviceIdBytes = Encoding.ASCII.GetBytes(SystemInfo.deviceUniqueIdentifier);
            var finalKey = new byte[key.Length];
            for (int i = 0; i < key.Length; i++) {
                finalKey[i] = (byte)(key[i] ^ deviceIdBytes[i % deviceIdBytes.Length]);
            }
            return finalKey;
        }

        public virtual string ObfuscateFileName(string filename) {
            const int MAX_LENGTH = 8;
            byte[] nameBytes = Encoding.ASCII.GetBytes(filename);
            byte[] md5Bytes = MD5.Create().ComputeHash(nameBytes);
            int length = Math.Min(md5Bytes.Length, MAX_LENGTH);
            return Convert.ToBase64String(md5Bytes, 0, length)
                .Replace('/', 'p').Replace('+', 'k').Replace("=", "q");
        }
    }
}