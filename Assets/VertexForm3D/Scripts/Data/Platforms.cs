using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Platform and Settings", menuName = "ScriptableObjects/Platform and Settings", order = 1)]
public class Platforms : ScriptableObject
{
    public platform platformChoice = platform.VR;

    [HideInInspector]
    public List<PlatformSetupGuide> platformGuides = new List<PlatformSetupGuide>();

    private void Reset()
    {
        platformGuides = new List<PlatformSetupGuide>
        {
            new PlatformSetupGuide
            {
                title = "Android \u00b7 VR",
                subtitle = "Meta Quest",
                steps = new List<string>
                {
                    "Select <b>VR</b> from the <b>Platform</b> dropdown in Project Data.",
                    "Go to <b>Edit \u2192 Build Profiles</b> and set <b>Android</b> as the active platform."
                }
            },
            new PlatformSetupGuide
            {
                title = "Desktop",
                subtitle = "Windows \u00b7 PC",
                steps = new List<string>
                {
                    "Select <b>Desktop</b> from the <b>Platform</b> dropdown in Project Data.",
                    "Go to <b>Edit \u2192 Build Profiles</b> and set <b>Windows</b> as the active platform."
                }
            },
            new PlatformSetupGuide
            {
                title = "Testing VR on Desktop via Quest Link",
                subtitle = "XR Plug-in Management \u00b7 Windows tab",
                steps = new List<string>
                {
                    "Open <b>Edit \u2192 Project Settings \u2192 XR Plug-in Management</b>",
                    "Select the <b>Windows</b> tab in the XR Plug-in panel.",
                    "Enable <b>Initialize XR on Startup</b>"
                },
                note = "Why this matters: Enabling \u2018Initialize XR on Startup\u2019 ensures your VR experience launches correctly when running the project on desktop through Quest Link \u2014 without this, XR may not initialize at play time."
            }
        };
    }
}

[Serializable]
public class PlatformSetupGuide
{
    public string title;
    public string subtitle;
    public List<string> steps = new List<string>();
    [TextArea(2, 5)]
    public string note;
}
