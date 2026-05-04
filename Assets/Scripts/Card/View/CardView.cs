using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Title;
    [SerializeField] private GameObject Wrapper;

    public CardData Data { get; private set; }
    public int OwnerTeam { get; private set; }

    // 新增：同一張「手牌實體」在所有 Client 的共同識別
    public int CardInstanceId { get; set; } = -1;

    public string CardName => Data != null ? Data.cardName : "";
    public string Description => Data != null ? Data.description : "";

    public void ResetCard()
    {
        OwnerTeam = -1;
        Data = null;
        CardInstanceId = -1;
        Title.text = "Title";
    }

    public void SetCardData(CardData data, int ownerTeam)
    {
        Data = data;
        OwnerTeam = ownerTeam;
        Title.text = data.cardName;
    }

    // 使用卡牌功能
    public void Use()
    {
        if (Data != null)
            Data.Use();
    }
}
