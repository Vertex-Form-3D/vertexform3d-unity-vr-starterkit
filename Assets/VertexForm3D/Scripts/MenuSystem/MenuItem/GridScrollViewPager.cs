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

    void Start()
    {
        // Initialize buttons
        prevButton.onClick.AddListener(GoToPreviousPage);
        nextButton.onClick.AddListener(GoToNextPage);

        // Calculate items per page (3 columns, assuming 2 rows for example)
        itemsPerPage = 3 * 2; // 3 columns x 2 rows = 6 items per page

        // Get all items in grid
        foreach (Transform child in gridLayout.transform)
        {
            items.Add(child.GetComponent<RectTransform>());
        }

        // Calculate total pages
        totalPages = Mathf.CeilToInt((float)items.Count / itemsPerPage);

        // Calculate page width (normalized scroll position)
        pageWidth = totalPages > 1 ? 1f / (totalPages - 1) : 1f;

        // Initialize UI
        UpdatePageUI();
        UpdateButtonStates();
        UpdatePageButtons();

        // Ensure initial scroll position
        ScrollToPage();
    }

    void UpdatePageUI()
    {
        // Update page text (e.g., "Page 1/3")
        pageText.text = $"{currentPage}/{totalPages}";

        // Show only items for the current page
        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage - 1, items.Count - 1);

        for (int i = 0; i < items.Count; i++)
        {
            bool isVisible = i >= startIndex && i <= endIndex;
            items[i].gameObject.SetActive(isVisible);
        }
    }

    void UpdateButtonStates()
    {
        // Enable/disable buttons based on current page
        prevButton.interactable = currentPage > 1;
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
        if (totalPages <= 1)
        {
            scrollRect.horizontalNormalizedPosition = 0f;
        }
        else
        {
            // Calculate normalized position for the current page
            float normalizedPosition = (float)(currentPage - 1) / (totalPages - 1);

            // Ensure position stays within bounds [0,1]
            normalizedPosition = Mathf.Clamp01(normalizedPosition);

            // Set scroll position
            scrollRect.horizontalNormalizedPosition = normalizedPosition;
        }

        // Update UI
        UpdatePageUI();
        UpdateButtonStates();
    }

    // Add item to the grid
    public void AddItem(GameObject itemPrefab)
    {
        // Instantiate the prefab as a child of the grid
        RectTransform newItemRect = itemPrefab.GetComponent<RectTransform>();
        items.Add(newItemRect);

        // Recalculate total pages
        totalPages = Mathf.CeilToInt((float)items.Count / itemsPerPage);
        pageWidth = totalPages > 1 ? 1f / (totalPages - 1) : 1f;

        // Adjust current page if necessary
        if (currentPage > totalPages)
        {
            currentPage = totalPages;
            ScrollToPage();
        }
        else
        {
            // Update UI to reflect new item visibility
            UpdatePageUI();
            UpdateButtonStates();
            UpdatePageButtons();
        }
    }

    // Clear all items from the grid
    public void ClearAllItems()
    {
        // Destroy all item GameObjects
        foreach (RectTransform item in items)
        {
            Destroy(item.gameObject);
        }

        // Clear the items list
        items.Clear();

        // Reset pagination
        currentPage = 1;
        totalPages = 1;
        pageWidth = 1f;

        // Reset scroll position to start
        scrollRect.horizontalNormalizedPosition = 0f;

        // Update UI
        UpdatePageUI();
        UpdateButtonStates();
        UpdatePageButtons();
    }
}