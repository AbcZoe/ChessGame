using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class RoomListUI : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject roomItemPrefab;

    private Dictionary<string, LobbyItemUI> roomMap = new();

    public void RefreshList(List<Lobby> lobbies)
    {
        HashSet<string> alive = new();

        foreach (var lobby in lobbies)
        {
            alive.Add(lobby.Id);

            if (roomMap.TryGetValue(lobby.Id, out var item))
            {
                item.Setup(lobby, OnClickJoin); // 更新
            }
            else
            {
                // 新房間
                GameObject go = Instantiate(roomItemPrefab, contentRoot);
                var ui = go.GetComponent<LobbyItemUI>();

                ui.Setup(lobby, OnClickJoin);
                roomMap[lobby.Id] = ui;
            }
        }

        // 移除不存在的房間
        var removeList = new List<string>();

        foreach (var kv in roomMap)
        {
            if (!alive.Contains(kv.Key))
                removeList.Add(kv.Key);
        }

        foreach (var id in removeList)
        {
            Destroy(roomMap[id].gameObject);
            roomMap.Remove(id);
        }
    }

    private void OnClickJoin(string lobbyId)
    {
        _ = JoinLobbyAsync(lobbyId);
    }

    private async Task JoinLobbyAsync(string lobbyId)
    {
        try
        {
            await MatchSystem.Instance.JoinLobbyById(lobbyId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Join Lobby Failed: {e}");
        }
    }

}