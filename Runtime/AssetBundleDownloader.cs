using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Panthea.Asset;
using UnityEngine;

namespace Panthea.Asset
{
    public class AssetBundleDownloader
    {
        private ABFileTrack mFilelogContext;
        private IDownloadPlatform mDownloadServices;

        public AssetBundleDownloader(ABFileTrack filelog,IDownloadPlatform platform)
        {
            this.mFilelogContext = filelog;
            this.mDownloadServices = platform;
        }
    
        /// <summary>
        /// 根据本地列表检测需要下载得文件内容
        /// </summary>
        /// <returns></returns>
        public async UniTask<List<AssetFileLog>> FetchDownloadList()
        {
            var updateList = await this.mFilelogContext.CheckUpdateList();
            return updateList;
        }
    
        /// <summary>
        /// 检查本地是否存在这个文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private string HasExist(string path)
        {
            var value = this.mFilelogContext.GetFileInfo(path);
            //这里我们返回的应该是最新版本的File
            if (value != null)
            {
                if (!value.Include)
                {
                    return AssetsConfig.AssetBundlePersistentDataPath + "/" + value.Info.Path;
                }

                return AssetsConfig.AssetBundleStreamingAssets + "/" + value.Info.Path;
            }
            else
            {
                //从Filelog中找不到,可能文件之前下载了但是玩家没有下载完.导致Filelog还未被写入.这里我们去本地查找一下
                if (File.Exists(AssetsConfig.AssetBundlePersistentDataPath + "/" + path))
                {
                    return AssetsConfig.AssetBundlePersistentDataPath + "/" + path;
                }
            }

            return null;
        }
    
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async UniTask Download(List<AssetFileLog> pathList,IProgress<float> progress = null)
        {
            //我们需要优先下载Catalog,这个文件是Addressable系统必须的文件.后续的加载和下载依赖等都需要使用这个文件
            List<UniTask> downloadTasks = new List<UniTask>();
            foreach (var path in pathList)
            {
                downloadTasks.Add(Download(path.Path));
            }
            int length = downloadTasks.Count - 1;
            if (length > 0)
            {
                int finished = 0;
                while (true)
                {
                    for (int i = downloadTasks.Count - 1; i >= 0; i--)
                    {
                        if (downloadTasks[i].GetAwaiter().IsCompleted)
                        {
                            finished++;
                            downloadTasks.RemoveAt(i);
                        }
                    }

                    if (finished >= length)
                    {
                        progress?.Report(100);
                        break;
                    }
                    progress?.Report(finished / length * 100);
                    await UniTask.DelayFrame(1);
                }
                this.mFilelogContext.SyncLocal();
            }
        }
    
        private async UniTask Download(string path)
        {
            IDownloadPlatform service = this.mDownloadServices;
            int tryTimes = 0;
            var thread = await service.FetchHeader(path);
            var existPath = this.HasExist(path);
            if (!string.IsNullOrEmpty(existPath))
            {
                if (AssetsUtils.CheckIntegrity(existPath, thread.Crc))
                {
                    await this.mFilelogContext.Update(path);
                    return;
                }
            }

            while (tryTimes++ < 5)
            {
                try
                {
                    await service.Download(thread);
                    if (!AssetsUtils.CheckIntegrity(thread))
                    {
                        Log.Error($"文件{thread.WritePath}下载不完整.重新下载");
                        continue;
                    }

                    Log.Print(path + "    下载完毕");
                    await this.mFilelogContext.Update(path);
                    break;
                }
                catch (RemoteFileNotFound e)
                {
                    Debug.LogError(e);
                }
                catch (Exception e)
                {
                    Log.Error($"下载文件{path}失败,正在重新尝试第{tryTimes}次,{e}");
                }
            }
        }
    }
}
