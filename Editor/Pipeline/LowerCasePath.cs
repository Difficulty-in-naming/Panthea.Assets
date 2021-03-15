using System.Collections.Generic;
using System.Threading.Tasks;

namespace Panthea.Editor.Asset
{
    public class LowerCasePath : AResPipeline
    {
        public List<string> BuildFiles;

        public LowerCasePath(List<string> buildFiles)
        {
            this.BuildFiles = buildFiles;
        }
    
        public override Task Do()
        {
            for (int i = 0; i < this.BuildFiles.Count; i++)
            {
                this.BuildFiles[i] = this.BuildFiles[i].ToLower();
            }
            return Task.CompletedTask;
        }
    }
}
