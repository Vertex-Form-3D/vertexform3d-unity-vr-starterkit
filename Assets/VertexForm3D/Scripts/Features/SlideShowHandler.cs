using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;

public class SlideShowHandler : NetworkBehaviour
{
    public List<GameObject> slides = new List<GameObject>();
    public int currentSlideIndex = 0;
    public Button previousButton;
    public Button nextButton;
    public bool isNetworked = true;

    void Start()
    {
        previousButton.onClick.AddListener(OnTapPreviousButton);
        nextButton.onClick.AddListener(OnTapNextButton);
        UpdateButtonStates();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_HandleSlide(int index)
    {
        foreach (GameObject slide in slides)
        {
            slide.gameObject.SetActive(false);
        }
        slides[index].SetActive(true);
        UpdateButtonStates();
    }

    public void OnTapNextButton()
    {
        if (Object.HasInputAuthority)
        {
            NextSlide();
        }
    }

    public void OnTapPreviousButton()
    {
        if (Object.HasInputAuthority)
        {
            PreviousSlide();
        }
    }

    public void NextSlide()
    {
        if (currentSlideIndex < (slides.Count - 1))
        {
            currentSlideIndex++;
        }

        if (VirtualRoomManager.Instance != null && isNetworked)
        {
            RPC_HandleSlide(currentSlideIndex);
        }
        else
        {
            HandleSlideLocal(currentSlideIndex);
        }
    }

    public void PreviousSlide()
    {
        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
        }

        if (VirtualRoomManager.Instance != null && isNetworked)
        {
            RPC_HandleSlide(currentSlideIndex);
        }
        else
        {
            HandleSlideLocal(currentSlideIndex);
        }
    }

    private void HandleSlideLocal(int index)
    {
        foreach (GameObject slide in slides)
        {
            slide.gameObject.SetActive(false);
        }
        slides[index].SetActive(true);
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Disable Previous button if at first slide
        previousButton.interactable = (currentSlideIndex > 0);

        // Disable Next button if at last slide
        nextButton.interactable = (currentSlideIndex < slides.Count - 1);
    }
}

