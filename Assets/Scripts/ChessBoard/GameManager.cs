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

    public NetworkVariable<bool> isWhiteTeamTurn = new NetworkVariable<bool>(true);
    public int CurrentTurnTeam => isWhiteTeamTurn.Value ? 0 : 1;

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

        isWhiteTeamTurn.OnValueChanged += OnTurnChanged;
        UpdateTurnUI();
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

    [ClientRpc]
    private void AssignTeamClientRpc(ulong clientId, int team)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

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

    #endregion

    #region Turn Control（安全版）

    public void SwitchTurn()
    {
        if (IsServer)
        {
            isWhiteTeamTurn.Value = !isWhiteTeamTurn.Value;
            Debug.Log($"Server 切換回合: {CurrentTurnTeam}");
        }
        else
        {
            SwitchTurnServerRpc();
        }
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

        SwitchTurn();
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

    [ClientRpc]
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

    [ClientRpc]
    public void ShowTurnClientRpc(int team) 
    {
        UIManager.Instance?.ShowCurrentTurn(team); 
    }

    [ClientRpc]
    public void ShowVictoryClientRpc(int team)
    {
        UIManager.Instance?.ShowVictory(team);
    }

    [ClientRpc]
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

    [Rpc(SendTo.Server)]
    private void SetPauseServerRpc(bool value)
    {
        IsPaused.Value = value;
        SetPauseClientRpc(value);
    }

    [ClientRpc]
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

    public void UseCard(CardView card) 
    { 
        // 使用卡牌效果
        card.Use(); 
        // 如果不是 ResetKingCard 就切換回合
        if (!(card.Data is ResetKingCard)) SwitchTurn(); 
    } 
    #endregion
}