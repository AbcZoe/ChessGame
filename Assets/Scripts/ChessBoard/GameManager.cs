using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    private ChessBoard chessBoard;
    public TileHighlighter tileHighlighter;

    private Dictionary<ulong, int> clientTeams = new Dictionary<ulong, int>();

    public NetworkVariable<bool> isWhiteTeamTurn = new NetworkVariable<bool>(true);
    public int CurrentTurnTeam => isWhiteTeamTurn.Value ? 0 : 1;
    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        int myTeam = NetworkManager.Singleton.IsHost ? 0 : 1;
        playerInput.Initialize(ChessBoard.Instance, tileHighlighter, myTeam);
    }

    public override void OnNetworkSpawn()
    {
        // 註冊同步事件（所有 Client 都會收到）
        isWhiteTeamTurn.OnValueChanged += OnTurnChanged;

        // 初始化 UI（確保同步後顯示正確）
        UpdateTurnUI();
    }

    public override void OnNetworkDespawn()
    {
        isWhiteTeamTurn.OnValueChanged -= OnTurnChanged;
    }


    #region Turn Control
    public void SwitchTurn()
    {
        if (IsServer)
        {
            // 直接切
            isWhiteTeamTurn.Value = !isWhiteTeamTurn.Value;
            Debug.Log($"Server 切換回合: {CurrentTurnTeam}");
        }
        else
        {
            // Client 發 RPC
            SwitchTurnServerRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SwitchTurnServerRpc()
    {
        SwitchTurn(); // 這裡會進 IsServer 分支
    }

    private void OnTurnChanged(bool oldValue, bool newValue)
    {
        UpdateTurnUI();
    }

    private void UpdateTurnUI()
    {
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
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

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ResetGameServerRpc()
    {
        ChessBoard.Instance.ResetBoard();

        //強制白隊先
        isWhiteTeamTurn.Value = true;
    }

    public void CheckMate(int winnerTeam)
    {
        ShowVictoryClientRpc(winnerTeam);
    }

    #endregion

    #region UINetControl
    [Rpc(SendTo.ClientsAndHost)]
    public void ShowMessageClientRpc(string msg, float time = 0)
    {
        StartCoroutine(WaitForUIAndShow(msg, time));
    }

    private IEnumerator WaitForUIAndShow(string msg, float time)
    {
        float timer = 0f;
        while (UIManager.Instance == null && timer < 5f) // 等 UIManager 最多 5 秒
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowMessage(msg, time);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowTurnClientRpc(int team)
    {
        UIManager.Instance?.ShowCurrentTurn(team);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void ShowVictoryClientRpc(int team)
    {
        UIManager.Instance.ShowVictory(team);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void HideVictoryClientRpc()
    {
        UIManager.Instance.HideVictory();
    }
    #endregion

    #region Pause Control
    public void SetPaused(bool value)
    {
        IsPaused = value;
    }

    // 供 UIManager 呼叫
    public void PauseGame()
    {
        SetPaused(true);
        Time.timeScale = 0f; // 停止遊戲世界，但 DOTween UI動畫仍播放
    }

    public void ResumeGame()
    {
        SetPaused(false);
        Time.timeScale = 1f;
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
        if (!(card.Data is ResetKingCard))
            SwitchTurn();
    }
    #endregion

}