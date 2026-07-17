using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmojiData", menuName = "ScriptableObjects/EmojiScriptable")]
[Icon("Assets/VertexForm3D/UI/vertexform-Logo.png")]
public class EmojiScriptable : ScriptableObject
{
    public List<Emoji> emojiData;
}

[System.Serializable]
public class Emoji
{
    public Sprite emojiSprite;
}