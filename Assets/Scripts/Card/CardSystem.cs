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
            DealCardClientRpc(0, card1.Id);
            yield return new WaitForSeconds(dealInterval);

            // 玩家 2
            CardData card2 = cardDatabase.GetRandomCard();
            DealCardClientRpc(1, card2.Id);
            yield return new WaitForSeconds(dealInterval);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ClearHandsClientRpc()
    {
        player1Hand.ClearHand();
        player2Hand.ClearHand();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DealCardClientRpc(int playerTeam, int cardId)
    {
        CardData data = cardDatabase.GetCardById(cardId);
        if (data == null) return;

        HandView hand = playerTeam == 0 ? player1Hand : player2Hand;

        CardView card = CardPool.Instance.GetCard(Vector3.zero, Quaternion.identity, hand.transform);

        // 刷新卡牌資料
        card.SetCardData(data, playerTeam);

        // 加到手牌
        StartCoroutine(hand.AddCard(card));
    }
}