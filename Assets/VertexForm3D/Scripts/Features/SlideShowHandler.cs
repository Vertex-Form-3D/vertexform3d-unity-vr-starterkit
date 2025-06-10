using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VertexFormCore;

public class SlideShowHandler : MonoBehaviour
{
    public List<GameObject> slides = new List<GameObject>();
    public int currentSlideIndex = 0;
    public Button previousButton;
    public Button nextButton;
    public PhotonView photonView;
    public bool isNetworked = true;
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        previousButton.onClick.AddListener(OnTapPreviousButton);
        nextButton.onClick.AddListener(OnTapNextButton);
        UpdateButtonStates();
    }

    [PunRPC]
    public void HandleSlide(int index)
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
        NextSlide();
    }

    public void OnTapPreviousButton()
    {
        PreviousSlide();
    }

    [PunRPC]
    public void NextSlide()
    {
        if (currentSlideIndex < (slides.Count - 1))
        {
            currentSlideIndex++;
        }
        if (VirtualRoomManager.Instance!=null)
        {
            photonView.RPC(nameof(HandleSlide), RpcTarget.AllBuffered, currentSlideIndex);
        }
        else
        {
            HandleSlide(currentSlideIndex);
        }
    }

    public void PreviousSlide()
    {
        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
        }
        if (VirtualRoomManager.Instance != null)
        {
            photonView.RPC(nameof(HandleSlide), RpcTarget.AllBuffered, currentSlideIndex);
        }
        else
        {
            HandleSlide(currentSlideIndex);
        }
    }
    private void UpdateButtonStates()
    {
        // Disable Previous button if at first slide
        previousButton.interactable = (currentSlideIndex > 0);

        // Disable Next button if at last slide
        nextButton.interactable = (currentSlideIndex < slides.Count - 1);
    }
}
