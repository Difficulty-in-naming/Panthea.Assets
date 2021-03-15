using System.IO;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace Panthea.Editor.Asset
{
    public class DeleteUnusedAssetBundle : IBuildTask
    {
#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;

        [InjectContext(ContextUsage.In)]
        IBundleBuildContent m_Content;
    
#pragma warning restore 649
    
        public ReturnCode Run()
        {
            var outputFolder = ((AddressableAssetsBundleBuildParameters) this.m_Parameters).OutputFolder;
            var allAssetbundle = Directory.GetFiles( outputFolder + "/","*.bundle",SearchOption.AllDirectories);
            foreach (var node in allAssetbundle)
            {
                string path = PathUtils.FormatFilePath(node.Replace(outputFolder + "/", ""));
                if (!this.m_Content.BundleLayout.ContainsKey(path))
                {
                    File.Delete(node);
                }
            }
            //foreach(var node in m_Content.BundleLayout)
            return ReturnCode.Success;
        }

        public int Version
        {
            get { return 1; }
        }
    }
}