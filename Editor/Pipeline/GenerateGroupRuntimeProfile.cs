using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class GenerateGroupRuntimeProfile : AResPipeline
    {
        private string template = @"///这个脚本是通过GenerateGroupRuntimeProfile自动生成的.请不要手动修改该脚本的任何内容///
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class AssetsRedirect
{
    private static Dictionary<string,FieldInfo> FieldLookup;

    static AssetsRedirect()
    {
        FieldLookup = new Dictionary<string, FieldInfo>();
        var fields = typeof(AssetsRedirect).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var node in fields)
        {
            FieldLookup.Add(node.Name, node);
        }
    }


{0}

    public static void SetAs(bool isStreamingAssets,string path)
    {
	    var index = path.LastIndexOf(""_assets_"");
	    if(index != -1)
            path = path.Substring(0, index);
        string key = path.Replace(""-"",""_"");
        if(FieldLookup.ContainsKey(key))
            FieldLookup[key].SetValue(null, (isStreamingAssets ? AssetsConfig.AssetBundleStreamingAssets : AssetsConfig.AssetBundlePersistentDataPath));
    }
}";
    
    
        private AddressableAssetSettings mAddressableBuilder = null;

        public GenerateGroupRuntimeProfile(AddressableAssetSettings settings)
        {
            this.mAddressableBuilder = settings;
        }
    
        private static readonly string CodeFilePath = Application.dataPath + "/" + "Scripts/Module/XResComponent/Runtime/AssetsRedirect.cs";
        public override Task Do()
        {
            var groups = this.mAddressableBuilder.groups;
            StringBuilder sb = new StringBuilder();
            foreach (var node in groups)
            {
                var variableName = node.Name.Replace("-", "_").Replace(" ", "_");
            
                sb.AppendLine("\tpublic static string " + variableName + " = Application.streamingAssetsPath + \"/\" + AssetsConfig.Platform;");
                var bundled = node.GetSchema<BundledAssetGroupSchema>();
                if (bundled != null)
                {
                    bundled.BuildPath.GetType().GetField("m_Id",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bundled.BuildPath, "[" + $"AssetsRedirect.{variableName}" + "]");
                    bundled.LoadPath.GetType().GetField("m_Id",BindingFlags.Instance | BindingFlags.NonPublic).SetValue(bundled.LoadPath, "{" + $"AssetsRedirect.{variableName}" + "}");
                }
            }

            var code = this.template.Replace("{0}",sb.ToString());
            File.WriteAllText(CodeFilePath, code);
            return Task.CompletedTask;
        }
    }
}
