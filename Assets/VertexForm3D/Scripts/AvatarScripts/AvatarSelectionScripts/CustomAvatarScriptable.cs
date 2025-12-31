using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CustomAvatarScriptable", menuName = "ScriptableObjects/CustomAvatarSO")]
public class CustomAvatarScriptable : ScriptableObject
{
    public List<AvatarData> avatarDatas = new List<AvatarData>();
}

[System.Serializable]
public class AvatarData
{
    public GameObject head;
    public GameObject body;
}