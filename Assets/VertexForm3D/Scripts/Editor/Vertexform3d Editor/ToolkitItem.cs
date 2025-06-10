using System;
using UnityEditor;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public class ToolkitItem
    {
        public string title;
        public string description;
        public string additionalInfo;
        public Action action;
        public Texture2D image;

        // Path constants
        private const string CUSTOM_ICONS_PATH = "ToolkitIcons/";

        public ToolkitItem(string title, string description, string additionalInfo, Action action)
        {
            this.title = title;
            this.description = description;
            this.additionalInfo = additionalInfo;
            this.action = action;

            // Load the icon in this priority order:
            // 1. Try to load a custom icon from Resources/ToolkitIcons/ folder
            // 2. Try to use Unity built-in icon
            // 3. Fall back to a generated texture

            // Step 1: Try loading from Resources/ToolkitIcons/
            string iconName = title.Replace(" ", "");
            this.image = Resources.Load<Texture2D>(CUSTOM_ICONS_PATH + iconName);

            // Step 2: Try built-in Unity icons if the custom icon wasn't found
            if (this.image == null)
            {
                // Create a list of potential icon names to try
                string[] potentialIconNames = new string[] {
                    "GameObject Icon",
                    "GameObject",
                    "d_GameObject Icon",
                    "d_GameObject",
                    "Prefab Icon",
                    "d_Prefab Icon",
                    "ToolIcon",
                    "d_ToolIcon"
                };

                // Try each potential icon name
                foreach (string iconNameToTry in potentialIconNames)
                {
                    try
                    {
                        GUIContent content = EditorGUIUtility.IconContent(iconNameToTry);
                        this.image = content?.image as Texture2D;
                        if (this.image != null)
                            break;  // Found a working icon
                    }
                    catch (System.Exception)
                    {
                        // Continue trying other names
                        continue;
                    }
                }

                // If still no icon found, try direct texture load as last resort for built-in
                if (this.image == null)
                {
                    try
                    {
                        this.image = EditorGUIUtility.FindTexture("GameObject");
                    }
                    catch (System.Exception)
                    {
                        // Failed to load from built-in textures
                    }
                }
            }

            // Step 3: Generate a fallback colored texture if all else fails
            if (this.image == null)
            {
                GenerateFallbackTexture();
            }
        }

        private void GenerateFallbackTexture()
        {
            // Create a simple colored texture that varies based on title
            this.image = new Texture2D(64, 64);

            // Hash the title to get a somewhat stable color per item
            int hash = title.GetHashCode();
            float r = Mathf.Abs((hash & 0xFF) / 255f);         // First byte for red
            float g = Mathf.Abs(((hash >> 8) & 0xFF) / 255f);  // Second byte for green
            float b = Mathf.Abs(((hash >> 16) & 0xFF) / 255f); // Third byte for blue

            // Ensure color isn't too dark or too light
            r = Mathf.Clamp(r, 0.2f, 0.8f);
            g = Mathf.Clamp(g, 0.2f, 0.8f);
            b = Mathf.Clamp(b, 0.2f, 0.8f);

            Color color = new Color(r, g, b, 1.0f);

            // Fill the texture with the color
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            this.image.SetPixels(colors);
            this.image.Apply();
        }
    }
}