using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GamePlay
{
    public interface IAssetBundleManager
    {
        /// <summary>
        /// 同步加载AB包
        /// </summary>
        AssetBundle LoadAssetBundle(string assetBundleName);
        /// <summary>
        /// 异步加载AB包
        /// </summary>
        void LoadAssetBundleAsync(string assetBundleName, Action<AssetBundle> callback);
        /// <summary>
        /// 卸载AB包
        /// </summary>
        void UnLoadAssetBundle(string assetBundleName);
    }
}
