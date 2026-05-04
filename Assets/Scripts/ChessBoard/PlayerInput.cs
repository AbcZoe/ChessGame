using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private ChessPiece currentlyDragging;   // PC
    private ChessPiece selectedPiece;       // Mobile

    [Header("Audio")]
    [SerializeField] private AudioData realsePieceAudioData;

    public int PlayerTeam { get; private set; } = -1;

    private bool IsMobile => Application.isMobilePlatform;

    #region Init
    public void Initialize(ChessBoard board, TileHighlighter highlighter, int team)
    {
        chessBoard = board;
        tileHighlighter = highlighter;
        PlayerTeam = team;

        CameraSetup cam = FindAnyObjectByType<CameraSetup>();
        cam?.SetupCamera(PlayerTeam);
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
    #endregion

    private void Update()
    {
        if (chessBoard == null) return;
        if (GameManager.Instance.IsPaused.Value) return;

        HandleInput();
    }

    #region Input Core
    private bool TryGetPointer(out Vector2 pos, out bool down, out bool up)
    {
        pos = default;
        down = false;
        up = false;

        if (Touchscreen.current != null)
        {
            var t = Touchscreen.current.primaryTouch;
            pos = t.position.ReadValue();
            down = t.press.wasPressedThisFrame;
            up = t.press.wasReleasedThisFrame;
            return t.press.isPressed || down || up;
        }

        if (Mouse.current != null)
        {
            pos = Mouse.current.position.ReadValue();
            down = Mouse.current.leftButton.wasPressedThisFrame;
            up = Mouse.current.leftButton.wasReleasedThisFrame;
            return true;
        }

        return false;
    }

    private void HandleInput()
    {
        if (!currentCamera) currentCamera = Camera.main;
        if (!currentCamera) return;

        if (!TryGetPointer(out Vector2 pos, out bool down, out bool up))
            return;

        Ray ray = currentCamera.ScreenPointToRay(pos);

        // Raycast
        bool hitPiece = Physics.Raycast(ray, out RaycastHit pieceHit, 100, pieceLayerMask);
        bool hitTile = Physics.Raycast(ray, out RaycastHit tileHit, 100, tileLayerMask);

        ChessPiece clicked = null;

        Vector2Int tilePos = hitTile ? chessBoard.GetHitTileIndex(tileHit.transform.gameObject) : -Vector2Int.one;
        if (hitPiece)
        {
            clicked = pieceHit.collider.GetComponent<ChessPiece>();
        }
        else if (hitTile)
        {
            clicked = FindPieceAtTile(tilePos); // 補這個
        }
        
        bool completed = ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed;

        // ===== PC =====
        if (!IsMobile)
        {
            HandleHover(tilePos, hitTile);

            if (completed && currentlyDragging != null)
                HandleDrag(ray);

            if (completed && currentlyDragging != null && up)
            {
                if (hitTile) HandleRelease(tilePos);
                else HandleReleaseEmpty();
                return;
            }

            if (down)
                HandlePCClick(clicked);
        }
        // ===== Mobile =====
        else
        {
            if (down)
                HandleMobileTap(clicked, tilePos, hitTile);
        }
    }
    #endregion

    #region PC (Drag)
    private void HandlePCClick(ChessPiece piece)
    {
        if (selectingCardTarget)
        {
            HandleCardSelect(piece);
            return;
        }

        if (piece == null || piece.IsDead.Value) return;

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
        piece.IsDragging = true;

        Services.NetInterface?.RequestHighlight(piece.NetworkObjectId);
    }

    private void HandleDrag(Ray ray)
    {
        Plane plane = new(Vector3.up, Vector3.up * chessBoard.yOffset);
        if (plane.Raycast(ray, out float dist))
        {
            currentlyDragging.SetPosition(ray.GetPoint(dist) + Vector3.up * chessBoard.dragOffset);
        }
    }

    private void HandleRelease(Vector2Int pos)
    {
        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        var p = currentlyDragging;
        p.IsDragging = false;

        chessBoard.ReleseToStart(p, pos.x, pos.y);
        Services.NetInterface?.RequestMove(p.NetworkObjectId, pos.x, pos.y);

        tileHighlighter.RemoveHighlight();
        currentlyDragging = null;
    }

    private void HandleReleaseEmpty()
    {
        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        var p = currentlyDragging;
        p.IsDragging = false;

        p.SetPosition(chessBoard.GetTileCenter(dragStartPos.x, dragStartPos.y));

        tileHighlighter.RemoveHighlight();
        currentlyDragging = null;
    }

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
    #endregion

    #region Mobile (Tap)
    private void HandleMobileTap(ChessPiece clicked, Vector2Int tilePos, bool hitTile)
    {
        // ===== 選王階段（手機也要有）=====
        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed)
        {
            if (clicked != null && clicked.Team.Value == PlayerTeam)
            {
                Services.NetInterface?.RequestSelectKing(clicked.NetworkObjectId);
            }
            return;
        }

        if (selectingCardTarget)
        {
            HandleCardSelect(clicked);
            return;
        }

        // 選棋
        if (selectedPiece == null)
        {
            if (IsOwnPiece(clicked))
            {
                selectedPiece = clicked;
                tileHighlighter.RemoveHighlight();
                Services.NetInterface?.RequestHighlight(clicked.NetworkObjectId);
            }
            return;
        }

        // 點自己棋 → 切換 / 取消
        if (IsOwnPiece(clicked))
        {
            if (clicked == selectedPiece)
            {
                selectedPiece = null;
                tileHighlighter.RemoveHighlight();
            }
            else
            {
                selectedPiece = clicked;
                tileHighlighter.RemoveHighlight();
                Services.NetInterface?.RequestHighlight(clicked.NetworkObjectId);
            }
            return;
        }

        // 點敵人 → 攻擊
        if (clicked != null && clicked.Team.Value != PlayerTeam)
        {
            MoveSelected(clicked.X.Value, clicked.Y.Value);
            return;
        }

        // 點地板 → 移動
        if (hitTile)
        {
            MoveSelected(tilePos.x, tilePos.y);
            return;
        }

        // 取消
        selectedPiece = null;
        tileHighlighter.RemoveHighlight();
    }

    private void MoveSelected(int x, int y)
    {
        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        tileHighlighter.RemoveHighlight();

        chessBoard.ReleseToStart(selectedPiece, x, y);
        Services.NetInterface?.RequestMove(selectedPiece.NetworkObjectId, x, y);

        selectedPiece = null;
        tileHighlighter.RemoveHighlight();
    }

    private bool IsOwnPiece(ChessPiece p)
    {
        return p != null &&
               p.Team.Value == PlayerTeam &&
               !p.IsDead.Value &&
               GameManager.Instance.IsTeamTurn(PlayerTeam);
    }
    #endregion

    #region Hover
    private void HandleHover(Vector2Int pos, bool hit)
    {
        if (!hit)
        {
            ResetHover();
            return;
        }

        if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != pos)
        {
            if (tileHighlighter.IsHighlighted(currentHoverdTile))
                tileHighlighter.RestoreTileMaterialIfHighlighted(currentHoverdTile);
            else
                chessBoard.SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);
        }

        currentHoverdTile = pos;
        chessBoard.GetTileRenderer(pos.x, pos.y).material = chessBoard.hoverMaterial;
    }

    private void ResetHover()
    {
        if (currentHoverdTile != -Vector2Int.one)
            chessBoard.SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);

        currentHoverdTile = -Vector2Int.one;
    }
    #endregion

    #region Cards
    private bool selectingCardTarget;
    private Action<ChessPiece> onCardTargetSelected;

    private void HandleCardSelect(ChessPiece piece)
    {
        if (piece != null && piece.Team.Value == PlayerTeam && !piece.IsDead.Value)
        {
            onCardTargetSelected?.Invoke(piece);
            EndSelectFriendlyPiece();
        }
        else
        {
            UIManager.Instance?.ShowMessage("Select your piece", 1.5f);
        }
    }

    public void BeginSelectFriendlyPiece(Action<ChessPiece> cb)
    {
        selectingCardTarget = true;
        onCardTargetSelected = cb;

        currentlyDragging = null;
        selectedPiece = null;
        tileHighlighter?.RemoveHighlight();
    }

    private void EndSelectFriendlyPiece()
    {
        selectingCardTarget = false;
        onCardTargetSelected = null;
    }
    #endregion
}