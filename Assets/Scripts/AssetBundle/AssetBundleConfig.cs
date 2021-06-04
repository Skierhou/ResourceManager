using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlay
{
    [System.Serializable]
    public class AssetBundleConfig
    {
        // 资源路径转crc
        public uint crc;
        // 资源路径
        public string path;
        // 资源包名
        public string assetBundleName;
        // 资源名：从资源包中加载的名称
        public string assetName;
        // 依赖包
        public List<string> dependceAssetBundles;
    }
    [System.Serializable]
    public class AssetBundleContainer
    {
        public List<AssetBundleConfig> configList;
    }
}
