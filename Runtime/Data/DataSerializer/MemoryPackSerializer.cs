#if ENABLE_MEMORY_PACK
namespace DataForge {
    public class MemoryPackSerializer : IDataSerializer {
        public T DeserializeTo<T>(byte[] bytes, T objectToOverwrite) {
            if (bytes == null || bytes.Length == 0)
                return default;

            if (objectToOverwrite == null) {
                return MemoryPack.MemoryPackSerializer.Deserialize<T>(bytes);
            } else {
                MemoryPack.MemoryPackSerializer.Deserialize<T>(bytes, ref objectToOverwrite);
                return objectToOverwrite;
            }
        }

        public byte[] Serialize<T>(T obj) {
            if (obj == null)
                return null;

            return MemoryPack.MemoryPackSerializer.Serialize<T>(obj);
        }
    }
}
#endif