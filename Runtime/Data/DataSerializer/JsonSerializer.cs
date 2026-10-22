using UnityEngine;
using System.Text;

namespace DataForge {
    public class JsonSerializer : IDataSerializer {
        public T DeserializeTo<T>(byte[] bytes, T objectToOverwrite) {
            if (bytes == null || bytes.Length == 0)
                return default;

            string json = Encoding.UTF8.GetString(bytes);
            if (string.IsNullOrEmpty(json))
                return default;

            if (objectToOverwrite != null) {
                JsonUtility.FromJsonOverwrite(json, objectToOverwrite);
                return objectToOverwrite;
            }

            return JsonUtility.FromJson<T>(json);
        }

        public byte[] Serialize<T>(T obj) {
            if (obj == null)
                return null;

            string json = JsonUtility.ToJson(obj);
            return Encoding.UTF8.GetBytes(json);
        }
    }
}