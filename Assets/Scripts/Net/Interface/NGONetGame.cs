using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class NGONetGame : NetworkBehaviour, NetInterface
{
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

    public override void OnNetworkSpawn()
    {
        Services.SetNetInterface(this);
    }

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

        if (piece.Team.Value != playerTeam) return;
        if (!GameManager.Instance.IsTeamTurn(playerTeam)) return;

        ChessBoard.Instance.MoveTo(piece, targetX, targetY, sender);
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

        // �u�b Host�]Server ���a�^���
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

        bool ok = CardServerExecutor.ExecuteCard_Server(cardId, team, targetPieceNetId: null, targetTile: null, senderClientId: sender);

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

        // piece �O�_�s�b + �O�_�O�ۤv���� + �s��
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return;

        var piece = netObj.GetComponent<ChessPiece>();
        if (piece == null) return;
        if (piece.Team.Value != team) return;
        if (piece.IsDead.Value) return;

        bool ok = CardServerExecutor.ExecuteCard_Server(cardId, team, targetPieceNetId: pieceNetId, targetTile: null, senderClientId: sender);

        if (ok)
            GameManager.Instance.EndAction();
    }

}
