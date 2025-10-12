using UnityEngine;

public abstract class CardData : ScriptableObject
{
    public string cardName;
    [TextArea] public string description;
    public Sprite image;

    public abstract void Use(int team);
}
