using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePlay
{
    public partial class ResourceManager : Singleton<ResourceManager>,IResourceManager
    {
        private Dictionary<uint, ResourceItem> m_loadedDict;

        private Dictionary<uint, LoadingItem> m_loadingDict;
        private Dictionary<uint, List<LoadSuccessCallback>> m_callBackDict;

        public void Init(MonoBehaviour mono)
        {
            m_loadedDict = new Dictionary<uint, ResourceItem>();
            m_loadingDict = new Dictionary<uint, LoadingItem>();
            m_callBackDict = new Dictionary<uint, List<LoadSuccessCallback>>();
            mono.StartCoroutine(LoadAsync());
        }

        IEnumerator LoadAsync()
        {
            while (true)
            {
                int loadCount = 0; int loadMax = 0;
                void LoadCallback(AssetBundle assetbundle)
                {
                    ++loadCount;
                }
                LoadingItem loadingItem = null;
                foreach (var item in m_loadingDict.Values)
                {
                    if (loadingItem == null || loadingItem.priority < item.priority)
                        loadingItem = item;
                }
                if (loadingItem != null)
                {
                    loadCount = 0; loadMax = loadingItem.config.dependceAssetBundles.Count + 1;
                    AssetBundleManager.Instance.LoadAssetBundleAsync(loadingItem.config.assetBundleName, LoadCallback);
                    foreach (string assetBundleName in loadingItem.config.dependceAssetBundles)
                    {
                        AssetBundleManager.Instance.LoadAssetBundleAsync(assetBundleName, LoadCallback);
                    }
                    while (loadCount < loadMax)
                    { 
                        yield return null;
                    }
                    AssetBundle assetBundle = AssetBundleManager.Instance.LoadAssetBundle(loadingItem.config.assetBundleName);
                    if (assetBundle != null)
                    {
                        AssetBundleRequest request = assetBundle.LoadAssetAsync(loadingItem.config.assetName);
                        yield return request;
                        if (request.isDone)
                        {
                            OnLoadSuccess(loadingItem.crc, request.asset, loadingItem.userData);
                        }
                        AssetBundleManager.Instance.UnLoadAssetBundle(loadingItem.config.assetBundleName);
                    }
                }
                yield return null;
            }
        }

        private void OnLoadSuccess(uint crc, UnityEngine.Object obj, object userData)
        {
            if (!m_loadedDict.TryGetValue(crc, out ResourceItem resourceItem))
            {
                resourceItem = new ResourceItem(crc, obj);
                m_loadedDict.Add(crc, resourceItem);
            }
            if (m_callBackDict.ContainsKey(crc))
            {
                foreach (LoadSuccessCallback item in m_callBackDict[crc])
                {
                    ++resourceItem.RefCount;
                    item.callback?.Invoke(obj, userData);
                }
                m_callBackDict.Remove(crc);
            }
        }

        public T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            uint crc = CRC32.GetCRC32(path);
            if (m_loadedDict.TryGetValue(crc, out ResourceItem resourceItem))
            {
                ++resourceItem.RefCount;
                return (T)resourceItem.Asset;
            }
            if (!AssetBundleManager.Instance.TryGetConfig(crc, out AssetBundleConfig config))
            {
                Debug.LogError(string.Format("Not Found Asset Config : {0}", path));
                return null;
            }
            AssetBundle assetBundle = AssetBundleManager.Instance.LoadAssetBundle(config.assetBundleName);
            foreach (string assetBundleName in config.dependceAssetBundles)
            {
                AssetBundleManager.Instance.LoadAssetBundle(assetBundleName);
            }
            if (assetBundle != null && assetBundle.Contains(config.assetName))
            {
                OnLoadSuccess(crc, assetBundle.LoadAsset(config.assetName), null);
                return LoadAsset<T>(path);
            }
            return null;
        }

        public void LoadAssetAsync(string path, Action<UnityEngine.Object, object> onLoaded, int priority = 0, object userData = null)
        {
            uint crc = CRC32.GetCRC32(path);
            if (m_loadedDict.TryGetValue(crc, out ResourceItem resourceItem))
            {
                ++resourceItem.RefCount;
                onLoaded?.Invoke(resourceItem.Asset, userData);
            }
            else
            {
                if (m_callBackDict.ContainsKey(crc))
                {
                    m_callBackDict[crc].Add(new LoadSuccessCallback(onLoaded, userData));
                    return;
                }
                if (!AssetBundleManager.Instance.TryGetConfig(crc, out AssetBundleConfig config))
                {
                    Debug.LogError(string.Format("Not Found Asset Config : {0}", path));
                    return;
                }
                m_loadingDict.Add(crc, new LoadingItem(crc, config, priority, userData));
                m_callBackDict.Add(crc, new List<LoadSuccessCallback> { new LoadSuccessCallback(onLoaded, userData) });
            }
        }

        public void UnLoadAsset(string path, bool isDestroy = false)
        {
            uint crc = CRC32.GetCRC32(path);
            if (m_loadedDict.TryGetValue(crc, out ResourceItem resourceItem))
            {
                --resourceItem.RefCount;
                if (isDestroy || resourceItem.RefCount <= 0)
                {
                    if (!AssetBundleManager.Instance.TryGetConfig(crc, out AssetBundleConfig config))
                    {
                        Debug.LogError(string.Format("Not Found Asset Config : {0}", path));
                        return;
                    }
                    AssetBundleManager.Instance.UnLoadAssetBundle(config.assetBundleName);
                    foreach (string assetBundleName in config.dependceAssetBundles)
                    {
                        AssetBundleManager.Instance.UnLoadAssetBundle(assetBundleName);
                    }
                    m_loadedDict.Remove(crc);
                }
            }
        }
    }
}
