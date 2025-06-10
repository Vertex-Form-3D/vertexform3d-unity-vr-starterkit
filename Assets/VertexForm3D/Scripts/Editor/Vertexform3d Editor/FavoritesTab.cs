using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VertexFormCore.Editor
{
    public class FavoritesTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();
        private List<string> favoriteItemTitles = new List<string>();
        private const string PrefsKey = "VertexFormToolkit_Favorites";

        public string Name => "FAVORITES";

        public string Description => "Your favorite tools and actions for quick access.";

        public bool HasSubTabs => false;

        /// <summary>
        /// Safely clears all items in the favorites list
        /// </summary>
        public void ClearItems()
        {
            items.Clear();
        }

        public List<SubTabCategory> GetSubTabCategories()
        {
            return new List<SubTabCategory>();
        }

        public List<ToolkitItem> GetItems()
        {
            // This will return the collected favorite items from other tabs
            return items;
        }

        public void InitializeItems()
        {
            // Clear current items before re-populating
            items.Clear();

            // Load favorite titles from EditorPrefs
            LoadFavorites();

            // We don't add any items directly here - they will be collected 
            // from other tabs when needed
        }

        /// <summary>
        /// Updates the items list with favorites from all provided tabs
        /// </summary>
        /// <param name="allTabs">List of all tab groups</param>
        public void UpdateFavoritesFromTabs(List<ITabGroup> allTabs)
        {
            items.Clear();

            // For each tab (except this Favorites tab)
            foreach (var tab in allTabs)
            {
                if (tab.Name == Name) continue; // Skip the favorites tab itself

                // Check each item in the tab
                foreach (var item in tab.GetItems())
                {
                    // If this item's title is in our favorites list, add it
                    if (favoriteItemTitles.Contains(item.title))
                    {
                        items.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Adds an item to favorites
        /// </summary>
        /// <param name="itemTitle">Title of the item to add</param>
        public void AddToFavorites(string itemTitle)
        {
            if (!favoriteItemTitles.Contains(itemTitle))
            {
                favoriteItemTitles.Add(itemTitle);
                SaveFavorites();
            }
        }

        /// <summary>
        /// Removes an item from favorites
        /// </summary>
        /// <param name="itemTitle">Title of the item to remove</param>
        public void RemoveFromFavorites(string itemTitle)
        {
            if (favoriteItemTitles.Contains(itemTitle))
            {
                // Remove from the titles list
                favoriteItemTitles.Remove(itemTitle);

                // Save changes to disk
                SaveFavorites();
            }
        }

        /// <summary>
        /// Checks if an item is in favorites
        /// </summary>
        /// <param name="itemTitle">Title of the item to check</param>
        /// <returns>True if the item is a favorite</returns>
        public bool IsFavorite(string itemTitle)
        {
            return favoriteItemTitles.Contains(itemTitle);
        }

        /// <summary>
        /// Saves favorites to EditorPrefs
        /// </summary>
        public void SaveFavorites()
        {
            string serializedFavorites = string.Join("|", favoriteItemTitles);
            EditorPrefs.SetString(PrefsKey, serializedFavorites);
        }

        /// <summary>
        /// Loads favorites from EditorPrefs
        /// </summary>
        public void LoadFavorites()
        {
            favoriteItemTitles.Clear();
            if (EditorPrefs.HasKey(PrefsKey))
            {
                string serializedFavorites = EditorPrefs.GetString(PrefsKey);
                if (!string.IsNullOrEmpty(serializedFavorites))
                {
                    favoriteItemTitles.AddRange(serializedFavorites.Split('|'));
                }
            }
        }
    }
}