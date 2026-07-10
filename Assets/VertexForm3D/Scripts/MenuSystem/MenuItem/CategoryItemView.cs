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

    WorldScreen _ownerWorldScreen;



    void Start()

    {

        categoryButton.onClick.AddListener(OnClicked);

    }



    void OnClicked()

    {

        OnClickEvent?.Invoke();

        _ownerWorldScreen?.InitWorlds(category);

    }



    public void SetCategory(Category cat, WorldScreen ownerWorldScreen)

    {

        _ownerWorldScreen = ownerWorldScreen;

        category = cat.Clone();

        categoryNameTxt.text = category.categoryName;

    }

}

