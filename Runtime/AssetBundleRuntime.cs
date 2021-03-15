using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Panthea.Asset
{
    public class AssetBundleRuntime
    {
        private ABFileTrack mFilelog;
        private AssetBundlePool mPool;
        private AssetBundleCounter mCounter;

        public AssetBundleRuntime(ABFileTrack filelog, AssetBundlePool pool, AssetBundleCounter counter)
        {
            mFilelog = filelog;
            mPool = pool;
            mCounter = counter;
        }
        
        public void ReleaseInstance<T>(T TObject) where T : Object
        {
            mCounter.RemoveCounter(TObject);
        }

        public void UnloadAllAssetBundle()
        {
            mPool.UnloadAllAssetBundle();
        }

        public void ReleaseAssetBundle(string path)
        {
            var directPath = mFilelog.GetFileInfo(path);
            mPool.Release(directPath);
        }

        public void ReleaseAssetBundleFromABKey(string path)
        {
            var directPath = mFilelog.GetABInfo(path);
            mPool.Release(directPath);
        }

        public Dictionary<string, AssetBundleRequest> GetLoadedAssetBundle()
        {
            return mPool.GetLoadedAssetBundle();
        }

        #region Async
        public async UniTask<T> LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            var tuple = await Internal_LoadAssetAsync<T>(path);
            return tuple.Item1;
        }

        public async UniTask<Dictionary<string,List<Object>>> LoadAllAssetAsync(string path)
        {
            var ab = await LoadAssetBundleByABPath(path);
            var allAssets = await ab.LoadAllAssetsAsync();
            return allAssets;
        }

        public async UniTask<AssetBundleRequest> LoadAssetBundleByFilePath(string path)
        {
            var directPath = mFilelog.GetFileInfo(path);
            if (directPath == null)
            {
                throw new AssetBundleNotFound(path);
            }

            var ab = await mPool.GetAsync(directPath);
            return ab;
        }

        public async UniTask<AssetBundleRequest> LoadAssetBundleByABPath(string abpath)
        {
            var directPath = mFilelog.GetABInfo(abpath);
            if (directPath == null)
            {
                throw new AssetBundleNotFound(abpath);
            }

            var ab = await mPool.GetAsync(directPath);
            return ab;
        }

        private async UniTask<Tuple<T, AssetBundleRequest>> Internal_LoadAssetAsync<T>(string path) where T : UnityEngine.Object
        {
            var ab = await LoadAssetBundleByFilePath(path);
            var asset = await ab.LoadAssetAsync<T>(path);
            mCounter.AddCounter(asset, ab);
            return new Tuple<T, AssetBundleRequest>(asset, ab);
        }

        public async UniTask<GameObject> Instantiate(string path, Vector3? position, Vector3? rotation, Transform parent = null)
        {
            var tuple = await Internal_LoadAssetAsync<GameObject>(path);
            var obj = Object.Instantiate(tuple.Item1, parent);
            var transform = obj.transform;
            if (position.HasValue)
                transform.localPosition = position.Value;
            if (rotation.HasValue)
                transform.localEulerAngles = rotation.Value;
            mCounter.AddCounter(obj, tuple.Item2);
            return obj;
        }
        #endregion
        
        #region Sync
        public T LoadAssetSync<T>(string path) where T : UnityEngine.Object
        {
            var tuple = Internal_LoadAssetSync<T>(path);
            return tuple.Item1;
        }

        public Dictionary<string,List<Object>> LoadAllAssetSync(string path)
        {
            var ab = LoadAssetBundleByABPathSync(path);
            var allAssets = ab.LoadAllAssetsSync();
            return allAssets;
        }
        
        public AssetBundleRequest LoadAssetBundleByFilePathSync(string path)
        {
            var directPath = mFilelog.GetFileInfo(path);
            if (directPath == null)
            {
                throw new AssetBundleNotFound(path);
            }

            var ab = mPool.GetSync(directPath);
            return ab;
        }

        public AssetBundleRequest LoadAssetBundleByABPathSync(string abpath)
        {
            var directPath = mFilelog.GetABInfo(abpath);
            if (directPath == null)
            {
                throw new AssetBundleNotFound(abpath);
            }

            var ab = mPool.GetSync(directPath);
            return ab;
        }
        
        private Tuple<T, AssetBundleRequest> Internal_LoadAssetSync<T>(string path) where T : UnityEngine.Object
        {
            var ab = LoadAssetBundleByFilePathSync(path);
            var asset = ab.LoadAssetSync<T>(path);
            mCounter.AddCounter(asset, ab);
            return new Tuple<T, AssetBundleRequest>(asset, ab);
        }
        #endregion
    }
}