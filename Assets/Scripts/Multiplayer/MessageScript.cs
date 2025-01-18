using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageScript : MonoBehaviour
{
    [SerializeField] Image slider;
    [SerializeField] TextMeshProUGUI messageText;

    void Start()
    {

    }

    public void ShowMessage(string message, Color color)
    {
        messageText.text = message;
        slider.color = color;
        DOTween.To(() => slider.fillAmount, x => slider.fillAmount = x, 0, 3).SetEase(Ease.Linear).OnComplete(() => { Destroy(gameObject); }); ;
    }
}
