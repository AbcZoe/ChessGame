using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

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

    [Header("Audio Data")]
    [SerializeField] private AudioData clickUIAudioData;
    [SerializeField] private AudioData messageUIAudioData;

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
        AudioManager.Instance.PlaySFX(messageUIAudioData);
        if (messagePanel == null || messageText == null) return;

        messageText.text = text;

        messagePanel.SetActive(true);

        // 重置狀態（避免動畫殘留）
        messagePanel.transform.localScale = Vector3.zero;
        messageCanvasGroup.alpha = 0f;

        UIEffect.Popup(messagePanel);
        UIEffect.FadeIn(messageCanvasGroup);

        if (autoHideTime > 0f)
        {
            DOVirtual.DelayedCall(autoHideTime, () =>
            {
                HideMessage();
            });
        }
    }

    public void HideMessage()
    {
        UIEffect.FadeOut(messageCanvasGroup);

        // 等淡出完成再關閉
        DOVirtual.DelayedCall(0.3f, () =>
        {
            messagePanel.SetActive(false);
        });
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

    // ===== 卡片說明 =====
    public void ShowCardDescription(string name, string description)
    {
        if (cardDescription != null)
        {
            string x = name +
                "\ndescription:\n" + description;
            cardDescription.text = x;
        }
    }

    public void HideCardDescription()
    {
        if (cardDescription != null)
            cardDescription.text = "";
    }
    #region Button Events
    public void OnPauseButton()
    {
        if (pausePanel == null || pauseCanvasGroup == null) return;
        AudioManager.Instance.PlaySFX(clickUIAudioData);

        // 顯示暫停面板
        pausePanel.SetActive(true);
        UIEffect.Popup(pausePanel);
        UIEffect.FadeIn(pauseCanvasGroup);
        if (resetButton != null) UIEffect.ButtonPopup(resumeButton, 0.6f);
        if (exitButton != null) UIEffect.ButtonPopup(exitPButton, 0.6f);

        // 遊戲暫停
        Time.timeScale = 0f;
        GameManager.Instance?.PauseGame();
    }

    public void OnResumeButton()
    {
        if (pausePanel == null || pauseCanvasGroup == null) return;
        AudioManager.Instance.PlaySFX(clickUIAudioData);

        // 停止面板動畫
        pauseCanvasGroup.alpha = 1f;
        pausePanel.transform.localScale = Vector3.one;
        pausePanel.SetActive(false);

        // 遊戲恢復
        Time.timeScale = 1f;
        GameManager.Instance?.ResumeGame();
    }

    public void OnResetButton()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        // Host：真的重置
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            ChessBoard board = FindAnyObjectByType<ChessBoard>();
            if (board != null)
            {
                Debug.Log("Resetting Game...");
                board.ResetBoard();
            }
            return;
        }

        // Client：送訊息給 Host，不重置
        Services.NetInterface?.RequestRematch();
        ShowMessage("Message has been sent to the other player.", 2f);
    }

    public void OnQuitButton()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        Time.timeScale = 1f;
        // 優先走網路通知（GameScene 會有 QuitNotifier）
        if (QuitNotifier.Instance != null)
        {
            QuitNotifier.Instance.NotifyQuitAndLeave();
            return;
        }

        // 保底（例如不在 GameScene）
        LocalReturnToMenu();
    }
    public void LocalReturnToMenu()
    {
        LanAutoMatchmaker.Instance.CancelMatch();
        
        SceneManager.LoadScene("StartScene");
    }

    public void ForceReturnToMenuAfterDelay(float seconds)
    {
        DOVirtual.DelayedCall(seconds, () =>
        {
            LocalReturnToMenu();
        });
    }
    #endregion
}