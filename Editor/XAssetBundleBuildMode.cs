using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.BuildPipelineTasks;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Panthea.Editor.Asset
{
    [CreateAssetMenu(fileName = "XAssetBundleBuildMode.asset", menuName = "XFramework AssetBundle Build")]
    public class XAssetBundleBuildMode : BuildScriptBase
    {
        public override string Name
        {
            get
            {
                return "XFramework AssetBundle Build";
            }
        }
    
        public override bool CanBuildData<T>()
        {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayerBuildResult));
        }
    
        private List<AssetBundleBuild> m_AllBundleInputDefs = new List<AssetBundleBuild>();
        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (assetGroup == null)
                return string.Empty;

            foreach (var schema in assetGroup.Schemas)
            {
                var errorString = this.ProcessGroupSchema(schema, assetGroup, aaContext);
                if(!string.IsNullOrEmpty(errorString))
                    return errorString;
            }

            return string.Empty;
        }
    
        protected virtual string ProcessGroupSchema(AddressableAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            var bundledAssetSchema = schema as BundledAssetGroupSchema;
            if (bundledAssetSchema != null)
                return this.ProcessBundledAssetSchema(bundledAssetSchema, assetGroup, aaContext);
            return string.Empty;
        }

        public override bool IsDataBuilt()
        {
            return true;
        }

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
        {
            TResult result = default(TResult);
            
            var timer = new Stopwatch();
            timer.Start();
            var aaSettings = builderInput.AddressableSettings;

            var locations = new List<ContentCatalogDataEntry>();
            this.m_AllBundleInputDefs = new List<AssetBundleBuild>();
            var bundleToAssetGroup = new Dictionary<string, string>();
            var runtimeData = new ResourceManagerRuntimeData();
            runtimeData.CertificateHandlerType = aaSettings.CertificateHandlerType;
            runtimeData.BuildTarget = builderInput.Target.ToString();
            runtimeData.ProfileEvents = builderInput.ProfilerEventsEnabled;
            runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;
            var aaContext = new AddressableAssetsBuildContext
            {
                settings = aaSettings,
                runtimeData = runtimeData,
                bundleToAssetGroup = bundleToAssetGroup,
                locations = locations,
                providerTypes = new HashSet<Type>()
            };

            var errorString = this.ProcessAllGroups(aaContext);
            if(!string.IsNullOrEmpty(errorString))
                result = AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);

            if (result == null)
            {
                result = this.DoBuild<TResult>(builderInput, aaContext);   
            }
            
            if(result != null)
                result.Duration = timer.Elapsed.TotalSeconds;

            return result;
        }

        protected override string ProcessAllGroups(AddressableAssetsBuildContext aaContext)
        {
            if (aaContext == null ||
                aaContext.settings == null ||
                aaContext.settings.groups == null)
            {
                return "No groups found to process in build script " + this.Name;
            }
            //intentionally for not foreach so groups can be added mid-loop.
            for(int index = 0; index < aaContext.settings.groups.Count; index++)  
            {
                AddressableAssetGroup assetGroup = aaContext.settings.groups[index];
                if (assetGroup == null)
                    continue;

                EditorUtility.DisplayProgressBar($"Processing Addressable Group", assetGroup.Name, (float)index/aaContext.settings.groups.Count);
                var errorString = this.ProcessGroup(assetGroup, aaContext);
                if (!string.IsNullOrEmpty(errorString))
                {
                    EditorUtility.ClearProgressBar();
                    return errorString;
                }
            }

            EditorUtility.ClearProgressBar();
            return string.Empty;
        }

        protected TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext) where TResult : IDataBuilderResult
        {
            ExtractDataTask extractData = new ExtractDataTask();

            if (this.m_AllBundleInputDefs.Count > 0)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, "Unsaved scenes");

                var buildTarget = builderInput.Target;
                var buildTargetGroup = builderInput.TargetGroup;

                var buildParams = new AddressableAssetsBundleBuildParameters(
                    aaContext.settings, 
                    aaContext.bundleToAssetGroup, 
                    buildTarget, 
                    buildTargetGroup, 
                    Application.streamingAssetsPath);

                var builtinShaderBundleName = aaContext.settings.DefaultGroup.Guid + "_unitybuiltinshaders.bundle";
                var buildTasks = RuntimeDataBuildTasks(builtinShaderBundleName);
                buildTasks.Add(extractData);

                string aaPath = aaContext.settings.AssetPath;
                IBundleBuildResults results;
                var exitCode = ContentPipeline.BuildAssetBundles(buildParams, new BundleBuildContent(this.m_AllBundleInputDefs), out results, buildTasks, aaContext);

                if (exitCode < ReturnCode.Success)
                    return AddressableAssetBuildResult.CreateResult<TResult>(null, 0, "SBP Error" + exitCode);
                if (aaContext.settings == null && !string.IsNullOrEmpty(aaPath))
                    aaContext.settings = AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(aaPath);

                //using (var progressTracker = new UnityEditor.Build.Pipeline.Utilities.ProgressTracker())
                //{
                //    progressTracker.UpdateTask("Generating Addressables Locations");
                //    GenerateLocationListsTask.Run(aaContext, extractData.WriteData);
                //}
            }
        
            var opResult = AddressableAssetBuildResult.CreateResult<TResult>("", aaContext.locations.Count);

            return opResult;
        }

        static IList<IBuildTask> RuntimeDataBuildTasks(string builtinShaderBundleName)
        {
            var buildTasks = new List<IBuildTask>();

            // Setup
            buildTasks.Add(new SwitchToBuildPlatform());
            //buildTasks.Add(new RebuildSpriteAtlasCache());

            // Player Scripts
            //buildTasks.Add(new BuildPlayerScripts());

            // Dependency
            buildTasks.Add(new CalculateSceneDependencyData());
            buildTasks.Add(new CalculateAssetDependencyData());
            //buildTasks.Add(new AddHashToBundleNameTask());
            buildTasks.Add(new StripUnusedSpriteSources());
            buildTasks.Add(new CreateBuiltInShadersBundle(builtinShaderBundleName));

            // Packing
            buildTasks.Add(new GenerateBundlePacking());
            buildTasks.Add(new UpdateBundleObjectLayout());

            buildTasks.Add(new GenerateBundleCommands());
            buildTasks.Add(new GenerateSubAssetPathMaps());
            buildTasks.Add(new GenerateBundleMaps());

            // Writing
            buildTasks.Add(new WriteSerializedFiles());
            buildTasks.Add(new ArchiveAndCompressBundles());
            
            //XAssetsFramework Need
            buildTasks.Add(new DeleteUnusedAssetBundle());
            buildTasks.Add(new GenerateFileLog());
            return buildTasks;
        }
    
        protected string ProcessBundledAssetSchema(BundledAssetGroupSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
        {
            if (schema == null || !schema.IncludeInBuild)
                return string.Empty;

            var bundleInputDefs = new List<AssetBundleBuild>();
            PrepGroupBundlePacking(assetGroup, bundleInputDefs, schema.BundleMode);
            for (int i = 0; i < bundleInputDefs.Count; i++)
            {
                string assetBundleName = bundleInputDefs[i].assetBundleName;
                if (aaContext.bundleToAssetGroup.ContainsKey(assetBundleName))
                {
                    int count = 1;
                    var newName = assetBundleName;
                    while (aaContext.bundleToAssetGroup.ContainsKey(newName) && count < 1000)
                        newName = assetBundleName.Replace(".bundle", string.Format("{0}.bundle", count++));
                    assetBundleName = newName;
                }

                string hashedAssetBundleName = assetBundleName;
                this.m_AllBundleInputDefs.Add(new AssetBundleBuild
                {
                    addressableNames = bundleInputDefs[i].addressableNames,
                    assetNames = bundleInputDefs[i].assetNames,
                    assetBundleName = hashedAssetBundleName,
                    assetBundleVariant = bundleInputDefs[i].assetBundleVariant
                });
                aaContext.bundleToAssetGroup.Add(hashedAssetBundleName, assetGroup.Guid);
            }
            return string.Empty;
        }
    
        internal static void PrepGroupBundlePacking(AddressableAssetGroup assetGroup, List<AssetBundleBuild> bundleInputDefs, BundledAssetGroupSchema.BundlePackingMode packingMode)
        {
            if (packingMode == BundledAssetGroupSchema.BundlePackingMode.PackTogether)
            {
                var allEntries = new List<AddressableAssetEntry>();
                foreach (var a in assetGroup.entries)
                    a.GatherAllAssets(allEntries, true, true, false);
                var name = assetGroup.Name;
                if (!assetGroup.Name.Contains("-"))
                    name += "-" + assetGroup.Name;
                GenerateBuildInputDefinitions(allEntries, bundleInputDefs, name.Replace("-","/"));
            }
            else
            {
                if (packingMode == BundledAssetGroupSchema.BundlePackingMode.PackSeparately)
                {
                    foreach (var a in assetGroup.entries)
                    {
                        var allEntries = new List<AddressableAssetEntry>();
                        a.GatherAllAssets(allEntries, true, true, false);
                        GenerateBuildInputDefinitions(allEntries, bundleInputDefs, a.address);
                    }
                }
                else
                {
                    var labelTable = new Dictionary<string, List<AddressableAssetEntry>>();
                    foreach (var a in assetGroup.entries)
                    {
                        var sb = new StringBuilder();
                        foreach (var l in a.labels)
                            sb.Append(l);
                        var key = sb.ToString();
                        List<AddressableAssetEntry> entries;
                        if (!labelTable.TryGetValue(key, out entries))
                            labelTable.Add(key, entries = new List<AddressableAssetEntry>());
                        entries.Add(a);
                    }

                    foreach (var entryGroup in labelTable)
                    {
                        var allEntries = new List<AddressableAssetEntry>();
                        foreach (var a in entryGroup.Value)
                            a.GatherAllAssets(allEntries, true, true, false);
                        GenerateBuildInputDefinitions(allEntries, bundleInputDefs, assetGroup.Name.Replace("-","/") + "/" + entryGroup.Key);
                    }
                }
            }
        }
    
    
        static void GenerateBuildInputDefinitions(List<AddressableAssetEntry> allEntries, List<AssetBundleBuild> buildInputDefs,string name)
        {
            var scenes = new List<AddressableAssetEntry>();
            var assets = new List<AddressableAssetEntry>();
            foreach (var e in allEntries)
            {
                if (string.IsNullOrEmpty(e.AssetPath))
                    continue;
                if (e.AssetPath.EndsWith(".unity"))
                    scenes.Add(e);
                else
                    assets.Add(e);
            }
            if (assets.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(assets, name + ".bundle"));
            if (scenes.Count > 0)
                buildInputDefs.Add(GenerateBuildInputDefinition(scenes, name + ".bundle"));
        }

        static AssetBundleBuild GenerateBuildInputDefinition(List<AddressableAssetEntry> assets, string name)
        {
            var assetsInputDef = new AssetBundleBuild();
            assetsInputDef.assetBundleName = name.ToLower().Replace(" ", "").Replace('\\', '/').Replace("//", "/");
            var assetIds = new string[assets.Count];
            var addressIds = new string[assets.Count];

            for (var index = 0; index < assets.Count; index++)
            {
                var a = assets[index];
                assetIds[index] = a.AssetPath;
                addressIds[index] = a.address;
            }

            assetsInputDef.assetNames = assetIds;
            assetsInputDef.addressableNames = addressIds;
            return assetsInputDef;
        }
    }
}
