using System.Collections.Generic;
using UnityEngine;

public class CardPool : Singleton<CardPool>
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private int initialCount = 20;

    private Transform poolRoot;
    private readonly Queue<CardView> pool = new();

    protected override void Awake()
    {
        base.Awake();

        // 建立 PoolRoot
        GameObject rootGO = new GameObject("CardPoolRoot");
        rootGO.transform.SetParent(this.transform);
        rootGO.transform.localPosition = Vector3.zero;
        poolRoot = rootGO.transform;

        // 預生成卡牌
        for (int i = 0; i < initialCount; i++)
        {
            CardView card = Instantiate(cardPrefab, poolRoot);
            card.gameObject.SetActive(false);
            pool.Enqueue(card);
        }
    }

    public CardView GetCard(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        CardView card;
        if (pool.Count > 0)
        {
            card = pool.Dequeue();
            card.gameObject.SetActive(true);
        }
        else
        {
            card = Instantiate(cardPrefab);
        }

        card.transform.SetParent(parent, false); // UI 卡牌要用 false 保持本地座標
        card.transform.localPosition = position;
        card.transform.localRotation = rotation;

        return card;
    }

    public void ReturnCard(CardView card)
    {
        card.ResetCard();
        card.gameObject.SetActive(false);
        card.transform.SetParent(poolRoot, false);
        pool.Enqueue(card);
    }
}