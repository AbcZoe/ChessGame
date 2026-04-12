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

    public void SelectKingPiece(ChessPiece cp)
    {
        SelectKingServerRpc(cp.NetworkObjectId);
    }

    [Rpc(SendTo.Server)]
    private void SelectKingServerRpc(ulong pieceId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceId, out var netObj))
            return;

        ChessPiece cp = netObj.GetComponent<ChessPiece>();
        if (cp == null) return;

        var board = ChessBoard.Instance;
        var state = board.kingSelectionState.Value;

        // 不合法直接拒絕
        if (!IsValidSelection(cp, state))
            return;

        // 清除舊 King（同隊）
        ClearTeamKing(cp.Team.Value);

        // 設新 King
        cp.isKing.Value = true;

        // 推進流程
        AdvanceState(board, state);
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
            board.RequestSetKingStateServerRpc(KingSelectionState.BlackTeamSelecting);
        }
        else if (state == KingSelectionState.BlackTeamSelecting)
        {
            board.RequestSetKingStateServerRpc(KingSelectionState.Completed);
        }
        else if (state == KingSelectionState.Reselecting)
        {
            board.RequestSetKingStateServerRpc(KingSelectionState.Completed);
            GameManager.Instance.EndAction();
        }
    }

    #region ResetKingSelection
    public void ResetKingSelectionForCurrentTeam()
    {
        ResetKingSelectionServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void ResetKingSelectionServerRpc()
    {
        int currentTeam = GameManager.Instance.CurrentTurnTeam;

        ChessBoard.Instance.RequestSetKingStateServerRpc(KingSelectionState.Reselecting);

        foreach (var piece in ChessBoard.Instance.chessPieces)
        {
            if (piece != null && piece.Team.Value == currentTeam)
            {
                piece.isKing.Value = false; // 直接改 NetworkVariable
            }
        }

    }
    #endregion
}