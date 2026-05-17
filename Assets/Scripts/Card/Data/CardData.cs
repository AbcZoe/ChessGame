using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "Cards/CardData")]
public abstract class CardData : ScriptableObject
{
    [Header("基本資訊")] 
    public string cardNameKey;
    public int Id;
    public string descriptionKey;

    public abstract void Use();
}
