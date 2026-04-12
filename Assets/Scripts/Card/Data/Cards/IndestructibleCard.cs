using UnityEngine;

[CreateAssetMenu(fileName = "IndestructibleCard", menuName = "Cards/Indestructible Card")]
public class IndestructibleCard : CardData
{
    public override void Use(int team)
    {
        UIManager.Instance.ShowMessage("Please select one of your pieces: Grant it \"Indestructible\" state (1 turn)", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            piece.ApplyIndestructibleServerRpc(2);
            UIManager.Instance.ShowMessage("\"Indestructible\" has been cast.", 1.5f);
            GameManager.Instance?.ShowMessageClientRpc("The enemy used the card \"Indestructible\".", 2f);

            GameManager.Instance.EndAction();
        });
    }
}