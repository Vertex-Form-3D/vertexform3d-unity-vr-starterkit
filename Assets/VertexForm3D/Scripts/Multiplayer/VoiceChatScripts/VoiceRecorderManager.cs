using Photon.Voice.Unity;
using UnityEngine;

public class VoiceRecorderManager : MonoBehaviour
{
    [SerializeField] private Recorder recorder;

    public static VoiceRecorderManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void EnableRecorder()
    {
        recorder.TransmitEnabled = true;
    }

    public void DisableRecorder()
    {
        recorder.TransmitEnabled = false;
    }

    public bool GetRecorderStatus()
    {
        return recorder.TransmitEnabled;
    }
}
