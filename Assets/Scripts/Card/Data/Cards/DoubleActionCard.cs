using UnityEngine;

[CreateAssetMenu(fileName = "DoubleActionCard", menuName = "Cards/Double Action Card")]
public class DoubleActionCard : CardData
{
    public override void Use(int team)
    {
        // 告知對手（可選）
        GameManager.Instance?.ShowMessageClientRpc("The enemy used the card \"Double Action\".", 2f);

        // Server 增加額外行動（這行要在 Server 執行：建議做一個 ServerRpc 或由 Server 本來就在處理用卡）
        if (GameManager.Instance.IsServer)
        {
            GameManager.Instance.GrantExtraActionServer(1);
        }
        else
        {
            GameManager.Instance.GrantExtraActionServerRpc(1);
        }

    }
}
