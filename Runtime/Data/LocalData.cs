using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DataForge {
    public class LocalData<T> {
        private readonly IDataSerializer dataWrapper;
        private readonly IDataSecurity dataSecurity;
        private readonly string path;
        private readonly string tempPath;
        private byte[] cache;
        private readonly SemaphoreSlim fileLock = new(1, 1);

        public LocalData(Config config) {
            dataWrapper = config.Serializer;
            dataSecurity = config.Security;
            var fileName = config.ObfuscateFileName(typeof(T).Name);
            path = Path.Combine(Application.persistentDataPath, fileName);
            tempPath = path + ".tmp";
        }

        internal async Task SaveAsync(T data) {
            var plainBytes = dataWrapper.Serialize(data);
            var encryptedBytes = dataSecurity.Encrypt(plainBytes);
            cache = encryptedBytes;

            await fileLock.WaitAsync();
            try {
                await File.WriteAllBytesAsync(tempPath, encryptedBytes);
                File.Copy(tempPath, path, overwrite: true);
            } finally {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                fileLock.Release();
            }
        }

        internal T Load(T objectToOverwrite) {
            if (cache == null && File.Exists(path)) {
                cache = File.ReadAllBytes(path);
            }
            if (cache != null) {
                var bytes = dataSecurity.Decrypt(cache);
                return dataWrapper.DeserializeTo(bytes, objectToOverwrite);
            }
            return objectToOverwrite;
        }

        internal void Delete() => File.Delete(path);
    }
}
