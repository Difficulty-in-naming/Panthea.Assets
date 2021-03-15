using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class BuildContent : AResPipeline
    {
        protected AddressableAssetSettings AddressableBuilder;

        public BuildContent(AddressableAssetSettings settings)
        {
            this.AddressableBuilder = settings;
        }
    
        public override Task Do()
        {
            bool hasBuilderMode = false;
            for (var index = 0; index < this.AddressableBuilder.DataBuilders.Count; index++)
            {
                var node = this.AddressableBuilder.DataBuilders[index];
                if (node is XAssetBundleBuildMode)
                {
                    this.AddressableBuilder.ActivePlayerDataBuilderIndex = index;
                    hasBuilderMode = true;
                    break;
                }
            }

            if (!hasBuilderMode)
            {
                XAssetBundleBuildMode asset = ScriptableObject.CreateInstance<XAssetBundleBuildMode>();
                AssetDatabase.CreateAsset(asset, "Assets/AddressableAssetsData/DataBuilders/XFrameworkBuild.asset");
                AssetDatabase.SaveAssets();
                this.AddressableBuilder.AddDataBuilder(asset,false);
                this.AddressableBuilder.SetDataBuilderAtIndex(0, asset, false);
                this.AddressableBuilder.ActivePlayerDataBuilderIndex = 0;
            }
            AddressableAssetSettings.BuildPlayerContent();
            return Task.CompletedTask;
        }
    }
}
