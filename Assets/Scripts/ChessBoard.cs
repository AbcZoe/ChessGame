using System.Collections.Generic;
using Unity.Netcode;
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
        if (!NetworkManager.Singleton.IsHost)
        {
            chessPieces = new ChessPiece[TILE_COUNT, TILE_COUNT];
        }

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
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Rook, firstTeam, 1, 0);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, firstTeam, 2, 0);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.King, firstTeam, 3, 0);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Queen, firstTeam, 4, 0);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, firstTeam, 5, 0);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Rook, firstTeam, 6, 0);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 1] = SpawnSinglePiece(ChessPieceType.Knight, firstTeam, x, 1);
            else
                chessPieces[x, 1] = SpawnSinglePiece(ChessPieceType.Pawn, firstTeam, x, 1);
        }

        // ===== Team 2 =====
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Rook, secondTeam, 1, 7);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, secondTeam, 2, 7);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.King, secondTeam, 3, 7);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Queen, secondTeam, 4, 7);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, secondTeam, 5, 7);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Rook, secondTeam, 6, 7);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                chessPieces[x, 6] = SpawnSinglePiece(ChessPieceType.Knight, secondTeam, x, 6);
            else
                chessPieces[x, 6] = SpawnSinglePiece(ChessPieceType.Pawn, secondTeam, x, 6);
        }

    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team, int x, int y)
    {
        ChessPiece cp = ChessPiecePool.Instance.GetPiece(type);

        NetworkObject netObj = cp.GetComponent<NetworkObject>();
        if (!netObj.IsSpawned && IsServer)
            netObj.Spawn();

        if (IsServer) // 只有 Host 寫入 NetworkVariable
        {
            cp.type = type;
            cp.Team.Value = team;
            cp.X.Value = x;
            cp.Y.Value = y;
            cp.isKing.Value = false;
        }
        cp.UpdateVisual();
        
        chessPieces[x, y] = cp;
        cp.SetPosition(GetTileCenter(x, y), true);

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
        if (!IsServer) return; // 只有 Host 控制重置流程

        // 將棋盤上的棋子回收到 Pool
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null)
                {
                    NetworkObject netObj = piece.GetComponent<NetworkObject>();

                    if (netObj != null && netObj.IsSpawned)
                        netObj.Despawn();

                    ChessPiecePool.Instance.ReturnPiece(piece);
                    chessPieces[x, y] = null;
                }
            }
        }

        // 重置死亡棋子
        GraveyardManager.Instance.ResetGraveyard();

        // 重新生成棋子
        SpawnAllPiece();

        // 確保每個棋子 NetworkObject 都已 Spawn 並設置 NetworkVariable
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null)
                {
                    NetworkObject netObj = piece.GetComponent<NetworkObject>();
                    if (!netObj.IsSpawned)
                        netObj.Spawn();

                    // Host 設定 NetworkVariable，Client 自動同步
                    piece.Team.Value = piece.Team.Value; // 強制 NetworkVariable 同步
                    piece.X.Value = x;
                    piece.Y.Value = y;
                    piece.isKing.Value = false;

                    // 立即更新位置與材質
                    piece.SetPosition(GetTileCenter(x, y), true);
                    piece.UpdateVisual();
                }
            }
        }

        // 重置選王階段
        kingSelectionState = KingSelectionState.WhiteTeamSelecting;

        // 更新 UI
        UIManager.Instance.HideVictory();
        UIManager.Instance.HideRoundText();
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");

        // 發牌 (如果有 CardSystem)
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