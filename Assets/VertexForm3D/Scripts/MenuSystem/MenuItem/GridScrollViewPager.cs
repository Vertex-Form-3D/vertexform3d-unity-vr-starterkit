using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GridScrollViewPager : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect; // Reference to ScrollView's ScrollRect
    [SerializeField] private GridLayoutGroup gridLayout; // GridLayoutGroup for items
    [SerializeField] private Button prevButton; // Previous page button
    [SerializeField] private Button nextButton; // Next page button
    [SerializeField] private TextMeshProUGUI pageText; // Text to show current/total pages
    [SerializeField] private GameObject pageButtonPrefab; // Prefab for page buttons
    [SerializeField] private Transform pageButtonContainer; // Container for page buttons (e.g., HorizontalLayoutGroup)

    private List<RectTransform> items = new List<RectTransform>(); // List of grid items
    private List<Button> pageButtons = new List<Button>(); // List of page buttons
    private int currentPage = 1;
    private int totalPages = 1;
    private int itemsPerPage;
    private float pageWidth;
    bool _initialized;

    void Start()
    {
        EnsureInitialized();
        UpdatePageUI();
        UpdateButtonStates();
        UpdatePageButtons();
        ScrollToPage();
    }

    void EnsureInitialized()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (prevButton != null)
            prevButton.onClick.AddListener(GoToPreviousPage);
        if (nextButton != null)
            nextButton.onClick.AddListener(GoToNextPage);

        itemsPerPage = 3 * 2; // 3 columns x 2 rows

        if (items.Count == 0 && gridLayout != null)
        {
            foreach (Transform child in gridLayout.transform)
            {
                var rect = child.GetComponent<RectTransform>();
                if (rect != null)
                    items.Add(rect);
            }
        }

        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));
        pageWidth = totalPages > 1 ? 1f / (totalPages - 1) : 1f;
    }

    void UpdatePageUI()
    {
        if (pageText == null)
            return;

        pageText.text = $"{currentPage}/{totalPages}";

        if (itemsPerPage <= 0 || items.Count == 0)
            return;

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage - 1, items.Count - 1);

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;
            bool isVisible = i >= startIndex && i <= endIndex;
            items[i].gameObject.SetActive(isVisible);
        }
    }

    void UpdateButtonStates()
    {
        if (prevButton != null)
            prevButton.interactable = currentPage > 1;
        if (nextButton != null)
            nextButton.interactable = currentPage < totalPages;

        // Highlight current page button
        for (int i = 0; i < pageButtons.Count; i++)
        {
            bool isCurrentPage = (i + 1) == currentPage;
            // Example: Change button color to highlight
            pageButtons[i].transform.GetChild(0).gameObject.SetActive(isCurrentPage);
        }
    }

    void UpdatePageButtons()
    {
        // Clear existing page buttons
        foreach (Button button in pageButtons)
        {
            Destroy(button.gameObject);
        }
        pageButtons.Clear();

        // Create new page buttons
        for (int i = 0; i < totalPages; i++)
        {
            int pageIndex = i + 1;
            GameObject buttonObj = Instantiate(pageButtonPrefab, pageButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => GoToPage(pageIndex));
            pageButtons.Add(button);
        }
        // Update button highlights
        UpdateButtonStates();
    }

    void GoToPreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            ScrollToPage();
        }
    }

    void GoToNextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            ScrollToPage();
        }
    }

    void GoToPage(int page)
    {
        if (page >= 1 && page <= totalPages)
        {
            currentPage = page;
            ScrollToPage();
        }
    }

    void ScrollToPage()
    {
        if (scrollRect != null)
        {
            if (totalPages <= 1)
                scrollRect.horizontalNormalizedPosition = 0f;
            else
            {
                float normalizedPosition = (float)(currentPage - 1) / (totalPages - 1);
                scrollRect.horizontalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
            }
        }

        // Update UI
        UpdatePageUI();
        UpdateButtonStates();
    }

    public void AddItem(GameObject itemPrefab)
    {
        EnsureInitialized();

        RectTransform newItemRect = itemPrefab.GetComponent<RectTransform>();
        items.Add(newItemRect);

        totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));
        pageWidth = totalPages > 1 ? 1f / (totalPages - 1) : 1f;

        if (currentPage > totalPages)
        {
            currentPage = totalPages;
            ScrollToPage();
        }
        else
        {
            UpdatePageUI();
            UpdateButtonStates();
            UpdatePageButtons();
        }
    }

    // Clear all items from the grid
    public void ClearAllItems()
    {
        EnsureInitialized();
        if (gridLayout != null)
        {
            for (int i = gridLayout.transform.childCount - 1; i >= 0; i--)
            {
                var child = gridLayout.transform.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
        else
        {
            foreach (RectTransform item in items)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }
        }

        items.Clear();
        currentPage = 1;
        totalPages = 1;
        pageWidth = 1f;

        if (scrollRect != null)
            scrollRect.horizontalNormalizedPosition = 0f;

        UpdatePageUI();
        UpdateButtonStates();
        UpdatePageButtons();
    }
}