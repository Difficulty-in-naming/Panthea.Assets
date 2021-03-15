/*
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Force.Crc32;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Zenject;

public class GenerateMD5 : AResPipeline
{
    [Inject] private DiContainer mContainer = null;
    private string mStreamingAssets = AssetsConfig.StreamingAssets + "/" + AssetsConfig.Platform;
    private readonly Dictionary<string, AssetFileLog> mSave = new Dictionary<string, AssetFileLog>();
    public override Task Do()
    {
        var buildFiles = Directory.GetFiles(mStreamingAssets, "*.*", SearchOption.AllDirectories).ToList();
        foreach (var loc in buildFiles)
        {
            var fileName = Path.GetFileName(loc);
            var dir = Path.GetDirectoryName(loc);
            var ext = Path.GetExtension(loc);
            var crc = Crc32CAlgorithm.Compute(loc);
            if (fileName == AssetsManager.kFileInfo)
                continue;
            if (ext == ".meta")
                continue;
            string loadPath = StringUtils.FormatFilePath(dir).Replace(AssetsConfig.AssetBundleStreamingAssets, "");
            if (loadPath.StartsWith("/"))
            {
                //多级目录
                loadPath = loadPath.Remove(0, 1);
                loadPath += "/" + fileName;
            }
            else
            {
                loadPath += fileName;
            }
            mSave.Add(loadPath, new AssetFileLog(crc, TimeUtils.GetUtcTimeStamp(), loadPath));
        }

        AddCatalog();
        if (!Directory.Exists(mStreamingAssets))
            Directory.CreateDirectory(mStreamingAssets);
        var json = JsonConvert.SerializeObject(mSave);
        File.WriteAllText(mStreamingAssets + "/" + AssetsManager.kFileInfo,json);
        mContainer.BindInstance(mSave);
        return Task.CompletedTask;
    }

    private void AddCatalog()
    {
        string file = "catalog.json";
        string path = Addressables.RuntimePath + "/" + file;
        var crc = Crc32CAlgorithm.Compute(path);
        mSave.Add(file, new AssetFileLog(crc, TimeUtils.GetUtcTimeStamp(), file));
    }
}
*/
