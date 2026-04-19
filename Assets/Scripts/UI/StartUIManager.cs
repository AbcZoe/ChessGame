using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;

public class StartUIManager : MonoBehaviour
{
    [Header("Start UI")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private GameObject StartPanelBackground;
    [SerializeField] private TMP_Text messageText;

    [Header("Choose Mode UI")]
    [SerializeField] private GameObject ModePanel;
    [SerializeField] private GameObject ModePanelBackground;
    [SerializeField] private GameObject MatchButton;
    [SerializeField] private GameObject ExitButton;
    private CanvasGroup modeCanvasGroup;

    [Header("Multiplayer UI")]
    [SerializeField] private GameObject ConnectingPanel;
    [SerializeField] private GameObject CancelMatchButton;

    [Header("Audio Data")]
    [SerializeField] private AudioData startUIAudioData;
    [SerializeField] private AudioData clickUIAudioData;

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
        if (ConnectingPanel != null) ConnectingPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (messageText != null)
        {
            UIEffect.Breathe(messageText.rectTransform, 1.1f, 1f);
        }
    }

    private void ShowModePanel()
    {
        AudioManager.Instance.PlaySFX(startUIAudioData);

        if (StartPanel != null) StartPanel.SetActive(false);
        if (ModePanel == null) return;

        ModePanel.SetActive(true);

        UIEffect.Popup(ModePanel);
        UIEffect.FadeIn(modeCanvasGroup);

        if (MatchButton != null)
            UIEffect.ButtonPopup(MatchButton, 0.6f);
        if (CancelMatchButton != null)
            UIEffect.ButtonPopup(CancelMatchButton, 0.6f);
        if (ExitButton != null)
            UIEffect.ButtonPopup(ExitButton, 0.6f);
    }

    private void HideModePanel()
    {
        if (ModePanel == null) return;

        // 面板淡出 + 縮小
        UIEffect.FadeOut(modeCanvasGroup, 0.3f);
        ModePanel.SetActive(false);

        // 可選：回到 StartPanel
        if (StartPanel != null) StartPanel.SetActive(true);
    }

    public void OnShowModeClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ShowModePanel();
    }

    public void OnCloseModeClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        HideModePanel();
    }

    public void OnMatchClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ConnectingPanel.SetActive(true);

        FindFirstObjectByType<MatchSystem>().QuickMatch();
    }

    public void OnCancelMatchClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ConnectingPanel.SetActive(false);
        FindFirstObjectByType<MatchSystem>().ExitMatch();
    }

    public void OnQuitClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        FindFirstObjectByType<MatchSystem>().ExitMatch();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

}