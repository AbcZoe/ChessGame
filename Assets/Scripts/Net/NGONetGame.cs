using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class NGONetGame : NetworkBehaviour, NetInterface
{
    public override void OnNetworkSpawn()
    {
        Services.SetNetGame(this);
    }

    public bool IsAuthoritative => IsServer;
    public int LocalTeam => GameManager.Instance != null ? GameManager.Instance.LocalTeam : -1;

    public void RequestMove(ulong pieceNetId, int targetX, int targetY)=> RequestMoveServerRpc(pieceNetId, targetX, targetY);
    public void RequestHighlight(ulong pieceNetId) => ForwardHighlightServerRpc(pieceNetId);
    public void RequestSelectKing(ulong pieceNetId) => SelectKingServerRpc(pieceNetId);
    public void RequestEndAction() => EndActionServerRpc();
    public void RequestRematch() => RequestRematchServerRpc();

    // Card
    public void RequestUseCard(int cardId) => UseCardServerRpc(cardId);
    public void RequestUseCardOnPiece(int cardId, ulong pieceNetId) => UseCardOnPieceServerRpc(cardId, pieceNetId);

    //RPC
    #region MoveRequest

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(ulong pieceNetId, int targetX, int targetY, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return;

        var piece = netObj.GetComponent<ChessPiece>();
        if (piece == null) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        int playerTeam = GameManager.Instance.GetClientTeam(sender);
        if (playerTeam == -1) return;

        // 權限：只能走自己的棋 + 必須自己回合（你原本少了“回合”檢查）
        if (piece.Team.Value != playerTeam) return;
        if (!GameManager.Instance.IsTeamTurn(playerTeam)) return;

        ChessBoard.Instance.MoveTo(piece, targetX, targetY);
    }

    #endregion
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SelectKingServerRpc(ulong pieceId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        KingManager.Instance.HandleSelectKingRequest_Server(rpcParams.Receive.SenderClientId, pieceId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ForwardHighlightServerRpc(ulong pieceNetId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        GameManager.Instance.HandleHighlightRequest_Server(rpcParams.Receive.SenderClientId, pieceNetId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void EndActionServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        GameManager.Instance.EndAction();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRematchServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;

        // 只在 Host（Server 本地）顯示
        UIManager.Instance?.ShowMessage("The opponent requested another round.", 2f);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UseCardServerRpc(int cardId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        int team = GameManager.Instance.GetClientTeam(sender);
        if (team == -1) return;

        if (team != GameManager.Instance.CurrentTurnTeam) return;
        if (GameManager.Instance.IsPaused.Value) return;
        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed) return;

        bool ok = CardServerExecutor.ExecuteCard_Server(cardId, team, targetPieceNetId: null);

        if (ok)
            GameManager.Instance.EndAction();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UseCardOnPieceServerRpc(int cardId, ulong pieceNetId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        int team = GameManager.Instance.GetClientTeam(sender);
        if (team == -1) return;

        if (team != GameManager.Instance.CurrentTurnTeam) return;
        if (GameManager.Instance.IsPaused.Value) return;
        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed) return;

        // piece 是否存在 + 是否是自己隊伍 + 存活
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return;

        var piece = netObj.GetComponent<ChessPiece>();
        if (piece == null) return;
        if (piece.Team.Value != team) return;
        if (piece.IsDead.Value) return;

        bool ok = CardServerExecutor.ExecuteCard_Server(cardId, team, pieceNetId);

        if (ok)
            GameManager.Instance.EndAction();
    }
}
