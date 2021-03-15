using UnityEngine;

namespace Panthea.Asset
{
    public class AssetsConfig
    {
        private static string mStreamingAssets;
        public static string StreamingAssets => mStreamingAssets ?? (mStreamingAssets = Application.streamingAssetsPath);
        public static string AssetBundleStreamingAssets => (mStreamingAssets ?? (mStreamingAssets = Application.streamingAssetsPath)) + "/" + Platform;

        private static string mPersistentDataPath;
        public static string PersistentDataPath
        {
            get
            {
#if DEVELOPMENT_BUILD
            return mPersistentDataPath ?? (mPersistentDataPath = Application.persistentDataPath);
#elif UNITY_ANDROID && !UNITY_EDITOR
            if (mPersistentDataPath == null)
            {
                using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                {
                    using (AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        mPersistentDataPath = currentActivity.Call<AndroidJavaObject>("getFilesDir").Call<string>("getCanonicalPath");
                    }
                }
            }
            return mPersistentDataPath;
#else
                return mPersistentDataPath ?? (mPersistentDataPath = Application.persistentDataPath);
#endif

            }
        }

        public const string Suffix = ".bundle";
    
        public static string AssetBundlePersistentDataPath
        {
            get { return PersistentDataPath + "/assetbundles"; }
        }
    
        public static string Platform
        {
            get
            {
#if UNITY_ANDROID
            return "android";
#elif UNITY_IOS
            return "ios";
#else
                return "pc";
#endif
            }
        }
    }
}
