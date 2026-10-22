namespace DataForge {
    public interface IDataSerializer {        
        T DeserializeTo<T>(byte[] bytes, T objectToOverwrite);
        byte[] Serialize<T>(T obj);
    }
}