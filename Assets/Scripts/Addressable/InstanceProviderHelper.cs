using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Addressable
{
    public class InstanceProviderHelper : IInstanceProvider
    {
        Dictionary<int, Queue<GameObject>> m_PoolDict = new Dictionary<int, Queue<GameObject>>();
        Dictionary<int, AsyncOperationHandle<GameObject>> m_AssetHandler = new Dictionary<int, AsyncOperationHandle<GameObject>>();
        Dictionary<int, int> m_InstanceRefDict = new Dictionary<int, int>();

        /// <inheritdoc/>
        public GameObject ProvideInstance(ResourceManager resourceManager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters)
        {
            int guid = prefabHandle.Result.GetInstanceID();
            if (!m_PoolDict.TryGetValue(guid, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                m_PoolDict.Add(guid, queue);
                m_AssetHandler.Add(guid, prefabHandle);
            }
            GameObject instance;
            if (queue.Count == 0)
            {
                instance = instantiateParameters.Instantiate(prefabHandle.Result);
                m_InstanceRefDict.Add(instance.GetInstanceID(), guid);
            }
            else
            {
                instance = queue.Dequeue();
                instance.gameObject.SetActive(true);
                instance.transform.SetParent(instantiateParameters.Parent);
                if (instantiateParameters.InstantiateInWorldPosition)
                    instance.transform.position = instantiateParameters.Position;
                else
                    instance.transform.localPosition = instantiateParameters.Position;
                if (instantiateParameters.SetPositionRotation)
                    instance.transform.rotation = instantiateParameters.Rotation;
            }
            return instance;
        }

        /// <inheritdoc/>
        public void ReleaseInstance(ResourceManager resourceManager, GameObject instance)
        {
            int instancId = instance.GetInstanceID();
            if (m_InstanceRefDict.TryGetValue(instancId, out int guid))
            {
                if (m_PoolDict.TryGetValue(guid, out Queue<GameObject> queue))
                {
                    instance.SetActive(false);
                    queue.Enqueue(instance);
                }
            }
        }
    }
}
