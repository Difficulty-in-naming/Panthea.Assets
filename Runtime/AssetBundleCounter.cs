using System.Collections.Generic;
using UnityEngine;

namespace Panthea.Asset
{
    /// <summary>
    /// AssetBundle 引用计数器
    /// 注册了AssetBundle计数器的AssetsRuntime再每次调用Destroy的时候会减少一次计数
    /// 计数为0时自动卸载AB
    /// </summary>
    public class AssetBundleCounter
    {
        private Dictionary<AssetBundleRequest,Dictionary<Object,int>> mCounter = new Dictionary<AssetBundleRequest, Dictionary<Object,int>>();
        private Dictionary<Object,AssetBundleRequest> mLookup = new Dictionary<Object,AssetBundleRequest>();
        private AssetBundlePool mPool;
        public AssetBundleCounter(AssetBundlePool pool)
        {
            this.mPool = pool;
        }
    
        public void AddCounter(Object obj,AssetBundleRequest ab)
        {
            Dictionary<Object, int> dict;
            if (!this.mCounter.TryGetValue(ab, out dict))
            {
                dict = new Dictionary<Object, int>();
                this.mCounter.Add(ab, dict);
                this.mLookup.Add(obj, ab);
            }

            if (!dict.ContainsKey(obj))
                dict.Add(obj, 1);
            else
                dict[obj]++;
        }

        public void RemoveCounter(Object obj)
        {
            this.mLookup.TryGetValue(obj, out AssetBundleRequest ab);
            if (ab != null)
            {
                if (this.mCounter.TryGetValue(ab, out Dictionary<Object, int> counter))
                {
                    if (counter.ContainsKey(obj))
                    {
                        int result = --counter[obj];
                        if (result == 0)
                        {
                            this.Internal_RemoveCounter(counter, obj,ab);
                        }
                    }
                    else
                    {
                        this.Internal_RemoveCounter(counter,obj,ab);
                    }
                }
            }
        }

        private void Internal_RemoveCounter(Dictionary<Object, int> dict,Object obj,AssetBundleRequest ab)
        {
            dict.Remove(obj);
            if (dict.Count == 0)
            {
                this.mLookup.Remove(obj);
                this.mCounter.Remove(ab);
                this.mPool.Release(ab);
                Log.Print("Release " + ab.Name);
            }
        }
    }
}
