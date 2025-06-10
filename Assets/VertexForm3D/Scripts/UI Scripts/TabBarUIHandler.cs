using UnityEngine;
using UnityEngine.UI;

public class TabBarUIHandler : MonoBehaviour
{
    public int CurrentTab;
    [SerializeField] Button[] tabButtons;
    [SerializeField] GameObject[] tabScreens;
    void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i; // Capture the current index
            tabButtons[i].onClick.AddListener(() => ShowTab(index));
        }
        ShowTab(CurrentTab);
    }

    public void ShowTab(int tabIndex)
    {
        CurrentTab = tabIndex;
        foreach (GameObject tab in tabScreens)
        {
            tab.SetActive(false);
        }
        tabScreens[tabIndex].SetActive(true);
    }
}
