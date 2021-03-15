using System;
using System.IO;
#if UNITY_ANDROID && !UNITY_EDITOR
using System.IO.Compression;
using System.Text;
using UnityEngine;
#endif

namespace Panthea.Asset
{
    public class BetterStreamingAssets
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    private static ZipArchive mArchive;

    public static ZipArchive Archive
    {
        get
        {
            if (mArchive == null)
            {
                mArchive = ZipFile.Open(Application.dataPath, ZipArchiveMode.Read);
            }

            return mArchive;
        }
    }
#endif
    
        /*public static async UniTask MoveStreamingAssetsOut()
    {
        string movedFlag = AssetsConfig.AssetBundlePersistentDataPath + "/" +  AssetsManager.kVersionInfo;
        long streamingAssetsFlag = 0;
        //开始移动文件
        var zip = Archive;
        {
            if (File.Exists(movedFlag))
            {
                var persistentFlag = BitConverter.ToInt64(File.ReadAllBytes(movedFlag), 0);
                byte[] bytes = new byte[8];
                try
                {
                    using (var stream = zip.GetEntry("assets/" + AssetsManager.kVersionInfo).Open())
                    {
                        stream.Read(bytes, 0, bytes.Length);
                        streamingAssetsFlag = BitConverter.ToInt64(bytes, 0);
                        if (persistentFlag >= streamingAssetsFlag)
                            return;
                    }
                }
                catch(Exception e)
                {
                    Debug.LogError(e);
                    return;
                }
            }
            foreach( var zipEntry in zip.Entries )
            {
                var fullName = zipEntry.FullName;
                if (fullName.StartsWith("assets/aa/"))
                {
                    var path = AssetsConfig.AssetBundlePersistentDataPath + "/" + fullName.Replace("assets/aa/","");
                    var dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    zipEntry.ExtractToFile(path,true);
                }
                else if (fullName.StartsWith("assets/"))
                {
                    var path = AssetsConfig.AssetBundlePersistentDataPath + "/" + fullName.Replace("assets/", "");
                    var dir = Path.GetDirectoryName(path);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                    zipEntry.ExtractToFile(path,true);
                }
            }
        }
        File.WriteAllBytes(movedFlag, BitConverter.GetBytes(streamingAssetsFlag));
    }*/

        public static byte[] GetBytes(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var stream = Internal_GetStream(path);
        if(stream == null)
            return null;
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);
        stream.Dispose();
        return bytes;
#endif
            return File.ReadAllBytes(AssetsConfig.StreamingAssets + "/" + path);
        }

        public static string GetText(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var stream = Internal_GetStream(path);
        if(stream == null)
            return null;
        var reader = new StreamReader(stream, Encoding.UTF8);
        var s = reader.ReadToEnd();
        stream.Dispose();
        reader.Dispose();
        return s;
#endif
            return File.ReadAllText(AssetsConfig.StreamingAssets + "/" + path);
        }
    
        private static Stream Internal_GetStream(string path)
        {
            Stream stream = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var entry = Archive.GetEntry(path);
        if (entry != null)
        {
            stream = entry.Open();
            return stream;
        }
        return null;
#endif
            stream = new FileStream(AssetsConfig.StreamingAssets + "/" + path, FileMode.Open, FileAccess.Read);
            return stream;
        }
    
        public static Stream GetStream(string path)
        {
            Stream stream = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var entry = Archive.GetEntry(path);
        if (entry != null)
        {
            using(stream = entry.Open())
            {
                byte[] buffer = new byte[stream.Length];
                stream.Read(buffer, 0, (int) stream.Length);
                var wrap = new MemoryStream(buffer);
                return wrap;
            }
        }
        return null;
#endif
            stream = new FileStream(AssetsConfig.StreamingAssets + "/" + path, FileMode.Open, FileAccess.Read);
            return stream;
        }

        public static bool HasExists(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var entry = Archive.GetEntry(path);
        if (entry != null)
        {
            return true;
        }
        return false;
#endif
            return File.Exists(AssetsConfig.StreamingAssets + "/" + path);
        }

        public static DateTime GetCreateTime(string path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!path.StartsWith("assets/"))
            path = "assets/" + path;
        var entry = Archive.GetEntry(path);
        if (entry != null)
        {
            return entry.LastWriteTime.UtcDateTime;
        }
        return DateTime.MinValue;
#endif

            return File.GetCreationTimeUtc(path);
        }
    
    }
}
