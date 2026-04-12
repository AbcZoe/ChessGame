using UnityEngine;

[CreateAssetMenu(fileName = "ResetKingCard", menuName = "Cards/Reset King Card")]
public class ResetKingCard : CardData
{
    public override void Use(int team)
    {
        // 透過 GameManager 統一處理重置 King
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetKingForTeam(team);
        }
    }
}