using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ChessPiece : NetworkBehaviour
{
    public NetworkVariable<int> Team = new();
    public NetworkVariable<int> X;
    public NetworkVariable<int> Y;
    public NetworkVariable<bool> isKing = new(false);

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 100f; // Inspector 可調比例大小
    public bool IsDragging = false;
    
    private MeshRenderer rend;

    private void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        desiredScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        if (IsDragging) return;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    #region move & attack & reset

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale * baseScale;
        if (force)
            transform.localScale = desiredScale;
    }

    public void SetKing(bool value)
    {
        isKing.Value = value;
    }

    public void ResetPiece()
    {
        if (IsServer)
        {
            Team.Value = 0;
            X.Value = 0;
            Y.Value = 0;
            isKing.Value = false;
        }

        desiredScale = Vector3.one * baseScale;
        transform.localScale = desiredScale;

        NetworkObject.Despawn(true);
    }

    #endregion

    #region Server Methods
    public NetworkObject NetObj => GetComponent<NetworkObject>();
    public ulong NetId => NetObj != null ? NetObj.NetworkObjectId : 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 訂閱 NetworkVariable 變化
        Team.OnValueChanged += (oldValue, newValue) => UpdateVisual();
        X.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        Y.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        isKing.OnValueChanged += (oldValue, newValue) => UpdateKingVisual();
        StartCoroutine(WaitForBoardReady());

        Debug.Log($"[NetSpawn] {name} NetId={NetworkObjectId} IsServer={IsServer}");
    }

    IEnumerator WaitForBoardReady()
    {
        // 等到 ChessBoard 完全 ready
        while (ChessBoard.Instance == null ||
               ChessBoard.Instance.teamMaterials == null ||
               ChessBoard.Instance.teamMaterials.Length == 0)
        {
            yield return null;
        }

        var board = ChessBoard.Instance;

        board.chessPieces[X.Value, Y.Value] = this;
        Debug.Log($"Register piece at {X.Value},{Y.Value}");

        UpdatePosition();
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (ChessBoard.Instance == null) return;

        if (rend != null)
            rend.material = ChessBoard.Instance.teamMaterials[Team.Value];
    }

    void UpdatePosition()
    {
        // 拖曳時不要被 NetworkVariable 拉回去
        if (IsDragging) return;

        var board = ChessBoard.Instance;
        if (board == null) return;

        SetPosition(board.GetTileCenter(X.Value, Y.Value));
    }

    private void UpdateKingVisual()
    {
        if (rend != null && isKing.Value)
            rend.material = ChessBoard.Instance.KingMaterials[Team.Value];
        else
            UpdateVisual();
    }

    #region MoveRequest
    public void RequestMove(int targetX, int targetY)
    {
        RequestMoveServerRpc(targetX, targetY);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(int targetX, int targetY, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 驗證這個 requester 是否有權移動此棋子（隊伍判斷）
        int pieceTeam = Team.Value;
        ulong sender = rpcParams.Receive.SenderClientId;
        int playerTeam = GameManager.Instance.GetClientTeam(sender);

        if (playerTeam == -1) return;
        if (Team.Value != playerTeam) return;
        if (pieceTeam != playerTeam) return; // 非自己隊伍棋子不能移動

        bool validMove = ChessBoard.Instance.MoveTo(this, targetX, targetY);

    }

    #endregion

    #endregion
}
