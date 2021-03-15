using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class BuildGroup : AResPipeline
    {
        public string PackPath;
        protected List<string> BuildFiles;
        protected AddressableAssetSettings AddressableBuilder;

        public BuildGroup(string packPath,List<string> buildFiles,AddressableAssetSettings settings)
        {
            this.PackPath = packPath;
            this.BuildFiles = buildFiles;
            this.AddressableBuilder = settings;
        }

        public override Task Do()
        {
            Dictionary<string,List<string>> mapping = new Dictionary<string, List<string>>();
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var node in this.BuildFiles)
                {
                    var group = PathUtils.FullPathToAssetbundlePath(Path.GetDirectoryName(node), this.PackPath);
                    List<string> list;
                    if (!mapping.TryGetValue(group, out list))
                    {
                        list = new List<string>();
                        mapping.Add(group, list);
                    }

                    list.Add(node);
                }
            
                var schemas = new List<AddressableAssetGroupSchema>();
                //var contentUpdate = ScriptableObject.CreateInstance<ContentUpdateGroupSchema>();
                var bundle = ScriptableObject.CreateInstance<BundledAssetGroupSchema>();
                bundle.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                bundle.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                bundle.UseAssetBundleCrc = false;
                bundle.UseAssetBundleCache = false;
                //schemas.Add(contentUpdate);
                schemas.Add(bundle);
            
                foreach (var node in mapping)
                {
                    if (node.Value.Count == 0)
                        continue;
#if DEBUG_ADDRESSABLE
            Debug.Log("Create Group :" + node);
#endif
                    var group = this.AddressableBuilder.FindGroup(node.Key.Replace("/", "-"));
                    if (group == null)
                        group = this.AddressableBuilder.CreateGroup(node.Key, false, false, false, schemas);
                    else
                    {
                        //检查Schemma
                        if (!group.HasSchema(typeof(BundledAssetGroupSchema)))
                            group.AddSchema(bundle);
                    }
                    foreach (var file in node.Value)
                    {
                        var entry = this.AddressableBuilder.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(PathUtils.FullPathToUnityPath(file)), group, false, false);
                        entry.SetAddress(Path.GetFileNameWithoutExtension(file).ToLower(), false);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            return Task.CompletedTask;
        }
    }
}
