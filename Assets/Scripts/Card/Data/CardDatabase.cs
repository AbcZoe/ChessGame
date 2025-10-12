using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    public CardData[] allCards;

    public CardData GetRandomCard()
    {
        if (allCards == null || allCards.Length == 0)
            return null;
        return allCards[Random.Range(0, allCards.Length)];
    }
}
