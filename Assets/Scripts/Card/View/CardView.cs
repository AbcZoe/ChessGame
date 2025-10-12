using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Title;
    [SerializeField] private SpriteRenderer CardImage;
    [SerializeField] private GameObject Wrapper;

    public CardData Data { get; private set; }
    public int OwnerTeam { get; private set; }
    public string Description => Data != null ? Data.description : "";

    public void ResetCard()
    {
        OwnerTeam = -1;
        Data = null;
        Title.text = "Title";
        CardImage.sprite = null;
    }

    public void SetCardData(CardData data, int ownerTeam)
    {
        Data = data;
        OwnerTeam = ownerTeam;
        Title.text = data.cardName;
        CardImage.sprite = data.image;
    }

    // 使用卡牌功能
    public void Use()
    {
        if (Data != null)
            Data.Use(OwnerTeam);
    }
}
