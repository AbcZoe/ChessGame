using UnityEngine;

[CreateAssetMenu(fileName = "ResetKingCard", menuName = "Cards/Reset King Card")]
public class ResetKingCard : CardData
{
    public override void Use()
    {
        Services.NetInterface?.RequestUseCard(Id);
    }
}