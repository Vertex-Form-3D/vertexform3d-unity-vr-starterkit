using System;
using System.Collections.Generic;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;
using UnityEngine.UI;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class AssetButtonCreator : MonoBehaviour
    {
        [SerializeField] private GameObject assetButtonPrefab;
        [SerializeField] private GameObject clearAssetSelectionButton;
        [SerializeField] private GameObject colorAssetButtonPrefab;

        private Dictionary<object, AssetButton> buttonsById;
        private Dictionary<AssetType, AssetButton> selectedButtonsByCategory;
        private Dictionary<AssetType, object> selectedAssetIdByCategory;
        private Dictionary<AssetType, AssetButton> clearButtonByCategory;

        private void Start()
        {
            buttonsById = new Dictionary<object, AssetButton>();
            selectedButtonsByCategory = new Dictionary<AssetType, AssetButton>();
            clearButtonByCategory = new Dictionary<AssetType, AssetButton>();
        }

        public void SetSelectedAssets(Dictionary<AssetType, object> assets)
        {
            selectedAssetIdByCategory = assets;
        }

        public void CreateAssetButtons(IEnumerable<string> assets, AssetType category, Action<string, AssetType> onClick)
        {
            buttonsById ??= new Dictionary<object, AssetButton>();

            var parentPanel = PanelSwitcher.CategoryPanelMap[category];
            foreach (var asset in assets)
            {
                AddAssetButton(asset, parentPanel.transform, category, onClick);
                if (selectedAssetIdByCategory.ContainsValue(asset))
                {
                    SetSelectedIcon(asset, category);
                }
            }
        }

        public void CreateClearButton(Action<string, AssetType> onClick)
        {
            foreach (var categoryPanelMap in PanelSwitcher.CategoryPanelMap)
            {
                var category = categoryPanelMap.Key;
                if (category.IsOptionalAsset())
                {
                    var categoryPanel = categoryPanelMap.Value;
                    AddClearSelectionButton(categoryPanel.transform, category, onClick);
                }
            }
        }

        private void SetSelectedIcon(string assetId, AssetType category)
        {
            if (category.IsColorAsset() && category != AssetType.EyeColor)
            {
                assetId = $"{category}_{assetId}";
            }

            if (!buttonsById.ContainsKey(assetId))
            {
                return;
            }
            SelectButton(category, buttonsById[assetId]);
        }

        public void CreateColorUI(Dictionary<AssetType, AssetColor[]> colorLibrary, Action<object, AssetType> onClick, Dictionary<AssetType, int> colorAssets)
        {
            foreach (var colorPalette in colorLibrary)
            {
                var parent = PanelSwitcher.CategoryPanelMap[colorPalette.Key];
                var assetIndex = 0;
                foreach (var assetColor in colorPalette.Value)
                {
                    var button = AddColorButton(assetIndex, parent.transform, colorPalette.Key, onClick);
                    button.SetColor(assetColor.HexColor);

                    int equippedValue = 0;
                    if (colorAssets.TryGetValue(assetColor.AssetType, out var value))
                    {
                        equippedValue = value;
                    }

                    if (assetIndex == equippedValue)
                    {
                        SelectButton(colorPalette.Key, button);
                    }
                    assetIndex++;
                }
            }
        }

        public void SetAssetIcon(string id, Texture texture)
        {
            if (buttonsById.TryGetValue(id, out AssetButton button))
            {
                button.SetIcon(texture);
            }
        }

        public void ResetUI()
        {
            foreach (var assetButton in buttonsById)
            {
                DestroyImmediate(assetButton.Value.gameObject);
            }

            foreach (var button in clearButtonByCategory)
            {
                DestroyImmediate(button.Value.gameObject);
            }

            buttonsById.Clear();
            selectedButtonsByCategory.Clear();
            selectedAssetIdByCategory.Clear();
            clearButtonByCategory.Clear();
        }

        private AssetButton AddColorButton(int index, Transform parent, AssetType category, Action<object, AssetType> onClick)
        {
            var assetButtonGameObject = Instantiate(colorAssetButtonPrefab, parent.GetComponent<ScrollRect>().content);
            var buttonName = $"{category}_{index}";
            assetButtonGameObject.name = buttonName;
            var assetButton = assetButtonGameObject.GetComponent<AssetButton>();
            assetButton.AddListener(() =>
            {
                SelectButton(category, assetButton);
                onClick?.Invoke(index, category);
            });
            buttonsById.Add(buttonName, assetButton);
            return assetButton;
        }

        private void AddAssetButton(string assetId, Transform parent, AssetType category, Action<string, AssetType> onClick)
        {
            if (buttonsById.ContainsKey(assetId)) return;

            var assetButtonGameObject = Instantiate(assetButtonPrefab, parent.GetComponent<ScrollRect>().content);
            assetButtonGameObject.name = "Asset-" + assetId;
            var assetButton = assetButtonGameObject.GetComponent<AssetButton>();
            assetButton.AddListener(() =>
            {
                SelectButton(category, assetButton);
                onClick?.Invoke(assetId, category);
            });
            buttonsById.Add(assetId, assetButton);
        }

        private void SelectButton(AssetType category, AssetButton assetButton)
        {
            ConfigureOutfitSelection(category);

            if (selectedButtonsByCategory.ContainsKey(category))
            {
                selectedButtonsByCategory[category].SetSelect(false);
                selectedButtonsByCategory[category] = assetButton;
            }
            else
            {
                selectedButtonsByCategory.Add(category, assetButton);
            }
            assetButton.SetSelect(true);
        }

        private void ConfigureOutfitSelection(AssetType category)
        {
            switch (category)
            {
                case AssetType.Top:
                case AssetType.Bottom:
                case AssetType.Footwear:
                    {
                        if (selectedButtonsByCategory.TryGetValue(AssetType.Outfit, out AssetButton outfitButton))
                        {
                            outfitButton.SetSelect(false);
                        }
                        break;
                    }
                case AssetType.Outfit:
                    {
                        if (selectedButtonsByCategory.TryGetValue(AssetType.Top, out AssetButton topButton))
                        {
                            topButton.SetSelect(false);
                        }

                        if (selectedButtonsByCategory.TryGetValue(AssetType.Bottom, out AssetButton bottomButton))
                        {
                            bottomButton.SetSelect(false);
                        }

                        if (selectedButtonsByCategory.TryGetValue(AssetType.Footwear, out AssetButton footwearButton))
                        {
                            footwearButton.SetSelect(false);
                        }
                        break;
                    }
            }
        }

        private void AddClearSelectionButton(Transform parent, AssetType category, Action<string, AssetType> onClick)
        {
            var assetButtonGameObject = Instantiate(clearAssetSelectionButton, parent.GetComponent<ScrollRect>().content);
            assetButtonGameObject.transform.SetAsFirstSibling();
            var assetButton = assetButtonGameObject.GetComponent<AssetButton>();
            assetButton.AddListener(() =>
            {
                SelectButton(category, assetButton);
                onClick?.Invoke(string.Empty, category);
            });

            if (IsSelectedAssetNotPresentForCategory(category))
            {
                SelectButton(category, assetButton);
            }
            clearButtonByCategory.Add(category, assetButton);
        }

        private bool IsSelectedAssetNotPresentForCategory(AssetType category)
        {
            return !selectedAssetIdByCategory.ContainsKey(category) ||
                   selectedAssetIdByCategory[category] as int? == 0 ||
                   string.IsNullOrEmpty(selectedAssetIdByCategory[category] as string);
        }

    }
}
