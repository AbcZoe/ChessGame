using UnityEngine;

[CreateAssetMenu(fileName = "TestCard", menuName = "Cards/Test Card")]
public class NothingCard : CardData
{
    public override void Use()
    {
        Services.NetGame?.RequestUseCard(Id);
    }
}