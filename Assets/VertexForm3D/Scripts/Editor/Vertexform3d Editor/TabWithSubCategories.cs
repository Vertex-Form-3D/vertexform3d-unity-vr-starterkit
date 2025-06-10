using System.Collections.Generic;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public abstract class TabWithSubCategories : ITabGroup
    {
        protected List<ToolkitItem> allItems = new List<ToolkitItem>();
        protected List<SubTabCategory> subCategories = new List<SubTabCategory>();

        public abstract string Name { get; }
        public abstract string Description { get; }

        public bool HasSubTabs => subCategories.Count > 0;

        public abstract void InitializeItems();

        public List<ToolkitItem> GetItems()
        {
            // If using subcategories, return all items for search purposes
            if (HasSubTabs)
            {
                return allItems;
            }

            // Otherwise just return the items directly
            return allItems;
        }

        public List<SubTabCategory> GetSubTabCategories()
        {
            return subCategories;
        }

        protected void AddSubCategory(string name, string description)
        {
            subCategories.Add(new SubTabCategory(name, description));
        }

        protected void AddItemToSubCategory(int categoryIndex, ToolkitItem item)
        {
            if (categoryIndex >= 0 && categoryIndex < subCategories.Count)
            {
                subCategories[categoryIndex].AddItem(item);

                // Also add to all items list for searching
                if (!allItems.Contains(item))
                {
                    allItems.Add(item);
                }
            }
        }

        protected void ClearAllItems()
        {
            allItems.Clear();
            foreach (var category in subCategories)
            {
                category.ClearItems();
            }
        }
    }
}