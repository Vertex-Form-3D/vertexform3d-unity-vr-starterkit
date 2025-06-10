using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmojiData", menuName = "ScriptableObjects/EmojiScriptable")]
public class EmojiScriptable : ScriptableObject
{
    public List<Emoji> emojiData;
}

[System.Serializable]
public class Emoji
{
    public Sprite emojiSprite;
}