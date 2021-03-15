using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Panthea.Asset
{
    public class AssetBundleRequest
    {
        public AssetBundle AssetBundle { get; }
        public string Name;

        /// <summary>
        /// TODO 是否自动销毁AB,当AB不在被引用的时候AB会被自动Unload(true),或者在调用IAssetsLocator.UnloadAllAssetBundle的时候不会被自动销毁.
        /// </summary>
        public bool Persistence { get; private set; } = false;

        private Dictionary<string, List<Object>> mLoadedObjects = new Dictionary<string, List<Object>>(StringComparer.OrdinalIgnoreCase);
        private AssetFileLog mFileLog;

        public AssetBundleRequest(AssetBundle assetBundle, AssetFileLog fileLog)
        {
            this.AssetBundle = assetBundle;
            this.Name = assetBundle.name;
            this.mFileLog = fileLog;
        }

        public void Unload(bool deepUnload)
        {
            this.AssetBundle.Unload(deepUnload);
            if (deepUnload)
            {
                this.mLoadedObjects.Clear();
                this.mFileLog = null;
            }
        }
        
        private void AddCache(string name, Object obj)
        {
            if (!this.mLoadedObjects.TryGetValue(name, out List<Object> list))
            {
                list = new List<Object>();
                this.mLoadedObjects.Add(name, list);
            }

            list.Add(obj);
        }

        public void MarkPersistence(bool persistence)
        {
            this.Persistence = persistence;
        }

        public override bool Equals(object obj)
        {
            var target = obj as AssetBundleRequest;
            if (target == null)
                return false;
            return ReferenceEquals(this.AssetBundle, target.AssetBundle);
        }

        public override int GetHashCode()
        {
            return this.AssetBundle.GetHashCode();
        }
        
        #region Sync
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Object LoadAssetSync(string name, Type type)
        {
            List<Object> obj;
            name = Path.GetFileNameWithoutExtension(name);
            if (this.mLoadedObjects.TryGetValue(name, out obj))
            {
                if (obj != null)
                {
                    foreach (var node in obj)
                    {
                        var o = node.GetType() == type;
                        if (o)
                            return node;
                    }
                }
            }

            var result = this.AssetBundle.LoadAsset(name, type);
            if (result == null)
            {
                Log.Error(name + "没有在" + this.Name + "中被找到.这个AssetBundle和Filelog不匹配.请重新生成AB,以避免后续使用发生异常");
                return null;
            }

            this.AddCache(name, result);
            return result;
        }

        /// <summary>
        /// 同步加载资源,为了获取最大的速度.我们这里没有将T转换为Type传入LoadAsset(Type)
        /// </summary>
        /// <param name="name"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadAssetSync<T>(string name) where T : Object
        {
            List<Object> obj;
            name = Path.GetFileNameWithoutExtension(name);
            if (this.mLoadedObjects.TryGetValue(name, out obj))
            {
                foreach (var node in obj)
                {
                    var o = node as T;
                    if (o != null)
                        return o;
                }
            }

            var result = this.AssetBundle.LoadAsset<T>(name);
            if (result == null)
            {
                Log.Error(name + "没有在" + this.Name + "中被找到.这个AssetBundle和Filelog不匹配.请重新生成AB,以避免后续使用发生异常");
                return null;
            }

            this.AddCache(name, result);
            return (T) result;
        }
        
        public Dictionary<string,List<Object>> LoadAllAssetsSync()
        {
            try
            {
                var files = mFileLog.Files;
                Dictionary<string,List<Object>> objects = new Dictionary<string,List<Object>>(files.Length);
                bool needReload = false;
                //由于Unity官方并没有AssetBundle.LoadAllAsset<T>(string name)的接口.我们这里只能是先判断资源是否加载过.如果没有加载完全.我们在进行完全的加载
                for (var index = 0; index < files.Length; index++)
                {
                    var node = files[index];
                    if (mLoadedObjects.TryGetValue(node, out List<Object> value))
                        objects[node] = value;
                    else
                    {
                        needReload = true;
                        break;
                    }
                }

                if (needReload)
                {
                    var assets = AssetBundle.LoadAllAssets();
                    for (var index = 0; index < assets.Length; index++)
                    {
                        var asset = assets[index];
                        var name = asset.name;
                        AddCache(name, asset);
                        objects[name] = this.mLoadedObjects[name];
                    }
                }
                return objects;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        #endregion

        #region Async
        public async UniTask<Object> LoadAssetAsync(string name,Type type)
        {
            List<Object> obj;
            name = Path.GetFileNameWithoutExtension(name);
            if (this.mLoadedObjects.TryGetValue(name, out obj))
            {
                if (obj != null)
                {
                    foreach (var node in obj)
                    {
                        var o = node.GetType() == type;
                        if (o)
                            return node;
                    }
                }
            }
            var request = AssetBundle.LoadAssetAsync(name,type);
            var result = await request.ToUniTask().Timeout(new TimeSpan(0, 0, 5));
            if (result == null)
            {
                Log.Error(name + "没有在" + this.Name + "中被找到.这个AssetBundle和Filelog不匹配.请重新生成AB,以避免后续使用发生异常");
                return null;
            }

            this.AddCache(name, result);
            return result;
        }
        
        public async UniTask<T> LoadAssetAsync<T>(string name) where T : Object
        {
            List<Object> obj;
            name = Path.GetFileNameWithoutExtension(name);
            if (this.mLoadedObjects.TryGetValue(name, out obj))
            {
                foreach (var node in obj)
                {
                    var o = node as T;
                    if (o != null)
                        return o;
                }
            }
            var request = AssetBundle.LoadAssetAsync<T>(name);
            var result = await request.ToUniTask().Timeout(new TimeSpan(0, 0, 5));
            if (result == null)
            {
                Log.Error(name + "没有在" + this.Name + "中被找到.这个AssetBundle和Filelog不匹配.请重新生成AB,以避免后续使用发生异常");
                return null;
            }

            this.AddCache(name, result);
            return (T) result;
        }
        
        public async UniTask<Dictionary<string,List<Object>>> LoadAllAssetsAsync()
        {
            try
            {
                var files = mFileLog.Files;
                Dictionary<string,List<Object>> objects = new Dictionary<string,List<Object>>(files.Length);
                bool needReload = false;
                //由于Unity官方并没有AssetBundle.LoadAllAsset<T>(string name)的接口.我们这里只能是先判断资源是否加载过.如果没有加载完全.我们在进行完全的加载
                for (var index = 0; index < files.Length; index++)
                {
                    var node = files[index];
                    if (mLoadedObjects.TryGetValue(node, out List<Object> value))
                        objects[node] = value;
                    else
                    {
                        needReload = true;
                        break;
                    }
                }

                if (needReload)
                {
                    var request = AssetBundle.LoadAllAssetsAsync();
                    await request;
                    var assets = request.allAssets;
                    for (var index = 0; index < assets.Length; index++)
                    {
                        var asset = assets[index];
                        var name = asset.name;
                        AddCache(name, asset);
                        objects[name] = this.mLoadedObjects[name];
                    }
                }
                return objects;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        }
        #endregion
    }
}