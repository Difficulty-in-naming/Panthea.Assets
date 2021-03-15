using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class CollectAllAssets : AResPipeline
    {
        public string PackPath;
        private Dictionary<string, object> mInject;
        public CollectAllAssets(string packPath,Dictionary<string,object> inject)
        {
            this.PackPath = packPath;
            this.mInject = inject;
        }
    
        public override Task Do()
        {
            var files = Directory.GetFiles(Application.dataPath + "/" + this.PackPath, "*.*", SearchOption.AllDirectories).ToList();

            for (var index = files.Count - 1; index >= 0; index--)
            {
                var path = files[index];
                var ext = Path.GetExtension(path);
                var fileName = Path.GetFileName(path);
                if (ext == ".meta")
                {
                    files.RemoveAt(index);
                    continue;
                }
                if (fileName.StartsWith("."))
                {
                    files.RemoveAt(index);
                    continue;
                }
                files[index] = PathUtils.FormatFilePath(files[index]);
            }
        
#if DEBUG_ADDRESSABLE
        foreach (var node in files)
        {
            Debug.Log("this file need to build:" + node);
        }
#endif

            this.mInject.Add("files", files);
            return Task.CompletedTask;
        }
    }
}
