using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using UnityEngine.UI;
using static UnityEngine.AddressableAssets.Addressables;

class CheckUpdate:MonoBehaviour
{
    public Button updateBtn;
    public Button downloadBtn;
    public Button generateBtn;
    public Text text;

    private List<string> updateList;
    private GameObject go;

    private void Start()
    {
        updateBtn.onClick.AddListener(()=> {
            CheckUpdater();
        });
        downloadBtn.onClick.AddListener(() =>{
            DownLoad();
        });
        generateBtn.onClick.AddListener(() => {
            Addressables.InstantiateAsync("Assets/GameAssets/Prefabs/UI/Image.prefab").Completed += (handler) => {
                text.text = "加载成功" + handler.Result;
                handler.Result.transform.SetParent(transform);
                handler.Result.transform.localPosition = Vector3.one;
                handler.Result.transform.localScale = Vector3.one;
            };
            Addressables.InstantiateAsync("Assets/GameAssets/Prefabs/Cube.prefab").Completed += (handler) => {
                text.text = "加载成功" + handler.Result;
                handler.Result.transform.position = Vector3.zero;
                handler.Result.transform.localScale = Vector3.one;
            };
        });
    }
    private async void CheckUpdater()
    {
        AsyncOperationHandle<List<string>> updateHandle = Addressables.CheckForCatalogUpdates(false);
        await updateHandle.Task;
        if (updateHandle.Status == AsyncOperationStatus.Succeeded)
        {
            updateList = updateHandle.Result;
        }

        if (updateList != null && updateList.Count > 0)
        {
            text.text = "需要更新：" + updateList.Count;
            Debug.Log("需要更新：" + updateList.Count);
            downloadBtn.gameObject.SetActive(true);
        }
        else
        {
            text.text = "不需要更新";
            Debug.Log("不需要更新");
        }
        Addressables.Release(updateHandle);
    }

    private async void DownLoad()
    {
        AsyncOperationHandle<List<IResourceLocator>> updateHandler = Addressables.UpdateCatalogs(updateList, false);
        await updateHandler.Task;

        List<string> updateKeys = new List<string>();

        foreach (IResourceLocator locator in updateHandler.Result)
        {
            if (locator is ResourceLocationMap map)
            {
                foreach (var item in map.Locations)
                {
                    if (item.Value.Count == 0) continue;
                    string key = item.Key.ToString();
                    if (int.TryParse(key, out int resKey)) continue;

                    if (!updateKeys.Contains(key))
                        updateKeys.Add(key);
                }
            }
        }

        AsyncOperationHandle<long> downLoadSize = Addressables.GetDownloadSizeAsync(updateKeys);
        await downLoadSize.Task;
        text.text = "下载大小：" + downLoadSize.Result;
        Debug.Log("下载大小：" + downLoadSize.Result);

        AsyncOperationHandle downLoad = Addressables.DownloadDependenciesAsync(updateKeys, MergeMode.None);
        await downLoad.Task;

        text.text = "下载成功" + downLoadSize.Result + "  " + downLoad.Result + "   " + downLoad.Status + "  " + updateList[0] + "   " + updateKeys.Count+"   " + updateKeys[0];
        Debug.Log("下载成功");

        Addressables.Release(updateHandler);
        Addressables.Release(downLoad);
    }

    private async void UpdateAndDownLoad()
    {
        // 1. 检查更新
        AsyncOperationHandle<List<string>> updateHandle = Addressables.CheckForCatalogUpdates(false);
        await updateHandle.Task;
        if (updateHandle.Status == AsyncOperationStatus.Succeeded)
        {
            updateList = updateHandle.Result;
        }

        // 2.开始更新
        AsyncOperationHandle<List<IResourceLocator>> updateHandler = Addressables.UpdateCatalogs(updateList, false);
        await updateHandler.Task;

        // 3.获取更新资源的key
        List<string> updateKeys = new List<string>();
        foreach (IResourceLocator locator in updateHandler.Result)
        {
            if (locator is ResourceLocationMap map)
            {
                foreach (var item in map.Locations)
                {
                    if (item.Value.Count == 0) continue;
                    string key = item.Key.ToString();
                    if (int.TryParse(key, out int resKey)) continue;

                    if (!updateKeys.Contains(key))
                        updateKeys.Add(key);
                }
            }
        }

        // 4.判断下载资源大小
        AsyncOperationHandle<long> downLoadSize = Addressables.GetDownloadSizeAsync(updateKeys);
        await downLoadSize.Task;

        // 5.下载
        AsyncOperationHandle downLoad = Addressables.DownloadDependenciesAsync(updateKeys, MergeMode.None);
        await downLoad.Task;

        // 6.清除
        Addressables.Release(updateHandler);
        Addressables.Release(downLoad);
    }
}
