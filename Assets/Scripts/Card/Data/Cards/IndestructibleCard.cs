using UnityEngine;

[CreateAssetMenu(fileName = "IndestructibleCard", menuName = "Cards/Indestructible Card")]
public class IndestructibleCard : CardData
{
    public override void Use()
    {
        UIManager.Instance.ShowMessage("Please select one of your pieces: Grant it \"Indestructible\" state (1 turns)", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            Services.NetInterface?.RequestUseCardOnPiece(Id, piece.NetworkObjectId); 
        });
    }
}