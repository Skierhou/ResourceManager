using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets;
using UnityEditor.Build.Pipeline.Utilities;

public class AddressableWindow : EditorWindow
{
    [MenuItem("Tools/Addressable")]
    private static void Create()
    {
        EditorWindow window = GetWindow<AddressableWindow>();
        if (window == null)
            window = CreateWindow<AddressableWindow>();
        window.Focus();
    }

    private Vector2 scorll;
    private string buildPath;
    private const string buildPathKey = "Build_Path";

    private AddressableAssetSettings setting;

    private void OnEnable()
    {
        buildPath = PlayerPrefs.GetString(buildPathKey, buildPath);
    }
    private void OnGUI()
    {
        scorll = EditorGUILayout.BeginScrollView(scorll, "TextArea");
        {
            EditorGUILayout.LabelField("选择Build路径后点击Build按钮!", EditorStyles.helpBox);
            EditorGUILayout.LabelField("打包会打包Build路径下所有资源,按文件夹路径区分包名,即一个文件夹一个包!", EditorStyles.helpBox);
            EditorGUILayout.LabelField("获取资源时的路径: 从Assets/开始 (相对路径) 如:Assets/Prefabs/test.prefab", EditorStyles.helpBox);

            setting = (AddressableAssetSettings)EditorGUILayout.ObjectField(setting, typeof(AddressableAssetSettings), false);
            if (GUILayout.Button(string.Format("选择build的路径:  {0}", buildPath)))
            {
                string path = EditorUtility.SaveFolderPanel("选择build的路径", buildPath, "");
                if (!string.IsNullOrEmpty(path))
                    buildPath = path;
                PlayerPrefs.SetString(buildPathKey, buildPath);
            }
            GUI.color = Color.green;
            if (GUILayout.Button("Build"))
            {
                NativeBuild();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private void NativeBuild()
    {
        var menu = new GenericMenu();
        var AddressablesPlayerBuildResultBuilderExists = false;
        for (int i = 0; i < setting.DataBuilders.Count; i++)
        {
            var m = setting.GetDataBuilder(i);
            if (m.CanBuildData<AddressablesPlayerBuildResult>())
            {
                AddressablesPlayerBuildResultBuilderExists = true;
                menu.AddItem(new GUIContent("New Build/" + m.Name), false, (index) => {
                    UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilderIndex = (int)index;
                    Build();
                }, i);
            }
        }

        if (!AddressablesPlayerBuildResultBuilderExists)
        {
            menu.AddDisabledItem(new GUIContent("New Build/No Build Script Available"));
        }

        menu.AddItem(new GUIContent("Update a Previous Build"), false, () => {
            var path = ContentUpdateScript.GetContentStateDataPath(true);
            if (!string.IsNullOrEmpty(path))
                ContentUpdateScript.BuildContentUpdate(AddressableAssetSettingsDefaultObject.Settings, path);
        });
        menu.AddItem(new GUIContent("Clean Build/All"), false, () => {
            AddressableAssetSettings.CleanPlayerContent(null);
            BuildCache.PurgeCache(true);
        });
        menu.AddItem(new GUIContent("Clean Build/Content Builders/All"), false, () => {
            AddressableAssetSettings.CleanPlayerContent(null);
        });
        for (int i = 0; i < setting.DataBuilders.Count; i++)
        {
            var m = setting.GetDataBuilder(i);
            menu.AddItem(new GUIContent("Clean Build/Content Builders/" + m.Name), false, (obj) => {
                AddressableAssetSettings.CleanPlayerContent(m);
            }, m);
        }
        menu.AddItem(new GUIContent("Clean Build/Build Pipeline Cache"), false, () => { BuildCache.PurgeCache(true); });
        menu.ShowAsContext();
    }

    private void Build()
    {
        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.LogError("必须选择buildPath");
            return;
        }
        //先清除一遍setting里设置的资源
        List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>();
        setting.GetAllAssets(entries, true);
        foreach (var item in entries)
        {
            if(item.ParentEntry != null)
                setting.RemoveAssetEntry(item.ParentEntry.guid);
            setting.RemoveAssetEntry(item.guid);
        }
        List<string> labels = setting.GetLabels();
        foreach (string label in labels)
        {
            setting.RemoveLabel(label);
        }

        DirectoryInfo directoryInfo = Directory.CreateDirectory(buildPath);
        BuildDirectory(directoryInfo);

        AddressableAssetSettings.BuildPlayerContent();

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Build完成！", "Build完成！", "确定");
    }

    private void BuildDirectory(DirectoryInfo directoryInfo)
    {
        DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();

        int length = 0;
        FileInfo[] fileInfos = directoryInfo.GetFiles();
        foreach (var fileInfo in fileInfos)
        {
            if (!fileInfo.FullName.EndsWith(".meta") && !fileInfo.FullName.EndsWith(".cs"))
                ++length;
        }
        
        if (length > 0)
        {
            string metaText = File.ReadAllText(string.Format("{0}{1}", directoryInfo.FullName, ".meta"));
            string guid = metaText.Split('\n')[1];
            guid = guid.Substring(6, guid.Length - 6);
            setting.RemoveAssetEntry(guid);

            string label = GetLabel(directoryInfo);
            setting.AddLabel(label);
            AddressableAssetEntry assetEntry = setting.CreateOrMoveEntry(guid, setting.DefaultGroup);
            assetEntry.SetLabel(label, true, true);
        }

        foreach (DirectoryInfo info in directoryInfos)
        {
            BuildDirectory(info);
        }
        EditorUtility.ClearProgressBar();
    }
    private string GetAssetPath(string path)
    {
        return Path.GetFullPath(path).Replace(Path.GetFullPath(Application.streamingAssetsPath + "/../../"), "");
    }

    private string GetLabel(DirectoryInfo directoryInfo)
    {
        string label = directoryInfo.FullName.Replace(Path.GetFullPath(buildPath), "").Replace('/', '-').Replace('\\', '-');
        if (string.IsNullOrEmpty(label))
            label = directoryInfo.Name;
        else if (label[0] == '-')
            label = label.Remove(0, 1);
        return label;
    }
    private void SetAddressableGroup(string name, string path)
    {
        Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out string guid, out long localId))
        {
            string label = GetLabel(path);
            setting.AddLabel(label);
            AddressableAssetEntry assetEntry = setting.FindAssetEntry(guid);
            if (assetEntry != null)
            {
                assetEntry.SetLabel(label, true, true);
            }
            Debug.Log(path + "    " + guid + "    " + localId + "    " + label);
        }
    }
    private string GetLabel(string path)
    {
        path = path.Replace(GetAssetPath(buildPath), "").Replace(Path.GetFileName(path), "");
        path = path.Substring(1, path.Length - 2).Replace('/', '-').Replace('\\', '-');
        return path;
    }

}
