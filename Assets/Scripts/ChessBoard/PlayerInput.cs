using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance { get; private set; }
    private Camera currentCamera;
    private ChessBoard chessBoard;
    private TileHighlighter tileHighlighter;

    [SerializeField] private LayerMask pieceLayerMask;
    [SerializeField] private LayerMask tileLayerMask;

    private Vector2Int dragStartPos;
    private Vector2Int currentHoverdTile = -Vector2Int.one;
    private ChessPiece currentlyDragging;

    [SerializeField] private float dragStartThresholdPixels = 18f; // 手機常用 12~24 可調

    private bool touchPendingDrag;           // 已按下，但尚未超過門檻
    private Vector2 touchStartScreenPos;     // 按下當下的螢幕座標
    private ChessPiece touchPressedPiece;    // 按下那一瞬間點到的棋子（候選）

    [Header("Audio Data")]
    [SerializeField] private AudioData realsePieceAudioData;

    public int PlayerTeam { get; private set; } = -1;

    // 手機操作：我們鎖定「同一根手指」的 touchId，避免多指干擾
    private int activeTouchId = -1;

    public void Initialize(ChessBoard board, TileHighlighter highlighter, int playerTeam)
    {
        chessBoard = board;
        tileHighlighter = highlighter;
        PlayerTeam = playerTeam;

        if (chessBoard == null)
            Debug.LogError("PlayerInput 初始化錯誤: chessBoard 為 null!");

        CameraSetup camSetup = UnityEngine.Object.FindAnyObjectByType<CameraSetup>();
        if (camSetup != null)
        {
            camSetup.SetupCamera(PlayerTeam);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (chessBoard == null) return;
        if (GameManager.Instance.IsPaused.Value) return;

        HandlePointerOrTouchInteraction();
    }

    // ------------------------------------------------------------
    // Mobile-friendly input
    // ------------------------------------------------------------
    private void HandlePointerOrTouchInteraction()
    {
        if (!currentCamera) currentCamera = Camera.main;
        if (!currentCamera) return;

        if (ChessBoard.Instance == null || ChessBoard.Instance.chessPieces == null)
            return;

        bool completed = ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed;

        // 1) 優先使用 Touch（更符合手機）
        if (Touchscreen.current != null)
        {
            HandleTouch(completed);
            return;
        }

        // 2) 沒有 Touchscreen 時，回退使用 Pointer（桌機/筆）
        HandlePointer(completed);
    }

    private void HandleTouch(bool completed)
    {
        var ts = Touchscreen.current;
        if (ts == null) return;

        var touch = ts.primaryTouch;

        bool isPressed = touch.press.isPressed;
        bool pressedThisFrame = touch.press.wasPressedThisFrame;
        bool releasedThisFrame = touch.press.wasReleasedThisFrame;

        // 沒有互動：清 hover（手機通常不 hover）
        if (!isPressed && !pressedThisFrame)
        {
            ResetHover();
            return;
        }

        // 鎖定 activeTouchId（只處理同一根手指）
        if (pressedThisFrame)
            activeTouchId = touch.touchId.ReadValue();

        if (activeTouchId != -1 && touch.touchId.ReadValue() != activeTouchId)
            return;

        Vector2 screenPos = touch.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(screenPos);

        bool hitTile = Physics.Raycast(ray, out RaycastHit tileHit, 100, tileLayerMask);
        Vector2Int hitPos = -Vector2Int.one;
        if (hitTile)
            hitPos = chessBoard.GetHitTileIndex(tileHit.transform.gameObject);

        // 按住時才 hover
        if (hitTile) HandleHover(hitPos);
        else ResetHover();

        // ------------------------------------------------------------
        // Press：先進入「待定拖曳」狀態，不立刻抓起來拖
        // ------------------------------------------------------------
        if (pressedThisFrame)
        {
            touchStartScreenPos = screenPos;
            touchPendingDrag = true;
            touchPressedPiece = null;

            ChessPiece clicked = null;
            if (Physics.Raycast(ray, out RaycastHit pieceHit, 100, pieceLayerMask))
                clicked = pieceHit.collider.GetComponent<ChessPiece>();
            else if (hitTile)
                clicked = FindPieceAtTile(hitPos);

            // 卡牌選目標：點一下就結束（不走拖曳門檻）
            if (selectingCardTarget)
            {
                if (clicked != null && clicked.Team.Value == PlayerTeam && !clicked.IsDead.Value)
                {
                    onCardTargetSelected?.Invoke(clicked);
                    EndSelectFriendlyPiece();
                }
                else
                {
                    UIManager.Instance?.ShowMessage("Please select one of our surviving pieces.", 1.5f);
                }

                // 結束這次觸控互動
                touchPendingDrag = false;
                touchPressedPiece = null;
                activeTouchId = -1;
                ResetHover();
                return;
            }

            // 先記住「按下時」點到的棋子，等超過門檻才真的 HandleClickPiece
            touchPressedPiece = clicked;
        }

        // ------------------------------------------------------------
        // Move / Hold：超過門檻才開始真正拖曳
        // ------------------------------------------------------------
        if (completed && isPressed)
        {
            // 尚未開始拖，但有候選棋子：檢查是否超過門檻
            if (touchPendingDrag && currentlyDragging == null && touchPressedPiece != null)
            {
                float dist = Vector2.Distance(screenPos, touchStartScreenPos);
                if (dist >= dragStartThresholdPixels)
                {
                    // 真的開始拖曳：這裡才呼叫你原本的 HandleClickPiece（會做回合/隊伍檢查）
                    HandleClickPiece(touchPressedPiece);

                    // 不管是否成功抓起來（例如不是自己回合），都結束 pending
                    touchPendingDrag = false;

                    // 如果成功開始拖曳，立刻更新拖曳位置一次
                    if (currentlyDragging != null)
                        HandleDrag(ray);
                }
            }
            // 已在拖曳中：持續拖
            else if (currentlyDragging != null)
            {
                HandleDrag(ray);
            }
        }

        // ------------------------------------------------------------
        // Release：如果已在拖曳 -> 放開落子；如果還在門檻內 -> 當作點擊（目前不做落子）
        // ------------------------------------------------------------
        if (releasedThisFrame)
        {
            if (completed && currentlyDragging != null)
            {
                if (hitTile) HandleRelease(hitPos);
                else HandleReleaseOnEmpty();
            }
            else
            {
                // 尚未超過門檻就放開：視為「點一下」
                if (touchPressedPiece != null)
                {
                    HandleClickPiece(touchPressedPiece);
                }
            }

            // 清狀態
            touchPendingDrag = false;
            touchPressedPiece = null;
            activeTouchId = -1;
            ResetHover();
            return;
        }
    }

    private void HandlePointer(bool completed)
    {
        var pointer = Pointer.current;
        if (pointer == null) return;

        Vector2 screenPos = pointer.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(screenPos);

        bool hitTile = Physics.Raycast(ray, out RaycastHit tileHit, 100, tileLayerMask);
        Vector2Int hitPos = -Vector2Int.one;
        if (hitTile)
            hitPos = chessBoard.GetHitTileIndex(tileHit.transform.gameObject);

        // 桌機：可以維持 hover
        if (hitTile) HandleHover(hitPos);
        else ResetHover();

        if (completed && currentlyDragging != null)
            HandleDrag(ray);

        if (completed && currentlyDragging != null && IsPointerReleasedThisFrame(pointer))
        {
            if (hitTile) HandleRelease(hitPos);
            else HandleReleaseOnEmpty();
            return;
        }

        if (IsPointerPressedThisFrame(pointer))
        {
            ChessPiece clicked = null;

            if (Physics.Raycast(ray, out RaycastHit pieceHit, 100, pieceLayerMask))
                clicked = pieceHit.collider.GetComponent<ChessPiece>();
            else if (hitTile)
                clicked = FindPieceAtTile(hitPos);

            if (selectingCardTarget)
            {
                if (clicked != null && clicked.Team.Value == PlayerTeam && !clicked.IsDead.Value)
                {
                    onCardTargetSelected?.Invoke(clicked);
                    EndSelectFriendlyPiece();
                }
                else
                {
                    UIManager.Instance?.ShowMessage("Please select one of our surviving pieces.", 1.5f);
                }
                return;
            }

            HandleClickPiece(clicked);
        }
    }

    private static bool IsPointerPressedThisFrame(Pointer pointer)
    {
        if (pointer?.press is ButtonControl b) return b.wasPressedThisFrame;
        return false;
    }

    private static bool IsPointerReleasedThisFrame(Pointer pointer)
    {
        if (pointer?.press is ButtonControl b) return b.wasReleasedThisFrame;
        return false;
    }

    // ------------------------------------------------------------
    // Existing code (Hover/Drag/Release/Cards)
    // ------------------------------------------------------------

    #region Hover
    private void HandleHover(Vector2Int hitPos)
    {
        if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != hitPos)
        {
            if (tileHighlighter != null && tileHighlighter.IsHighlighted(currentHoverdTile))
                tileHighlighter.RestoreTileMaterialIfHighlighted(currentHoverdTile);
            else
                chessBoard.SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);
        }

        currentHoverdTile = hitPos;
        chessBoard.GetTileRenderer(hitPos.x, hitPos.y).material = chessBoard.hoverMaterial;
    }

    private void ResetHover()
    {
        if (currentHoverdTile != -Vector2Int.one)
            chessBoard.SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);

        currentHoverdTile = -Vector2Int.one;
    }
    #endregion

    #region Drag
    private ChessPiece FindPieceAtTile(Vector2Int tilePos)
    {
        if (tilePos == -Vector2Int.one) return null;

        var pieces = FindObjectsByType<ChessPiece>(FindObjectsSortMode.None);
        foreach (var p in pieces)
        {
            if (!p.IsSpawned) continue;
            if (p.X.Value == tilePos.x && p.Y.Value == tilePos.y)
                return p;
        }
        return null;
    }

    private void HandleClickPiece(ChessPiece piece)
    {
        if (piece == null) return;
        if (piece.IsDead.Value) return;

        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed)
        {
            if (piece.Team.Value == PlayerTeam)
                Services.NetInterface?.RequestSelectKing(piece.NetworkObjectId);
            return;
        }

        if (piece.Team.Value != PlayerTeam) return;
        if (!GameManager.Instance.IsTeamTurn(PlayerTeam)) return;

        dragStartPos = new Vector2Int(piece.X.Value, piece.Y.Value);
        currentlyDragging = piece;
        currentlyDragging.IsDragging = true;
        Services.NetInterface?.RequestHighlight(currentlyDragging.NetworkObjectId);
    }

    private void HandleDrag(Ray ray)
    {
        if (currentlyDragging == null) return;
        Plane plane = new(Vector3.up, Vector3.up * chessBoard.yOffset);
        if (plane.Raycast(ray, out float distance))
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * chessBoard.dragOffset);
    }
    #endregion

    #region Release
    private void HandleRelease(Vector2Int hitPos)
    {
        if (currentlyDragging == null) return;

        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        var piece = currentlyDragging;
        piece.IsDragging = false;

        ulong pieceNetId = piece.NetworkObjectId;

        chessBoard.ReleseToStart(piece, hitPos.x, hitPos.y);
        Services.NetInterface?.RequestMove(pieceNetId, hitPos.x, hitPos.y);

        tileHighlighter.RemoveHighlight();
        currentlyDragging = null;
    }

    private void HandleReleaseOnEmpty()
    {
        if (currentlyDragging == null) return;

        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        var piece = currentlyDragging;
        piece.IsDragging = false;

        piece.SetPosition(chessBoard.GetTileCenter(dragStartPos.x, dragStartPos.y));

        tileHighlighter.RemoveHighlight();
        currentlyDragging = null;
    }
    #endregion

    #region cards
    private bool selectingCardTarget;
    private Action<ChessPiece> onCardTargetSelected;

    public void BeginSelectFriendlyPiece(Action<ChessPiece> onSelected)
    {
        selectingCardTarget = true;
        onCardTargetSelected = onSelected;

        if (currentlyDragging != null)
        {
            currentlyDragging.IsDragging = false;
            currentlyDragging = null;
            tileHighlighter?.RemoveHighlight();
        }
    }

    private void EndSelectFriendlyPiece()
    {
        selectingCardTarget = false;
        onCardTargetSelected = null;
    }
    #endregion
}