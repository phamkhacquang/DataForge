using System;

namespace DataForge {
    public class XORCipher : IDataSecurity {
        private readonly byte[] _key;

        public XORCipher(byte[] key) {
            if (key == null || key.Length == 0)
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            _key = key;
        }

        private byte[] XorOperation(byte[] input) {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++) {
                output[i] = (byte)(input[i] ^ _key[i % _key.Length]);
            }
            return output;
        }

        public byte[] Encrypt(byte[] plainBytes) => XorOperation(plainBytes);

        public byte[] Decrypt(byte[] cipherBytes) => XorOperation(cipherBytes);
    }
}
