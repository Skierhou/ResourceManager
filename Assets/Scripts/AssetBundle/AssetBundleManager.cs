using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GamePlay
{
    public partial class AssetBundleManager : Singleton<AssetBundleManager>,IAssetBundleManager
    {
        private Dictionary<uint, AssetBundleConfig> m_configDict;
        private Dictionary<string, AssetBundleItem> m_loadedBundleDict;
        private Dictionary<string,Action<AssetBundle>> m_loadingDict;

        private MonoBehaviour mono;

        public void Init(MonoBehaviour mono)
        {
            this.mono = mono;
            m_configDict = new Dictionary<uint, AssetBundleConfig>();
            m_loadedBundleDict = new Dictionary<string, AssetBundleItem>();
            m_loadingDict = new Dictionary<string, Action<AssetBundle>>();

            AssetBundleContainer container = SerializeHelper.ReadByte<AssetBundleContainer>(Path.Combine(Application.streamingAssetsPath,"config.byte"));
            foreach (AssetBundleConfig config in container.configList)
            {
                m_configDict.Add(config.crc, config);
            }
        }

        public bool TryGetConfig(uint crc, out AssetBundleConfig config) 
        {
            return m_configDict.TryGetValue(crc, out config);
        }

        public AssetBundle LoadAssetBundle(string assetBundleName)
        {
            if (m_loadedBundleDict.TryGetValue(assetBundleName, out AssetBundleItem assetBundleItem))
            {
                ++assetBundleItem.RefCount;
            }
            else
            {
                AssetBundle assetBundle = AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, assetBundleName));
                if (assetBundle == null)
                {
                    Debug.LogError(string.Format("Not Found AssetBundle By Path : {0}", assetBundleName));
                    return null;
                }
                assetBundleItem = new AssetBundleItem(assetBundle);
                ++assetBundleItem.RefCount;
                m_loadedBundleDict.Add(assetBundleName, assetBundleItem);
            }
            return assetBundleItem.AssetBundle;
        }

        public void UnLoadAssetBundle(string assetBundleName)
        {
            if (m_loadedBundleDict.TryGetValue(assetBundleName, out AssetBundleItem assetBundleItem))
            {
                if (--assetBundleItem.RefCount <= 0)
                {
                    assetBundleItem.AssetBundle.Unload(true);
                    m_loadedBundleDict.Remove(assetBundleName);
                }
            }
        }

        public void LoadAssetBundleAsync(string assetBundleName, Action<AssetBundle> callback)
        {
            if (m_loadedBundleDict.TryGetValue(assetBundleName, out AssetBundleItem assetBundleItem))
            {
                ++assetBundleItem.RefCount;
                callback?.Invoke(assetBundleItem.AssetBundle);
                return;
            }
            if (m_loadingDict.ContainsKey(assetBundleName))
                m_loadingDict[assetBundleName] += callback;
            else
            {
                m_loadingDict.Add(assetBundleName, callback);
                mono.StartCoroutine(LoadAsync(assetBundleName));
            }
        }

        IEnumerator LoadAsync(string assetBundleName)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, assetBundleName));
            yield return request;
            m_loadedBundleDict.Add(assetBundleName, new AssetBundleItem(request.assetBundle, 1));

            m_loadingDict[assetBundleName]?.Invoke(request.assetBundle);
            m_loadingDict.Remove(assetBundleName);
        }
    }
}

