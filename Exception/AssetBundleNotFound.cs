using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Panthea.Asset
{
    public class AssetBundleNotFound : Exception
    {
        private string mPath;
        public override string Message
        {
            get
            {
                return "无法从StreamingAssets或Persistent目录下找到{" + mPath + "}文件";
            }
        }

        public AssetBundleNotFound(string path)
        {
            mPath = path;
        }
    }
}
