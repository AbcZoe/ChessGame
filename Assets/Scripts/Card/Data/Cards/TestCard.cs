using UnityEngine;

[CreateAssetMenu(fileName = "TestCard", menuName = "Cards/Test Card")]
public class TestCard : CardData
{
    public override void Use(int team)
    {
        UIManager.Instance.ShowMessage($"Nothing happend...", 2f);
    }
}
