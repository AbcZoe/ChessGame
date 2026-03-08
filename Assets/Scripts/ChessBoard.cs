using System.Collections.Generic;
using UnityEngine;
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

    [Header("Materials")]
    [SerializeField] public Material blackMaterial;
    [SerializeField] public Material whiteMaterial;
    [SerializeField] public Material hoverMaterial;
    [SerializeField] public Material moveEreaMaterial;
    [SerializeField] public Material attackEreaMaterial;
    [SerializeField] public Material[] teamMaterials;
    [SerializeField] public Material[] KingMaterials;


    [Header("Board Settings")]
    public float tileSize = 1f;
    public float yOffset = 0.2f;
    public float dragOffset = 1f;
    public int TILE_COUNT = 8;

    [Header("References")]
    public GameObject[,] tiles;
    public Renderer[,] tileRenderers;
    public ChessPiece[,] chessPieces;

    // Logic
    public KingSelectionState kingSelectionState = KingSelectionState.WhiteTeamSelecting;

    #region Unity Methods
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GenerateGrid(tileSize, TILE_COUNT, TILE_COUNT);
        SpawnAllPiece();
        PositionAllPiece();
    }

    private void Start()
    {
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");
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

    public void SetTileDefaultMaterial(int x, int y)
    {
        tileRenderers[x, y].material = ((x + y) % 2 == 0) ? blackMaterial : whiteMaterial;
    }

    public Renderer GetTileRenderer(int x, int y) => tileRenderers[x, y];

    public Vector2Int GetHitTileIndex(GameObject hitInfo)
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

        for (int x = 1; x < TILE_COUNT - 1; x++)
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

    public void PositionAllPiece()
    {
        for (int x = 0; x < TILE_COUNT; x++)
            for (int y = 0; y < TILE_COUNT; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y);
    }

    public void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    public Vector3 GetTileCenter(int x, int y) => new(x * tileSize + tileSize / 2, yOffset, y * tileSize + tileSize / 2);
    #endregion

    #region Move / Attack
    public bool MoveTo(ChessPiece cp, int x, int y)
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

        GameManager.Instance.SwitchTurn();
        return true;
    }

    private void HandleAttack(ChessPiece attacker, ChessPiece target)
    {
        if (target == null) return;

        if (target.isKing)
            GameManager.Instance.CheckMate(attacker.team);

        GraveyardManager.Instance.AddDeadPiece(target);
    }
    #endregion

    public void ResetBoard()
    {
        foreach (var piece in chessPieces) {
            if (piece != null) 
                ChessPiecePool.Instance.ReturnPiece(piece);
        }
        // 重置死亡棋子
        GraveyardManager.Instance.ResetGraveyard();
        SpawnAllPiece();
        PositionAllPiece();
        foreach (var piece in chessPieces) {
            if (piece != null) piece.isKing = false; 
        }
        kingSelectionState = KingSelectionState.WhiteTeamSelecting;
        UIManager.Instance.HideVictory(); 
        UIManager.Instance.HideRoundText(); 
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");
        StartCoroutine(CardSystem.Instance.DealStartingCards()); 
    } 
}