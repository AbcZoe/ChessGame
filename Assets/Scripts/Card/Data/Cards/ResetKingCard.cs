using UnityEngine;

[CreateAssetMenu(fileName = "ResetKingCard", menuName = "Cards/Reset King Card")]
public class ResetKingCard : CardData
{
    public override void Use(int team)
    {
        ChessBoard.Instance.ResetKingSelectionForCurrentTeam();
        UIManager.Instance.ShowMessage($"Team {team + 1} can reselect their King!", 2f);
    }
}
