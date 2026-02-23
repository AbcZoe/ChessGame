using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class HandView : MonoBehaviour
{
    [SerializeField] private SplineContainer SplineContainer;
    [SerializeField] private bool facePlayer = true;

    [SerializeField] private float cardSpacing = 1f / 10f;

    private readonly List<CardView> cards = new();

    public IEnumerator AddCard(CardView cardView)
    {
        // 把卡牌放到手牌下
        cardView.transform.SetParent(this.transform);

        cards.Add(cardView);
        yield return UpdateCardPosition(0.15f);
    }

    private IEnumerator UpdateCardPosition(float duration)
    {
        if (cards.Count == 0) yield break;

        float firstCardPosition = 0.5f - (cards.Count - 1) * cardSpacing / 2f;
        Spline spline = SplineContainer.Spline;

        for (int i = 0; i < cards.Count; i++)
        {
            float p = firstCardPosition + i * cardSpacing;
            Vector3 splinePosition = spline.EvaluatePosition(p);

            cards[i].transform
                .DOMove(splinePosition + transform.position + 0.01f * i * Vector3.back, duration)
                .SetEase(Ease.OutQuad);

            Vector3 direction = spline.EvaluateTangent(p);
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            //旋轉調整
            targetRotation *= Quaternion.Euler(-45, -90, 0);
            if (!facePlayer) targetRotation *= Quaternion.Euler(-90, 180, 0);

            cards[i].transform
                .DORotateQuaternion(targetRotation, duration)
                .SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(duration);
    }

    public void ClearHand()
    {
        foreach (var card in cards)
        {
            CardViewCreator.Instance.RecycleCard(card);
        }
        cards.Clear();
    }
}
