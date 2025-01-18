namespace Photon.Voice.Unity.Demos
{
    using UnityEngine;
    using UnityEngine.UI;

    public class BackgroundMusicController : MonoBehaviour
    {
        #pragma warning disable 649
        [SerializeField]
        private Text volumeText;
        [SerializeField]
        private Slider volumeSlider;
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private float initialVolume = 0.125f;
        #pragma warning restore 649

        private void Awake()
        {
            this.volumeSlider.minValue = 0f;
            this.volumeSlider.maxValue = 1f;
            this.volumeSlider.SetSingleOnValueChangedCallback(this.OnVolumeChanged);
            this.volumeSlider.value = this.initialVolume;
            this.OnVolumeChanged(this.initialVolume);
        }

        private void OnVolumeChanged(float newValue)
        {
            this.volumeText.text = string.Format("BG Volume: {0:0.###}", newValue);
            this.audioSource.volume = newValue;
        }
    }
}