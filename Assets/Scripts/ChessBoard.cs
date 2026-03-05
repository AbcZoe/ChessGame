using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static ChessPiece;

public enum KingSelectionState
{
    None,
    WhiteTeamSelecting,
    BlackTeamSelecting,
    Completed,
    Reselecting
}

public class ChessBoard : MonoBehaviour
{
    public static ChessBoard Instance { get; private set; }
    public bool IsPaused { get; private set; } = false;

    [Header("Art References")]
    [SerializeField] private Material blackMaterial = null;
    [SerializeField] private Material whiteMaterial = null;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material moveEreaMaterial;
    [SerializeField] private Material attackEreaMaterial;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float deathSize = 0.5f;
    [SerializeField] private float deathSpacing = 0.4f;
    [SerializeField] private float dragOffset = 1f;

    [Header("Prefabs & Materials")]
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private Material[] KingMaterials;

    // Logic
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private readonly List<Vector2Int> availableMoves = new();
    private readonly List<Vector2Int> availableAttacks = new();
    private readonly Dictionary<int, List<ChessPiece>> deadPieces = new();
    private const int TILE_COUNT = 8;
    private GameObject[,] tiles;
    private Renderer[,] tileRenderers;
    private Camera currentCamera;
    private Vector2Int currentHoverdTile = -Vector2Int.one;
    private Vector2Int currentSelectedTile = -Vector2Int.one;
    private bool isWhiteTeam_Turn;
    private KingSelectionState kingSelectionState = KingSelectionState.WhiteTeamSelecting;

    private enum TileHighlightType { None, Move, Attack }
    private readonly Dictionary<Vector2Int, TileHighlightType> highlightedTilesDict = new();

    // Properties
    public int CurrentTurnTeam => isWhiteTeam_Turn ? 0 : 1;

    public int SelectingKingTeam
    {
        get
        {
            return kingSelectionState switch
            {
                KingSelectionState.WhiteTeamSelecting => 0,
                KingSelectionState.BlackTeamSelecting => 1,
                KingSelectionState.Reselecting => CurrentTurnTeam,
                _ => -1
            };
        }
    }

    #region Pause Control
    public void SetPaused(bool value)
    {
        IsPaused = value;
    }

    // 供 UIManager 呼叫
    public void PauseGame()
    {
        SetPaused(true);
        Time.timeScale = 0f; // 停止遊戲世界，但 DOTween UI動畫仍播放
    }

    public void ResumeGame()
    {
        SetPaused(false);
        Time.timeScale = 1f;
    }
    #endregion

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isWhiteTeam_Turn = true;

        GenerateGrid(tileSize, TILE_COUNT, TILE_COUNT);
        SpawnAllPiece();
        PositionAllPiece();
    }

    private void Start()
    {
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");
        deadPieces[0] = new();
        deadPieces[1] = new();
    }

    private void Update()
    {
        if (IsPaused) return;
        HandleMouseInteraction();
    }
    #endregion

    #region Tile Generation
    private void GenerateGrid(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];
        tileRenderers = new Renderer[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
                tileRenderers[x, y] = tiles[x, y].GetComponent<Renderer>();
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new($"X:{x}, Y:{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>();

        Vector3[] vertices =
        {
            new(x * tileSize, 0, y * tileSize),
            new(x * tileSize, 0, (y + 1) * tileSize),
            new((x + 1) * tileSize, 0, y * tileSize),
            new((x + 1) * tileSize, 0, (y + 1) * tileSize)
        };

        int[] tris = { 0, 1, 2, 2, 1, 3 };
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        Material tileMat = ((x + y) % 2 == 0) ? blackMaterial : whiteMaterial;
        tileObject.GetComponent<Renderer>().material = tileMat;

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    private void SetTileDefaultMaterial(int x, int y)
    {
        tileRenderers[x, y].material = ((x + y) % 2 == 0) ? blackMaterial : whiteMaterial;
    }
    #endregion

    #region Chess Piece Spawn & Position
    private void SpawnAllPiece()
    {
        chessPieces = new ChessPiece[TILE_COUNT, TILE_COUNT];

        int firstTeam = 0;
        int secondTeam = 1;

        // ===== Team 1 主力 =====
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Rook, firstTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, firstTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.King, firstTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Queen, firstTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, firstTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Rook, firstTeam);

        // ===== Team 1 Pawn 與 Knight 排 =====
        for (int x = 1; x < TILE_COUNT-1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 1] = SpawnSinglePiece(ChessPieceType.Knight, firstTeam);
            else
                chessPieces[x, 1] = SpawnSinglePiece(ChessPieceType.Pawn, firstTeam);
        }

        // ===== Team 2 主力 =====
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Rook, secondTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, secondTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.King, secondTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Queen, secondTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, secondTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Rook, secondTeam);

        // ===== Team 2 Pawn 與 Knight 排 =====
        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 6] = SpawnSinglePiece(ChessPieceType.Knight, secondTeam);
            else
                chessPieces[x, 6] = SpawnSinglePiece(ChessPieceType.Pawn, secondTeam);
        }
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = ChessPiecePool.Instance.GetPiece(type, transform);
        cp.type = type;
        cp.team = team;
        cp.isKing = false;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    private void PositionAllPiece()
    {
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y);
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new(x * tileSize + tileSize / 2, yOffset, y * tileSize + tileSize / 2);
    }

    private Vector2Int GetHitTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                if (tiles[x, y] == hitInfo)
                    return new(x, y);
            }
        }
        return -Vector2Int.one;
    }
    #endregion

    #region Highlight Tiles
    private void HighlightTiles(ChessPiece cp)
    {
        highlightedTilesDict.Clear();

        if (cp == null) return;

        var moves = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT);
        var attacks = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT);

        foreach (var pos in moves)
            highlightedTilesDict[pos] = TileHighlightType.Move;

        foreach (var pos in attacks)
            highlightedTilesDict[pos] = TileHighlightType.Attack; // Attack 優先覆蓋 Move

        foreach (var kvp in highlightedTilesDict)
        {
            tileRenderers[kvp.Key.x, kvp.Key.y].material =
                kvp.Value == TileHighlightType.Move ? moveEreaMaterial : attackEreaMaterial;
        }
    }

    private void RemoveHighlightTile()
    {
        foreach (var kvp in highlightedTilesDict)
        {
            SetTileDefaultMaterial(kvp.Key.x, kvp.Key.y);
        }

        highlightedTilesDict.Clear();
        currentlyDragging = null;
    }

    private void ResetTileMaterial(Vector2Int pos)
    {
        if (highlightedTilesDict.TryGetValue(pos, out TileHighlightType type))
        {
            tileRenderers[pos.x, pos.y].material =
                type == TileHighlightType.Move ? moveEreaMaterial : attackEreaMaterial;
        }
        else
        {
            SetTileDefaultMaterial(pos.x, pos.y);
        }
    }
    #endregion

    #region Mouse Interaction
    private void HandleMouseInteraction()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            if (!currentCamera) return; // 沒有 Camera 就跳過
        }

        if (Mouse.current == null) return; // 防止 Input System 沒啟動

        Vector2 mousePos = Mouse.current.position.ReadValue(); // 新系統取得滑鼠位置
        Ray ray = currentCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPos = GetHitTileIndex(info.transform.gameObject);
            HandleHover(hitPos);
            HandleClick(hitPos);
            HandleDrag(ray);
            HandleRelease(hitPos);
        }
        else
        {
            ResetHover();
            HandleReleaseOnEmpty();
        }
    }

    private void HandleHover(Vector2Int hitPos)
    {
        if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != hitPos)
            ResetTileMaterial(currentHoverdTile);

        currentHoverdTile = hitPos;

        if (currentHoverdTile != currentSelectedTile)
            tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = hoverMaterial;
    }

    private void HandleClick(Vector2Int hitPos)
    {
        if (IsPaused) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        ChessPiece clickedPiece = chessPieces[hitPos.x, hitPos.y];

        if (kingSelectionState == KingSelectionState.WhiteTeamSelecting ||
            kingSelectionState == KingSelectionState.BlackTeamSelecting ||
            kingSelectionState == KingSelectionState.Reselecting)
        {
            if (clickedPiece != null) SelectKingPiece(clickedPiece);
            return;
        }

        if (clickedPiece != null && clickedPiece.team == CurrentTurnTeam)
        {
            currentlyDragging = clickedPiece;
            HighlightTiles(currentlyDragging); // 只依棋子生成格子
        }
    }

    private void HandleDrag(Ray ray)
    {
        if (IsPaused) return;
        if (!currentlyDragging) return;

        Plane plane = new(Vector3.up, Vector3.up * yOffset);
        if (plane.Raycast(ray, out float distance))
            currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
    }

    private void HandleRelease(Vector2Int hitPos)
    {
        if (IsPaused) return;
        if (!currentlyDragging || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        Vector2Int prevPos = new(currentlyDragging.currentX, currentlyDragging.currentY);
        bool validMove = MoveTo(currentlyDragging, hitPos.x, hitPos.y);

        if (!validMove)
            currentlyDragging.SetPosition(GetTileCenter(prevPos.x, prevPos.y));

        currentlyDragging = null;
        RemoveHighlightTile();
    }

    private void ResetHover()
    {
        if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != currentSelectedTile)
            ResetTileMaterial(currentHoverdTile);

        currentHoverdTile = -Vector2Int.one;
    }

    private void HandleReleaseOnEmpty()
    {
        if (IsPaused) return;
        if (!currentlyDragging || !Mouse.current.leftButton.wasReleasedThisFrame) return;

        currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
        currentlyDragging = null;
        RemoveHighlightTile();
    }

    #endregion

    #region Move / Attack
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;

        Vector2Int targetPos = new(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        bool isMove = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);
        bool isAttack = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);

        if (!isMove && !isAttack) return false;

        if (isAttack && targetPiece != null && targetPiece.team != cp.team)
        {
            HandleAttack(cp, targetPiece);
        }
        else if (isMove && targetPiece != null)
        {
            return false;
        }

        Vector2Int previousPos = new(cp.currentX, cp.currentY);
        chessPieces[x, y] = cp;
        chessPieces[previousPos.x, previousPos.y] = null;
        PositionSinglePiece(x, y, true);

        isWhiteTeam_Turn = !isWhiteTeam_Turn;
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);

        return true;
    }
    private void HandleAttack(ChessPiece attacker, ChessPiece target)
    {
        int defeatedTeam = target.team;

        if (target.isKing)
            CheckMate(attacker.team);

        deadPieces[defeatedTeam].Add(target);
        target.SetScale(Vector3.one * deathSize, true);

        Vector3 deathBase = defeatedTeam == 0
            ? new Vector3(8 * tileSize, yOffset, -1 * tileSize) + Vector3.one * tileSize / 2
            : new Vector3(-1 * tileSize, yOffset, 8 * tileSize) + Vector3.one * tileSize / 2;

        float zOffset = (defeatedTeam == 0 ? 1f : -1f) * deathSpacing * deadPieces[defeatedTeam].Count * 0.5f;
        Vector3 deathOffset = new(0f, 0f, zOffset);
        target.SetPosition(deathBase + deathOffset);

        chessPieces[target.currentX, target.currentY] = null;
    }
    public bool ContainsValidMove(List<Vector2Int> moves, Vector2Int pos) => moves.Contains(pos);
    public bool ContainsValidAttack(List<Vector2Int> attacks, Vector2Int pos) => attacks.Contains(pos);
    public void SwitchTurn()
    {
        isWhiteTeam_Turn = !isWhiteTeam_Turn;
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
    }
    #endregion

    #region King Selection
    public void SelectKingPiece(ChessPiece cp)
    {
        if (IsPaused) return;
        if (kingSelectionState == KingSelectionState.None || kingSelectionState == KingSelectionState.Completed) return;
        if (!IsValidKingSelection(cp))
        {
            UIManager.Instance.ShowMessage("You cannot select the opponent's piece as King!");
            return;
        }

        cp.SetKing(true);
        if (KingMaterials != null && cp.team < KingMaterials.Length)
            cp.GetComponent<MeshRenderer>().material = KingMaterials[cp.team];

        if (kingSelectionState == KingSelectionState.Reselecting) FinishReselectKing();
        else AdvanceKingSelectionState();
    }
    private bool IsValidKingSelection(ChessPiece cp)
    {
        return (kingSelectionState == KingSelectionState.WhiteTeamSelecting && cp.team == 0) ||
               (kingSelectionState == KingSelectionState.BlackTeamSelecting && cp.team == 1) ||
               (kingSelectionState == KingSelectionState.Reselecting && cp.team == CurrentTurnTeam);
    }
    private void FinishReselectKing()
    {
        UIManager.Instance.ShowMessage($"Team {CurrentTurnTeam + 1} has reselected their King!", 2f);
        kingSelectionState = KingSelectionState.Completed;
        SwitchTurn();
    }
    private void AdvanceKingSelectionState()
    {
        if (kingSelectionState == KingSelectionState.WhiteTeamSelecting)
        {
            kingSelectionState = KingSelectionState.BlackTeamSelecting;
            UIManager.Instance.ShowMessage("Team 2, please select a piece as your King!");
        }
        else
        {
            kingSelectionState = KingSelectionState.Completed;
            UIManager.Instance.ShowMessage("Both teams have chosen their Kings. The game starts!", 2f);
            UIManager.Instance.ShowCurrentTurn(0);
        }
    }
    // ===== Cards ===== 
    public void ResetKingSelectionForCurrentTeam()
    {
        int currentTeam = CurrentTurnTeam;
        kingSelectionState = KingSelectionState.Reselecting;

        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == currentTeam)
            {
                piece.SetKing(false);
                piece.GetComponent<MeshRenderer>().material = teamMaterials[currentTeam];
            }
        }

        UIManager.Instance.ShowMessage($"Team {currentTeam + 1}, please select a new King!");
    }
    #endregion

    #region CheckMate / Reset
    private void CheckMate(int team) => UIManager.Instance.ShowVictory(team);
    public void ResetBoard()
    {
        UIManager.Instance.HideVictory();
        currentlyDragging = null;
        availableMoves.Clear();
        availableAttacks.Clear();

        foreach (var piece in chessPieces)
        {
            if (piece != null) ChessPiecePool.Instance.ReturnPiece(piece);
        }

        foreach (var kvp in deadPieces)
        {
            foreach (var piece in kvp.Value)
            {
                if (piece != null) ChessPiecePool.Instance.ReturnPiece(piece);
            }
            kvp.Value.Clear();
        }

        SpawnAllPiece();
        PositionAllPiece();
        isWhiteTeam_Turn = true;

        foreach (var piece in chessPieces)
        {
            if (piece != null) piece.isKing = false;
        }

        kingSelectionState = KingSelectionState.WhiteTeamSelecting;

        UIManager.Instance.HideRoundText();
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");
        StartCoroutine(CardSystem.Instance.DealStartingCards());
    }
    #endregion
}
