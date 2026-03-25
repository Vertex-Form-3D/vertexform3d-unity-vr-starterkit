using UnityEngine;
using UnityEngine.Events;

public class ToggleEvent : MonoBehaviour
{
    public UnityEvent ToggleOnEvent;
    public UnityEvent ToggleOffEvent;
    public bool isOn = false;
    void Start()
    {
        
    }

    public void Toggle()
    {
        if (isOn)
        {
            if (ToggleOffEvent!=null)
            {
                ToggleOffEvent?.Invoke();
            }
            isOn = false;
        }
        else
        {
            if (ToggleOnEvent != null)
            {
                ToggleOnEvent?.Invoke();
            }
            isOn = true;
        }
    }
}
