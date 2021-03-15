using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Panthea.Asset
{
    public interface IDownloadHandler
    {
        UniTask<DownloadResult> Download(DownloadThread thread);
        UniTask<Dictionary<string, string>> GetHeaders(string url);
        UniTask<string> GetText(string url);
        UniTask<byte[]> GetBytes(string url);

    }
}
