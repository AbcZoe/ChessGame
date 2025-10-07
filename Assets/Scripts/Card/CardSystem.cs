using System.Collections;
using UnityEngine;

public class CardSystem : MonoBehaviour
{
    public static CardSystem Instance;

    [Header("手牌區（雙方玩家）")]
    [SerializeField] private HandView player1Hand;
    [SerializeField] private HandView player2Hand;

    [Header("開局設定")]
    [SerializeField] private int startCardCount = 3;   // 每位玩家開局卡片數量
    [SerializeField] private float dealInterval = 0.3f; // 發牌間隔

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        StartCoroutine(DealStartingCards());
    }

    public IEnumerator DealStartingCards()
    {
        // 先清空手牌（會回收到 Pool）
        player1Hand.ClearHand();
        player2Hand.ClearHand();

        for (int i = 0; i < startCardCount; i++)
        {
            // 玩家1
            CardView card1 = CardViewCreator.Instance.CreateCardView(
                player1Hand.transform.position,
                Quaternion.identity,
                player1Hand.transform // 指定 parent 為 HandView
            );
            yield return player1Hand.StartCoroutine(player1Hand.AddCard(card1));
            yield return new WaitForSeconds(dealInterval);

            // 玩家2
            CardView card2 = CardViewCreator.Instance.CreateCardView(
                player2Hand.transform.position,
                Quaternion.identity,
                player2Hand.transform // 指定 parent 為 HandView
            );
            yield return player2Hand.StartCoroutine(player2Hand.AddCard(card2));
            yield return new WaitForSeconds(dealInterval);
        }
    }
}
