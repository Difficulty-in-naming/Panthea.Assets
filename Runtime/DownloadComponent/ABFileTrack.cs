using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Panthea.Asset;
using Newtonsoft.Json;

namespace Panthea.Asset
{
    public class ABFileTrack
    {
        public class RedirectAsset
        {
            public AssetFileLog Info;
            public bool Include;
            public RedirectAsset(AssetFileLog info, bool include)
            {
                this.Info = info;
                this.Include = include;
            }
        }
    
        private Dictionary<string, RedirectAsset> PublicAssets { get; }
        private Dictionary<string, RedirectAsset> PublicBundle { get; }
        private Dictionary<string, AssetFileLog> DownloadedFiles { get; set; }
        private Dictionary<string, AssetFileLog> RemoteFiles { get; set; }

        private IDownloadPlatform DownloadPlatform { get; set; }
        public ABFileTrack()
        {
            this.PublicAssets = new Dictionary<string, RedirectAsset>(StringComparer.OrdinalIgnoreCase);
            this.PublicBundle = new Dictionary<string, RedirectAsset>(StringComparer.OrdinalIgnoreCase);
            this.DownloadedFiles = new Dictionary<string, AssetFileLog>(StringComparer.OrdinalIgnoreCase);
            this.Init();
        }

        //todo 这个方法可能会被废弃.这个类不应该获取外部的内容
        [Obsolete]
        public void ConfigureDownloadPlatform(IDownloadPlatform platform)
        {
            this.DownloadPlatform = platform;
        }

        private void Init()
        {
            Dictionary<string, AssetFileLog> includeFile = null;
            try
            {
                string json;
                if (File.Exists(AssetsConfig.AssetBundlePersistentDataPath + "/" + AssetsManager.kFileInfo))
                {
                    json = File.ReadAllText(AssetsConfig.AssetBundlePersistentDataPath + "/" + AssetsManager.kFileInfo);
                    this.DownloadedFiles = JsonConvert.DeserializeObject<Dictionary<string, AssetFileLog>>(json);
                }

                json = BetterStreamingAssets.GetText(AssetsManager.kFileInfo);
                includeFile = JsonConvert.DeserializeObject<Dictionary<string, AssetFileLog>>(json);
            }
            catch (Exception e)
            {
                Log.Error("找不到FileInfo.json" + e);
                return;
            }

            if (this.DownloadedFiles != null)
            {
                foreach (var node in this.DownloadedFiles)
                {
                    this.SetPublicAssets(node.Value,false);
                    this.PublicBundle[node.Key] = new RedirectAsset(node.Value,false);
                }
            }

            if (includeFile != null)
            {
                foreach (var node in includeFile)
                {
                    if (this.PublicBundle.TryGetValue(node.Key, out var downloaded))
                    {
                        if (downloaded.Info.Version > node.Value.Version)
                            continue;
                        this.SetPublicAssets(node.Value,true);
                        this.PublicBundle[node.Key] = new RedirectAsset(node.Value,true);
                    }
                    else
                    {
                        this.SetPublicAssets(node.Value,true);
                        this.PublicBundle[node.Key] = new RedirectAsset(node.Value,true);
                    }
                }
            }
        }

        private void SetPublicAssets(AssetFileLog node,bool include)
        {
            foreach (var file in node.Files)
            {
                this.PublicAssets[file] = new RedirectAsset(node, include);
            }
        }

        public async UniTask<Dictionary<string, AssetFileLog>> GetRemoteFiles()
        {
            if (this.DownloadPlatform == null)
                return null;
            var json = await this.DownloadPlatform.GetText(AssetsManager.kFileInfo);
            this.RemoteFiles = JsonConvert.DeserializeObject<Dictionary<string, AssetFileLog>>(json);
            return this.RemoteFiles;
        }
    
        public async UniTask<List<AssetFileLog>> CheckUpdateList()
        {
            List<AssetFileLog> fileList = new List<AssetFileLog>();
            var remoteFiles = await this.GetRemoteFiles();
            if (remoteFiles != null)
            {
                foreach (var network in remoteFiles)
                {
                    RedirectAsset exist;
                    if (this.PublicBundle.TryGetValue(network.Key, out exist))
                    {
                        if (exist.Info.Crc != network.Value.Crc)
                        {
                            fileList.Add(network.Value);
                        }
                    }
                }
            }
            return fileList;
        }

        public RedirectAsset GetFileInfo(string path)
        {
            if (this.PublicAssets.TryGetValue(path, out var value))
            {
                return value;
            }

            return null;
        }
    
        public RedirectAsset GetABInfo(string path)
        {
            if (this.PublicBundle.TryGetValue(path, out var value))
            {
                return value;
            }

            return null;
        }
    
        public async UniTask Update(string path)
        {
            var remoteFiles = await this.GetRemoteFiles();
            if(remoteFiles != null && remoteFiles.TryGetValue(path,out var value))
            {
                this.DownloadedFiles[path] = value;
                this.SetPublicAssets(value, false);
                this.PublicBundle[path] = new RedirectAsset(value, false);
            }
        }

        public void SyncLocal()
        {
            var json = JsonConvert.SerializeObject(this.DownloadedFiles);
            File.WriteAllText(AssetsConfig.AssetBundlePersistentDataPath + "/" + AssetsManager.kFileInfo,json);
        }

        public List<string> GetFilterAssetBundle(string[] path)
        {
            List<string> list = new List<string>();
            foreach (var node in path)
            {
                foreach (var bundle in this.PublicBundle)
                {
                    if (bundle.Key.StartsWith(node, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(bundle.Key);
                    }
                }
            }
            return list;
        }
    }
}
