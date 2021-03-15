#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Panthea.Asset
{
    public class EDITOR_AssetsManager : IAssetsLocator
    {
        public async UniTask<T> Load<T>(string filePath) where T : Object
        {
            var name = System.IO.Path.GetFileName(filePath);
            string packPath = "Res/";
            filePath = filePath.ToLower();
            var allAssetGuids = AssetDatabase.FindAssets("t:" + typeof(T).Name + " " + name);
            for (int i = 0; i < allAssetGuids.Length; i++)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(allAssetGuids[i]);
                //查找最后一个AssetBundle并删除
                int lastIndexOf = assetPath.LastIndexOf(packPath);
                if (lastIndexOf == -1)
                    continue;
                string tempAssetPath = assetPath.Substring(lastIndexOf + packPath.Length);
                //移除后缀名
                tempAssetPath = tempAssetPath.Substring(0,tempAssetPath.LastIndexOf('.')).ToLower();
                if (tempAssetPath == filePath)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                    return obj;
                }
            }

            return null;
        }

        public async UniTask<Dictionary<string,List<Object>>> LoadAll(string path)
        {
            string packPath = "Res/";
            DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/" + packPath + path.Replace(AssetsConfig.Suffix,""));
            Dictionary<string,List<Object>> objects = new Dictionary<string,List<Object>>();
            foreach (var node in dir.GetFiles())
            {
                var p = PathUtils.FullPathToUnityPath(node.FullName);
                var obj = AssetDatabase.LoadAssetAtPath<Object>(p);
                if (obj != null)
                {
                    var key = Path.GetFileNameWithoutExtension(p);
                    if (!objects.TryGetValue(key, out var list))
                    {
                        list = new List<Object>();
                        objects.Add(key, list);
                    }
                    list.Add(obj);
                }
            }

            return objects;
        }

        public T LoadSync<T>(string filePath) where T : Object
        {
            return Load<T>(filePath).AsTask().Result;
        }

        public Dictionary<string, List<Object>> LoadAllSync(string path)
        {
            return LoadAll(path).AsTask().Result;
        }

        public async UniTask<AssetBundleRequest> LoadAssetBundle(string filePath)
        {
            return null;
        }

        public async UniTask<AssetBundleRequest> LoadAssetBundleFromABKey(string abPath)
        {
            return null;
        }

        public void ReleaseAssetBundle(string filePath)
        {
            return;
        }

        public void ReleaseAssetBundleFromABKey(string abPath)
        {
            return;
        }

        public void ReleaseInstance<TObject>(TObject obj) where TObject : Object
        {
            return;
        }

        public async UniTask<UnityObject> Instantiate(string filePath, Vector3? position = null, Vector3? rotation = null, Transform parent = null)
        {
            var go = await this.Load<GameObject>(filePath);
            var obj = Object.Instantiate(go, parent);
            var transform = obj.transform;
            if (position.HasValue)
                transform.localPosition = position.Value;
            if (rotation.HasValue)
                transform.localEulerAngles = rotation.Value;
            return new UnityObject(obj);
        }

        public void UnloadAllAssetBundle()
        {
            return;
        }

        public List<string> GetFilterAssetBundle(string[] path)
        {
            return new List<string>();
        }

        public string[] GetDepenciences(string path)
        {
            return new string[0];
        }

        public Dictionary<string, AssetBundleRequest> GetLoadedAssetBundle()
        {
            return new Dictionary<string, AssetBundleRequest>();
        }
    }
}
#endif