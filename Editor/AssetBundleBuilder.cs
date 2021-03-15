using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class AssetBundleBuilder
    {
        List<Type> mProcess = new List<Type>();
        private const string mPackPath = AssetsPackPath.Path;
        Dictionary<string, object> mInject = new Dictionary<string, object>();

        public async void Init()
        {
            BuildScript.buildCompleted -= OnBuildCompleted;
            BuildScript.buildCompleted += OnBuildCompleted;

            mInject.Add("inject", mInject);
            mInject.Add("packPath", mPackPath);
            mInject.Add("settings", AddressableAssetSettingsDefaultObject.GetSettings(true));

            //收集所有的需要打包的文件
            AddProcess<CollectAllAssets>();
            //将上面收集到的文件路径全部转换为小写
            AddProcess<LowerCasePath>();
            //将收集到的文件自动分为不同的Group,你可以在后续调整每个Group的参数.让某个Group使用不同的压缩或者分包
            AddProcess<BuildGroup>();
            //打包内容
            AddProcess<BuildContent>();
            //压缩内容
            AddProcess<ZipAssets>();
            //提交服务器
            //mProcess.Add(typeof(UploadS3));
            await DoPipeline();
        }

        private void OnBuildCompleted(AddressableAssetBuildResult result)
        {
            if (!string.IsNullOrEmpty(result.Error)) throw new Exception(result.Error);
        }

        public static void Pack()
        {
            var builder = new AssetBundleBuilder();
            builder.Init();
        }

        private void AddProcess<T>()
        {
            mProcess.Add(typeof (T));
        }

        public async Task DoPipeline()
        {
            try
            {
                for (var index = 0; index < mProcess.Count; index++)
                {
                    var node = mProcess[index];
                    var constructor = node.GetConstructors()[0];
                    var paramters = constructor.GetParameters();
                    List<object> args = new List<object>();
                    foreach (var p in paramters)
                    {
                        foreach (var inject in mInject)
                        {
                            if (inject.Value.GetType() == p.ParameterType)
                            {
                                args.Add(inject.Value);
                            }
                        }
                    }

                    var pipeline = constructor.Invoke(args.ToArray()) as AResPipeline;
                    if (pipeline == null)
                    {
                        Debug.LogError(node + "没有继承自 AResPipeline");
                        continue;
                    }

                    await pipeline.Do();
                }

                Debug.Log("打包完成");
            }
            catch (Exception e)
            {
                Debug.LogError("打包失败,具体错误看下面内容");
                Debug.LogError(e);
            }
            finally
            {
                BuildScript.buildCompleted -= OnBuildCompleted;
            }
        }
    }
}