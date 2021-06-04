using GamePlay;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetBundleExample : MonoBehaviour
{
    void Awake()
    {
        AssetBundleManager.Instance.Init(this);
        ResourceManager.Instance.Init(this);
    }

    private void Start()
    {
        GameObject gameObject = ResourceManager.Instance.LoadAsset<GameObject>("Assets/GameAssets/Prefabs/UI/Image.prefab");
        Debug.Log(gameObject.name);

        ResourceManager.Instance.LoadAssetAsync("Assets/GameAssets/Prefabs/Cube.prefab", (obj, userData) =>
        {
            Debug.Log(obj.name);
            Instantiate(obj, Vector3.zero, Quaternion.identity);
        });
    }

    void Update()
    {
        
    }
}
