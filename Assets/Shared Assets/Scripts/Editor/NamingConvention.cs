using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// NamingConvention Convention Cleanup Script
/// Currently set for:
///Material, Mesh, Prefab, ShaderGraphs, Textures
/// </summary>
public class NamingConvention : MonoBehaviour
{
    private static Dictionary<string, string> assetTypes;

    #region MENU_ITEMS
    [MenuItem("Assets/PascalCase/Rename Selected Object", false, 1)]
    public static void RenameSelected()
    {
        Init();
        Object obj = Selection.activeObject;
        RenameAsset(AssetDatabase.GetAssetPath(obj));
    }

    [MenuItem("Assets/PascalCase/Fix Letter Case", false, 3)]
    public static void FixLetterCase()
    {
        Init();
        Object obj = Selection.activeObject;

        string assetPath = AssetDatabase.GetAssetPath(obj);
        string fileExtension = Path.GetExtension(assetPath);
        if (assetTypes.ContainsKey(fileExtension))
        {
            string fixedLetterCase = obj.name.Substring(0, 1).ToUpper() + obj.name.Substring(1);

            AssetDatabase.RenameAsset(assetPath, fixedLetterCase);
        }
        else
        {
            Debug.LogWarning("File type not supported: " + obj.name);
        }
    }

    [MenuItem("Assets/PascalCase/Rename All Assets In Folder", false, 2)]
    public static void RenameAssetsInFolder()
    {
        Init();
        Object obj = Selection.activeObject;
        string path = AssetDatabase.GetAssetPath(obj);
        if (Directory.Exists(path))
        {
            string[] assets = AssetDatabase.FindAssets(
                "t:Texture t:Prefab t:Model t:Shader t:Material", new[] { path }); //Material, Mesh, Prefab, ShaderGraphs, Textures   
            foreach (var asset in assets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(asset);
                RenameAsset(assetPath);
            }

        }
        else
        {
            Debug.LogWarning("Select a folder.");
        }
    }

    [MenuItem("Assets/PascalCase/Texture/Albedo")]
    public static void SetAlbedo()
    {
        Init();
        Object obj = Selection.activeObject;
        SetTextureName(assetTypes["albedo"], AssetDatabase.GetAssetPath(obj), obj.name);
    }

    [MenuItem("Assets/PascalCase/Texture/Normal")]
    public static void SetNormal()
    {
        Init();
        Object obj = Selection.activeObject;
        SetTextureName(assetTypes["normal"], AssetDatabase.GetAssetPath(obj), obj.name);
    }

    [MenuItem("Assets/PascalCase/Texture/Mask")]
    public static void SetMask()
    {
        Init();
        Object obj = Selection.activeObject;
        SetTextureName(assetTypes["mask"], AssetDatabase.GetAssetPath(obj), obj.name);
    }

    [MenuItem("Assets/PascalCase/Texture/Emission Map")]
    public static void SetEmission()
    {
        Init();
        Object obj = Selection.activeObject;
        SetTextureName(assetTypes["emission"], AssetDatabase.GetAssetPath(obj), obj.name);
    }
    #endregion

    /// <summary>
    /// PascalCase names + post-fixes for their type
    /// </summary>
    /// <param name="_assetPath"></param>
    /// <param name="_assetName"></param>
    static void RenameAsset(string _assetPath)
    {
        string assetName = Path.GetFileNameWithoutExtension(_assetPath);
        string assetType = "";

        //Only rename certain assets (for now Meshes, Materials, Textures, ShaderGraphs, and Prefabs)
        string fileExtension = Path.GetExtension(_assetPath);
        if (assetTypes.ContainsKey(fileExtension))
        {

            //Set post-fix by asset type. Setting it here to aid in searching for possible name clashes
            //but will add it to the name after looking for the name clashes
            assetType = assetTypes[fileExtension];



            //Check if it already meets the criteria before changing the name(ie. already has correct postfix)
            if (!FollowsFormat(assetName, assetType))
            {
                //Textures have two post fixes (ie. Texture_T_A)
                if (assetType == "_T")
                {
                    var importer = (TextureImporter)AssetImporter.GetAtPath(_assetPath);
                    if (importer.textureType == TextureImporterType.NormalMap)
                    {
                        assetType += assetTypes["normal"];
                    }
                    else
                    {
                        if (assetName.Contains("_basecolor", StringComparison.OrdinalIgnoreCase) || assetName.Contains("_BaseMap", StringComparison.OrdinalIgnoreCase) ||
                            assetName.Contains("_Color", StringComparison.OrdinalIgnoreCase)
                            || assetName.Contains("_Albedo", StringComparison.OrdinalIgnoreCase))
                        {
                            assetType += assetTypes["albedo"];
                        }
                        else
                        {
                            //Masks and Emission maps will be set to Unknown
                            assetType += assetTypes["default"];
                        }

                    }

                }

                string pascalCaseName = ConvertToPascalCase(assetName);
                pascalCaseName = NameClash(pascalCaseName, assetType);

                string success = AssetDatabase.RenameAsset(_assetPath, pascalCaseName);
                if (success.Length > 0)
                    Debug.LogError(success);
            }//don't rename if it already follows format
        }
        else
        {
            Debug.LogWarning("Unsupported asset type " + assetName);

        }

    }

    private static bool FollowsFormat(string _assetName, string _assetType)
    {
        return Char.IsUpper(_assetName[0]) && _assetName.Contains(_assetType);
    }

    private static void SetTextureName(string _textureType, string _assetPath, string _assetName)
    {
        if (_assetPath.Contains(".png"))
        {

            if (_assetName.Contains(assetTypes["default"]))
            {
                var t = _assetName.Split("_")[..^1];//all elements except the last
                _assetName = string.Join("_", t);
                _assetName += _textureType;

                string success = AssetDatabase.RenameAsset(_assetPath, _assetName);
                if (success.Length > 0)
                    Debug.LogError(success);
            }
            else
            {
                string assetType = assetTypes[".png"] + _textureType;

                //already follows the desired format, no need to re do it
                if (FollowsFormat(_assetPath, assetType))
                    return;

                //Check if it already has *some* format
                if (_assetName.Contains("_T_")) //we can assume it already has some kind of texture format (_T_A, _T_N)
                {
                    //override the format to the new one being set 
                    var t = _assetName.Split("_")[..^1];//all elements except the last
                    _assetName = string.Join("_", t);
                    _assetName += _textureType;

                    string result = AssetDatabase.RenameAsset(_assetPath, _assetName);
                    if (result.Length > 0)
                        Debug.LogError(result);
                }
                else
                {
                    //if not, rename as usual
                    string pascalCaseName = ConvertToPascalCase(_assetName);
                    pascalCaseName = NameClash(pascalCaseName, assetType);

                    //Add post-fix
                    string result = AssetDatabase.RenameAsset(_assetPath, pascalCaseName);
                    if (result.Length > 0)
                        Debug.LogError(result);
                }

            }

        }
        else
        {
            Debug.LogWarning("Asset is not a texture.");
        }
    }

    private static void Init()
    {
        if (assetTypes == null)
        {
            assetTypes = new Dictionary<string, string>();
        }

        if (assetTypes.Count == 0)
        {
            assetTypes.Add(".mat", "_Mat");
            assetTypes.Add(".fbx", "_Mesh");
            assetTypes.Add(".prefab", "_Prefab");
            assetTypes.Add(".shadergraph", "_Graph");
            assetTypes.Add(".shadersubgraph", "_SubGraph");

            assetTypes.Add(".png", "_T"); //Texture
            assetTypes.Add("albedo", "_A"); //Albedo
            assetTypes.Add("normal", "_N"); //Normal
            assetTypes.Add("mask", "_MSAO"); //Mask
            assetTypes.Add("emission", "_E"); //Emission map
            assetTypes.Add("default", "_UNKNOWN"); //Default
        }
    }

    /// <summary>
    /// Format string to PascalCase 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static string ConvertToPascalCase(string str)
    {
        //Replace empty spaces with underscore (in case we have a name like "My Sphere")
        string result = str.Replace(" ", "_");

        //Split by the underscore we added and make the first letter upper case (MySphere)
        string[] array = result.Split("_", StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < array.Length; i++)
        {
            array[i] = array[i].Substring(0, 1).ToUpper() + array[i].Substring(1);
        }

        //Final result
        result = string.Join("", array);

        return result;
    }

    /// <summary>
    /// Handle the case where assets might receive the same name and insert an ID
    /// for example, LandscapeStone_01_Prefab  and LandscapeStone_02_Prefab
    /// </summary>
    /// <param name="pascalCaseName"></param>
    /// <param name="assetType"></param>
    /// <returns></returns>
    private static string NameClash(string pascalCaseName, string assetType)
    {
        var searchResults = AssetDatabase.FindAssets(pascalCaseName); //Checking across project
        if (searchResults.Length > 0)
        {
            //Split file extension
            int counter = 1;
            bool format = false;
            for (int i = 0; i < searchResults.Length; i++)
            {
                //Also check the asset type (ie. there could be a MySphere_Prefab and a MySphere_Graph and they are different assets)
                string clashPath = AssetDatabase.GUIDToAssetPath(searchResults[i]);
                if (clashPath.Split("/")[^1].Contains(assetType))
                {
                    format = true;
                    string finalName = pascalCaseName + "_" + counter.ToString("00") + assetType;
                    string result = AssetDatabase.RenameAsset(clashPath, finalName);
                    if (result.Length > 0)
                        Debug.LogError(result);
                    counter++;
                }
            }

            if (format)
                pascalCaseName += "_" + counter.ToString("00");
        }
        //Add the post fix after finishing the name clash
        pascalCaseName += assetType;
        return pascalCaseName;
    }

    private void OnDestroy()
    {
        assetTypes.Clear();
        assetTypes = null;
    }
}


//TODO: FIX USE CASE SHADERGRAPH