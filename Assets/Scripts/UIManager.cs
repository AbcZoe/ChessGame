using TMPro;
using UnityEngine;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Message UI")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;
    private CanvasGroup messageCanvasGroup;

    [Header("Persistent UI")]
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text cardDescription;

    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject resumeButton;
    [SerializeField] private GameObject exitPButton;
    private CanvasGroup pauseCanvasGroup;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text victoryText;    
    [SerializeField] private GameObject resetButton;
    [SerializeField] private GameObject exitButton;
    private CanvasGroup victoryCanvasGroup;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        messageCanvasGroup = GetOrAddCanvasGroup(messagePanel);
        victoryCanvasGroup = GetOrAddCanvasGroup(victoryPanel); 
        pauseCanvasGroup = GetOrAddCanvasGroup(pausePanel);

        if (pausePanel != null) pausePanel.SetActive(false); 
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null) return null;
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    // ===== 短暫訊息 =====
    public void ShowMessage(string text, float autoHideTime = 0f)
    {
        if (messagePanel == null || messageText == null) return;

        messageText.text = text;
        messagePanel.SetActive(true);
        UIEffect.FadeIn(messageCanvasGroup);

        if (autoHideTime > 0f)
        {
            DOVirtual.DelayedCall(autoHideTime, () =>
            {
                UIEffect.FadeOut(messageCanvasGroup);
            });
        }
    }

    // ===== 回合顯示 =====
    public void ShowCurrentTurn(int team)
    {
        if (roundText == null) return;
        roundText.text = $"Round: Team {team + 1}";
        // 可加入動畫：UIEffect.Shake(roundText.rectTransform);
    }

    public void HideRoundText()
    {
        if (roundText != null)
            roundText.text = "";
    }

    // ===== 卡片說明 =====
    public void ShowCardDescription(string text)
    {
        if (cardDescription != null)
        {
            cardDescription.text = text;
            // 可加入動畫，例如淡入
            // UIEffect.FadeIn(cardDescription.GetComponent<CanvasGroup>());
        }
    }

    public void HideCardDescription()
    {
        if (cardDescription != null)
            cardDescription.text = "";
    }

    // ===== 勝利畫面 =====
    public void ShowVictory(int winnerTeam)
    {
        if (victoryPanel == null || victoryText == null) return;

        victoryText.text = $"Team {winnerTeam + 1} Wins!";
        UIEffect.Breathe(victoryText.rectTransform, scaleFactor: 1.1f, duration: 1f);

        // 顯示面板
        victoryPanel.SetActive(true);

        // 先 Popup 整個 VictoryScreen
        UIEffect.Popup(victoryPanel);

        // 按鈕慢慢放大
        if (resetButton != null) UIEffect.ButtonPopup(resetButton, 0.6f);
        if (exitButton != null) UIEffect.ButtonPopup(exitButton, 0.6f);
    }

    public void HideVictory()
    {
        if (victoryPanel == null || victoryCanvasGroup == null) return;

        UIEffect.StopBreathe(victoryText.rectTransform);

        // 立即把 alpha 還原，防止下一次動畫看不到
        victoryCanvasGroup.alpha = 1f;
        victoryPanel.transform.localScale = Vector3.one;

        victoryPanel.SetActive(false);
    }

    // ===== 按鈕事件 =====
    public void OnPauseButton()
    {
        if (pausePanel == null || pauseCanvasGroup == null) return;
        
        // 顯示暫停面板
        pausePanel.SetActive(true);
        UIEffect.Popup(pausePanel);
        UIEffect.FadeIn(pauseCanvasGroup);
        if (resetButton != null) UIEffect.ButtonPopup(resumeButton, 0.6f);
        if (exitButton != null) UIEffect.ButtonPopup(exitPButton, 0.6f);

        // 遊戲暫停
        Time.timeScale = 0f;
        ChessBoard.Instance?.PauseGame();
    }

    public void OnResumeButton()
    {
        if (pausePanel == null || pauseCanvasGroup == null) return;

        // 停止面板動畫
        pauseCanvasGroup.alpha = 1f;
        pausePanel.transform.localScale = Vector3.one;
        pausePanel.SetActive(false);

        // 遊戲恢復
        Time.timeScale = 1f;
        ChessBoard.Instance?.ResumeGame();
    }

    public void OnResetButton()
    {
        ChessBoard board = FindAnyObjectByType<ChessBoard>();
        if (board != null)
        {
            Debug.Log("Resetting Game...");
            board.ResetBoard();
        }
    }

    public void OnExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}