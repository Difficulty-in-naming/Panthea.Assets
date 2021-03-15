using System.Collections.Generic;
using System.IO;
using Force.Crc32;
using Newtonsoft.Json;
using Panthea.Asset;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace Panthea.Editor.Asset
{
    public class GenerateFileLog : IBuildTask
    {
#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_Content;

        [InjectContext]
        IBundleBuildResults m_Results;

        [InjectContext(ContextUsage.In, true)]
        IProgressTracker m_Tracker;

        [InjectContext(ContextUsage.In, true)]
        IBuildCache m_Cache;
#pragma warning restore 649
    
        private string mStreamingAssets = AssetsConfig.StreamingAssets;
        private readonly Dictionary<string, AssetFileLog> mSave = new Dictionary<string, AssetFileLog>();

        public ReturnCode Run()
        {
            var outputFolder = ((AddressableAssetsBundleBuildParameters) this.m_Parameters).OutputFolder;

            foreach (var node in this.m_Content.BundleLayout)
            {
                string[] files = new string[node.Value.Count];
                for (var index = 0; index < node.Value.Count; index++)
                {
                    GUID sub = node.Value[index];
                    string file = PathUtils.FormatFilePath(AssetDatabase.GUIDToAssetPath(sub.ToString()));
                    string path = PathUtils.RemoveFileExtension(file.Replace("Assets/Res/", ""));
                    files[index] = path.ToLower();
                }

                string filePath = outputFolder + "/" + node.Key;
                var fileInfo = new FileInfo(filePath);
                var crc = Crc32CAlgorithm.Compute(filePath);
                var dependencies = this.m_Results.BundleInfos[node.Key].Dependencies;
                this.mSave.Add(node.Key, new AssetFileLog(crc, TimeUtils.GetUtcTimeStamp(), node.Key, files, dependencies, (int) fileInfo.Length));
            }
            var json = JsonConvert.SerializeObject(this.mSave);
            File.WriteAllText(this.mStreamingAssets + "/" + AssetsManager.kFileInfo,json);

            return ReturnCode.Success;
        }
  
        public int Version
        {
            get { return 1; }
        }
    }
}
