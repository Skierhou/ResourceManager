using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class Test : MonoBehaviour
{
    [SerializeField]
    private string _entryName = "Assets/Prefabs/Cube.prefab";

    public AssetReference ar;

    private void Start()
    {
        var _ = StartAsync();

        ar.LoadAssetAsync<GameObject>().Completed += LoadFinish;
        ar.InstantiateAsync(Vector3.one, Quaternion.identity);
    }

    private void LoadFinish(AsyncOperationHandle<GameObject> loadHandle)
    {
        Debug.Log("LoadFinish");
        if (loadHandle.IsDone && loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            //这里Result是预制体
            Debug.Log(loadHandle.Result);
            Addressables.Release(loadHandle);
        }
    }

    private async Task StartAsync()
    {
        var instance = await Addressables.InstantiateAsync(_entryName).Task;
        Addressables.ReleaseInstance(instance);
        instance = await Addressables.InstantiateAsync(_entryName).Task;
        Addressables.ReleaseInstance(instance);
    }
}
