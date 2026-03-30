using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/CardData")]
public abstract class CardData : ScriptableObject
{
    [Header("基本資訊")]
    public string cardName;
    public int Id;
    [TextArea] public string description;
    public Sprite image;

    public abstract void Use(int team);
}
