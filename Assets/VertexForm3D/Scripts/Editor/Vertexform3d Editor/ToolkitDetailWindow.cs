using UnityEngine;
using UnityEditor;

namespace VertexFormCore.Editor
{
    public class ToolkitDetailWindow : EditorWindow
    {
        public ToolkitItem item;
        public EditorWindow parentWindow;
        private Vector2 contentScrollPos;
        private FavoritesTab favoritesTab;
        private VertexFormToolkit parentToolkitWindow;

        public static void Open(ToolkitItem item, EditorWindow parentWindow, FavoritesTab favoritesTab)
        {
            ToolkitDetailWindow window = GetWindow<ToolkitDetailWindow>("Toolkit Item Details");
            window.item = item;
            window.parentWindow = parentWindow;
            window.favoritesTab = favoritesTab;
            window.parentToolkitWindow = parentWindow as VertexFormToolkit;
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnGUI()
        {
            if (item == null)
            {
                Close();
                return;
            }

            // Draw Banner
            GUILayout.Space(5);
            Texture2D banner = Resources.Load<Texture2D>("VF3DBannerEditor");
            if (banner != null)
            {
                float bannerWidth = Mathf.Min(banner.width, position.width - 10);
                float bannerHeight = (bannerWidth / banner.width) * banner.height;
                GUILayout.Label(banner, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight), GUILayout.ExpandWidth(true));
            }

            // Back Button
            GUILayout.Space(10);
            if (GUILayout.Button("< Back", GUILayout.Width(100), GUILayout.Height(25)))
            {
                Close();
            }

            GUILayout.Space(20);

            // Begin scrollable content area that includes everything below the Back button
            contentScrollPos = GUILayout.BeginScrollView(contentScrollPos, false, true, GUILayout.ExpandHeight(true));

            // Detail View
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(item.title, titleStyle);

            // Image
            Rect imageRect = GUILayoutUtility.GetRect(position.width - 20, 200);
            EditorGUI.DrawRect(imageRect, EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f));
            GUI.DrawTexture(imageRect, item.image, ScaleMode.ScaleToFit);

            // Favorite toggle button
            bool isFavorite = favoritesTab.IsFavorite(item.title);
            GUILayout.BeginHorizontal();

            // Make image clickable
            if (GUI.Button(imageRect, GUIContent.none, GUIStyle.none))
            {
                item.action?.Invoke();
                Close();
            }

            // Add to favorites button
            string favoriteButtonText = isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";
            if (GUILayout.Button(favoriteButtonText, GUILayout.Height(25)))
            {
                if (isFavorite)
                {
                    favoritesTab.RemoveFromFavorites(item.title);
                    favoritesTab.SaveFavorites();

                    // Update the favorites tab with items from all tabs
                    if (parentToolkitWindow != null)
                    {
                        parentToolkitWindow.UpdateFavoritesTab();
                        parentToolkitWindow.Repaint(); // Ensure UI updates properly
                    }
                }
                else
                {
                    favoritesTab.AddToFavorites(item.title);
                    favoritesTab.SaveFavorites();

                    // Update the favorites tab with items from all tabs
                    if (parentToolkitWindow != null)
                    {
                        parentToolkitWindow.UpdateFavoritesTab();
                        parentToolkitWindow.Repaint(); // Ensure UI updates properly
                    }
                }

                // Close the window AFTER the list has been updated
                EditorApplication.delayCall += () => Close();
            }

            GUILayout.EndHorizontal();

            // Full Description and Additional Info
            GUILayout.Space(10);
            GUIStyle descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            descriptionStyle.richText = true;

            // Main description
            EditorGUILayout.LabelField(item.description, descriptionStyle);

            // Display additional info if available
            if (!string.IsNullOrEmpty(item.additionalInfo))
            {
                GUILayout.Space(15);
                EditorGUILayout.LabelField("Instructions:", EditorStyles.boldLabel);
                GUILayout.Space(5);
                EditorGUILayout.LabelField(item.additionalInfo, descriptionStyle);
            }

            EditorGUILayout.EndVertical();

            // End the scroll view
            GUILayout.EndScrollView();
        }
    }
}