using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CategoryItemView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI categoryNameTxt;
    [SerializeField] Button categoryButton;
    public UnityEvent OnClickEvent;
    public Category category = new Category();
    MenuManager _ownerMenuManager;

    void Start()
    {
        categoryButton.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        if (OnClickEvent != null)
        {
            OnClickEvent?.Invoke();
        }
        MenuManager mgr = _ownerMenuManager != null ? _ownerMenuManager : MenuManager.Instance;
        if (mgr != null)
            mgr.InitWorlds(category);
    }

    public void SetCategory(Category cat, MenuManager ownerMenuManager)
    {
        _ownerMenuManager = ownerMenuManager;
        category = cat.Clone();
        categoryNameTxt.text = category.categoryName;
    }

}
