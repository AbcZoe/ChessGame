using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using static ChessPiece;
using static UnityEngine.Audio.ProcessorInstance;
using static UnityEngine.Rendering.DebugUI;

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
    private int deadCountTeam0 = 0;
    private int deadCountTeam1 = 0;
    private int AllocateDeadIndex(int team) => team == 0 ? deadCountTeam0++ : deadCountTeam1++;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab; // Quad + Collider + NetworkObject

    [Header("Pieces Prefab")]
    [SerializeField] private List<ChessPieceData> allPieceData;
    private Dictionary<string, ChessPieceData> pieceDataDict;

    [Header("References")]
    public GameObject[,] tiles;
    public Renderer[,] tileRenderers;
    public ChessPiece[,] chessPieces; // Server-authoritative board map. Client side may be empty/out-of-date; do not use for gameplay decisions.

    [Header("Audio Data")]
    [SerializeField] private AudioData atePieceAudioData;

    public NetworkVariable<KingSelectionState> kingSelectionState =
        new NetworkVariable<KingSelectionState>(
            KingSelectionState.WhiteTeamSelecting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server  
        );

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

        // Client 也先建立陣列，避免 NullReference
        if (chessPieces == null)
            chessPieces = new ChessPiece[TILE_COUNT, TILE_COUNT];
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnAllPiece();
            kingSelectionState.Value = KingSelectionState.WhiteTeamSelecting;
        }

        kingSelectionState.OnValueChanged += OnKingStateChanged;
        StartCoroutine(DelayedHandleKingStateUI(kingSelectionState.Value));

    }
    #endregion

    #region Unity King Selection State
    private void OnKingStateChanged(KingSelectionState oldState, KingSelectionState newState)
    {
        HandleKingStateUI(newState);
    }
    private IEnumerator DelayedHandleKingStateUI(KingSelectionState state)
    {
        yield return null; // 等一幀
        if (GameManager.Instance != null)
            HandleKingStateUI(state);
    }

    private void HandleKingStateUI(KingSelectionState state)
    {
        var rpcParams = new RpcParams
        {
            Send = new RpcSendParams
            {
                Target = RpcTarget.ClientsAndHost
            }
        };

        switch (state)
        {
            case KingSelectionState.WhiteTeamSelecting:
                GameManager.Instance?.ShowMessageToClient("Team 1, please select a piece as your King!", 0f, rpcParams);
                break;

            case KingSelectionState.BlackTeamSelecting:
                GameManager.Instance?.ShowMessageToClient("Team 2, please select a piece as your King!", 0f, rpcParams);
                break;

            case KingSelectionState.Completed:
                GameManager.Instance?.ShowMessageToClient("Both teams have chosen their Kings. The game starts!", 2f, rpcParams);
                UIManager.Instance?.ShowCurrentTurn(GameManager.Instance.CurrentTurnTeam);
                break;

            case KingSelectionState.Reselecting:
                GameManager.Instance?.ShowMessageToClient($"Team {GameManager.Instance.CurrentTurnTeam + 1}, please select a new King!", 0f, rpcParams);
                break;
        }
    }

    public void SetKingState_Server(KingSelectionState newState)
    {
        if (!IsServer) return;
        kingSelectionState.Value = newState;
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
        int blackTeam = 0;
        int whiteTeam = 1;

        // ===== Team 1 =====
        SpawnSinglePiece("Rook", blackTeam, 1, 0);
        SpawnSinglePiece("Bishop", blackTeam, 2, 0);
        SpawnSinglePiece("King", blackTeam, 3, 0);
        SpawnSinglePiece("Queen", blackTeam, 4, 0);
        SpawnSinglePiece("Bishop", blackTeam, 5, 0);
        SpawnSinglePiece("Rook", blackTeam, 6, 0);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                SpawnSinglePiece("Knight", blackTeam, x, 1);
            else
                SpawnSinglePiece("Pawn", blackTeam, x, 1);
        }

        // ===== Team 2 =====
        SpawnSinglePiece("Rook", whiteTeam, 1, 7);
        SpawnSinglePiece("Bishop", whiteTeam, 2, 7);
        SpawnSinglePiece("King", whiteTeam, 3, 7);
        SpawnSinglePiece("Queen", whiteTeam, 4, 7);
        SpawnSinglePiece("Bishop", whiteTeam, 5, 7);
        SpawnSinglePiece("Rook", whiteTeam, 6, 7);

        for (int x = 1; x < TILE_COUNT - 1; x++)
        {
            if (x == 2 || x == 5)
                SpawnSinglePiece("Knight", whiteTeam, x, 6);
            else
                SpawnSinglePiece("Pawn", whiteTeam, x, 6);
        }
    }

    private void SpawnSinglePiece(string pieceName, int team, int x, int y)
    {
        ChessPiece obj = Instantiate(pieceDataDict[pieceName].prefab).GetComponent<ChessPiece>();

        var netObj = obj.GetComponent<NetworkObject>();
        ChessPiece piece = obj.GetComponent<ChessPiece>();
        netObj.Spawn();

        // 先設初始狀態（Server）
        if (IsServer)
        {
            piece.Team.Value = team;
            piece.X.Value = x;
            piece.Y.Value = y;
            piece.isKing.Value = false;

            chessPieces[x, y] = obj;
        }
 
    }

    public Vector3 GetTileCenter(int x, int y) => new(x * tileSize + tileSize / 2, yOffset, y * tileSize + tileSize / 2);

    public void ResetBoard()
    {
        if (!IsServer) return;

        // 先清理棋盤上的棋子
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
        // 再清理死亡區
        GraveyardManager.Instance?.ClearDeadPieces();

        // 生成棋盤上的新棋子
        SpawnAllPiece();

        // 重置 UI
        GameManager.Instance.isWhiteTeamTurn.Value = true;
        kingSelectionState.Value = KingSelectionState.WhiteTeamSelecting;
        UIManager.Instance.HideRoundText();
        GameManager.Instance.HideVictoryClientRpc();
        //GameManager.Instance.ShowMessageClientRpc("Team 1, please select a piece as your King!", 0f);

        StartCoroutine(CardSystem.Instance.DealStartingCardsCoroutine());
    }

    #endregion

    #region Move / Attack
    public void MoveTo(ChessPiece cp, int x, int y, ulong senderClientId)
    {
        if (!IsServer) return;
        bool attackerAlsoDies = false;

        Vector2Int previousPos = new(cp.X.Value, cp.Y.Value);
        Vector2Int targetPos = new(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        bool isMove = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);
        bool isAttack = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);

        if (!isMove && !isAttack) return;

        if (isAttack && targetPiece != null && targetPiece.Team.Value != cp.Team.Value)
        {
            // 不滅：不能被吃
            if (targetPiece.IndestructibleActive.Value)
            {
                GameManager.Instance?.ShowMessageToClient("This piece is in \"Indestructible\" state, cannot be captured.", 2f, senderClientId);
                ReleseToStart(cp, previousPos.x, previousPos.y);
                return;
            }

            // 記下「被吃子」是否有同歸於盡
            bool mutual = targetPiece.MutualDestructionActive.Value;

            if (targetPiece.isKing.Value)
                GameManager.Instance.CheckMate(cp.Team.Value);

            // 吃掉對方
            int idxVictim = AllocateDeadIndex(targetPiece.Team.Value);
            targetPiece.DeadIndex.Value = idxVictim;
            targetPiece.IsDead.Value = true;
            AudioManager.Instance.PlaySFX(atePieceAudioData);

            // 同歸於盡：攻擊者也死（但不要把死棋移到 targetPos）
            if (mutual)
            {
                attackerAlsoDies = true;
                GameManager.Instance?.ShowMessageToClient("The piece is in \"Mutual Destruction\" state, so both pieces have been captured..", 2f, senderClientId);
                int idxAttacker = AllocateDeadIndex(cp.Team.Value);
                cp.DeadIndex.Value = idxAttacker;
                cp.IsDead.Value = true;
                AudioManager.Instance.PlaySFX(atePieceAudioData);
                if (targetPiece.isKing.Value && cp.isKing.Value)
                {
                    GameManager.Instance.CheckMate(2);
                    return;
                }
                else if(cp.isKing.Value)
                    GameManager.Instance.CheckMate(targetPiece.Team.Value);
            }
        }
        else if (isMove && targetPiece != null)
        {
            // 目標格有棋子且不是攻擊格 -> 不允許走
            return;
        }

        // 更新棋盤陣列與座標
        if (attackerAlsoDies)
        {
            // 兩顆都死：原位清空、座標格也清空
            chessPieces[targetPos.x, targetPos.y] = null;
            chessPieces[previousPos.x, previousPos.y] = null;

        }
        else
        {
            chessPieces[targetPos.x, targetPos.y] = cp;
            chessPieces[previousPos.x, previousPos.y] = null;

            cp.X.Value = targetPos.x;
            cp.Y.Value = targetPos.y;
        }

        // 升變檢查
        if (cp is Pawn)
        {
            bool shouldPromote =
                (cp.Team.Value == 0 && cp.Y.Value == 7) ||
                (cp.Team.Value == 1 && cp.Y.Value == 0);

            if (shouldPromote)
            {
                PromoteRequestClientRpc(cp.NetworkObjectId, cp.Team.Value);
                return;
            }
        }

        GameManager.Instance.EndAction();
        return;
    }

    public void ReleseToStart(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPos = new(cp.X.Value, cp.Y.Value);
        Vector2Int targetPos = new(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        bool isMove = cp.GetAvailableMoves(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);
        bool isAttack = cp.GetAvailableAttacks(ref chessPieces, TILE_COUNT, TILE_COUNT).Contains(targetPos);

        if (!isMove && !isAttack)
        {
            cp.SetPosition(GetTileCenter(previousPos.x, previousPos.y));
            return;
        }

        return;
    }

    /// 升變處理：Server 驗證、Client 顯示 UI、Server 執行升變

    [Rpc(SendTo.ClientsAndHost)]
    private void PromoteRequestClientRpc(ulong pawnId, int team)
    {
        if (GameManager.Instance.LocalTeam != team)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pawnId, out var obj))
            return;

        ChessPiece pawn = obj.GetComponent<ChessPiece>();
        if (pawn.IsDead.Value) return;

        UIManager.Instance.ShowPromotionOptions((type) =>
        {
            RequestPromotionServerRpc(pawnId, type);
        });
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPromotionServerRpc(ulong pawnId, PromotionType type, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pawnId, out var obj))
            return;

        ChessPiece pawn = obj.GetComponent<ChessPiece>();

        int senderTeam = GameManager.Instance.GetClientTeam(sender);

        // 防作弊
        if (pawn.Team.Value != senderTeam)
            return;

        PromotePawn(pawn, type);
    }

    private void PromotePawn(ChessPiece pawn, PromotionType type)
    {
        int x = pawn.X.Value;
        int y = pawn.Y.Value;
        int team = pawn.Team.Value;

        pawn.GetComponent<NetworkObject>().Despawn(true);

        string pieceName = type switch
        {
            PromotionType.Queen => "Queen",
            PromotionType.Rook => "Rook",
            PromotionType.Bishop => "Bishop",
            PromotionType.Knight => "Knight",
            _ => "Queen"
        };

        ChessPiece newPiece = Instantiate(pieceDataDict[pieceName].prefab)
            .GetComponent<ChessPiece>();

        var netObj = newPiece.GetComponent<NetworkObject>();
        netObj.Spawn();

        newPiece.Team.Value = team;
        newPiece.X.Value = x;
        newPiece.Y.Value = y;

        chessPieces[x, y] = newPiece;

        GameManager.Instance.EndAction();
    }
    #endregion
}