using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public TileHighlighter tileHighlighter;

    // ClientId -> Team
    private Dictionary<ulong, int> clientTeams = new Dictionary<ulong, int>();
    public int LocalTeam { get; private set; } = -1;

    public NetworkVariable<bool> isWhiteTeamTurn = new NetworkVariable<bool>(true);
    public int CurrentTurnTeam => isWhiteTeamTurn.Value ? 0 : 1;

    public NetworkVariable<int> ExtraActionsLeft = new(0);

    public NetworkVariable<bool> IsPaused = new NetworkVariable<bool>(false);

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            AssignTeams();
        }

        if (tileHighlighter == null)
            tileHighlighter = FindAnyObjectByType<TileHighlighter>();

        isWhiteTeamTurn.OnValueChanged += OnTurnChanged;
    }

    public override void OnNetworkDespawn()
    {
        isWhiteTeamTurn.OnValueChanged -= OnTurnChanged;
    }

    #region Team System

    private void AssignTeams()
    {
        int team = 0;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            clientTeams[clientId] = team;

            // 發送給該玩家自己的隊伍
            AssignTeamClientRpc(clientId, team);

            team = (team + 1) % 2; // 0 / 1 輪流
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void AssignTeamClientRpc(ulong clientId, int team)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;
        LocalTeam = team;
        Debug.Log($"我是 Client {clientId}，我的隊伍是 {team}");

        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        playerInput.Initialize(ChessBoard.Instance, tileHighlighter, team);
    }

    public int GetClientTeam(ulong clientId)
    {
        if (clientTeams.TryGetValue(clientId, out int team))
            return team;

        return -1;
    }

    private int[] PackPositions(List<Vector2Int> positions)
    {
        if (positions == null || positions.Count == 0) return System.Array.Empty<int>();
        int[] data = new int[positions.Count * 2];
        for (int i = 0; i < positions.Count; i++)
        {
            data[i * 2] = positions[i].x;
            data[i * 2 + 1] = positions[i].y;
        }
        return data;
    }

    private List<Vector2Int> UnpackPositions(int[] data)
    {
        var list = new List<Vector2Int>();
        if (data == null) return list;

        for (int i = 0; i + 1 < data.Length; i += 2)
            list.Add(new Vector2Int(data[i], data[i + 1]));

        return list;
    }

    #endregion

    #region Highlight
    [Rpc(SendTo.Server)]
    public void RequestHighlightServerRpc(ulong pieceNetId, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;

        // 權限：必須是已分配隊伍的玩家
        int playerTeam = GetClientTeam(sender);
        if (playerTeam == -1) return;

        // 找到那顆棋子
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return;

        var cp = netObj.GetComponent<ChessPiece>();
        if (cp == null) return;

        // 權限：只能要求自己隊伍棋子的 highlight（你想允許看敵方也可以移除）
        if (cp.Team.Value != playerTeam) return;

        // 用 server 權威棋盤算 moves/attacks
        var moves = cp.GetAvailableMoves(ref ChessBoard.Instance.chessPieces, ChessBoard.Instance.TILE_COUNT, ChessBoard.Instance.TILE_COUNT);
        var attacks = cp.GetAvailableAttacks(ref ChessBoard.Instance.chessPieces, ChessBoard.Instance.TILE_COUNT, ChessBoard.Instance.TILE_COUNT);

        int[] movesPacked = PackPositions(moves);
        int[] attacksPacked = PackPositions(attacks);

        var sendParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(sender, RpcTargetUse.Temp)
            }
        };

        ReceiveHighlightClientRpc(movesPacked, attacksPacked, sendParams);
        Debug.Log($"[Server] Highlight request from {sender} for piece {pieceNetId}");
    }
    //傳送給特定client
    [Rpc(SendTo.SpecifiedInParams)]
    private void ReceiveHighlightClientRpc(int[] movesPacked, int[] attacksPacked, RpcParams rpcParams = default)
    {
        var moves = UnpackPositions(movesPacked);
        var attacks = UnpackPositions(attacksPacked);

        // 用你 GameManager 裡的 tileHighlighter 顯示
        if (tileHighlighter != null)
            tileHighlighter.HighlightTilesFromServer(moves, attacks);

        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] ReceiveHighlight moves={movesPacked?.Length ?? 0} attacks={attacksPacked?.Length ?? 0}");
    }
    #endregion

    #region Turn Control

    public void EndAction()
    {
        if (IsServer)
        {
            // 有額外行動：消耗一次，不換手
            if (ExtraActionsLeft.Value > 0)
            {
                ExtraActionsLeft.Value--;
                Debug.Log($"Extra action consumed, remaining={ExtraActionsLeft.Value}. Team keeps turn: {CurrentTurnTeam}");
                return;
            }

            // 正常換手（這裡用你修好的 Tick->Switch 順序）
            foreach (var piece in FindObjectsByType<ChessPiece>(FindObjectsSortMode.None))
                piece.TickStatusOnTurnSwitchServer();

            isWhiteTeamTurn.Value = !isWhiteTeamTurn.Value;
            Debug.Log($"Server 切換回合: {CurrentTurnTeam}");
        }
        else
        {
            EndActionServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void EndActionServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;
        EndAction();
    }

    [Rpc(SendTo.Server)]
    private void SwitchTurnServerRpc(RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (!clientTeams.ContainsKey(sender)) return;

        int team = clientTeams[sender];

        // 只有當前回合玩家能切
        if (team != CurrentTurnTeam)
        {
            Debug.Log($"Client {sender} 想切回合，但不是他的回合");
            return;
        }

        EndAction();
    }

    private void OnTurnChanged(bool oldValue, bool newValue)
    {
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        UIManager.Instance?.ShowCurrentTurn(CurrentTurnTeam);
    }

    public bool IsTeamTurn(int team)
    {
        return team == CurrentTurnTeam;
    }

    #endregion

    #region Game Control

    public void ResetGame()
    {
        ResetGameServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void ResetGameServerRpc()
    {
        ChessBoard.Instance.ResetBoard();
        isWhiteTeamTurn.Value = true;
    }

    public void CheckMate(int winnerTeam)
    {
        ShowVictoryClientRpc(winnerTeam);
    }

    #endregion

    #region UI

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowMessageClientRpc(string msg, float time = 0)
    {
        StartCoroutine(WaitForUIAndShow(msg, time));
    }

    private IEnumerator WaitForUIAndShow(string msg, float time)
    {
        float timer = 0f;
        while (UIManager.Instance == null && timer < 5f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        UIManager.Instance?.ShowMessage(msg, time);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowTurnClientRpc(int team) 
    {
        UIManager.Instance?.ShowCurrentTurn(team); 
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowVictoryClientRpc(int team)
    {
        UIManager.Instance?.ShowVictory(team);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void HideVictoryClientRpc()
    {
        UIManager.Instance?.HideVictory();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestRematchServerRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong sender = rpcParams.Receive.SenderClientId;

        // 只在 Host（Server 本地）顯示
        UIManager.Instance?.ShowMessage("The opponent requested another round.", 2f);
    }
    #endregion

    #region Pause

    public void PauseGame()
    {
        SetPauseServerRpc(true);
    }

    public void ResumeGame()
    {
        SetPauseServerRpc(false);
    }

    [Rpc(SendTo.Server)]
    private void SetPauseServerRpc(bool value)
    {
        IsPaused.Value = value;
        SetPauseClientRpc(value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetPauseClientRpc(bool value)
    {
        Time.timeScale = value ? 0f : 1f;
    }

    #endregion

    #region Card Control 
    public void ResetKingForTeam(int team) 
    { 
        // 先切換 KingManager 狀態
        KingManager.Instance?.ResetKingSelectionForCurrentTeam(); 
    }

    public void GrantExtraActionServer(int count = 1)
    {
        if (!IsServer) return;
        ExtraActionsLeft.Value += Mathf.Max(1, count);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GrantExtraActionServerRpc(int count, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        // 權限：只能給自己隊伍（可選但建議）
        int playerTeam = GetClientTeam(rpcParams.Receive.SenderClientId);
        if (playerTeam == -1) return;
        if (playerTeam != CurrentTurnTeam) return; // 只能自己回合用

        GrantExtraActionServer(count);
    }
    #endregion
}