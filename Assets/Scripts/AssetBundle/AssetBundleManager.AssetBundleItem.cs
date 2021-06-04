using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GamePlay
{
    public partial class AssetBundleManager : Singleton<AssetBundleManager>
    {
        private sealed class AssetBundleItem
        {
            private AssetBundle m_AssetBundle;
            private int m_RefCount;
            public int RefCount 
            {
                set { m_RefCount = value; }
                get { return m_RefCount; }
            }
            public AssetBundle AssetBundle => m_AssetBundle;

            public AssetBundleItem(AssetBundle assetBundle, int refCount = 0)
            {
                m_AssetBundle = assetBundle;
                m_RefCount = refCount;
            }
        }
    }
}
