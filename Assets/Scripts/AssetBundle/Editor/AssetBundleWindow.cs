using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Runtime.Serialization.Formatters.Binary;

namespace GamePlay
{

    public class AssetBundleWindow : EditorWindow
    {
        [MenuItem("Tools/AssetBundle")]
        private static void Create()
        {
            EditorWindow window = GetWindow<AssetBundleWindow>();
            if (window == null)
                window = CreateWindow<AssetBundleWindow>();
            window.Focus();
        }

        private Vector2 scorll;
        private string buildPath;
        private string outputPath;
        private const string buildPathKey = "Build_Path";
        private const string outputPathKey = "Build_OutPutPath";

        private void OnEnable()
        {
            buildPath = PlayerPrefs.GetString(buildPathKey, buildPath);
            outputPath = PlayerPrefs.GetString(outputPathKey, buildPath);
        }
        private void OnGUI()
        {
            scorll = EditorGUILayout.BeginScrollView(scorll, "TextArea");
            {
                EditorGUILayout.LabelField("选择Build路径以及Output路径后点击Build按钮!", EditorStyles.helpBox);
                EditorGUILayout.LabelField("打包会打包Build路径下所有资源,按文件夹路径区分包名,即一个文件夹一个包!", EditorStyles.helpBox);
                EditorGUILayout.LabelField("获取资源时的路径: 从Assets/开始 (相对路径) 如:Assets/Prefabs/test.prefab", EditorStyles.helpBox);

                if (GUILayout.Button(string.Format("选择build的路径:  {0}", buildPath)))
                {
                    string path = EditorUtility.SaveFolderPanel("选择build的路径", buildPath, "");
                    if (!string.IsNullOrEmpty(path))
                        buildPath = path;
                    PlayerPrefs.SetString(buildPathKey, buildPath);
                }
                if (GUILayout.Button(string.Format("选择output的路径:  {0}", outputPath)))
                {
                    string path = EditorUtility.SaveFolderPanel("选择output的路径", outputPath, "");
                    if (!string.IsNullOrEmpty(path))
                        outputPath = path;
                    PlayerPrefs.SetString(outputPathKey, outputPath);
                }
                GUI.color = Color.green;
                if (GUILayout.Button("Build"))
                {
                    Build();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private void Build()
        {
            if (string.IsNullOrEmpty(buildPath) || string.IsNullOrEmpty(outputPath))
            {
                Debug.LogError("必须选择buildPath以及outputPath");
                return;
            }
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            DirectoryInfo directoryInfo = Directory.CreateDirectory(buildPath);
            BuildDirectory(directoryInfo);

            BuildAssetBundle();
            
            ClearABName();
            ClearManifestFile();

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("打包完成！", string.Format("输出路径：{0}", outputPath), "确定");
        }

        private void BuildDirectory(DirectoryInfo directoryInfo)
        {
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                string path = GetAssetPath(fileInfos[i].FullName);
                if (path.EndsWith(".meta") || path.EndsWith(".cs")) continue;
                EditorUtility.DisplayProgressBar("正在设置AssetBundleName", "Object: " + path, i * 1.0f / fileInfos.Length);
                SetABName(directoryInfo.Name, path);
            }
            DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
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

        private void SetABName(string name, string path)
        {
            AssetImporter assetImporter = AssetImporter.GetAtPath(path);
            if (assetImporter == null)
            {
                Debug.LogError("不存在该路径：" + path);
            }
            else
            {
                assetImporter.assetBundleName = name;
                assetImporter.assetBundleVariant = null;
            }
        }

        private void BuildAssetBundle()
        {
            string[] allBundleNames = AssetDatabase.GetAllAssetBundleNames();    //获取所有包名

            AssetBundleContainer container = new AssetBundleContainer();
            container.configList = new List<AssetBundleConfig>();
            for (int i = 0; i < allBundleNames.Length; i++)
            {
                string[] allBundlePaths = AssetDatabase.GetAssetPathsFromAssetBundle(allBundleNames[i]); //获取该包名下的所有路径

                for (int j = 0; j < allBundlePaths.Length; j++)
                {
                    if (allBundlePaths[j].EndsWith(".meta") || allBundlePaths[j].EndsWith(".cs")) continue;

                    //EditorUtility.DisplayProgressBar("正在设置打包", allBundlePaths[i] + " : " + allBundlePaths[j], j * 1.0f / allBundlePaths.Length);
                    string[] dependencies = AssetDatabase.GetDependencies(allBundlePaths[j]);
                    List<string> dependceList = new List<string>();
                    for (int k = 0; k < dependencies.Length; k++)
                    {
                        string tempPath = dependencies[k];
                        if (tempPath == allBundlePaths[j] || tempPath.EndsWith(".cs") || allBundlePaths[j].EndsWith(".meta")) continue;
                        string assetBundleName = AssetImporter.GetAtPath(tempPath).assetBundleName;
                        if (!dependceList.Contains(assetBundleName))
                        {
                            dependceList.Add(assetBundleName);
                        }
                    }
                    container.configList.Add(new AssetBundleConfig 
                    {
                        crc = CRC32.GetCRC32(allBundlePaths[j]),
                        path = allBundlePaths[j],
                        assetBundleName = allBundleNames[i],
                        assetName = Path.GetFileNameWithoutExtension(allBundlePaths[j]),
                        dependceAssetBundles = dependceList
                    });
                }
            }
            SerializeHelper.JsonSerialize(Path.Combine(outputPath, "config.json"), container);
            SerializeHelper.BinarySerialize(Path.Combine(outputPath, "config.byte"), container);

            //SerializeHelper.JsonSerialize(Path.Combine(buildPath, "config.json"), container);
            //SerializeHelper.BinarySerialize(Path.Combine(buildPath, "config.byte"), container);
            //SetABName("config", GetAssetPath(Path.Combine(buildPath, "config.byte")));

            //打包
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.ChunkBasedCompression,
            EditorUserBuildSettings.activeBuildTarget);
        }
        private void ClearABName()
        {
            string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
            for (int i = 0; i < oldABNames.Length; i++)
            {
                if (oldABNames[i].EndsWith(".meta") || oldABNames[i].EndsWith(".cs")) continue;
                AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
                EditorUtility.DisplayProgressBar("清除AB包", "名字：" + oldABNames[i], i * 1.0f / oldABNames.Length);
            }
            EditorUtility.ClearProgressBar();
        }

        private void ClearManifestFile()
        {
            DirectoryInfo directoryInfo = Directory.CreateDirectory(outputPath);
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            for (int i = 0; i < fileInfos.Length; i++)
            {
                string path = fileInfos[i].FullName;
                if (path.EndsWith(".manifest") || path.EndsWith(".manifest.meta") || Path.GetFileName(path).Contains(directoryInfo.Name))
                {
                    EditorUtility.DisplayProgressBar("清除Manifest文件", "路径：" + path, i * 1.0f / fileInfos.Length);
                    File.Delete(path);
                }
            }
            EditorUtility.ClearProgressBar();
        }
    }
}
