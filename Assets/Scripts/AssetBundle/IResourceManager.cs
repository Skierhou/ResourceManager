using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamePlay
{
    public interface IResourceManager
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        T LoadAsset<T>(string path) where T : UnityEngine.Object;
        /// <summary>
        /// 异步加载资源
        /// </summary>
        void LoadAssetAsync(string path, Action<UnityEngine.Object, object> onLoaded, int priority = 0, object userData = null);
        /// <summary>
        /// 卸载资源
        /// </summary>
        void UnLoadAsset(string path, bool isDestroy = false);
    }
}
