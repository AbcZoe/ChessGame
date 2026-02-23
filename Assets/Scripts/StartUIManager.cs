using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StartUIManager : MonoBehaviour
{
    [Header("Start UI")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private TMP_Text messageText;

    [Header("Choose Mode UI")]
    [SerializeField] private GameObject ModePanel;
    [SerializeField] private GameObject PvPButton;
    [SerializeField] private GameObject PvCButton;

    private CanvasGroup modeCanvasGroup;

    private void Awake()
    {
        // 確保 ModePanel 有 CanvasGroup
        if (ModePanel != null)
        {
            modeCanvasGroup = ModePanel.GetComponent<CanvasGroup>();
            if (modeCanvasGroup == null)
                modeCanvasGroup = ModePanel.AddComponent<CanvasGroup>();
        }

        // 先隱藏 ModePanel
        if (ModePanel != null) ModePanel.SetActive(false);

        // 文字呼吸
        if (messageText != null)
            UIEffect.Breathe(messageText.rectTransform, 1.1f, 1f);
    }

    private void Update()
    {
        // 點擊任意地方顯示 ModePanel
        if (Input.GetMouseButtonDown(0))
        {
            ShowModePanel();
        }
    }

    private void ShowModePanel()
    {
        if (StartPanel != null) StartPanel.SetActive(false);
        if (ModePanel == null) return;

        ModePanel.SetActive(true);

        // Panel 彈出 + 淡入
        UIEffect.Popup(ModePanel);
        UIEffect.FadeIn(modeCanvasGroup);

        // 按鈕彈出（確保使用 RectTransform）
        if (PvPButton != null)
            UIEffect.ButtonPopup(PvPButton, 0.6f);
        if (PvCButton != null)
            UIEffect.ButtonPopup(PvCButton, 0.6f);
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