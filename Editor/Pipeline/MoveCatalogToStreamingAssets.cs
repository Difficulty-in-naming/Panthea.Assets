using System.IO;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace Panthea.Editor.Asset
{
    public class MoveCatalogToStreamingAssets : AResPipeline
    {
        public override Task Do()
        {
            string sourcePath = Addressables.BuildPath;
            if (!Directory.Exists(sourcePath))
            {
                return Task.CompletedTask;
            }
            string destinationPath = Addressables.PlayerBuildDataPath;
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*",
                SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", 
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
            return Task.CompletedTask;
        }
    }
}
