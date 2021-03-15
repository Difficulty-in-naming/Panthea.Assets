using System;

namespace Panthea.Asset
{
    public class DownloadThread
    {
        public string Url;
        public string WritePath;
        public long Length;
        public long Version;
        public uint Crc;
        public DownloadThread(string url,string path,long length,long version,uint crc)
        {
            if(string.IsNullOrEmpty(url))
                throw new Exception("Url 不能为空！！！！");
            if (string.IsNullOrEmpty(path))
                throw new Exception("下载路径不可为空");
            if (length == 0)
                throw new Exception("文件长度不能为0");
            if (version == 0)
                throw new Exception("版本号不能为0");
            this.Url = url;
            this.WritePath = path;
            this.Length = length;
            this.Version = version;
            this.Crc = crc;
        }
    }
}