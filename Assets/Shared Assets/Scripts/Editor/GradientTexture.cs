using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GradientTexture", menuName = "Gradient Texture", order = 310)]
public class GradientTexture : ScriptableObject
{
    [Delayed]
    public int width = 64;
    
    public List<Gradient> gradients = new List<Gradient>();
    private Texture2D _texture;
    
    private void OnEnable()
    {
        if(_texture == null && gradients.Count > 0)
            Generate();
    }

    [ContextMenu("Generate")]
    private void Generate()
    {
        // create texture that is width x gradients.Count
        // for each gradient, draw a line from left to right with the gradient
        // save to disk
        
        if(_texture != null)
            DestroyImmediate(_texture);
        _texture = new Texture2D(width, gradients.Count)
        {
            name = name
        };
        
        for (int i = 0; i < gradients.Count; i++)
        {
            var gradient = gradients[i];
            for (int x = 0; x < width; x++)
            {
                var color = gradient.Evaluate((float)x / width);
                _texture.SetPixel(x, i, color);
            }
        }
        _texture.Apply();
        
        #if UNITY_EDITOR
        // delayed call to save the icon
        UnityEditor.EditorApplication.delayCall += SaveIcon;
        #endif
    }

    private void OnValidate()
    {
        if(gradients.Count > 0)
            Generate();
    }

    [ContextMenu("Save")]
    private void SaveToDisk()
    {
#if UNITY_EDITOR
        // save to disk the texture using the AssetDatabase API
        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        path = path.Replace(".asset", ".png");
        var bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

#endif
    }
    
    #if UNITY_EDITOR

    private void SaveIcon()
    {
        // use _texture as the icon for this object
        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        
        // add as sub asset or replace existing
        var icon = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        
        if (icon == null)
        {
            UnityEditor.AssetDatabase.AddObjectToAsset(_texture, this);
        }
        else
        {
            UnityEditor.EditorUtility.CopySerialized(_texture, icon);
        }
        
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
    
    #endif
    
}
