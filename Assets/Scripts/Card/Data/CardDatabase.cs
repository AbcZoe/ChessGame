using UnityEngine;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    public CardData[] allCards;

    // 隨機挑一張卡
    public CardData GetRandomCard()
    {
        if (allCards == null || allCards.Length == 0)
            return null;

        return allCards[Random.Range(0, allCards.Length)];
    }

    // 根據 ID 取得卡片
    public CardData GetCardById(int id)
    {
        if (allCards == null || allCards.Length == 0)
            return null;

        foreach (var card in allCards)
        {
            if (card.Id == id)
                return card;
        }

        return null; // 找不到回傳 null
    }
}