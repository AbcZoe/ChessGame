using UnityEngine;

[CreateAssetMenu(fileName = "DoubleActionCard", menuName = "Cards/Double Action Card")]
public class DoubleActionCard : CardData
{
    public override void Use()
    {
        Services.NetInterface?.RequestUseCard(Id);
    }
}
