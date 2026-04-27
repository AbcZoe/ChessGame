using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class RoomListUI : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject roomItemPrefab;

    public async void RefreshList()
    {
        Clear();

        var lobbies = await MatchSystem.Instance.GetLobbyList();

        Debug.Log($"©Ð¶¡¼Æ: {lobbies.Count}");

        foreach (var lobby in lobbies)
        {
            GameObject item = Instantiate(roomItemPrefab, contentRoot);

            var ui = item.GetComponent<LobbyItemUI>();

            ui.Setup(lobby, (id) =>
            {
                MatchSystem.Instance.JoinLobbyById(id);
            });
        }
    }

    void Clear()
    {
        foreach (Transform child in contentRoot)
            Destroy(child.gameObject);
    }
}