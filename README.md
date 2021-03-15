> **⚠ WARNING: 这个项目目前完成度还比较低**  
> 如果用于项目当中可能需要修改一些源码才能更好的与项目进行兼容.
> 你可以随时在这里获取最新的代码更新 https://github.com/Difficulty-in-naming/Panthea.Assets

## **初始化**
```c#
//如果是在编辑器中使用,我们可以直接使用这个类,它不需要初始化任何AB的缓存和AB计数,也不需要任何下载功能
#if UNITY_EDITOR
var assetsManager = new EDITOR_AssetsManager();
AssetsKit.Inst = assetsManager;
return;
#endif

//Release
//目前版本下列内容必须初始化
//文件跟踪,正式版本中可能包体内和包体外都会存在AB.我们需要跟踪确认使用哪个路径下的AB
var fileTrack = new ABFileTrack();
//注册AssetBundle加载缓存池.加载过的物体会在池中缓存起来.不需要存的话取消这个注册就好
var pool = new AssetBundlePool(fileTrack);
//注册AssetBundle的引用计数器.当引用计数清零时自动卸载AB
var counter = new AssetBundleCounter(pool);
//注册运行时加载AB的支持模块
var runtime = new AssetBundleRuntime(fileTrack, pool, counter;
//注册需要使用的下载工具(你也可以自己继承IDownloadHandler然后实现自己的下载器)
var downloader = new UnityWebDownloader();
//注册确切使用的下载服务器(目前仅支持索引一个下载服务器)
var downloadPlatform = new S3Download(GameConstWWWLocalzation,
    AssetsConfig.AssetBundlePersistentDataPath + "/", downloader);
//使AssetsManager支持下载功能(只是注册S3Download只是注册了服务,是并没有开启下载功能)
var abDownloader = new AssetBundleDownloader(fileTrackdownloadPlatform);
fileTrack.ConfigureDownloadPlatform(downloadPlatform);
//创建AssetManager,AssetManager可以理解为底层接口的一层包装
var assetsManager = new AssetsManager(fileTrack, runtime,abDownloader);
AssetsKit.Inst = assetsManager;
```

## **打包** ##
你可以在AssetBundleBuilder脚本中修改打包内容
AssetBundlerBuilder打包AssetBundle采用的是管道模式进行处理.
```c#
//收集项目中Res/目录下面的所有文件并自动筛选掉所有.meta文件和无文件名文件如.DS.Store等非法文件
AddProcess<CollectAllAssets>();
//将之前收集的所有文件路径转换为小写的路径
AddProcess<LowerCasePath>();
//将所有文件按照路径划分.每个文件夹下的所有文件会被设置为一个Bundle
AddProcess<BuildGroup>();
//打包AB
AddProcess<BuildContent>();
//根据项目需求.将批量AB压缩成ZIP
AddProcess<ZipAssets>();
//上传服务器
AddProcess<UploadS3>();
/////
/////你可以在以上管道之间随意插入你的处理逻辑.以适应项目的变化.
/////
```

## **下载**
```c#
//获取远程下载列表
var list = FetchDownloadList();
//开始下载
await AssetsKit.Inst.Download(list);
```
下载文件.你不需要检查文件的一致性,因为Manager内部已经采用了Crc32的方式进行检查.

## **加载** ##
```c#
string path = "UI/Hello World"//加载文件不需要后缀名
string abPath = "UI/UI.asset"//UI文件夹(不包含子文件夹)下的内容被打包成了一个单独的AB文件
//加载文件
Object obj = await AssetsKit.Inst.Load<Object>(path);
//加载AB文件
AssetBundleRequest ab = await AssetsKit.Inst.LoadAssetBundle(path)
//加载AB文件2
AssetBundleRequest ab = await AssetsKit.Inst.LoadAssetBundleFromABKey(abPath);
//根据给定的文件前缀,得到匹配的AB文件路径
var paths = AssetsKit.Inst.GetFilterAssetBundle(new string[] {"UI/Common","UI/Main Panel","Model","Sound"});
List<UniTask> tasks = new List<UniTask>(); 
foreach (var node in paths)
{
    //加载AB文件中的所有Object
    Object[] objs = AssetsKit.Inst.LoadAll(node);
}
```

## **如何拆分包体**
首先你需要通过Tools/Addressable/Pack打包所有的AB文件,此时每个文件夹下会生成一个对应的AB配置文件.通过修改配置文件Bundle Mode为Pack Separately即可

## **FAQ**
Q : 什么时候资源会被卸载.  
A : 当你的资源是通过AssetsManager.Load<>或AssetsManager.Initialize进行加载创建的.并且使用AssetsManager.Destroy进行卸载.那么当资源的引用计数为0时.资源会自动释放内存.  
Q ：运行效率怎么样.  
A ：下图为XFramework与Addressables的加载耗时![图片](https://github.com/Noname-Studio/XFramework/blob/master/Docs/pic/XFrameworkResourcesLoadPerformance.png)  
通常大多数框架在加载AB的时候使用的都是UnityWebRequest进行加载.因为UnityWebRequest在安卓下可以加载StreamingAssets目录下的AB文件.XFramework中只有在下载AB文件的时候使用了UnityWebRequest,加载的时候使用的是AssetBundle.LoadFromFileAsync/LoadFromStreamAsync.这是官方加载AssetBundle最快的方式.  
Q ：为什么占用内存这么高  
A ：首先你要确认你使用的压缩方式是否为LZMA.因为这种方式Unity在加载的时候会将所有资源解压到Memory中.方便后续调用.一般我建议单张大图才使用LZMA压缩.但是如果你依然决定使用LZMA压缩的话.你可以在下载AB之后使用AssetBundle.RecompressAssetBundleAsync将LZMA转换为LZ4压缩.其次你需要检查代码中是否有资源没有按照规范卸载.这会导致AB一直保留在内存中无法被自动卸载.如果不太在意资源卸载的话.你们可以在切换场景的时候调用IAssetLocator.UnloadAllAssetBundle().卸载所有的AB


