using UnityEngine;

[CreateAssetMenu(fileName = "ResetKingCard", menuName = "Cards/Reset King Card")]
public class ResetKingCard : CardData
{
    public override void Use()
    {
        Services.NetGame?.RequestUseCard(Id);
    }
}