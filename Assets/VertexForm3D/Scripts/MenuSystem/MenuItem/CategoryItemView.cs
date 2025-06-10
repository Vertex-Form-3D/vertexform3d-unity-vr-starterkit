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
        MenuManager.Instance.InitWorlds(category);
    }

    public void SetCategory(Category cat)
    {
        category = cat.Clone();
        categoryNameTxt.text = category.categoryName;
    }

}
