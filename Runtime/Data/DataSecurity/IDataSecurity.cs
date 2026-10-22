namespace DataForge {
    public interface IDataSecurity {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] data);
    }
}