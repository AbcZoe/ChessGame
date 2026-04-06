using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class ChessPiece : NetworkBehaviour
{
    public NetworkVariable<int> Team = new();
    public NetworkVariable<int> X = new();
    public NetworkVariable<int> Y = new();
    public NetworkVariable<bool> isKing = new(false);

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 100f; // Inspector 可調比例大小

    private MeshRenderer rend;

    private void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        desiredScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

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

        gameObject.SetActive(false);
    }

    #region Server Methods
    public NetworkObject NetObj => GetComponent<NetworkObject>();
    public ulong NetId => NetObj != null ? NetObj.NetworkObjectId : 0;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // 這裡保證每個 Client 都會把材質套好
        var rend = GetComponent<MeshRenderer>();
        if (rend != null && ChessBoard.Instance != null)
        {
            rend.material = ChessBoard.Instance.teamMaterials[Team.Value];
        }
        // 訂閱 NetworkVariable 變化
        Team.OnValueChanged += (oldValue, newValue) => UpdateVisual();
        X.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        Y.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        isKing.OnValueChanged += (oldValue, newValue) => UpdateKingVisual();        

        UpdatePosition();
        UpdateVisual();

        // 登記到棋盤陣列
        if (ChessBoard.Instance != null && ChessBoard.Instance.chessPieces != null)
        {
            ChessBoard.Instance.chessPieces[X.Value, Y.Value] = this;
        }
    }

    // 這個方法只在本地呼叫
    public void RequestMove(int targetX, int targetY)
    {
        if (!IsOwner) return; // 只能自己發起移動請求
        RequestMoveServerRpc(targetX, targetY);
    }

    // ServerRpc 寫法：所有人都可呼叫，Server 驗證
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMoveServerRpc(int targetX, int targetY)
    {
        if (!IsServer) return;

        // 這裡直接用 Owner 判斷
        if (OwnerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        bool validMove = ChessBoard.Instance.MoveTo(this, targetX, targetY);

        if (!validMove)
        {
            RevertPositionClientRpc(X.Value, Y.Value);
        }
    }

    // ClientRpc：還原位置
    [ClientRpc]
    private void RevertPositionClientRpc(int x, int y, ClientRpcParams clientRpcParams = default)
    {
        SetPosition(ChessBoard.Instance.GetTileCenter(x, y), true);
    }

    public void UpdateVisual()
    {
        if (ChessBoard.Instance == null) return;

        if (rend != null)
            rend.material = ChessBoard.Instance.teamMaterials[Team.Value];
    }

    void UpdatePosition()
    {
        if (ChessBoard.Instance == null) return;

        Vector3 pos = ChessBoard.Instance.GetTileCenter(X.Value, Y.Value);
        SetPosition(pos, true);
    }

    private void UpdateKingVisual()
    {
        if (rend != null && isKing.Value)
            rend.material = ChessBoard.Instance.KingMaterials[Team.Value];
        else
            UpdateVisual();
    }

    #endregion
}
