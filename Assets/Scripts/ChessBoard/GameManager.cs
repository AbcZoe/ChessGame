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

    public NetworkVariable<bool> IsPaused =
    new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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
    public void HandleHighlightRequest_Server(ulong senderClientId, ulong pieceNetId)
    {
        if (!IsServer) return;

        int playerTeam = GetClientTeam(senderClientId);
        if (playerTeam == -1) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return;

        var cp = netObj.GetComponent<ChessPiece>();
        if (cp == null) return;
        if (cp.Team.Value != playerTeam) return;

        var moves = cp.GetAvailableMoves(ref ChessBoard.Instance.chessPieces, ChessBoard.Instance.TILE_COUNT, ChessBoard.Instance.TILE_COUNT);
        var attacks = cp.GetAvailableAttacks(ref ChessBoard.Instance.chessPieces, ChessBoard.Instance.TILE_COUNT, ChessBoard.Instance.TILE_COUNT);

        int[] movesPacked = PackPositions(moves);
        int[] attacksPacked = PackPositions(attacks);

        var sendParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(senderClientId, RpcTargetUse.Temp)
            }
        };

        ReceiveHighlightClientRpc(movesPacked, attacksPacked, sendParams);
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

        ulong sender = rpcParams.Receive.SenderClientId;
        int team = GetClientTeam(sender);
        if (team == -1) return;

        // 只有自己回合才能結束行動 / 換手
        if (team != CurrentTurnTeam) return;

        EndAction();
    }

    private void OnTurnChanged(bool oldValue, bool newValue)
    {
        UpdateTurnUI();
    }

    public void UpdateTurnUI()
    {
        UIManager.Instance?.ShowCurrentTurn(CurrentTurnTeam);
    }

    public bool IsTeamTurn(int team)
    {
        return team == CurrentTurnTeam;
    }

    #endregion

    #region Game Control

    public void CheckMate(int winnerTeam)
    {
        ShowVictoryClientRpc(winnerTeam);
    }

    #endregion

    #region UI

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowMessageTargetRpc(string msg, float time = 0, RpcParams rpcParams = default)
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

    public void ShowMessageToClient(string msg, float time, ulong clientId)
    {
        var sendParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
            }
        };

        ShowMessageTargetRpc(msg, time, sendParams);
    }

    public void ShowMessageToClient(string msg, float time, RpcParams sendParams)
    {
        ShowMessageTargetRpc(msg, time, sendParams);
    }

    public void ShowMessageToOthers(string msg, float time, ulong senderClientId)
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == senderClientId) continue;

            var sendParams = new RpcParams
            {
                Send = new RpcSendParams
                {
                    Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
                }
            };

            ShowMessageTargetRpc(msg, time, sendParams);
        }
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetPauseServerRpc(bool value, RpcParams rpcParams = default)
    {
        if (!IsServer) return;

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
    public void GrantExtraActionServer(int count = 1)
    {
        if (!IsServer) return;
        ExtraActionsLeft.Value += Mathf.Max(1, count);
    }

    #endregion
}