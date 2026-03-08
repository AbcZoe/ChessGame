using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public TileHighlighter tileHighlighter;

    private bool isWhiteTeamTurn = true;
    public int CurrentTurnTeam => isWhiteTeamTurn ? 0 : 1;
    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>();
        playerInput.Initialize(ChessBoard.Instance, tileHighlighter);
    }

    #region Game Control

    public void SwitchTurn()
    {
        isWhiteTeamTurn = !isWhiteTeamTurn;
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
    }

    public bool IsTeamTurn(int team)
    {
        return team == CurrentTurnTeam;
    }

    public void CheckMate(int winnerTeam)
    {
        UIManager.Instance.ShowVictory(winnerTeam);
    }

    public void ResetGame()
    {
        ChessBoard.Instance.ResetBoard();

        isWhiteTeamTurn = true;
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
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