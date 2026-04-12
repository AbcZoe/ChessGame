using UnityEngine;

[CreateAssetMenu(fileName = "TestCard", menuName = "Cards/Test Card")]
public class NothingCard : CardData
{
    public override void Use(int team)
    {
        GameManager.Instance?.ShowMessageClientRpc("The enemy used the card \"Nothing Happend\".", 2f);
        GameManager.Instance.EndAction();
    }
}