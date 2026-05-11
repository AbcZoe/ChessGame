using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Netcode;
using System.Threading.Tasks;
using System;

public class StartUIManager : MonoBehaviour
{
    [Header("Start UI")]
    [SerializeField] private GameObject StartPanel;
    [SerializeField] private GameObject StartPanelBackground;
    [SerializeField] private TMP_Text messageText;

    [Header("Multiplayer UI")]
    [SerializeField] private GameObject ModePanel; 
    [SerializeField] private GameObject MatchButton;
    [SerializeField] private GameObject HostButton;
    [SerializeField] private GameObject ExitButton;
    [SerializeField] private GameObject ConnectingPanel;
    [SerializeField] private RoomListUI roomListUI;
    [SerializeField] private TMP_Text roomDataText;

    [Header("Audio Data")]
    [SerializeField] private AudioData startUIAudioData;
    [SerializeField] private AudioData clickUIAudioData;

    private void Awake()
    {
        // 確保時間恢復（非常重要）
        Time.timeScale = 1f;

        if (ModePanel != null) ModePanel.SetActive(false);
        if (ConnectingPanel != null) ConnectingPanel.SetActive(false);

    }

    private void OnEnable()
    {
        if (messageText != null)
        {
            UIEffect.Breathe(messageText.rectTransform, 1.1f, 1f);
        }
        if (MatchSystem.Instance != null)
        {
            MatchSystem.Instance.OnRoomCreated += UpdateRoomDataText;
            MatchSystem.Instance.OnLobbyListChanged += RefreshLobby;
        }

    }

    private void OnDisable()
    {
        if (MatchSystem.Instance != null)
        {
            MatchSystem.Instance.OnRoomCreated -= UpdateRoomDataText;
            MatchSystem.Instance.OnLobbyListChanged -= RefreshLobby;
        }
    }

    private void RefreshLobby()
    {
        var list = MatchSystem.Instance.GetCachedLobbies();
        roomListUI.RefreshList(list);
    }

    public void UpdateRoomDataText(string msg)
    {
        if (roomDataText != null)
            roomDataText.text = msg;
    }

    public void ClearRoomData()
    {
        if (roomDataText != null)
            roomDataText.text = "";
    }

    private void ShowModePanel()
    {
        AudioManager.Instance.PlaySFX(startUIAudioData);

        if (StartPanel != null) StartPanel.SetActive(false);
        ModePanel.SetActive(true);
        UIEffect.Popup(ModePanel);

        if (MatchButton != null)
            UIEffect.ButtonPopup(MatchButton, 0.6f);
        if (HostButton != null)
            UIEffect.ButtonPopup(HostButton, 0.6f);
        if (ExitButton != null)
            UIEffect.ButtonPopup(ExitButton, 0.6f);
    }

    public void OnShowModeClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ShowModePanel();
    }

    public void OnCloseModeClicked()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ModePanel.SetActive(false);

        if (StartPanel != null) StartPanel.SetActive(true);
    }

    public void OnQuickMatchButton()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData); 
        ConnectingPanel.SetActive(true);
        _ = QuickMatchAsync();
    }

    private async Task QuickMatchAsync()
    {
        try
        {
            await MatchSystem.Instance.QuickMatch();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void OnHostButton()
    {        
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ConnectingPanel.SetActive(true);
        _ = HostAsync();
    }

    private async Task HostAsync()
    {
        try
        {
            await MatchSystem.Instance.HostGame();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void OnExitMatchButton()
    {
        AudioManager.Instance.PlaySFX(clickUIAudioData);
        ConnectingPanel.SetActive(false);
        ClearRoomData();
        _ = ExitAsync();
    }

    private async Task ExitAsync()
    {
        try
        {
            await MatchSystem.Instance.ExitMatch();
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void onExitButton()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }

}