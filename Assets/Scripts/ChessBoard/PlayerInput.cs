using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Audio.ProcessorInstance;

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

    public int PlayerTeam { get; private set; } = -1; // -1 表示未設定


    public void Initialize(ChessBoard board, TileHighlighter highlighter, int playerTeam)
    {
        chessBoard = board;
        tileHighlighter = highlighter;
        PlayerTeam = playerTeam;


        if (chessBoard == null)
            Debug.LogError("PlayerInput 初始化錯誤: chessBoard 為 null!");

        // 找到 CameraSetup 並翻轉相機
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

        HandleMouseInteraction();
    }

    private void HandleMouseInteraction()
    {
        if (!currentCamera) currentCamera = Camera.main;
        if (!currentCamera || Mouse.current == null) return;

        // 確認 ChessBoard 和陣列都存在
        if (ChessBoard.Instance == null || ChessBoard.Instance.chessPieces == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(mousePos);

        // 1) Hover用 Tile raycast
        bool hitTile = Physics.Raycast(ray, out RaycastHit tileHit, 100, tileLayerMask);
        Vector2Int hitPos = -Vector2Int.one;
        if (hitTile)
            hitPos = chessBoard.GetHitTileIndex(tileHit.transform.gameObject);

        bool completed = ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed;

        // A) Hover：只有在「沒在拖曳」時才做
        if (currentlyDragging == null)
        {
            if (hitTile) HandleHover(hitPos);
            else ResetHover();
        }

        // B) Drag：只要正在拖曳就永遠更新（不依賴 hitTile）
        if (completed && currentlyDragging != null)
            HandleDrag(ray);

        // C) Release：在 releasedThisFrame 那一幀分流
        if (completed && currentlyDragging != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (hitTile) HandleRelease(hitPos);
            else HandleReleaseOnEmpty();
            return; // 避免同一幀又進入按下邏輯
        }

        // D) Press：你原本的按下開始拖曳邏輯（維持）
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ChessPiece clicked = null;

            if (Physics.Raycast(ray, out RaycastHit pieceHit, 100, pieceLayerMask))
                clicked = pieceHit.collider.GetComponent<ChessPiece>();
            else if (hitTile)
                clicked = FindPieceAtTile(hitPos);
            if (selectingCardTarget)
            {
                // 只能選自己隊伍、且要是活著的棋子（你用 IsDead）
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

            // 正常拖曳流程
            HandleClickPiece(clicked);
        }
    }

    #region Hover
    private void HandleHover(Vector2Int hitPos)
    {
        // 把之前 hover 過的格子恢復
        if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != hitPos)
        {
            // 如果之前的 tile 被 TileHighlighter 高亮過，就保留 move/attack 材質
            if (tileHighlighter != null && tileHighlighter.IsHighlighted(currentHoverdTile))
            {
                tileHighlighter.RestoreTileMaterialIfHighlighted(currentHoverdTile);
            }
            else
            {
                chessBoard.SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);
            }
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
            if (!p.IsSpawned) continue; // NGO safety
            if (p.X.Value == tilePos.x && p.Y.Value == tilePos.y)
                return p;
        }
        return null;
    }

    private void HandleClickPiece(ChessPiece piece)
    {
        if (piece == null) return;
        if (piece.IsDead.Value) return;

        // 選王階段
        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed)
        {
            if (piece.Team.Value == PlayerTeam)
                Services.NetInterface?.RequestSelectKing(piece.NetworkObjectId);
            return;
        }

        // 要自己回合 + 自己隊伍才允許開始拖曳
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
            //只改本地 transform，不動 NetworkVariable
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * chessBoard.dragOffset);
    }
    #endregion

    #region Release
    private void HandleRelease(Vector2Int hitPos)
    {
        if (currentlyDragging == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;
        AudioManager.Instance.PlaySFX(realsePieceAudioData);

        var piece = currentlyDragging;
        piece.IsDragging = false; // 先清拖曳，讓之後 NV 更新能套用位置

        // 用棋子的 NetworkObjectId 當 pieceNetId
        ulong pieceNetId = piece.NetworkObjectId;

        // 再發送移動請求（合法就會被 server 拉到新位置）
        Services.NetInterface?.RequestMove(pieceNetId, hitPos.x, hitPos.y);

        tileHighlighter.RemoveHighlight();
        currentlyDragging = null;
    }

    private void HandleReleaseOnEmpty()
    {
        if (currentlyDragging == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;
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

        // 防止正在拖曳時切模式造成怪狀態
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