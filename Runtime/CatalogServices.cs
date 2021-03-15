using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Panthea.Asset;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Panthea.Asset
{
    public class CatalogServices
    {
        public IDownloadPlatform mDownloadServices;
        private const string mCatalogName = "catalog.json";

        public enum CatalogState
        {
            Null,//不存在
            Include,//在包体内
            PersistentData,//在下载目录内
        }
    
        private async UniTask<CatalogState> Download()
        {
            IDownloadPlatform service = this.mDownloadServices;
            int tryTimes = 0;
            var thread = await service.FetchHeader(mCatalogName);
            string existPath = AssetsConfig.AssetBundlePersistentDataPath + "/" + mCatalogName;
            if (File.Exists(existPath))
            {
                if (AssetsUtils.CheckIntegrity(existPath, thread.Crc))
                {
                    return CatalogState.PersistentData;
                }
            }
            else
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            var stream = BetterStreamingAssets.GetStream("assets/" + Addressables.StreamingAssetsSubFolder + "/" + PlatformMappingService.GetPlatform() + "/" + mCatalogName);
#else
                var stream = new FileStream(Addressables.RuntimePath + "/" + mCatalogName, FileMode.Open, FileAccess.Read);
#endif
                if (stream != null)
                {
                    if (AssetsUtils.CheckIntegrity(stream, thread.Crc))
                    {
                        return CatalogState.Include;
                    }
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

                    Log.Print(mCatalogName + "已经更新完毕");
                    return CatalogState.PersistentData;
                }
                catch (RemoteFileNotFound e)
                {
                    Debug.LogError(e);
                    return CatalogState.Null;
                }
                catch (Exception e)
                {
                    Log.Error($"更新{mCatalogName}文件失败,正在重新尝试第{tryTimes}次,{e}");
                }
            }
            return CatalogState.Null;
        }

        public async UniTask Redirect()
        {
            CatalogState result;
            //对比修改时间
#if UNITY_ANDROID && !UNITY_EDITOR
            var includeDt = BetterStreamingAssets.GetCreateTime("assets/" + Addressables.StreamingAssetsSubFolder + "/" + PlatformMappingService.GetPlatform() + "/" + mCatalogName);
#else
            var includeDt = File.GetCreationTimeUtc(Addressables.RuntimePath + "/" + mCatalogName);
#endif
            if (File.Exists(AssetsConfig.AssetBundlePersistentDataPath + "/" + mCatalogName))
            {
                var persistentDt = File.GetLastWriteTimeUtc(AssetsConfig.AssetBundlePersistentDataPath + "/" + mCatalogName);
                if (persistentDt > includeDt)
                {
                    result = CatalogState.PersistentData;
                }
                else
                {
                    result = CatalogState.Include;
                }
            }
            else
            {
                result = CatalogState.Include;
            }

            await this.Redirect(result);
        }
    
        public async UniTask Update()
        {
            CatalogState result;
            if(this.mDownloadServices != null)
            {
                result = await this.Download();
                await this.Redirect(result);
            }
            else
            {
                await this.Redirect();
            }
        }

        private async UniTask Redirect(CatalogState result)
        {
            if (result == CatalogState.Include)
            {
                //默认就是使用包体内的Catalog.如果是包体内部最新我们就不做Load操作
                //await Addressables.LoadContentCatalogAsync(result).Task;
            }
            else if (result == CatalogState.PersistentData)
            {
                await Addressables.LoadContentCatalogAsync(AssetsConfig.AssetBundlePersistentDataPath + "/" + mCatalogName).Task;
            }
        }
    }
}
