using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class CardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalPosition;
    private bool isDragging = false;

    private Camera currentCamera;
    public float dragOffset = 1f;
    public float yOffset = 0.2f;

    private CardView cardView;
    private Coroutine hoverCoroutine;

    [Header("Audio Data")]
    [SerializeField] private AudioData clickCardAudioData;

    private void Awake()
    {
        currentCamera = Camera.main;
        if (currentCamera && !currentCamera.GetComponent<PhysicsRaycaster>())
        {
            currentCamera.gameObject.AddComponent<PhysicsRaycaster>();
        }

        originalPosition = transform.position;
        cardView = GetComponent<CardView>();
    }

    private void Update()
    {
        if (!currentCamera || cardView == null) return;
        if (GameManager.Instance == null || ChessBoard.Instance == null) return;

        // 遊戲暫停或選王階段，卡牌不可操作
        if (GameManager.Instance.IsPaused.Value || ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed)
            return;

        if (GameManager.Instance.IsPaused.Value)
            return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        Ray ray = currentCamera.ScreenPointToRay(pointer.position.ReadValue());

        HandleCardDrag(pointer, ray);
    }

    private void HandleCardDrag(Pointer pointer, Ray ray)
    {
        // 按下開始拖曳
        if (pointer.press is ButtonControl press && press.wasPressedThisFrame)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                if (hit.transform == transform)
                {
                    AudioManager.Instance.PlaySFX(clickCardAudioData);
                    // 判斷是否是自己回合
                    if (GameManager.Instance.CurrentTurnTeam != cardView.OwnerTeam) return;

                    isDragging = true;
                    originalPosition = transform.position;
                }
            }
        }

        // 拖曳中
        if (isDragging && pointer.press is ButtonControl pressHold && pressHold.isPressed)
        {
            Plane plane = new Plane(Vector3.up, Vector3.up * yOffset);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance) + Vector3.up * dragOffset;
                transform.position = point;
            }
        }

        // 放開
        if (isDragging && pointer.press is ButtonControl pressUp && pressUp.wasReleasedThisFrame)
        {
            isDragging = false;

            // 判斷是否放到棋盤上
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100, LayerMask.GetMask("Tile")))
            {
                UseCard();
            }
            else
            {
                // 放到其他地方 → 回原位
                transform.position = originalPosition;
            }

            // 無論如何都隱藏描述
            UIManager.Instance.HideCardDescription();
        }
    }

    // ==== 卡牌描述顯示 ====
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (PlayerInput.Instance != null && cardView.OwnerTeam == PlayerInput.Instance.PlayerTeam)
            UIManager.Instance.ShowCardDescription(cardView.CardNameKey, cardView.DescriptionKey);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.HideCardDescription();
    }

    private void UseCard()
    {
        cardView.Use();

        // 卡牌回收
        CardSystem.Instance.UseCardServerRpc(cardView.CardInstanceId, cardView.OwnerTeam);
    }
}