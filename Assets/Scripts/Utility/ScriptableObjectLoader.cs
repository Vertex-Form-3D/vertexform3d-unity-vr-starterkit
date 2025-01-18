using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
#if UNITY_EDITOR

namespace VertextFormCore
{
    public class ScriptableObjectLoader
    {
        public static List<T> LoadScriptableObjects<T>(string folderPath) where T : ScriptableObject
        {
            List<T> loadedScriptableObjects = LoadAssetsFromFolder<T>(folderPath);
            return loadedScriptableObjects;
        }

        private static List<T> LoadAssetsFromFolder<T>(string folderPath) where T : ScriptableObject
        {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folderPath });

            foreach (var guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            return assets;
        }
    }
}
#endif


