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

        // 只有 Host 生成棋子
        if (NetworkManager.Singleton.IsHost)
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

        cp.team = team;
        cp.isKing = false;

        // Host 設材質
        MeshRenderer rend = cp.GetComponent<MeshRenderer>();
        if (rend != null)
            rend.material = teamMaterials[team];

        cp.currentX = x;
        cp.currentY = y;
        cp.SetPosition(GetTileCenter(x, y), true);

        // Spawn NetworkObject
        NetworkObject netObj = cp.GetComponent<NetworkObject>();
        if (!netObj.IsSpawned)
            netObj.Spawn();

        chessPieces[x, y] = cp;

        // 告訴 Client 初始化棋子
        InitPieceClientRpc(netObj.NetworkObjectId, x, y, team);

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

    public void ResetBoard()
    {
        // 先把棋盤上的棋子回收到 Pool
        for (int x = 0; x < TILE_COUNT; x++)
        {
            for (int y = 0; y < TILE_COUNT; y++)
            {
                ChessPiece piece = chessPieces[x, y];
                if (piece != null)
                {
                    ChessPiecePool.Instance.ReturnPiece(piece);
                    chessPieces[x, y] = null;
                }
            }
        }

        // 重置死亡棋子
        GraveyardManager.Instance.ResetGraveyard();

        // 重新生成棋子
        SpawnAllPiece();
        PositionAllPiece();

        // Host 生成的棋子確保 NetworkObject Spawn
        if (NetworkManager.Singleton.IsHost)
        {
            foreach (var piece in chessPieces)
            {
                if (piece != null)
                {
                    NetworkObject netObj = piece.GetComponent<NetworkObject>();
                    if (netObj != null && !netObj.IsSpawned)
                        netObj.Spawn();
                }
            }
        }

        // 所有棋子都不是 King
        foreach (var piece in chessPieces)
        {
            if (piece != null)
                piece.isKing = false;
        }

        // 重置選王階段
        kingSelectionState = KingSelectionState.WhiteTeamSelecting;

        // 更新 UI
        UIManager.Instance.HideVictory();
        UIManager.Instance.HideRoundText();
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");

        // 發牌 (如果有 CardSystem)
        if (NetworkManager.Singleton.IsHost)
        {
            StartCoroutine(CardSystem.Instance.DealStartingCardsCoroutine());
        }
    }

    #endregion

    #region Move / Attack
    public bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null || !NetworkManager.Singleton.IsHost) return false;

        Vector2Int previousPos = new(cp.currentX, cp.currentY);
        Vector2Int targetPos = new(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        bool isMove = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);
        bool isAttack = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);

        if (!isMove && !isAttack) return false;

        if (isAttack && targetPiece != null && targetPiece.team != cp.team)
        {
            if (targetPiece.isKing)
                GameManager.Instance.CheckMate(cp.team);

            GraveyardManager.Instance.AddDeadPiece(targetPiece);
        }
        else if (isMove && targetPiece != null)
            return false;

        chessPieces[x, y] = cp;
        chessPieces[previousPos.x, previousPos.y] = null;

        cp.SetPosition(GetTileCenter(x, y), true);

        GameManager.Instance.SwitchTurn();

        // 通知 Client
        MovePieceClientRpc(previousPos.x, previousPos.y, x, y);

        return true;
    }

    [ClientRpc]
    private void InitPieceClientRpc(ulong pieceId, int x, int y, int team)
    {
        if (NetworkManager.Singleton.IsHost) return; // Host 不需要初始化

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceId, out NetworkObject netObj))
            return;

        ChessPiece cp = netObj.GetComponent<ChessPiece>();
        if (cp == null) return;

        // 設隊伍材質
        cp.team = team;
        MeshRenderer rend = cp.GetComponent<MeshRenderer>();
        if (rend != null)
            rend.material = teamMaterials[team];

        // 設位置
        cp.currentX = x;
        cp.currentY = y;
        cp.SetPosition(ChessBoard.Instance.GetTileCenter(x, y), true);

        // 存入棋盤陣列
        if (ChessBoard.Instance.chessPieces == null)
            ChessBoard.Instance.chessPieces = new ChessPiece[ChessBoard.Instance.TILE_COUNT, ChessBoard.Instance.TILE_COUNT];

        ChessBoard.Instance.chessPieces[x, y] = cp;
    }

    [ClientRpc]
    private void MovePieceClientRpc(int fromX, int fromY, int toX, int toY)
    {
        if (NetworkManager.Singleton.IsHost) return;

        ChessPiece cp = chessPieces[fromX, fromY];
        if (cp == null) return;

        chessPieces[toX, toY] = cp;
        chessPieces[fromX, fromY] = null;
        cp.SetPosition(GetTileCenter(toX, toY), true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMovePieceServerRpc(ulong pieceId, int x, int y)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceId, out NetworkObject netObj))
            return;

        ChessPiece piece = netObj.GetComponent<ChessPiece>();

        if (piece == null)
            return;

        MoveTo(piece, x, y);
    }
    #endregion
}