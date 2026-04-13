using Unity.Netcode;
using UnityEngine;
using static ChessPiece;

public class KingManager : NetworkBehaviour
{
    public static KingManager Instance;

    private void Awake()
    {
        Instance = this;
    }


    private bool IsValidSelection(ChessPiece cp, KingSelectionState state)
    {
        return (state == KingSelectionState.WhiteTeamSelecting && cp.Team.Value == 0) ||
               (state == KingSelectionState.BlackTeamSelecting && cp.Team.Value == 1) ||
               (state == KingSelectionState.Reselecting && cp.Team.Value == GameManager.Instance.CurrentTurnTeam);
    }

    private void ClearTeamKing(int team)
    {
        foreach (var piece in ChessBoard.Instance.chessPieces)
        {
            if (piece != null && piece.Team.Value == team && piece.isKing.Value)
            {
                piece.isKing.Value = false;
            }
        }
    }

    private void AdvanceState(ChessBoard board, KingSelectionState state)
    {
        if (state == KingSelectionState.WhiteTeamSelecting)
        {
            ChessBoard.Instance.SetKingState_Server(KingSelectionState.BlackTeamSelecting); 
        }
        else if (state == KingSelectionState.BlackTeamSelecting)
        {
            ChessBoard.Instance.SetKingState_Server(KingSelectionState.Completed);
        }
        else if (state == KingSelectionState.Reselecting)
        {
            ChessBoard.Instance.SetKingState_Server(KingSelectionState.Completed);
            GameManager.Instance.EndAction();
        }
    }

    #region SelectKing
    public void HandleSelectKingRequest_Server(ulong senderClientId, ulong pieceId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceId, out var netObj))
            return;

        ChessPiece cp = netObj.GetComponent<ChessPiece>();
        if (cp == null) return;

        var board = ChessBoard.Instance;
        var state = board.kingSelectionState.Value;

        // 不合法直接拒絕
        if (!IsValidSelection(cp, state))
            return;

        cp.isKing.Value = true;

        // 推進流程
        AdvanceState(board, state);

    }

    public void HandleResetKingSelection_Server(int team)
    {
        int currentTeam = team;

        ChessBoard.Instance.SetKingState_Server(KingSelectionState.Reselecting);

        ClearTeamKing(currentTeam);
    }

    #endregion
}