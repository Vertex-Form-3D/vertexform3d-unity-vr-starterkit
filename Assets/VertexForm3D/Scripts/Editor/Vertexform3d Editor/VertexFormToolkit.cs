using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace VertexFormCore.Editor
{
    public class VertexFormToolkit : EditorWindow
    {
        // Window state
        private Vector2 scrollPosition;
        private Vector2 favoritesScrollPosition;
        private Texture2D banner;
        private int selectedTab = 0;
        private string searchFilter = "";

        // UI Layout settings
        private const float tileWidth = 180f;
        private const float tileHeight = 180f;
        private const float spacing = 10f;
        private float tabHeight = 25f;

        private const float tabPadding = 10f; // Padding for the tab content
        private const float minTabWidth = 90f; // Minimum width for a tab
        private Dictionary<int, float> tabWidths = new Dictionary<int, float>(); // Store calculated tab widths

        // Store the selected item for detail view
        private ToolkitItem selectedItem;

        // Tab management
        private List<ITabGroup> tabGroups = new List<ITabGroup>();
        private FavoritesTab favoritesTab;

        // Sub Tab management
        private int selectedSubTab = 0;
        private Dictionary<int, int> tabToSelectedSubTab = new Dictionary<int, int>();
        private float subTabHeight = 25f;
        private Color subTabColor = new Color(0.2f, 0.2f, 0.2f);

        /*[MenuItem("VertexForm3D SDK/Open VertexForm Toolkit", false, 0)]
        public static void ShowWindow()
        {
            VertexFormToolkit window = GetWindow<VertexFormToolkit>("VertexForm 3D");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }*/

        public void SelectTab(string tabName)
        {
            for (int i = 0; i < tabGroups.Count; i++)
            {
                if (tabGroups[i].Name == tabName)
                {
                    selectedTab = i;
                    break;
                }
            }
        }

        public void SelectItemByTitle(string title)
        {
            foreach (var tabGroup in tabGroups)
            {
                foreach (var item in tabGroup.GetItems())
                {
                    if (item.title == title)
                    {
                        selectedItem = item;
                        OpenDetailWindow(item);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Selects a specific subtab within a main tab by name
        /// </summary>
        /// <param name="mainTabName">Name of the main tab</param>
        /// <param name="subTabName">Name of the subtab to select</param>
        public void SelectSubTabByName(string mainTabName, string subTabName)
        {
            // First select the main tab
            SelectTab(mainTabName);

            // Use delayCall to ensure the main tab is fully loaded before selecting the subtab
            EditorApplication.delayCall += () =>
            {
                if (selectedTab >= 0 && selectedTab < tabGroups.Count && tabGroups[selectedTab].HasSubTabs)
                {
                    var subCategories = tabGroups[selectedTab].GetSubTabCategories();
                    for (int i = 0; i < subCategories.Count; i++)
                    {
                        if (subCategories[i].Name == subTabName)
                        {
                            selectedSubTab = i;
                            tabToSelectedSubTab[selectedTab] = i;
                            Repaint();
                            break;
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Selects a subtab by its index within the current main tab
        /// </summary>
        /// <param name="subTabIndex">Index of the subtab to select</param>
        public void SelectSubTabByIndex(int subTabIndex)
        {
            if (selectedTab >= 0 && selectedTab < tabGroups.Count && tabGroups[selectedTab].HasSubTabs)
            {
                var subCategories = tabGroups[selectedTab].GetSubTabCategories();
                if (subTabIndex >= 0 && subTabIndex < subCategories.Count)
                {
                    selectedSubTab = subTabIndex;
                    tabToSelectedSubTab[selectedTab] = subTabIndex;
                    Repaint();
                }
            }
        }

        public void UpdateFavoritesTab()
        {
            // Update favorites with items from other tabs
            if (favoritesTab != null)
            {
                // Use the public method to update favorites
                favoritesTab.UpdateFavoritesFromTabs(tabGroups);

                // Force repaint to ensure UI updates correctly
                Repaint();
            }
        }

        private void OnEnable()
        {
            banner = Resources.Load<Texture2D>("VF3DBannerEditor");

            InitializeTabGroups();

            // Set up toolbar button style for two-line tabs
            /*if (EditorStyles.toolbarButton != null)
            {
                var toolbarButtonStyle = EditorStyles.toolbarButton;
                toolbarButtonStyle.wordWrap = true;
                toolbarButtonStyle.alignment = TextAnchor.MiddleCenter;
                toolbarButtonStyle.fixedHeight = tabHeight * 1.5f;
            }*/
        }

        private void OnFocus()
        {
            // Refresh banner if needed
            if (banner == null)
            {
                banner = Resources.Load<Texture2D>("VF3DBannerEditor");
            }
        }

        private void InitializeTabGroups()
        {
            tabGroups.Clear();

            // Create favorites tab first (it needs special handling)
            favoritesTab = new FavoritesTab();
            tabGroups.Add(favoritesTab);

            // Add all other tabs with new organization

            tabGroups.Add(new XRGameObjectTab()); // New consolidated tab
            tabGroups.Add(new SceneSwitchingTab()); // New consolidated tab
            tabGroups.Add(new PlayerPositionTab());  // Renamed in the class definition
            tabGroups.Add(new UIElementsTab());     // New consolidated tab
            tabGroups.Add(new PresentationTab());     // New consolidated tab
            tabGroups.Add(new DevToolsTab());

            // Initialize all tabs
            foreach (var tabGroup in tabGroups)
            {
                tabGroup.InitializeItems();
            }

            // Update favorites with items from other tabs
            favoritesTab.UpdateFavoritesFromTabs(tabGroups);
        }

        private void OnGUI()
        {
            // First, draw the banner using layout
            GUILayout.Space(5);
            if (banner != null)
            {
                float bannerWidth = Mathf.Min(banner.width, position.width - 100);
                float bannerHeight = ((bannerWidth / banner.width) * banner.height) / 1.2f;
                GUILayout.Label(banner, GUILayout.Width(bannerWidth), GUILayout.Height(bannerHeight), GUILayout.ExpandWidth(true));
            }
            else
            {
                EditorGUILayout.HelpBox("Banner image not found. Make sure 'VF3DBannerEditor' is inside the Resources folder.", MessageType.Warning);
            }

            GUIStyle boldStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold, fontSize = 25 };
            GUILayout.Label("CREATOR TOOLKIT", boldStyle);
            // Add space after banner
            GUILayout.Space(10);

            // Reserve space for tabs using GUILayout
            Rect tabAreaRect = GUILayoutUtility.GetRect(position.width, tabHeight * 1.5f);

            // Calculate available width for tabs
            float availableWidth = position.width - 10;
            float tabWidth = availableWidth / tabGroups.Count;

            // Calculate dynamic font size based on tab width
            // Start with default size at full width and scale down
            float defaultFontSize = 12f; // Default size
            float minFontSize = 4f;      // Minimum font size (changed from 8 to 4)
            float fullWidthReference = 120f; // Reference width for normal font size

            // Calculate font size based on tab width with limits
            float fontScaleFactor = Mathf.Clamp(tabWidth / fullWidthReference, 0.5f, 1f);
            int fontSize = Mathf.RoundToInt(Mathf.Lerp(minFontSize, defaultFontSize, fontScaleFactor));

            // Create base tab style for both selected and non-selected tabs
            GUIStyle baseTabStyle = new GUIStyle(EditorStyles.toolbarButton);
            baseTabStyle.fontStyle = FontStyle.Bold;
            baseTabStyle.fixedHeight = tabAreaRect.height;
            baseTabStyle.wordWrap = true;
            baseTabStyle.padding = new RectOffset(5, 5, 5, 5);
            baseTabStyle.alignment = TextAnchor.MiddleCenter;
            baseTabStyle.fontSize = fontSize; // Apply dynamic font size

            // Draw tabs
            for (int i = 0; i < tabGroups.Count; i++)
            {
                Rect tabRect = new Rect(
                    tabAreaRect.x + (i * tabWidth),
                    tabAreaRect.y,
                    tabWidth,
                    tabAreaRect.height);

                // Create a special style for the selected tab
                GUIStyle tabStyle = new GUIStyle(baseTabStyle);

                if (i == selectedTab)
                {
                    Color bgColor = EditorGUIUtility.isProSkin ?
                        new Color(0.3f, 0.3f, 0.3f) :
                        new Color(0.8f, 0.8f, 0.8f);

                    Texture2D bgTexture = new Texture2D(1, 1);
                    bgTexture.SetPixel(0, 0, bgColor);
                    bgTexture.Apply();

                    tabStyle.normal.background = bgTexture;
                    tabStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }

                // Toggle group using the custom style
                bool toggled = GUI.Toggle(tabRect, i == selectedTab, tabGroups[i].Name, tabStyle);
                if (toggled && i != selectedTab)
                {
                    selectedTab = i;
                    // Initialize the selected sub-tab if not already set
                    if (!tabToSelectedSubTab.ContainsKey(selectedTab))
                    {
                        tabToSelectedSubTab[selectedTab] = 0;
                    }
                    else
                    {
                        selectedSubTab = tabToSelectedSubTab[selectedTab];
                    }
                    Repaint();
                }
            }

            // Continue with the rest of the UI
            GUILayout.Space(5);

            // Draw tab description
            if (selectedTab >= 0 && selectedTab < tabGroups.Count)
            {
                GUIStyle descriptionStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                descriptionStyle.padding = new RectOffset(10, 10, 10, 10);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(tabGroups[selectedTab].Description, descriptionStyle);
                EditorGUILayout.EndVertical();
            }

            // Draw Sub-tabs if available
            if (selectedTab >= 0 && selectedTab < tabGroups.Count && tabGroups[selectedTab].HasSubTabs)
            {
                GUILayout.Space(5);
                DrawSubTabs();
                GUILayout.Space(5);
            }
            else
            {
                GUILayout.Space(10);
            }

            // Search/filter bar
            /*{
                GUILayout.BeginHorizontal();
                GUILayout.Label("Search:", GUILayout.Width(50));
                string newSearchFilter = GUILayout.TextField(searchFilter, GUILayout.Height(searchBarHeight));
                if (newSearchFilter != searchFilter)
                {
                    searchFilter = newSearchFilter;
                    // Try to switch to a tab with search results when filter changes
                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        TrySwitchToTabWithSearchResults(searchFilter);
                    }
                }
                if (GUILayout.Button("Clear", GUILayout.Width(60), GUILayout.Height(searchBarHeight)))
                {
                    searchFilter = "";
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);*/

            // Begin the scroll view for grid items
            if (selectedTab == 0) // Favorites tab
            {
                favoritesScrollPosition = GUILayout.BeginScrollView(favoritesScrollPosition, false, true);
                DrawFavoritesTab();
                GUILayout.EndScrollView();
            }
            else // Regular category tabs
            {
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

                if (selectedTab >= 0 && selectedTab < tabGroups.Count)
                {
                    List<ToolkitItem> items;

                    // Get items based on whether we have sub-tabs and which sub-tab is selected
                    if (tabGroups[selectedTab].HasSubTabs)
                    {
                        var subCategories = tabGroups[selectedTab].GetSubTabCategories();
                        if (selectedSubTab >= 0 && selectedSubTab < subCategories.Count)
                        {
                            items = subCategories[selectedSubTab].Items;
                        }
                        else
                        {
                            items = new List<ToolkitItem>();
                        }
                    }
                    else
                    {
                        items = tabGroups[selectedTab].GetItems();
                    }

                    // Apply search filter if any
                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        string lowerFilter = searchFilter.ToLower();
                        items = items.Where(item =>
                            item.title.ToLower().Contains(lowerFilter) ||
                            item.description.ToLower().Contains(lowerFilter)).ToList();
                    }

                    DrawResponsiveGrid(items);
                }

                GUILayout.EndScrollView();
            }
        }

        private void DrawSubTabs()
        {
            var currentTab = tabGroups[selectedTab];
            var subCategories = currentTab.GetSubTabCategories();

            if (subCategories == null || subCategories.Count == 0)
                return;

            // Make sure the selected sub-tab is valid
            if (!tabToSelectedSubTab.ContainsKey(selectedTab))
            {
                tabToSelectedSubTab[selectedTab] = 0;
                selectedSubTab = 0;
            }
            else
            {
                selectedSubTab = tabToSelectedSubTab[selectedTab];
                if (selectedSubTab >= subCategories.Count)
                {
                    selectedSubTab = 0;
                    tabToSelectedSubTab[selectedTab] = 0;
                }
            }

            // Calculate available width for subtabs (same as main tabs)
            float availableWidth = position.width - 10;
            float subTabWidth = availableWidth / subCategories.Count;

            // Calculate dynamic font size based on tab width (same logic as main tabs)
            float defaultFontSize = 12f; // Default size
            float minFontSize = 4f;      // Minimum font size
            float fullWidthReference = 120f; // Reference width for normal font size
            float fontScaleFactor = Mathf.Clamp(subTabWidth / fullWidthReference, 0.5f, 1f);
            int fontSize = Mathf.RoundToInt(Mathf.Lerp(minFontSize, defaultFontSize, fontScaleFactor));

            // Draw subtab area
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Draw subtitle
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.boldLabel);
            subtitleStyle.alignment = TextAnchor.MiddleLeft;
            EditorGUILayout.LabelField("Sub-Categories:", subtitleStyle);

            // Reserve space for subtabs using GUILayout (similar to main tabs)
            Rect subTabAreaRect = GUILayoutUtility.GetRect(position.width - 20, subTabHeight * 1.5f);

            // Create base subtab style mirroring the main tab base style
            GUIStyle baseSubTabStyle = new GUIStyle(EditorStyles.toolbarButton);
            baseSubTabStyle.fontStyle = FontStyle.Bold;
            baseSubTabStyle.fixedHeight = subTabAreaRect.height;
            baseSubTabStyle.wordWrap = true;
            baseSubTabStyle.padding = new RectOffset(5, 5, 5, 5);
            baseSubTabStyle.alignment = TextAnchor.MiddleCenter;
            baseSubTabStyle.fontSize = fontSize; // Apply dynamic font size

            // Draw subtabs
            for (int i = 0; i < subCategories.Count; i++)
            {
                Rect subTabRect = new Rect(
                    subTabAreaRect.x + (i * subTabWidth),
                    subTabAreaRect.y,
                    subTabWidth,
                    subTabAreaRect.height);

                // Create a special style for the selected subtab (identical to main tabs)
                GUIStyle subTabStyle = new GUIStyle(baseSubTabStyle);

                if (i == selectedSubTab)
                {
                    Color bgColor = EditorGUIUtility.isProSkin ?
                        new Color(0.3f, 0.3f, 0.3f) :
                        new Color(0.8f, 0.8f, 0.8f);

                    Texture2D bgTexture = new Texture2D(1, 1);
                    bgTexture.SetPixel(0, 0, bgColor);
                    bgTexture.Apply();

                    subTabStyle.normal.background = bgTexture;
                    subTabStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
                }

                // Toggle group using the custom style
                bool toggled = GUI.Toggle(subTabRect, i == selectedSubTab, subCategories[i].Name, subTabStyle);
                if (toggled && i != selectedSubTab)
                {
                    selectedSubTab = i;
                    tabToSelectedSubTab[selectedTab] = i;
                    Repaint();
                }
            }

            // Draw sub-tab description (matching main tab description spacing and style)
            if (selectedSubTab >= 0 && selectedSubTab < subCategories.Count &&
                !string.IsNullOrEmpty(subCategories[selectedSubTab].Description))
            {
                GUILayout.Space(5); // Same spacing as main tab description

                GUIStyle subDescStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
                subDescStyle.padding = new RectOffset(10, 10, 10, 10);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
                EditorGUILayout.LabelField(subCategories[selectedSubTab].Description, subDescStyle);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void TrySwitchToTabWithSearchResults(string searchText)
        {
            // Skip if we're already searching in a tab that has results
            string lowerFilter = searchText.ToLower();
            bool hasResultsInCurrentTab = false;

            // Check current tab and current subtab first
            if (selectedTab >= 0 && selectedTab < tabGroups.Count)
            {
                var currentTab = tabGroups[selectedTab];

                // If we have subtabs, check current subtab first
                if (currentTab.HasSubTabs && tabToSelectedSubTab.ContainsKey(selectedTab))
                {
                    int currentSubTab = tabToSelectedSubTab[selectedTab];
                    var subCategories = currentTab.GetSubTabCategories();

                    if (currentSubTab >= 0 && currentSubTab < subCategories.Count)
                    {
                        // Check if current subtab has results
                        var subTabItems = subCategories[currentSubTab].Items;
                        hasResultsInCurrentTab = subTabItems.Any(item =>
                            item.title.ToLower().Contains(lowerFilter) ||
                            item.description.ToLower().Contains(lowerFilter));
                    }
                }

                // If no results in current subtab or no subtabs, check entire tab
                if (!hasResultsInCurrentTab)
                {
                    var allTabItems = currentTab.GetItems();
                    hasResultsInCurrentTab = allTabItems.Any(item =>
                        item.title.ToLower().Contains(lowerFilter) ||
                        item.description.ToLower().Contains(lowerFilter));
                }
            }

            // If current tab has results, stay here but try to switch to appropriate subtab
            if (hasResultsInCurrentTab && tabGroups[selectedTab].HasSubTabs)
            {
                var currentTab = tabGroups[selectedTab];
                var subCategories = currentTab.GetSubTabCategories();

                // Find the first subtab with matching items
                for (int i = 0; i < subCategories.Count; i++)
                {
                    var subTabItems = subCategories[i].Items;
                    bool hasResultsInSubTab = subTabItems.Any(item =>
                        item.title.ToLower().Contains(lowerFilter) ||
                        item.description.ToLower().Contains(lowerFilter));

                    if (hasResultsInSubTab)
                    {
                        // Found a subtab with results, switch to it
                        selectedSubTab = i;
                        tabToSelectedSubTab[selectedTab] = i;
                        Repaint();
                        return;
                    }
                }
            }

            // If current tab has no results, try other tabs
            if (!hasResultsInCurrentTab)
            {
                // Check each tab for matching items
                for (int i = 0; i < tabGroups.Count; i++)
                {
                    // Skip the current tab since we already checked it
                    if (i == selectedTab)
                        continue;

                    var tab = tabGroups[i];
                    var tabItems = tab.GetItems();

                    bool hasResultsInTab = tabItems.Any(item =>
                        item.title.ToLower().Contains(lowerFilter) ||
                        item.description.ToLower().Contains(lowerFilter));

                    if (hasResultsInTab)
                    {
                        // Found a tab with results, switch to it
                        selectedTab = i;

                        // Check for subtabs in new tab and find the first one with results
                        if (tab.HasSubTabs)
                        {
                            var subCategories = tab.GetSubTabCategories();

                            for (int j = 0; j < subCategories.Count; j++)
                            {
                                var subTabItems = subCategories[j].Items;
                                bool hasResultsInSubTab = subTabItems.Any(item =>
                                    item.title.ToLower().Contains(lowerFilter) ||
                                    item.description.ToLower().Contains(lowerFilter));

                                if (hasResultsInSubTab)
                                {
                                    // Found a subtab with results, switch to it
                                    selectedSubTab = j;
                                    tabToSelectedSubTab[selectedTab] = j;
                                    break;
                                }
                            }
                        }

                        // Reset scroll position when switching tabs
                        scrollPosition = Vector2.zero;
                        Repaint();
                        return;
                    }
                }
            }

            // If no results found anywhere, we remain on the current tab
        }

        // New method to handle drawing the tab bar
        private void DrawTabBar()
        {
            // Calculate tab widths if not already done
            if (tabWidths.Count == 0)
            {
                GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton);
                tabStyle.fontStyle = FontStyle.Bold;
                tabStyle.wordWrap = true;
                tabStyle.padding = new RectOffset(5, 5, 5, 5);
                tabStyle.alignment = TextAnchor.MiddleCenter;

                for (int i = 0; i < tabGroups.Count; i++)
                {
                    string tabName = tabGroups[i].Name;
                    // Use narrower width for multi-word tabs to encourage wrapping
                    float maxWidth = tabName.Contains(" ") ? 100f : 120f;
                    Vector2 textSize = tabStyle.CalcSize(new GUIContent(tabName));
                    float requiredWidth = Mathf.Max(minTabWidth, Mathf.Min(maxWidth, textSize.x + tabPadding * 2));
                    tabWidths[i] = requiredWidth;
                }
            }

            // Calculate total required width and available width
            float totalRequiredWidth = tabWidths.Values.Sum();
            float availableWidth = position.width - 10;
            bool tabsNeedOverlap = totalRequiredWidth > availableWidth;

            // Use a horizontal layout for the tabs
            EditorGUILayout.BeginHorizontal();

            // Use GUILayout.Toolbar for a clean tab implementation
            int newSelectedTab = GUILayout.Toolbar(
                selectedTab,
                tabGroups.Select(t => t.Name).ToArray(),
                EditorStyles.toolbarButton,
                GUILayout.Height(tabHeight * 2) // Double height for two lines
            );

            if (newSelectedTab != selectedTab)
            {
                selectedTab = newSelectedTab;
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFavoritesTab()
        {
            // Get the favorites items through the public method
            List<ToolkitItem> favoriteItems = new List<ToolkitItem>();

            // Create a defensive copy to avoid modification issues
            if (favoritesTab != null)
            {
                favoriteItems.AddRange(favoritesTab.GetItems());
            }

            // Apply search filter if any
            if (!string.IsNullOrEmpty(searchFilter))
            {
                string lowerFilter = searchFilter.ToLower();
                favoriteItems = favoriteItems.Where(item =>
                    item != null &&
                    (item.title.ToLower().Contains(lowerFilter) ||
                    item.description.ToLower().Contains(lowerFilter))).ToList();
            }

            if (favoriteItems.Count == 0)
            {
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    EditorGUILayout.HelpBox("No items match your search criteria.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("You don't have any favorites yet. Add items to your favorites by clicking 'Add to Favorites' in the detail view of any item.", MessageType.Info);
                }
                return;
            }

            DrawResponsiveGrid(favoriteItems);
        }

        private void DrawResponsiveGrid(List<ToolkitItem> items)
        {
            // Ensure our items list is not null
            if (items == null)
            {
                EditorGUILayout.HelpBox("No items to display.", MessageType.Warning);
                return;
            }

            int itemCount = items.Count;
            if (itemCount == 0)
            {
                if (!string.IsNullOrEmpty(searchFilter))
                {
                    EditorGUILayout.HelpBox("No items match your search criteria.", MessageType.Info);
                }
                return;
            }

            // Calculate how many tiles can fit in a row based on window width
            float availableWidth = position.width - 20; // Subtract for margins
            int tilesPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (tileWidth + spacing)));
            int rowCount = Mathf.CeilToInt((float)itemCount / tilesPerRow);

            for (int row = 0; row < rowCount; row++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int col = 0; col < tilesPerRow; col++)
                {
                    int index = row * tilesPerRow + col;
                    if (index < itemCount && index >= 0)
                    {
                        // Extra safety check to ensure item exists
                        if (items[index] != null)
                        {
                            DrawGridTile(items[index]);
                        }
                    }
                    else
                    {
                        // Empty space to maintain grid
                        GUILayout.Space(tileWidth + spacing);
                    }
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(spacing);
            }
        }

        private void DrawGridTile(ToolkitItem item)
        {
            // Fixed section heights
            float favoritesButtonHeight = 30f;  // Favorites button now at the top
            float imageHeight = tileHeight * 0.5f;
            float titleSectionHeight = 30f;
            float descriptionSectionHeight = 42f; // Exact height for 2 lines
            float moreInfoButtonHeight = 25f;
            float favoriteIndicatorHeight = 20f;

            // Ensure total height is sufficient
            float totalHeight = favoritesButtonHeight + imageHeight + titleSectionHeight + descriptionSectionHeight + moreInfoButtonHeight + favoriteIndicatorHeight;

            GUILayout.BeginVertical(GUILayout.Width(tileWidth), GUILayout.Height(totalHeight));

            Rect tileRect = GUILayoutUtility.GetRect(tileWidth, totalHeight);

            // Draw background
            EditorGUI.DrawRect(tileRect, EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.85f, 0.85f, 0.85f));

            // Favorite status 
            bool isFavorite = favoritesTab.IsFavorite(item.title);

            // Draw "Add to Favorites" button section at the top
            Rect favoritesButtonSectionRect = new Rect(
                tileRect.x,
                tileRect.y,
                tileRect.width,
                favoritesButtonHeight
            );

            // Use a slightly different color for the favorites section to make it stand out
            Color favoriteSectionColor = EditorGUIUtility.isProSkin ?
                new Color(0.28f, 0.28f, 0.28f) :
                new Color(0.82f, 0.82f, 0.82f);

            EditorGUI.DrawRect(favoritesButtonSectionRect, favoriteSectionColor);

            // Create a full-width button with consistent padding
            float buttonPadding = 3f; // Consistent padding all around
            Rect favoritesButtonRect = new Rect(
                favoritesButtonSectionRect.x + buttonPadding,
                favoritesButtonSectionRect.y + buttonPadding,
                favoritesButtonSectionRect.width - (buttonPadding * 2),
                favoritesButtonSectionRect.height - (buttonPadding * 2)
            );

            // Favorite button style
            GUIStyle favoriteButtonStyle = new GUIStyle(EditorStyles.miniButton);
            favoriteButtonStyle.alignment = TextAnchor.MiddleCenter;
            favoriteButtonStyle.padding = new RectOffset(5, 5, 2, 2);
            favoriteButtonStyle.fontSize = 10;
            favoriteButtonStyle.fontStyle = FontStyle.Bold;

            // Make the star yellow for better visibility if it's a favorite
            if (isFavorite)
            {
                favoriteButtonStyle.normal.textColor = Color.black;
                favoriteButtonStyle.hover.textColor = Color.black;
            }

            // Adjust the favorite button text based on current status
            string favoriteButtonText = isFavorite ? "★ Remove from Favorites" : "☆ Add to Favorites";

            // Draw the Add/Remove Favorites button
            if (GUI.Button(favoritesButtonRect, favoriteButtonText, favoriteButtonStyle))
            {
                if (isFavorite)
                {
                    // Store the title locally to avoid any reference issues
                    string titleToRemove = item.title;

                    // Use EditorApplication.delayCall to handle removal after the current GUI frame
                    EditorApplication.delayCall += () =>
                    {
                        favoritesTab.RemoveFromFavorites(titleToRemove);
                        favoritesTab.SaveFavorites();

                        // Force a full update of the favorites tab
                        UpdateFavoritesTab();

                        // If we're on the favorites tab, ensure the UI refreshes properly
                        if (selectedTab == 0)
                        {
                            // Reset scroll position to avoid out of bounds issues
                            favoritesScrollPosition = Vector2.zero;
                            Repaint();
                        }
                    };
                }
                else
                {
                    favoritesTab.AddToFavorites(item.title);
                    favoritesTab.SaveFavorites();
                    UpdateFavoritesTab();
                }
            }

            // Draw image with darker background - now positioned right below the favorites button with no gap
            Rect imageRect = new Rect(
                tileRect.x + 5,
                favoritesButtonSectionRect.y + favoritesButtonSectionRect.height, // No gap
                tileRect.width - 10,
                imageHeight - 10);

            EditorGUI.DrawRect(
                new Rect(
                    tileRect.x,
                    favoritesButtonSectionRect.y + favoritesButtonSectionRect.height, // No gap
                    tileRect.width,
                    imageHeight),
                EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.7f, 0.7f, 0.7f));

            GUI.DrawTexture(imageRect, item.image, ScaleMode.ScaleToFit);

            // Check if the item represents a stub class/feature that's not implemented
            bool isNotImplemented =
                item.title == "Create Custom Spawn Point" ||
                item.title == "Create Safety Net" ||
                item.title == "Create Local Teleporter";

            if (isNotImplemented)
            {
                // Draw semi-transparent overlay
                EditorGUI.DrawRect(imageRect, new Color(0.8f, 0.1f, 0.1f, 0.3f));

                // Draw warning icon if available
                Texture2D warningIcon = EditorGUIUtility.IconContent("console.warnicon").image as Texture2D;
                if (warningIcon != null)
                {
                    Rect iconRect = new Rect(
                        imageRect.x + (imageRect.width - 32) / 2,
                        imageRect.y + 10,
                        32,
                        32);
                    GUI.DrawTexture(iconRect, warningIcon);

                    // Only draw text once, positioned below the icon
                    Rect textRect = new Rect(
                        imageRect.x,
                        iconRect.y + iconRect.height + 5,
                        imageRect.width,
                        20);

                    GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel);
                    warningStyle.alignment = TextAnchor.MiddleCenter;
                    warningStyle.normal.textColor = Color.white;
                    GUI.Label(textRect, "Not Implemented", warningStyle);
                }
                else
                {
                    // Fallback if icon is not available
                    GUIStyle warningStyle = new GUIStyle(EditorStyles.boldLabel);
                    warningStyle.alignment = TextAnchor.MiddleCenter;
                    warningStyle.normal.textColor = Color.white;
                    GUI.Label(imageRect, "Not Implemented", warningStyle);
                }
            }

            // Favorite indicator - keep this as a small star in corner for quick visual reference
            if (isFavorite)
            {
                // Draw a star in the top-right corner of the image section
                Rect favoriteIndicatorRect = new Rect(
                    imageRect.x + imageRect.width - 20,
                    imageRect.y + 5,
                    20,
                    20);

                GUIStyle starStyle = new GUIStyle(EditorStyles.label);
                starStyle.normal.textColor = Color.yellow;
                starStyle.fontSize = 16;
                starStyle.fontStyle = FontStyle.Bold;

                GUI.Label(favoriteIndicatorRect, "★", starStyle);
            }

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.wordWrap = true;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.fontSize = 12;
            titleStyle.alignment = TextAnchor.UpperLeft;
            titleStyle.clipping = TextClipping.Clip;
            // Brighter white title text
            titleStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f) : new Color(0.1f, 0.1f, 0.1f);

            // Make title and description sections the same color
            Color sectionColor = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.25f, 0.25f) : new Color(0.78f, 0.78f, 0.78f);

            // Draw title section - now positioned below the image
            Rect titleSectionRect = new Rect(
                tileRect.x,
                imageRect.y + imageHeight + 5,
                tileRect.width,
                titleSectionHeight);

            EditorGUI.DrawRect(titleSectionRect, sectionColor);

            // Draw title
            Rect titleRect = new Rect(
                tileRect.x + 5,
                titleSectionRect.y + 2,
                tileRect.width - 10,
                titleSectionHeight - 4);

            string truncatedTitle = TruncateTextToFit(item.title, titleStyle, titleRect.width);
            GUI.Label(titleRect, truncatedTitle, titleStyle);

            // Description section
            Rect descSectionRect = new Rect(
                tileRect.x,
                titleSectionRect.y + titleSectionRect.height,
                tileRect.width,
                descriptionSectionHeight
            );
            EditorGUI.DrawRect(descSectionRect, sectionColor);

            // Manually truncate description to fit exactly 2 lines with ellipsis
            GUIStyle descStyle = new GUIStyle(EditorStyles.label);
            descStyle.wordWrap = true;
            descStyle.richText = false;
            descStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.3f, 0.3f, 0.3f);

            // Calculate description area
            Rect descriptionRect = new Rect(
                descSectionRect.x + 5,
                descSectionRect.y + 5,
                descSectionRect.width - 10,
                descriptionSectionHeight - 10
            );

            // Create truncated description that fits exactly 2 lines
            string description = item.description;
            float twoLineHeight = descStyle.lineHeight * 2;

            // Binary search to find the maximum characters that fit in 2 lines
            int low = 0;
            int high = description.Length;
            string truncated = description;

            while (low < high)
            {
                int mid = (low + high) / 2;
                string testString = description.Substring(0, mid) + "...";
                float height = descStyle.CalcHeight(new GUIContent(testString), descriptionRect.width);

                if (height <= twoLineHeight)
                {
                    low = mid + 1;
                    truncated = testString;
                }
                else
                {
                    high = mid;
                }
            }

            // Make sure we end at a word boundary
            if (truncated.Length < description.Length)
            {
                int lastSpace = truncated.LastIndexOf(' ', truncated.Length - 4);
                if (lastSpace > truncated.Length / 2)
                {
                    truncated = truncated.Substring(0, lastSpace) + "...";
                }
            }

            // Draw the description
            GUI.Label(descriptionRect, truncated, descStyle);

            // Draw "More Info" button section
            Rect moreInfoSectionRect = new Rect(
                tileRect.x,
                descSectionRect.y + descSectionRect.height,
                tileRect.width,
                moreInfoButtonHeight
            );
            EditorGUI.DrawRect(moreInfoSectionRect, sectionColor);

            // Create button style
            GUIStyle moreInfoButtonStyle = new GUIStyle(EditorStyles.miniButton);
            moreInfoButtonStyle.alignment = TextAnchor.MiddleCenter;
            moreInfoButtonStyle.padding = new RectOffset(5, 5, 2, 2);
            moreInfoButtonStyle.fontSize = 10;
            moreInfoButtonStyle.fontStyle = FontStyle.Bold;

            // Draw "More Info" button
            Rect moreInfoButtonRect = new Rect(
                moreInfoSectionRect.x + moreInfoSectionRect.width - 75,
                moreInfoSectionRect.y + (moreInfoSectionRect.height - 20) / 2,
                70,
                20
            );

            // Make the image, title and description area clickable (excluding the favorites button and More Info button)
            Rect clickableRect = new Rect(
                tileRect.x,
                favoritesButtonSectionRect.y + favoritesButtonSectionRect.height,
                tileRect.width,
                imageHeight + titleSectionHeight + descriptionSectionHeight);

            // Draw hover effect
            if (clickableRect.Contains(Event.current.mousePosition) &&
                !moreInfoButtonRect.Contains(Event.current.mousePosition) &&
                Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(clickableRect, new Color(1, 1, 1, 0.1f));
            }

            // Handle click on clickable area
            if (GUI.Button(clickableRect, GUIContent.none, GUIStyle.none) &&
                !moreInfoButtonRect.Contains(Event.current.mousePosition))
            {
                item.action?.Invoke();
            }

            // Draw the More Info button
            if (GUI.Button(moreInfoButtonRect, "More Info", moreInfoButtonStyle))
            {
                OpenDetailWindow(item);
            }

            GUILayout.EndVertical();
            GUILayout.Space(spacing);
        }

        // Helper method to get text that fits in a specific rect
        private string GetTextThatFitsRect(string text, GUIStyle style, float maxWidth, float maxHeight)
        {
            // If the text fits, return it as is
            if (style.CalcHeight(new GUIContent(text), maxWidth) <= maxHeight)
                return text;

            // Binary search for the maximum number of characters that will fit
            int min = 0;
            int max = text.Length;
            string result = "";

            while (min <= max)
            {
                int mid = (min + max) / 2;
                string testString = text.Substring(0, mid) + "...";
                float height = style.CalcHeight(new GUIContent(testString), maxWidth);

                if (height <= maxHeight)
                {
                    result = testString;
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }

            // Ensure we don't cut in the middle of a word if possible
            if (result.Length > 4) // "..." plus at least one character
            {
                int lastSpace = result.LastIndexOf(' ', result.Length - 4); // -4 to avoid searching in "..."
                if (lastSpace > result.Length / 2)
                {
                    result = result.Substring(0, lastSpace) + "...";
                }
            }

            return result;
        }

        // Helper method for text truncation
        private string TruncateTextToFit(string text, GUIStyle style, float maxWidth, float maxHeight = float.MaxValue)
        {
            // Short-circuit if text fits
            if (style.CalcHeight(new GUIContent(text), maxWidth) <= maxHeight)
                return text;

            // Truncate
            string truncated = text;
            while (truncated.Length > 3 && style.CalcHeight(new GUIContent(truncated + "..."), maxWidth) > maxHeight)
            {
                truncated = truncated.Substring(0, truncated.Length - 1);
            }

            return truncated.Length > 3 ? truncated + "..." : truncated;
        }

        private void OpenDetailWindow(ToolkitItem item)
        {
            selectedItem = item;
            ToolkitDetailWindow.Open(item, this, favoritesTab);
        }
    }
}