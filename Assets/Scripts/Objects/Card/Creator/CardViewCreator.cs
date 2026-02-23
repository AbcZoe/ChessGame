using DG.Tweening;
using UnityEngine;

public class CardViewCreator : Singleton<CardViewCreator>
{
    public CardView CreateCardView(Vector3 position, Quaternion rotation, Transform parent = null)
    {
        return CardPool.Instance.GetCard(position, rotation, parent);
    }

    public void RecycleCard(CardView cardView)
    {
        CardPool.Instance.ReturnCard(cardView);
    }
}
