using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
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

public class ChessBoard : NetworkBehaviour
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

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab; // Quad + Collider + NetworkObject

    [Header("Pieces Prefab")]
    [SerializeField] private List<ChessPieceData> allPieceData;
    private Dictionary<string, ChessPieceData> pieceDataDict;

    [Header("References")]
    public GameObject[,] tiles;
    public Renderer[,] tileRenderers;
    public ChessPiece[,] chessPieces;

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

        pieceDataDict = new Dictionary<string, ChessPieceData>();
        foreach (var data in allPieceData)
        {
            pieceDataDict[data.pieceName] = data;
        }

        // 所有人都生成棋盤
        GenerateGrid(tileSize, TILE_COUNT, TILE_COUNT);

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnAllPiece();
            PositionAllPiece();
        }
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
                GameObject tileObject = Instantiate(tilePrefab, transform);
                tileObject.name = $"Tile X:{x} Y:{y}";

                // 放到正確位置
                tileObject.transform.position = new Vector3(x * tileSize + tileSize / 2, 0, y * tileSize + tileSize / 2);

                // 調整尺寸
                tileObject.transform.localScale = new Vector3(tileSize, 1, tileSize);

                // 旋轉 Quad 平放
                //tileObject.transform.rotation = Quaternion.Euler(90, 0, 0);

                // 設材質
                Material tileMat = ((x + y) % 2 == 0) ? blackMaterial : whiteMaterial;
                Renderer rend = tileObject.GetComponent<Renderer>();
                if (rend != null)
                    rend.material = tileMat;

                tiles[x, y] = tileObject;
                tileRenderers[x, y] = rend;

                // Collider
                if (tileObject.GetComponent<Collider>() == null)
                    tileObject.AddComponent<BoxCollider>();

            }
        }
    }

    public void SetTileDefaultMaterial(int x, int y)
    {
        tileRenderers[x, y].material = ((x + y) % 2 == 0) ? blackMaterial : whiteMaterial;
    }

    public Renderer GetTileRenderer(int x, int y) => tileRenderers[x, y];

    public Vector2Int GetHitTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT; x++)
            for (int y = 0; y < TILE_COUNT; y++)
                if (tiles[x, y] == hitInfo)
                    return new(x, y);
        return -Vector2Int.one;
    }
    #endregion

    #region Chess Piece Spawn & Position
    private void SpawnAllPiece()
    {
        chessPieces = new ChessPiece[TILE_COUNT, TILE_COUNT];
        int firstTeam = 0;
        int secondTeam = 1;

        // ===== Team 1 =====
        chessPieces[1, 0] = SpawnSinglePiece("Rook", firstTeam, 1, 0);
        chessPieces[2, 0] = SpawnSinglePiece("Bishop", firstTeam, 2, 0);
        chessPieces[3, 0] = SpawnSinglePiece("King", firstTeam, 3, 0);
        chessPieces[4, 0] = SpawnSinglePiece("Queen", firstTeam, 4, 0);
        chessPieces[5, 0] = SpawnSinglePiece("Bishop", firstTeam, 5, 0);
        chessPieces[6, 0] = SpawnSinglePiece("Rook", firstTeam, 6, 0);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 1] = SpawnSinglePiece("Knight", firstTeam, x, 1);
            else
                chessPieces[x, 1] = SpawnSinglePiece("Pawn", firstTeam, x, 1);
        }

        // ===== Team 2 =====
        chessPieces[1, 7] = SpawnSinglePiece("Rook", secondTeam, 1, 7);
        chessPieces[2, 7] = SpawnSinglePiece("Bishop", secondTeam, 2, 7);
        chessPieces[3, 7] = SpawnSinglePiece("King", secondTeam, 3, 7);
        chessPieces[4, 7] = SpawnSinglePiece("Queen", secondTeam, 4, 7);
        chessPieces[5, 7] = SpawnSinglePiece("Bishop", secondTeam, 5, 7);
        chessPieces[6, 7] = SpawnSinglePiece("Rook", secondTeam, 6, 7);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 6] = SpawnSinglePiece("Knight", secondTeam, x, 6);
            else
                chessPieces[x, 6] = SpawnSinglePiece("Pawn", secondTeam, x, 6);
        }

    }

    private ChessPiece SpawnSinglePiece(string pieceName, int team, int x, int y)
    {
        if (!pieceDataDict.ContainsKey(pieceName))
        {
            Debug.LogError($"找不到棋子資料：{pieceName}");
            return null;
        }

        ChessPieceData data = pieceDataDict[pieceName];
        ChessPiece cp = Instantiate(data.prefab, transform);

        // **直接給材質**
        cp.GetComponent<MeshRenderer>().material = ChessBoard.Instance.teamMaterials[team];

        NetworkObject netObj = cp.GetComponent<NetworkObject>();
        if (IsServer)
            netObj.Spawn();
        
        if (IsServer)
        {
            cp.Team.Value = team;
            cp.X.Value = x;
            cp.Y.Value = y;
            cp.isKing.Value = false;
        }

        cp.SetPosition(GetTileCenter(x, y), true);

        chessPieces[x, y] = cp;

        return cp;
    }

    public void PositionAllPiece()
    {
        for (int x = 0; x < TILE_COUNT; x++)
            for (int y = 0; y < TILE_COUNT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null)
                {
                    piece.SetPosition(GetTileCenter(x, y), true);
                }
            }
    }

    public Vector3 GetTileCenter(int x, int y) => new(x * tileSize + tileSize / 2, yOffset, y * tileSize + tileSize / 2);

    public void ResetBoard()
    {
        if (!IsServer) return;

        // 先回收棋盤上的棋子
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece == null) continue;

                NetworkObject netObj = piece.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);

                chessPieces[x, y] = null;
            }
        }

        // 回收死亡棋子
        GraveyardManager.Instance.ResetGraveyard(); // 確保這裡只做 ReturnPiece，且不要 spawn/despawn

        // 生成棋盤上的新棋子
        SpawnAllPiece();
        PositionAllPiece();

        GameManager.Instance.isWhiteTeamTurn.Value = true;
        // 重置 UI
        kingSelectionState = KingSelectionState.WhiteTeamSelecting;
        UIManager.Instance.HideVictory();
        UIManager.Instance.HideRoundText();
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");

        StartCoroutine(CardSystem.Instance.DealStartingCardsCoroutine());
    }

    #endregion

    #region Move / Attack
    public bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (!IsServer) return false;

        Vector2Int previousPos = new(cp.X.Value, cp.Y.Value);
        Vector2Int targetPos = new(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        bool isMove = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);
        bool isAttack = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);

        if (!isMove && !isAttack) return false;

        if (isAttack && targetPiece != null && targetPiece.Team != cp.Team)
        {
            if (targetPiece.isKing.Value)
                GameManager.Instance.CheckMate(cp.Team.Value);

            GraveyardManager.Instance.AddDeadPiece(targetPiece);
        }
        else if (isMove && targetPiece != null)
            return false;

        chessPieces[x, y] = cp;
        chessPieces[previousPos.x, previousPos.y] = null;

        cp.X.Value = x;
        cp.Y.Value = y;

        GameManager.Instance.SwitchTurn();

        return true;
    }
    #endregion
}