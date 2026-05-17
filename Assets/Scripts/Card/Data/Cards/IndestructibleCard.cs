using UnityEngine;

[CreateAssetMenu(fileName = "IndestructibleCard", menuName = "Cards/Indestructible Card")]
public class IndestructibleCard : CardData
{
    public override void Use()
    {
        UIManager.Instance.ShowMessage("Indestructible_Msg", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            Services.NetInterface?.RequestUseCardOnPiece(Id, piece.NetworkObjectId); 
        });
    }
}