///这个脚本是通过GenerateGroupRuntimeProfile自动生成的.请不要手动修改该脚本的任何内容///

using System.Collections.Generic;
using System.Reflection;
using Panthea.Asset;
using UnityEngine;

namespace Panthea.Asset
{
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


		public static string Built_In_Data = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string Default_Local_Group = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_collection = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_food = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_food_breakfast = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_food_food_truck = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_gaming = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_head = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_ingredient = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_picture = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_questicon_breakfast = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_replacefurniture_breakfast = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_replacefurniture_food_truck = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string image_share_k1_story = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_common = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_amanda = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_artist_man = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_cowboy = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_fat_man = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_office_lady = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_old_woman = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_racing_driver = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_singer_girl = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_student_girl = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_customer_weird_man = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_cw = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_gaming = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string sound_story = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_blueskin = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_gamecoreui = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_login = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_main = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_register = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;
		public static string ui_shop = Application.streamingAssetsPath + "/" + AssetsConfig.Platform;


		public static void SetAs(bool isStreamingAssets,string path)
		{
			var index = path.LastIndexOf("_assets_");
			if(index != -1)
				path = path.Substring(0, index);
			string key = path.Replace("-","_");
			if(FieldLookup.ContainsKey(key))
				FieldLookup[key].SetValue(null, (isStreamingAssets ? AssetsConfig.AssetBundleStreamingAssets : AssetsConfig.AssetBundlePersistentDataPath));
		}
	}
}