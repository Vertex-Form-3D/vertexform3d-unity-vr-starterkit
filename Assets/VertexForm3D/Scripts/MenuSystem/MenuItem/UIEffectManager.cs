using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class UIEffectManager : MonoBehaviour
{
    public bool IsInputFieldSelected;
    public static UIEffectManager Instance;
    public float percentageOfScaling = 5f; // Percentage of the screen width for the animation
    public float ScreenTimeforAnimation = .5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    void Start()
    {

    }
}
