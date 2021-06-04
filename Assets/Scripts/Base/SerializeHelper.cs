using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GamePlay
{
    public static class SerializeHelper
    {

        #region 序列化
        public static void JsonSerialize(string path, object instance)
        {
            try
            {
                string jsonStr = JsonUtility.ToJson(instance);
                using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(jsonStr);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("找不到路径：" + path + "， 错误：" + e);
            }
        }
        public static bool BinarySerialize(string path, System.Object obj)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fs, obj);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("无法完成Binary序列化：" + path + "，错误：" + e);
                if (File.Exists(path)) File.Delete(path);
            }
            return false;
        }
        #endregion

        #region 反序列化
        public static T ReadByte<T>(string path) where T : class
        {
            T t = null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T)bf.Deserialize(fs);
            }
            return t;
        }

        public static T ReadByte<T>(byte[] bytes) where T : class
        {
            T t = null;
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                BinaryFormatter bf = new BinaryFormatter();
                t = (T)bf.Deserialize(ms);
            }
            return t;
        }
        #endregion
    }
}
