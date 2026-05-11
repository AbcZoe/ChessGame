using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;
using System.Threading.Tasks;

public enum PromotionType
{
    Queen,
    Rook,
    Bishop,
    Knight
}
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

    [Header("RoundChange UI")]
    [SerializeField] private GameObject RoundChangePanel;
    [SerializeField] private TMP_Text roundText_inchange;

    [Header("Promotion UI")]
    [SerializeField] private GameObject PromotionPanel;
    [SerializeField] private Button[] promotionButtons;

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
        if (team == 0)
        {
            roundText.text = $"Round:Yellow Team";
        }else if (team == 1)
        {
            roundText.text = $"Round:Purple Team";
        }
    }

    public void HideRoundText()
    {
        if (roundText != null)
            roundText.text = "";
    }

    public void ShowRoundChange(float autoHideTime = 0f)
    {
        if (roundText_inchange == null || RoundChangePanel == null) return;

        AudioManager.Instance.PlaySFX(messageUIAudioData);
        roundText_inchange.text = roundText.text; // 顯示同樣的回合資訊
        RoundChangePanel.SetActive(true);

        UIEffect.Popup(RoundChangePanel);
        if (autoHideTime > 0f)
        {
            DOVirtual.DelayedCall(autoHideTime, () =>
            {
                HideRoundChange();
            });
        }
    }

    public void HideRoundChange()
    {
        if (RoundChangePanel != null)
        // 等淡出完成再關閉
        DOVirtual.DelayedCall(0.3f, () =>
        {
            RoundChangePanel.SetActive(false);
        });
    }

    // ===== 升變選擇 ===== 
    public void ShowPromotionOptions(Action<PromotionType> onSelect)
    {
        GameManager.Instance.SetPauseServerRpc(true);
        if (PromotionPanel == null || promotionButtons == null) return;

        PromotionPanel.SetActive(true);

        for (int i = 0; i < promotionButtons.Length; i++)
        {
            PromotionType type = (PromotionType)i;

            var btn = promotionButtons[i];

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlaySFX(clickUIAudioData);
                onSelect?.Invoke(type);
                HidePromotionOptions();
                GameManager.Instance.SetPauseServerRpc(false);
            });
        }
    }

    public void HidePromotionOptions()
    {
        if (PromotionPanel != null)
            PromotionPanel.SetActive(false);

        // 清 listener（保險）
        foreach (var btn in promotionButtons)
            btn.onClick.RemoveAllListeners();
    }

    // ===== 勝利畫面 =====
    public void ShowVictory(int winnerTeam)
    {
        if (victoryPanel == null || victoryText == null) return;
        if(winnerTeam == 2)
            victoryText.text = $"Draw!!!";
        else
            if (winnerTeam == 0)
                victoryText.text = $"Yellow Team Wins!";
            else if (winnerTeam == 1)
                victoryText.text = $"Purple Team Wins!";
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
        Debug.Log("Show Card");
        Debug.Log(cardDescription);
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

    //按鈕事件
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

        if (QuitNotifier.Instance != null)
        {
            QuitNotifier.Instance.NotifyQuitAndLeave();
            return;
        }

        _ = ReturnToMenuAsync();
    }

    public async Task ReturnToMenuAsync()
    {
        try
        {
            if (MatchSystem.Instance != null)
            {
                await MatchSystem.Instance.ExitMatch();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                $"[UI] 返回主選單清理失敗：{e.Message}");
        }

        SceneManager.LoadScene("StartScene");
    }
    public void ForceReturnToMenuAfterDelay(float seconds)
    {
        DOVirtual.DelayedCall(seconds, () =>
        {
            _ = ReturnToMenuAsync();
        });
    }
    #endregion
}