using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System;

public class LobbyItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text playerText;
    [SerializeField] private Button joinButton;

    private string lobbyId;

    public void Setup(Lobby lobby, Action<string> onJoinClicked)
    {
        lobbyId = lobby.Id;

        int players = lobby.MaxPlayers - lobby.AvailableSlots;

        nameText.text = lobby.Name;
        playerText.text = $"{players}/{lobby.MaxPlayers}";

        bool isFull = players >= lobby.MaxPlayers;

        joinButton.interactable = !isFull;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() =>
        {
            onJoinClicked?.Invoke(lobbyId);
        });
    }
}