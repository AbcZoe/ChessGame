using UnityEngine;

public class CardHover : MonoBehaviour
{
    private Vector3 originalPosition;
    private bool isDragging = false;

    private Camera currentCamera;
    public float dragOffset = 1f;
    public float yOffset = 0.2f;

    private CardView cardView;

    private void Awake()
    {
        currentCamera = Camera.main;
        originalPosition = transform.position;
        cardView = GetComponent<CardView>();
    }

    private void Update()
    {
        if (!currentCamera || cardView == null) return;

        // 遊戲暫停或選王階段，卡牌不可操作
        if (ChessBoard.Instance.IsPaused || ChessBoard.Instance.SelectingKingTeam != -1)
            return;

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        // 按下滑鼠開始拖曳
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                if (hit.transform == transform)
                {
                    // 判斷是否是自己回合
                    if (ChessBoard.Instance.CurrentTurnTeam != cardView.OwnerTeam) return;

                    isDragging = true;
                    originalPosition = transform.position;
                }
            }
        }

        // 拖曳中
        if (isDragging && Input.GetMouseButton(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.up * yOffset);
            if (plane.Raycast(ray, out float distance))
            {
                Vector3 point = ray.GetPoint(distance) + Vector3.up * dragOffset;
                transform.position = point;
            }
        }

        // 放開滑鼠
        if (isDragging && Input.GetMouseButtonUp(0))
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

    private void UseCard()
    {
        cardView.Use();

        // 只有一般功能卡才切換回合
        if (!(cardView.Data is ResetKingCard))
        {
            ChessBoard.Instance.SwitchTurn();
        }

        // 卡牌消失
        CardViewCreator.Instance.RecycleCard(cardView);

    }

    // ===== 顯示卡牌描述 =====
    private void OnMouseEnter()
    {
        // 只顯示自己隊伍的卡牌描述
        if (UIManager.Instance != null && cardView != null)
        {
            if (cardView.OwnerTeam == ChessBoard.Instance.CurrentTurnTeam)
            {
                UIManager.Instance.ShowCardDescription(cardView.Description);
            }
        }
    }
    private void OnMouseExit()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideCardDescription();
        }
    }
}
