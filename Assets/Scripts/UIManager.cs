using System.Collections;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Temporary Message UI")]
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Persistent UI")]
    [SerializeField] private TMP_Text roundText;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text victoryText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ===== 提示訊息與畫面 =====
    // 短暫訊息
    public void ShowMessage(string text, float autoHideTime = 0f)
    {
        if (messagePanel == null || messageText == null) return;

        messagePanel.SetActive(true);
        messageText.text = text;

        if (autoHideTime > 0f)
            StartCoroutine(HideMessageText(autoHideTime));
    }
    private IEnumerator HideMessageText(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (messageText != null)
            messageText.text = ""; // 只清空文字，不關閉 Panel
    }

    // 持續顯示回合
    public void ShowCurrentTurn(int team)
    {
        if (roundText == null) return;
        roundText.text = $"Round: Team {team + 1}";
    }
    // 隱藏回合文字
    public void HideRoundText()
    {
        if (roundText != null)
            roundText.text = "";
    }

    // 勝利畫面
    public void ShowVictory(int winnerTeam)
    {
        if (victoryPanel == null || victoryText == null) return;
        victoryPanel.SetActive(true);
        victoryText.text = $"Team {winnerTeam + 1} Wins!";
    }
    public void HideVictory()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    // ===== 按鈕事件 =====
    // 重置遊戲
    public void OnResetButton()
    {
        ChessBoard board = FindAnyObjectByType<ChessBoard>();
        if (board != null)
        {
            board.ResetBoard();
        }
    }
    // 離開遊戲
    public void OnExitButton()
    {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // 在 Editor 停止遊戲
    #else
            Application.Quit(); // Build 出來時關閉應用程式
    #endif
    }

}
