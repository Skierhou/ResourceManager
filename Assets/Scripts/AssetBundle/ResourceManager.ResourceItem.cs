using UnityEngine;

namespace GamePlay
{
    public partial class ResourceManager
    {
        private sealed class ResourceItem
        {
            private uint m_Crc;
            private Object m_Asset;
            private int m_RefCount;

            public uint Crc => m_Crc;
            public Object Asset => m_Asset;
            public int RefCount
            {
                set { m_RefCount = value; }
                get { return m_RefCount; }
            }

            public ResourceItem(uint crc, Object asset, int refCount = 0)
            {
                m_Crc = crc;
                m_Asset = asset;
                m_RefCount = refCount;
            }
        }
    }
}
