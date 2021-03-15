using Cysharp.Threading.Tasks;

namespace Panthea.Asset
{
    public interface IDownloadPlatform
    {
        UniTask<DownloadResult> Download(DownloadThread thread);
        UniTask<DownloadThread> FetchHeader(string path);
        UniTask<string> GetText(string path);
        UniTask<byte[]> GetBytes(string path);

    }
}