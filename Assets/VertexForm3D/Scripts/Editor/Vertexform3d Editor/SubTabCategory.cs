using System.Collections.Generic;
using UnityEngine;

namespace VertexFormCore.Editor
{
    public class SubTabCategory
    {
        public string Name { get; private set; }
        public string Description { get; private set; }
        public List<ToolkitItem> Items { get; private set; }

        public SubTabCategory(string name, string description)
        {
            Name = name;
            Description = description;
            Items = new List<ToolkitItem>();
        }

        public void AddItem(ToolkitItem item)
        {
            Items.Add(item);
        }

        public void ClearItems()
        {
            Items.Clear();
        }
    }
}