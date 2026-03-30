using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private Camera currentCamera;
    private ChessBoard chessBoard;
    private TileHighlighter tileHighlighter;

    private Vector2Int currentHoverdTile = -Vector2Int.one;
    private ChessPiece currentlyDragging;

    public void Initialize(ChessBoard board, TileHighlighter highlighter)
    {
        chessBoard = board;
        tileHighlighter = highlighter;
    }

    private void Update()
    {
        HandleMouseInteraction();
    }

    private void HandleMouseInteraction()
    {
        if (!currentCamera) currentCamera = Camera.main;
        if (!currentCamera || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = currentCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPos = chessBoard.GetHitTileIndex(info.transform.gameObject);

            HandleHover(hitPos);
            HandleClick(hitPos);

            // 選王階段禁止拖動
            if (ChessBoard.Instance.kingSelectionState == KingSelectionState.Completed)
            {
                HandleDrag(ray);
                HandleRelease(hitPos);
            }
        }
        else
        {
            if (ChessBoard.Instance.kingSelectionState == KingSelectionState.Completed)
                ResetHover();

            if (ChessBoard.Instance.kingSelectionState == KingSelectionState.Completed)
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
        if (ChessBoard.Instance.kingSelectionState != KingSelectionState.Completed)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                KingManager.Instance.SelectKingPiece(clickedPiece);

            return; // 不進入拖動
        }

        // 一般遊戲階段，自己回合可選棋子
        if (clickedPiece.Team.Value == GameManager.Instance.CurrentTurnTeam)
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
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * chessBoard.dragOffset);
    }

    private void HandleRelease(Vector2Int hitPos)
    {
        if (currentlyDragging == null || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        Vector2Int prevPos = new(currentlyDragging.X.Value, currentlyDragging.Y.Value);
        bool valid = chessBoard.MoveTo(currentlyDragging, hitPos.x, hitPos.y);

        if (!valid)
            currentlyDragging.SetPosition(chessBoard.GetTileCenter(prevPos.x, prevPos.y));

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