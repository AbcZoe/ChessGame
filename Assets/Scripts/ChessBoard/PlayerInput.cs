using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public static PlayerInput Instance { get; private set; }
    private Camera currentCamera;
    private ChessBoard chessBoard;
    private TileHighlighter tileHighlighter;

    private Vector2Int currentHoverdTile = -Vector2Int.one;
    private ChessPiece currentlyDragging;

    public int PlayerTeam { get; private set; } = -1; // -1 表示未設定

    public void Initialize(ChessBoard board, TileHighlighter highlighter, int playerTeam)
    {
        chessBoard = board;
        tileHighlighter = highlighter;
        PlayerTeam = playerTeam;


        if (chessBoard == null)
            Debug.LogError("PlayerInput 初始化錯誤: chessBoard 為 null!");

        // 找到 CameraSetup 並翻轉相機
        CameraSetup camSetup = Object.FindAnyObjectByType<CameraSetup>();
        if (camSetup != null)
        {
            camSetup.SetupCamera(PlayerTeam);
        }
        else
        {
            Debug.LogError("CameraSetup 找不到 main camera!");
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

        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPos = chessBoard.GetHitTileIndex(info.transform.gameObject);

            HandleHover(hitPos);
            HandleClick(hitPos);

            // 選王階段禁止拖動
            if (ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed)
            {
                HandleDrag(ray);
                HandleRelease(hitPos);
            }
        }
        else
        {
            if (ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed)
                ResetHover();

            if (ChessBoard.Instance.kingSelectionState.Value == KingSelectionState.Completed)
                HandleReleaseOnEmpty();
        }
    }

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

    private void HandleClick(Vector2Int hitPos)
    {
        ChessPiece clickedPiece = chessBoard.chessPieces[hitPos.x, hitPos.y];
        if (clickedPiece == null) return;
       
        // 選王階段
        if (ChessBoard.Instance.kingSelectionState.Value != KingSelectionState.Completed)
        {
            // 只允許選自己隊伍的棋子
            if (clickedPiece.Team.Value == PlayerTeam && Mouse.current.leftButton.wasPressedThisFrame)
                KingManager.Instance.SelectKingPiece(clickedPiece);

            return; // 不進入拖動
        }

        // 一般遊戲階段，自己回合可選棋子
        if (clickedPiece.Team.Value == PlayerTeam && GameManager.Instance.IsTeamTurn(PlayerTeam))
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                currentlyDragging = clickedPiece;
                tileHighlighter.HighlightTiles(currentlyDragging);
            }
        }
    }

    private void HandleDrag(Ray ray)
    {
        if (currentlyDragging == null) return;

        Plane plane = new(Vector3.up, Vector3.up * chessBoard.yOffset);
        if (plane.Raycast(ray, out float distance))
            //currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * chessBoard.dragOffset);
            //只改本地 transform，不動 NetworkVariable
            currentlyDragging.SetLocalPosition(ray.GetPoint(distance) + Vector3.up * chessBoard.dragOffset);
    }

    private void HandleRelease(Vector2Int hitPos)
    {
        if (currentlyDragging == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        Vector2Int prevPos = new(currentlyDragging.X.Value, currentlyDragging.Y.Value);
        bool valid = chessBoard.MoveTo(currentlyDragging, hitPos.x, hitPos.y);

        // 發 ServerRpc 請求移動
        currentlyDragging.RequestMove(hitPos.x, hitPos.y);

        currentlyDragging = null;
        tileHighlighter.RemoveHighlight();
    }

    private void HandleReleaseOnEmpty()
    {
        if (currentlyDragging == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        currentlyDragging.SetPosition(chessBoard.GetTileCenter(currentlyDragging.X.Value, currentlyDragging.Y.Value));
        currentlyDragging = null;
        tileHighlighter.RemoveHighlight();
    }
}