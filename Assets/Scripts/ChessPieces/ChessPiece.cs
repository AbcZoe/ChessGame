using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChessPiece : NetworkBehaviour
{

    [Header("Piece Settings")]
    public NetworkVariable<int> Team = new();
    public NetworkVariable<int> X = new();
    public NetworkVariable<int> Y = new();
    public NetworkVariable<bool> isKing = new(false);
    public NetworkVariable<bool> IsDead = new(false);
    public NetworkVariable<int> DeadIndex = new(-1);

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 100f; // Inspector 可調比例大小
    public bool IsDragging = false;


    [Header("Piecce Status")]
    public NetworkVariable<bool> MutualDestructionActive = new(false); // 是否正在同歸於盡狀態
    public NetworkVariable<int> MutualDestructionTurnsLeft = new(0); // 2 = 還能撐到下一次換手前，因為選完(用完卡後)之後會換回合，所以是2
    public NetworkVariable<bool> IndestructibleActive = new(false); //是否正在無敵狀態
    public NetworkVariable<int> IndestructibleTurnsLeft = new(0);

    private MeshRenderer rend;

    private void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        desiredScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        //if (IsDragging) return;
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
        if (!IsServer) return;

        Team.Value = 0;
        X.Value = 0;
        Y.Value = 0;
        isKing.Value = false;

        desiredScale = Vector3.one * baseScale;
        transform.localScale = desiredScale;

        NetworkObject.Despawn(true);
    }

    #endregion

    #region Server Methods
    public NetworkObject NetObj => GetComponent<NetworkObject>();
    public ulong NetId => NetObj != null ? NetObj.NetworkObjectId : 0;

    private Coroutine applyDeadRoutine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // 訂閱 NetworkVariable 變化
        Team.OnValueChanged += (oldValue, newValue) => UpdateVisual();
        X.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        Y.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        isKing.OnValueChanged += (oldValue, newValue) => UpdateKingVisual();
        IsDead.OnValueChanged += OnDeadChanged;
        DeadIndex.OnValueChanged += OnDeadIndexChanged;

        ApplyDeadState(); // 讓 late join 也能立即套用
        StartCoroutine(WaitForBoardReady());
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
        //if (IsDead.Value) return;

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

    private void OnDeadChanged(bool oldV, bool newV) => ApplyDeadState();
    private void OnDeadIndexChanged(int oldV, int newV) => ApplyDeadState();

    private void ApplyDeadState()
    {
        if (!IsSpawned) return;

        if (applyDeadRoutine != null)
            StopCoroutine(applyDeadRoutine);

        applyDeadRoutine = StartCoroutine(ApplyDeadStateWhenReady());
    }

    private IEnumerator ApplyDeadStateWhenReady()
    {
        // 等 GraveyardManager ready（避免 client 端 Instance 為 null）
        while (IsDead.Value && GraveyardManager.Instance == null)
            yield return null;

        // 死亡：放進死亡區
        if (IsDead.Value)
        {
            GraveyardManager.Instance.AddDeadPiece(this, DeadIndex.Value);
            yield break;
        }

        // 非死亡：回到棋盤格（處理 reset/復活）
        var board = ChessBoard.Instance;
        while (board == null)
            yield return null;

        SetScale(Vector3.one, true); // 可選：恢復大小（看你想不想）
        SetPosition(board.GetTileCenter(X.Value, Y.Value));
    }

    #endregion

    #region Cards

    // 同歸於盡：持續幾次「換手」(turn switch)
    public void ApplyMutualDestructionServer(int turns = 1)
    {
        if (!IsServer) return;

        MutualDestructionActive.Value = true;
        MutualDestructionTurnsLeft.Value = Mathf.Max(1, turns);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyMutualDestructionServerRpc(int turns, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 權限：只能對自己的隊伍施放
        ulong sender = rpcParams.Receive.SenderClientId;
        int playerTeam = GameManager.Instance.GetClientTeam(sender);
        if (playerTeam == -1) return;
        if (Team.Value != playerTeam) return;
        if (IsDead.Value) return;

        ApplyMutualDestructionServer(turns);
    }

    public void ClearMutualDestructionServer()
    {
        if (!IsServer) return;

        MutualDestructionActive.Value = false;
        MutualDestructionTurnsLeft.Value = 0;
    }

    // 無敵：持續幾次「換手」(turn switch)
    public void ApplyIndestructibleServer(int turns = 1)
    {
        if (!IsServer) return;
        IndestructibleActive.Value = true;
        IndestructibleTurnsLeft.Value = Mathf.Max(1, turns);
    }

    public void ClearIndestructibleServer()
    {
        if (!IsServer) return;
        IndestructibleActive.Value = false;
        IndestructibleTurnsLeft.Value = 0;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ApplyIndestructibleServerRpc(int turns, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 權限：只能對自己的棋子施放
        ulong sender = rpcParams.Receive.SenderClientId;
        int playerTeam = GameManager.Instance.GetClientTeam(sender);

        if (playerTeam == -1) return;
        if (Team.Value != playerTeam) return;
        if (IsDead.Value) return;

        ApplyIndestructibleServer(turns);
    }

    public void TickStatusOnTurnSwitchServer()
    {
        if (!IsServer) return;

        // 同歸於盡倒數
        if (MutualDestructionActive.Value)
        {
            MutualDestructionTurnsLeft.Value--;
            if (MutualDestructionTurnsLeft.Value <= 0)
            {
                MutualDestructionActive.Value = false;
                MutualDestructionTurnsLeft.Value = 0;
            }
        }

        // 不滅倒數
        if (IndestructibleActive.Value)
        {
            IndestructibleTurnsLeft.Value--;
            if (IndestructibleTurnsLeft.Value <= 0)
            {
                IndestructibleActive.Value = false;
                IndestructibleTurnsLeft.Value = 0;
            }
        }
    }

    #endregion
}
