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

        // primaryTouch 沒在按就視為沒有互動
        bool isPressed = touch.press.isPressed;
        bool pressedThisFrame = touch.press.wasPressedThisFrame;
        bool releasedThisFrame = touch.press.wasReleasedThisFrame;

        // 手機：不按住就不 hover，並且把 hover 清掉（避免一直亮著）
        if (!isPressed && !pressedThisFrame)
        {
            ResetHover();
            return;
        }

        // 鎖定 activeTouchId（只處理同一根手指）
        if (pressedThisFrame)
            activeTouchId = touch.touchId.ReadValue();

        // 如果 touchId 變了（例如多指或系統切換），忽略這一幀避免亂跳
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

        // 1) Press：開始拖曳 / 選卡牌目標
        if (pressedThisFrame)
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

        // 2) Drag：按住期間更新拖曳位置
        if (completed && currentlyDragging != null && isPressed)
            HandleDrag(ray);

        // 3) Release：放開時落子（或回原位）
        if (completed && currentlyDragging != null && releasedThisFrame)
        {
            if (hitTile) HandleRelease(hitPos);
            else HandleReleaseOnEmpty();

            activeTouchId = -1;
            ResetHover(); // 放開後清 hover，更像手機
            return;
        }

        // 放開但沒有正在拖曳，也把 touchId 解鎖
        if (releasedThisFrame)
        {
            activeTouchId = -1;
            ResetHover();
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