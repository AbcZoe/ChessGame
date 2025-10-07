using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Title;
    [SerializeField] private TMP_Text Description;
    [SerializeField] private SpriteRenderer CardImage;
    [SerializeField] private GameObject Wrapper;

    public void ResetCard()
    {
        // 可在回收前清空資料，避免顯示錯誤的舊資訊
        Title.text = "";
        Description.text = "";
        CardImage.sprite = null;
    }

}
