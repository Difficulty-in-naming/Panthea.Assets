using Cysharp.Threading.Tasks;
using Panthea.Asset;
using UnityEngine;

namespace Panthea.Asset
{
    public class S3Download : IDownloadPlatform
    {
        private IDownloadHandler mDownloadHandler;
        private const string WEB_CONTENT_HOST = "https://dum5uv7xb9d66.cloudfront.net/";
        private readonly string mRelativePath;
        private readonly string mSavePath;
        private static readonly string Version = Application.version;
        private static readonly string Platform = AssetsConfig.Platform;

        public S3Download(string relativePath,string savePath,IDownloadHandler downloadHandler)
        {
            this.mRelativePath = relativePath;
            this.mSavePath = savePath;
            this.mDownloadHandler = downloadHandler;
        }
    
        private string FormatUrl(string path)
        {
            string url = WEB_CONTENT_HOST + this.mRelativePath + Version + "/" + Platform + "/" +  path;
            return url;
        }
    
        public async UniTask<DownloadResult> Download(DownloadThread thread)
        {
            return await this.mDownloadHandler.Download(thread);
        }

        public async UniTask<DownloadThread> FetchHeader(string path)
        {
            long length = 0;
            uint crc32 = 0;
            long version = 0;
            string url = this.FormatUrl(path);
            var headers = await this.mDownloadHandler.GetHeaders(url);
            if (headers != null)
            {
                long.TryParse(headers["Content-Length"], out length);
                uint.TryParse(headers["x-amz-meta-crc32"], out crc32);
                long.TryParse(headers["x-amz-meta-version"], out version);
            }
            else
            {
                throw new RemoteFileNotFound(url + "上找不到这个文件");
            }
            return new DownloadThread(url, this.mSavePath + path, length, version,crc32);
        }

        public async UniTask<string> GetText(string path)
        {
            string url = this.FormatUrl(path);
            return await this.mDownloadHandler.GetText(url);
        }
    
        public async UniTask<byte[]> GetBytes(string path)
        {
            string url = this.FormatUrl(path);
            return await this.mDownloadHandler.GetBytes(url);
        }
    }
}
