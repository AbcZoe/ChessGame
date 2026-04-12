using UnityEngine;

[CreateAssetMenu(fileName = "MutualDestructionCard", menuName = "Cards/Mutual Destruction Card")]
public class MutualDestructionCard : CardData
{
    public override void Use(int team)
    {
        // team 參數你可以不用，因為 PlayerInput 已知道 PlayerTeam
        UIManager.Instance.ShowMessage("Please select one of your pieces: Grant it \"Indestructible\" state (1 turn)", 2f);

        PlayerInput.Instance.BeginSelectFriendlyPiece(piece =>
        {
            piece.ApplyMutualDestructionServerRpc(2);
            UIManager.Instance.ShowMessage("\"Mutual Destruction\" has been cast.", 1.5f);
            GameManager.Instance?.ShowMessageClientRpc("The enemy used the card \"Mutual Destruction\".", 2f);

            GameManager.Instance.EndAction();
        });
    }
}