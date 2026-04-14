using UnityEngine;

[CreateAssetMenu(fileName = "MutualDestructionCard", menuName = "Cards/Mutual Destruction Card")]
public class MutualDestructionCard : CardData
{
    public override void Use()
    {
        UIManager.Instance.ShowMessage("Please select one of your pieces: Grant it \"Mutual Destruction\" state (1 turns)", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            Services.NetInterface?.RequestUseCardOnPiece(Id, piece.NetworkObjectId);
        });
    }
}