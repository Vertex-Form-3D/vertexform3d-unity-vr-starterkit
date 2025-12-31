using System.Collections.Generic;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public static class PanelSwitcher
    {
        public static Dictionary<AssetType, GameObject> CategoryPanelMap { get; private set; }
        public static GameObject OutfitCategoryPanel;
        public static GameObject FaceCategoryPanel;

        public static void AddPanel(AssetType category, GameObject widget)
        {
            CategoryPanelMap ??= new Dictionary<AssetType, GameObject>();
            CategoryPanelMap.Add(category, widget);
        }

        public static void Switch(AssetType category)
        {
            DisableAllPanels();

            switch (category)
            {
                case AssetType.FaceShape:
                    FaceCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    SetActivePanel(AssetType.SkinColor, true);
                    break;
                case AssetType.EyebrowStyle:
                    FaceCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    SetActivePanel(AssetType.EyebrowColor, true);
                    break;
                case AssetType.BeardStyle:
                    FaceCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    SetActivePanel(AssetType.BeardColor, true);
                    break;
                case AssetType.HairStyle:
                    SetActivePanel(category, true);
                    SetActivePanel(AssetType.HairColor, true);
                    break;
                case AssetType.NoseShape:
                case AssetType.LipShape:
                    FaceCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    break;
                case AssetType.EyeShape:
                    FaceCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    SetActivePanel(AssetType.EyeColor, true);
                    break;
                case AssetType.Top:
                case AssetType.Bottom:
                case AssetType.Footwear:
                case AssetType.Outfit:
                    OutfitCategoryPanel.SetActive(true);
                    SetActivePanel(category, true);
                    break;
                default:
                    SetActivePanel(category, true);
                    break;
            }
        }

        public static void Clear()
        {
            CategoryPanelMap?.Clear();
        }

        private static void DisableAllPanels()
        {
            foreach (var panels in CategoryPanelMap)
            {
                panels.Value.SetActive(false);
            }

            FaceCategoryPanel.SetActive(false);
            OutfitCategoryPanel.SetActive(false);
        }

        private static void SetActivePanel(AssetType category, bool enable)
        {
            if (CategoryPanelMap.TryGetValue(category, out GameObject panel))
            {
                panel.SetActive(enable);
            }
        }
    }
}
