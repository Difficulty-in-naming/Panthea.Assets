using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Panthea.Asset
{
    public interface IAssetsLocator
    {
        UniTask<T> Load<T>(string filePath) where T : Object;
        UniTask<Dictionary<string,List<Object>>> LoadAll(string path);
        T LoadSync<T>(string filePath) where T : Object;
        Dictionary<string,List<Object>> LoadAllSync(string path);
        UniTask<AssetBundleRequest> LoadAssetBundle(string filePath);
        UniTask<AssetBundleRequest> LoadAssetBundleFromABKey(string abPath);
        void ReleaseAssetBundle(string filePath);
        void ReleaseAssetBundleFromABKey(string abPath);
        void ReleaseInstance<TObject>(TObject obj) where TObject : Object;
        UniTask<UnityObject> Instantiate(string filePath, Vector3? position = null, Vector3? rotation = null, Transform parent = null);
        void UnloadAllAssetBundle();
        List<string> GetFilterAssetBundle(string[] path);
        string[] GetDepenciences(string abPath);
        Dictionary<string, AssetBundleRequest> GetLoadedAssetBundle();
    }
}
