using System;
using System.Collections.Generic;

namespace GamePlay
{
    public partial class ResourceManager
    {
        private sealed class LoadingItem
        {
            public uint crc;
            public AssetBundleConfig config;
            public int priority;
            public object userData;

            public LoadingItem(uint crc,AssetBundleConfig config, int priority, object userData)
            {
                this.crc = crc;
                this.config = config;
                this.priority = priority;
                this.userData = userData;
            }
        }
    }
}
