using UnityEngine;

[CreateAssetMenu(fileName = "MutualDestructionCard", menuName = "Cards/Mutual Destruction Card")]
public class MutualDestructionCard : CardData
{
    public override void Use()
    {
        UIManager.Instance.ShowMessage("Mutual Destruction_Msg", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            Services.NetInterface?.RequestUseCardOnPiece(Id, piece.NetworkObjectId);
        });
    }
}