using System.Collections.Generic;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public interface ITabGroup
    {
        /// <summary>
        /// The name of the tab group
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description for the tab group
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Initializes all items belonging to this tab group
        /// </summary>
        void InitializeItems();

        /// <summary>
        /// Gets all available items in this tab group
        /// </summary>
        /// <returns>List of toolkit items</returns>
        List<ToolkitItem> GetItems();

        /// <summary>
        /// Whether this tab group has sub-tabs
        /// </summary>
        bool HasSubTabs { get; }

        /// <summary>
        /// Gets all sub-tab categories in this tab group
        /// </summary>
        /// <returns>List of sub-tab categories</returns>
        List<SubTabCategory> GetSubTabCategories();
    }
}