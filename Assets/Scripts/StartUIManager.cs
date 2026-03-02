using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartUIManager : MonoBehaviour
{
    [Header("Start UI")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private GameObject StartPanelBackground;
    [SerializeField] private TMP_Text messageText;

    [Header("Choose Mode UI")]
    [SerializeField] private GameObject ModePanel;
    [SerializeField] private GameObject ModePanelBackground;
    [SerializeField] private GameObject PvPButton;
    [SerializeField] private GameObject PvCButton;

    private CanvasGroup modeCanvasGroup;

    private void Awake()
    {
        // 確保時間恢復（非常重要）
        Time.timeScale = 1f;

        if (ModePanel != null)
        {
            modeCanvasGroup = ModePanel.GetComponent<CanvasGroup>();
            if (modeCanvasGroup == null)
                modeCanvasGroup = ModePanel.AddComponent<CanvasGroup>();
        }

        if (ModePanel != null) ModePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (messageText != null)
        {
            UIEffect.Breathe(messageText.rectTransform, 1.1f, 1f);
        }
    }

    // 顯示 ModePanel
    private void ShowModePanel()
    {
        if (StartPanel != null) StartPanel.SetActive(false);
        if (ModePanel == null) return;

        ModePanel.SetActive(true);

        UIEffect.Popup(ModePanel);
        UIEffect.FadeIn(modeCanvasGroup);

        if (PvPButton != null)
            UIEffect.ButtonPopup(PvPButton, 0.6f);
        if (PvCButton != null)
            UIEffect.ButtonPopup(PvCButton, 0.6f);
    }

    // 隱藏 ModePanel
    private void HideModePanel()
    {
        if (ModePanel == null) return;

        // 面板淡出 + 縮小
        UIEffect.FadeOut(modeCanvasGroup, 0.3f);
        ModePanel.SetActive(false);

        // 可選：回到 StartPanel
        if (StartPanel != null) StartPanel.SetActive(true);
    }

    public void OnShowModePClicked()
    {
        ShowModePanel();
    }

    public void OnCloseModeClicked()
    {
        HideModePanel();
    }


    public void OnPvPClicked()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void OnPvCClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}