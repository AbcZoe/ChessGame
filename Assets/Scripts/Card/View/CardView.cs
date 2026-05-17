using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Settings;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Title;
    [SerializeField] private GameObject Wrapper;

    public CardData Data { get; private set; }
    public int OwnerTeam { get; private set; }

    // 新增：同一張「手牌實體」在所有 Client 的共同識別
    public int CardInstanceId { get; set; } = -1;

    // 回傳 localization key
    public string CardNameKey => Data != null ? Data.cardNameKey : "";
    public string DescriptionKey => Data != null ? Data.descriptionKey : "";

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

        // Localization
        Title.text = LocalizationSettings.StringDatabase.GetLocalizedString("UI_Text", data.cardNameKey);
    }

    // 使用卡牌功能
    public void Use()
    {
        if (Data != null)
            Data.Use();
    }
}
