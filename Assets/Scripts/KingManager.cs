using UnityEngine;
using static ChessPiece;

public class KingManager : MonoBehaviour
{
    public static KingManager Instance { get; private set; }
    private ChessPiece[,] chessPieces => ChessBoard.Instance.chessPieces;
    private Material[] KingMaterials => ChessBoard.Instance.KingMaterials;
    private Material[] teamMaterials => ChessBoard.Instance.teamMaterials;
    private int CurrentTurnTeam => GameManager.Instance.CurrentTurnTeam;
    private KingSelectionState kingSelectionState => ChessBoard.Instance.kingSelectionState;
    private bool IsPaused => GameManager.Instance.IsPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region King Selection
    public void SelectKingPiece(ChessPiece cp)
    {
        if (IsPaused) return;
        if (kingSelectionState == KingSelectionState.None || kingSelectionState == KingSelectionState.Completed) return;

        if (!IsValidKingSelection(cp))
        {
            UIManager.Instance.ShowMessage("You cannot select the opponent's piece as King!");
            return;
        }

        cp.SetKing(true);
        if (KingMaterials != null && cp.Team.Value < KingMaterials.Length)
            cp.GetComponent<MeshRenderer>().material = KingMaterials[cp.Team.Value];

        if (kingSelectionState == KingSelectionState.Reselecting)
            FinishReselectKing();
        else
            AdvanceKingSelectionState();
    }

    private bool IsValidKingSelection(ChessPiece cp)
    {
        return (kingSelectionState == KingSelectionState.WhiteTeamSelecting && cp.Team.Value == 0) ||
               (kingSelectionState == KingSelectionState.BlackTeamSelecting && cp.Team.Value == 1) ||
               (kingSelectionState == KingSelectionState.Reselecting && cp.Team.Value == CurrentTurnTeam);
    }

    private void FinishReselectKing()
    {
        UIManager.Instance.ShowMessage($"Team {CurrentTurnTeam + 1} has reselected their King!", 2f);
        ChessBoard.Instance.kingSelectionState = KingSelectionState.Completed;
        GameManager.Instance.SwitchTurn();
    }

    private void AdvanceKingSelectionState()
    {
        if (kingSelectionState == KingSelectionState.WhiteTeamSelecting)
        {
            ChessBoard.Instance.kingSelectionState = KingSelectionState.BlackTeamSelecting;
            UIManager.Instance.ShowMessage("Team 2, please select a piece as your King!");
        }
        else
        {
            ChessBoard.Instance.kingSelectionState = KingSelectionState.Completed;
            UIManager.Instance.ShowMessage("Both teams have chosen their Kings. The game starts!", 2f);
            UIManager.Instance.ShowCurrentTurn(0);
        }
    }

    public void ResetKingSelectionForCurrentTeam()
    {
        int currentTeam = CurrentTurnTeam;
        ChessBoard.Instance.kingSelectionState = KingSelectionState.Reselecting;

        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.Team.Value == currentTeam)
            {
                piece.SetKing(false);
                piece.GetComponent<MeshRenderer>().material = teamMaterials[currentTeam];
            }
        }

        UIManager.Instance.ShowMessage($"Team {currentTeam + 1}, please select a new King!");
    }
    #endregion
}