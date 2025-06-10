using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioPlayer : MonoBehaviour
{
    public bool isSpatialAudio = false;
    [SerializeField] AudioSource audioSource;
    [SerializeField] private AudioClip audioClip; // Audio clip to play
    [SerializeField] private Color minDistanceGizmoColor = Color.green; // Color for min distance gizmo
    [SerializeField] private Color maxDistanceGizmoColor = Color.red; // Color for max distance gizmo


    void Awake()
    {
        // Get or add AudioSource component
        audioSource = GetComponent<AudioSource>();

        // Configure AudioSource for spatial audio
        if (audioClip != null)
        {
            audioSource.clip = audioClip;
        }
        else
        {
            Debug.LogWarning("AudioClip is not assigned. Please assign an AudioClip to the AudioPlayer component.", this);
        }
        audioSource.rolloffMode = AudioRolloffMode.Linear; // Use logarithmic rolloff for natural decay
    }


    // Draw gizmos to visualize audio distances
    void OnDrawGizmos()
    {
        // Draw minimum distance sphere
        if (audioSource.spatialBlend != 0)
        {
            Gizmos.color = minDistanceGizmoColor;
            Gizmos.DrawWireSphere(transform.position, audioSource.minDistance);

            // Draw maximum distance sphere
            Gizmos.color = maxDistanceGizmoColor;
            Gizmos.DrawWireSphere(transform.position, audioSource.maxDistance);
        }
    }

    public void PlayAudio()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}
