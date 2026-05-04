using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CardSystem : NetworkBehaviour
{
    public static CardSystem Instance;

    [Header("手牌區")]
    [SerializeField] private HandView player1Hand;
    [SerializeField] private HandView player2Hand;

    [Header("卡牌資料庫")]
    [SerializeField] private CardDatabase cardDatabase;

    [Header("開局設定")]
    [SerializeField] private int startCardCount = 5;
    [SerializeField] private float dealInterval = 0.3f;

    // 在 CardSystem 類別內新增
    private int nextCardInstanceId = 1;



    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(DealStartingCardsCoroutine());
        }
    }

    public IEnumerator DealStartingCardsCoroutine()
    {
        // 先讓所有 Client 清空手牌
        ClearHandsClientRpc();

        for (int i = 0; i < startCardCount; i++)
        {
            // 玩家 1
            CardData card1 = cardDatabase.GetRandomCard();
            int inst1 = nextCardInstanceId++;
            DealCardClientRpc(0, card1.Id, inst1);
            yield return new WaitForSeconds(dealInterval);

            // 玩家 2
            CardData card2 = cardDatabase.GetRandomCard();
            int inst2 = nextCardInstanceId++;
            DealCardClientRpc(1, card2.Id, inst2);
            yield return new WaitForSeconds(dealInterval);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClearHandsClientRpc()
    {
        player1Hand.ClearHand();
        player2Hand.ClearHand();
    }


    // 修改 DealCardClientRpc 簽名與內容
    [Rpc(SendTo.ClientsAndHost)]
    private void DealCardClientRpc(int playerTeam, int cardId, int cardInstanceId)
    {
        CardData data = cardDatabase.GetCardById(cardId);
        if (data == null) return;

        HandView hand = playerTeam == 0 ? player1Hand : player2Hand;

        CardView card = CardPool.Instance.GetCard(Vector3.zero, Quaternion.identity, hand.transform);
        card.SetCardData(data, playerTeam);

        // 關鍵：保存 instanceId（所有 Client 對應同一張）
        card.CardInstanceId = cardInstanceId;

        StartCoroutine(hand.AddCard(card));
    }

    // 新增：用牌請求交給 Server
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UseCardServerRpc(int cardInstanceId, int ownerTeam)
    {
        // 這裡建議做回合/玩家驗證（避免作弊），先略
        RemoveCardClientRpc(ownerTeam, cardInstanceId);
    }

    // 新增：所有人一起移除
    [Rpc(SendTo.ClientsAndHost)]
    private void RemoveCardClientRpc(int ownerTeam, int cardInstanceId)
    {
        HandView hand = ownerTeam == 0 ? player1Hand : player2Hand;

        CardView card = hand.FindCardByInstanceId(cardInstanceId);
        if (card == null) return;

        hand.RemoveCard(card);
        CardViewCreator.Instance.RecycleCard(card);
    }

}