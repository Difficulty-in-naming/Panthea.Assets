namespace Panthea.Asset
{
    public class AssetFileLog
    {
        public uint Crc;
        public long Version;
        public string Path;
        public string[] Dependencies;
        public string[] Files;
        public int Size;
        public AssetFileLog(uint crc, long version, string path,string[] files,string[] dependencies,int size)
        {
            Crc = crc;
            Version = version;
            Path = path;
            Files = files;
            Dependencies = dependencies;
            Size = size;
        }
    }
}
