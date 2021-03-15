using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Panthea.Asset;
using UnityEngine;

namespace Panthea.Asset
{
    /// <summary>
    /// AssetBundle数据缓存池.
    /// </summary>
    public class AssetBundlePool
    {
        private Dictionary<string,AssetBundleRequest> Pool = new Dictionary<string, AssetBundleRequest>();
        private Dictionary<AssetBundleRequest,string> Lookup = new Dictionary<AssetBundleRequest,string>();
        /// <summary>
        /// 等待加载的列表,因为可能在同一帧当中多次请求同一个AB.第二次的时候我们判断是否在等待列表中.如果在的话我们就一直等到等待列表被移除以后返回结果
        /// </summary>
        private Dictionary<string,UniTaskCompletionSource> WaitList = new Dictionary<string,UniTaskCompletionSource>();
        private ABFileTrack mFileLog;
        public AssetBundlePool(ABFileTrack filelog)
        {
            this.mFileLog = filelog;
        }
    
        private AssetBundleRequest internal_Get(ABFileTrack.RedirectAsset path)
        {
            if (this.Pool.TryGetValue(path.Info.Path, out AssetBundleRequest value))
            {
                return value;
            }

            return null;
        }

        private List<UniTask> LoadDependencies(ABFileTrack.RedirectAsset path)
        {
            if (path.Info.Dependencies == null || path.Info.Dependencies.Length == 0)
                return null;
            List<UniTask> tasks = new List<UniTask>();
            foreach (var node in path.Info.Dependencies)
            {
                var directPath = this.mFileLog.GetABInfo(node);
                if (directPath == null)
                {
                    throw new AssetBundleNotFound(node);
                }
                else
                {
                    tasks.Add(this.GetAsync(directPath));
                }
            }
            return tasks;
        }
    
        private async UniTask<AssetBundleRequest> internal_Load(ABFileTrack.RedirectAsset path)
        {
            List<UniTask> tasks = this.LoadDependencies(path);
            string address;
            AssetBundleCreateRequest ab;
            if (!path.Include)
            {
                address = AssetsConfig.AssetBundlePersistentDataPath + "/" + path.Info.Path;
                ab = AssetBundle.LoadFromFileAsync(address);
            }
            else
            {
                ab = AssetBundle.LoadFromFileAsync(AssetsConfig.StreamingAssets + "/" + path.Info.Path);
            }
            await ab;
            var assetBundle = ab.assetBundle;
            var request = new AssetBundleRequest(assetBundle,path.Info);
            this.Pool.Add(path.Info.Path, request);
            this.Lookup.Add(request,path.Info.Path);
            if (this.WaitList.TryGetValue(path.Info.Path, out UniTaskCompletionSource ucs))
            {
                ucs.TrySetResult();
                this.WaitList.Remove(path.Info.Path);
            }
            if(tasks != null)
                await UniTask.WhenAll(tasks); 
            return request;
        }
    
        public async UniTask<AssetBundleRequest> GetAsync(ABFileTrack.RedirectAsset abPath)
        {
            try
            {
                if (this.WaitList.TryGetValue(abPath.Info.Path, out UniTaskCompletionSource waitTask))
                {
                    await waitTask.Task;
                }

                var ab = this.internal_Get(abPath);
                if (ab != null)
                    return ab;
                else
                {
                    UniTaskCompletionSource ucs = new UniTaskCompletionSource();
                    this.WaitList.Add(abPath.Info.Path, ucs);
                    var task = this.internal_Load(abPath);
                    return await task;
                }
            }
            catch(Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }

        public AssetBundleRequest GetSync(ABFileTrack.RedirectAsset abPath)
        {
            var ab = this.internal_Get(abPath);
            return ab;
        }

        public void Release(ABFileTrack.RedirectAsset path)
        {
            var ab = this.internal_Get(path);
            if (ab != null)
            {
                ab.AssetBundle.Unload(true);
                this.Pool.Remove(path.Info.Path);
                this.Lookup.Remove(ab);
            }
        }
    
        public void Release(AssetBundleRequest ab)
        {
            if (ab != null)
            {
                if (this.Lookup.TryGetValue(ab, out string value))
                {
                    this.Lookup.Remove(ab);
                    this.Pool.Remove(value);
                    ab.Unload(true);
                }   
            }
        }

        public void UnloadAllAssetBundle()
        {
            List<AssetBundleRequest> temp = new List<AssetBundleRequest>(this.Lookup.Keys);
            foreach (var node in temp)
            {
                this.Release(node);
            }
        }

        public Dictionary<string, AssetBundleRequest> GetLoadedAssetBundle()
        {
            return new Dictionary<string, AssetBundleRequest>(this.Pool);
        }
    }
}
