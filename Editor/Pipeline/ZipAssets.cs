using System.Threading.Tasks;
using UnityEngine;

namespace Panthea.Editor.Asset
{
    public class ZipAssets : AResPipeline
    {
        public override Task Do()
        {
            Debug.Log("等待实现");
            return Task.CompletedTask;
        }
    }
}
