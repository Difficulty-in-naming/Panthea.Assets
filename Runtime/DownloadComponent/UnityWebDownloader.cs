using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Panthea.Asset
{
    public class UnityWebDownloader : IDownloadHandler
    {
        public async UniTask<DownloadResult> Download(DownloadThread thread)
        {
            return await this.Internal_Download(thread);
        }

        public async UniTask<Dictionary<string, string>> GetHeaders(string url)
        {
            //获取文件长度
            using (UnityWebRequest headRequest = UnityWebRequest.Head(url))
            {
                await headRequest.SendWebRequest();
                if (string.IsNullOrEmpty(headRequest.error))
                    return headRequest.GetResponseHeaders();
                else
                    return null;
            }
        }

        private async UniTask<DownloadResult> Internal_Download(DownloadThread thread)
        {
            var request = new UnityWebRequest {downloadHandler = new DownloadHandlerFile(thread.WritePath + ".temp"), url = thread.Url};
            //先删除本地文件
            File.Delete(thread.WritePath);
            var versionFile = thread.WritePath + ".bytes";
            var tempFile = thread.WritePath + ".temp";
            if (File.Exists(versionFile))
            {
                long localVersion = 0;
                long.TryParse(File.ReadAllText(thread.WritePath + ".bytes"), out localVersion);
                if (localVersion != thread.Version)
                {
                    //删除所有文件
                    File.Delete(versionFile);
                    File.Delete(tempFile);
                }
            }
            if (File.Exists(tempFile))
            {
                var length = new FileInfo(tempFile).Length;
                request.SetRequestHeader("Range", $"bytes={length}-{thread.Length}");
            }
            await request.SendWebRequest();

            File.Delete(versionFile);
            File.Move(tempFile, thread.WritePath);
            if (!string.IsNullOrEmpty(request.error))
            {
                throw new Exception(request.error);
            }
            //因为DownloadHandlerFile是直接写入本地的我们不需要操作Stream自己写入.
            //我们这里把本地写入的文件找出来
            //thread.WritePath;
            return new DownloadResult
            {
                RemoteCrc32 = thread.Crc,
                WritePath = thread.WritePath,
            };
        }

        public async UniTask<string> GetText(string url)
        {
            var request = new UnityWebRequest();
            request.downloadHandler = new DownloadHandlerBuffer();
            request.url = url;
            await request.SendWebRequest();
            if (!string.IsNullOrEmpty(request.error))
            {
                throw new Exception(request.error);
            }

            return request.downloadHandler.text;
        }

        public async UniTask<byte[]> GetBytes(string url)
        {
            var request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();
            await request.SendWebRequest();
            if (!string.IsNullOrEmpty(request.error))
            {
                throw new Exception(request.error);
            }

            return request.downloadHandler.data;
        }
    }
}
