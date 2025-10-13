using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using static ChessPiece;
using static UnityEditor.PlayerSettings;

public class ChessBoard : MonoBehaviour
{
    public static ChessBoard Instance { get; private set; }

    [Header("Art References")]
    [SerializeField] private Material blackMaterial = null;    // 黑格材質
    [SerializeField] private Material whiteMaterial = null;    // 白格材質
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material moveEreaMaterial;
    [SerializeField] private Material attackEreaMaterial;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float deathSize = 50f;
    [SerializeField] private float deathSpacing = 0.4f;
    [SerializeField] private float dragOffset = 1f;
    TMP_Text messageText;

    [Header("Prefabs & Materials")]
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private Material[] KingMaterials;

    //LOGIC
    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    private List<Vector2Int> availableAttacks = new List<Vector2Int>();
    private List<ChessPiece> deadTeam_1 = new List<ChessPiece>();
    private List<ChessPiece> deadTeam_2 = new List<ChessPiece>();
    private const int Tile_COUNT_X = 8;
    private const int Tile_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Renderer[,] tileRenderers; //Renderer（渲染器）
    private Camera currentCamera;
    private Vector2Int currentHoverdTile;
    private Vector2Int currentSelectedTile;
    private bool isTeam_1_Turn; //true = team 1 , false = team 2
    private int selectingKingTeam = 0; // 0 = Team1 選王, 1 = Team2 選王, -1 = 已選完

    //Use with CardSystem
    public int CurrentTurnTeam => isTeam_1_Turn ? 0 : 1;
    public int SelectingKingTeam => selectingKingTeam;
    private bool isReselectingKing = false;


    private void Awake()
    {
        // 單例設定
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        isTeam_1_Turn = true;

        GenerateGrid(1, Tile_COUNT_X, Tile_COUNT_Y);
        SpawnAllPiece();
        PositionAllPiece();
 
    }
    private void Start()
    {
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");
    }
    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile")))
        {
            //Get the indexes of the tile i've hit 取得所有被點擊的棋格位置(索引值)
            Vector2Int hitPosition = GetHitTileIndex(info.transform.gameObject);

            //If we are hovering a tile after not hovering any tiles
            if (currentHoverdTile == -Vector2Int.one)
            {
                //First time hovering over a tile
                currentHoverdTile = hitPosition;
                tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = hoverMaterial;
            }

            //If we were already hovering a tile,change the previous tile
            if (currentHoverdTile != hitPosition)
            {
                //Change the previous tile material back to default
                if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != currentSelectedTile)
                {
                    if (ContainsValidMove(availableMoves, currentHoverdTile))
                        tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = moveEreaMaterial;
                    else if (ContainsValidAttack(availableAttacks, currentHoverdTile))
                        tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = attackEreaMaterial;
                    else
                        SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);
                }
                //Set the new tile to be the current hovered tile
                currentHoverdTile = hitPosition;
                if (currentHoverdTile != currentSelectedTile)
                    tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = hoverMaterial;
            }

            //If we press down on the mouse
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    ChessPiece clickedPiece = chessPieces[hitPosition.x, hitPosition.y];

                    // === 選王模式 ===
                    if (selectingKingTeam != -1)
                    {
                        SelectKingPiece(clickedPiece);
                        return; // 選王時不繼續其他流程
                    }

                    // === 一般遊戲模式 ===
                    if ((clickedPiece.team == 0 && isTeam_1_Turn) || (clickedPiece.team == 1 && !isTeam_1_Turn))
                    {
                        currentlyDragging = clickedPiece;

                        availableMoves = currentlyDragging.GetAvailableMoves(ref chessPieces, Tile_COUNT_X, Tile_COUNT_Y);
                        availableAttacks = currentlyDragging.GetAvailableAttacks(ref chessPieces, Tile_COUNT_X, Tile_COUNT_Y);
                        HighlightTiles();
                    }
                }
            }

            //If we release the mouse button
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                
                bool vailMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                if (!vailMove) //如果移動失敗
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));

                currentlyDragging = null;
                RemoveHighlightTile();
            }

        }
        else
        {
            //If we were hovering a tile but now we're not
            if (currentHoverdTile != -Vector2Int.one && currentHoverdTile != currentSelectedTile)
            {
                // 如果這個格子是可移動格子
                if (ContainsValidMove(availableMoves, currentHoverdTile))
                {
                    // 恢復成移動材質
                    tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = moveEreaMaterial;
                }
                else if(ContainsValidAttack(availableAttacks, currentHoverdTile))
                {
                    // 恢復成攻擊材質
                    tileRenderers[currentHoverdTile.x, currentHoverdTile.y].material = attackEreaMaterial;
                }
                else
                {
                    // 恢復成黑白格
                    SetTileDefaultMaterial(currentHoverdTile.x, currentHoverdTile.y);
                }
                currentHoverdTile = -Vector2Int.one;
            }
            
            if(currentlyDragging && Input.GetMouseButtonUp(0))
            {
                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                currentlyDragging = null;
                RemoveHighlightTile();
            }

        }

        //If we're dragging a piece, update its position
        if (currentlyDragging)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
            float distance = 0.0f;
            if (horizontalPlane.Raycast(ray, out distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
            }
        }


    }

    // ===== Generate Tile Board =====
    private void GenerateGrid(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];
        tileRenderers = new Renderer[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++)
        {
            for(int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
                tileRenderers[x, y] = tiles[x, y].GetComponent<Renderer>(); // 把 Renderer 存起來
            }
        }
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject =new GameObject(string.Format("X:{0}, Y:{1}", x, y));
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh; tileObject.AddComponent<MeshRenderer>();

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(x * tileSize, 0, y * tileSize);
        vertices[1] = new Vector3(x * tileSize, 0, (y + 1) * tileSize);
        vertices[2] = new Vector3((x + 1) * tileSize, 0, y * tileSize);
        vertices[3] = new Vector3((x + 1) * tileSize, 0, (y + 1) * tileSize);

        int[] tris = new int[] { 0, 1, 2, 2, 1, 3 };
        mesh.vertices = vertices;
        mesh.triangles = tris;

        mesh.RecalculateNormals();

        // 設置黑白格材質
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


    // ===== Spawn Chess Piece =====
    private void SpawnAllPiece()
    {
        chessPieces = new ChessPiece[Tile_COUNT_X, Tile_COUNT_Y];
        int firstTeam = 0;
        int secondTeam = 1;

        List<Vector2Int> usedPositions = new List<Vector2Int>();

        // 要生成的棋子類型（你可依需要增減）
        ChessPieceType[] pieceTypes = {
        ChessPieceType.King,
        ChessPieceType.Queen,
        ChessPieceType.Rook,
        ChessPieceType.Bishop,
        ChessPieceType.Knight,
        ChessPieceType.Pawn
    };

        foreach (var type in pieceTypes)
        {
            Vector2Int pos = GetRandomEmptyPosition(usedPositions, 0, 1); // team1範圍 (y=0~1)
            usedPositions.Add(pos);

            // 生成 team1 棋子
            chessPieces[pos.x, pos.y] = SpawnSinglePiece(type, firstTeam);

            // 對稱座標（沿 Y 軸鏡像）
            Vector2Int mirrorPos = new Vector2Int(pos.x, Tile_COUNT_Y - 1 - pos.y);

            // 生成 team2 棋子
            chessPieces[mirrorPos.x, mirrorPos.y] = SpawnSinglePiece(type, secondTeam);
        }

        PositionAllPiece();
    }

    // 隨機取得空格位置（限制在特定Y範圍）
    private Vector2Int GetRandomEmptyPosition(List<Vector2Int> used, int minY, int maxY)
    {
        Vector2Int pos;
        do
        {
            pos = new Vector2Int(UnityEngine.Random.Range(0, Tile_COUNT_X), UnityEngine.Random.Range(minY, maxY + 1));
        } while (used.Contains(pos));
        return pos;
    }
    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // 從 Pool 取得棋子
        ChessPiece cp = ChessPiecePool.Instance.GetPiece(type, transform);

        cp.type = type;
        cp.team = team;
        cp.isKing = false;

        // 設置材質
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        return cp;
    }


    // ===== Position Piece =====
    private void PositionAllPiece()
    {
        for (int x = 0; x < Tile_COUNT_X; x++)
            for (int y = 0; y < Tile_COUNT_Y; y++)
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y);
    }
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y),force);
    }
    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize + tileSize / 2, yOffset, y * tileSize + tileSize / 2);
    }

    // ===== Highlight Tiles =====
    private void HighlightTiles()
    {
        // 遍歷所有可移動位置
        foreach (Vector2Int pos in availableMoves)
        {
            // 檢查位置是否有效
            if (pos.x >= 0 && pos.x < Tile_COUNT_X && pos.y >= 0 && pos.y < Tile_COUNT_Y)
            {
                tileRenderers[pos.x, pos.y].material = moveEreaMaterial;
            }
        }
        // 遍歷所有可攻擊位置
        foreach (Vector2Int pos in availableAttacks)
        {
            // 檢查位置是否有效
            if (pos.x >= 0 && pos.x < Tile_COUNT_X && pos.y >= 0 && pos.y < Tile_COUNT_Y)
            {
                tileRenderers[pos.x, pos.y].material = attackEreaMaterial;
            }
        }
    }
    private void RemoveHighlightTile()
    {
        // 遍歷所有可移動位置
        foreach (Vector2Int pos in availableMoves)
        {
            // 檢查位置是否有效
            if (pos.x >= 0 && pos.x < Tile_COUNT_X && pos.y >= 0 && pos.y < Tile_COUNT_Y)
            {
                SetTileDefaultMaterial(pos.x, pos.y);
            }
        }
        // 遍歷所有可攻擊位置
        foreach (Vector2Int pos in availableAttacks)
        {
            // 檢查位置是否有效
            if (pos.x >= 0 && pos.x < Tile_COUNT_X && pos.y >= 0 && pos.y < Tile_COUNT_Y)
            {
                SetTileDefaultMaterial(pos.x, pos.y);
            }
        }

        // 清空可移動格子
        availableMoves.Clear();
        // 清空可攻擊格子
        availableAttacks.Clear();
    }

    // ===== Check Mate =====
    public void SelectKingPiece(ChessPiece cp)
    {
        // 檢查是否正在選王
        if (selectingKingTeam == -1) return;

        // 只能選自己隊伍的棋子
        if (cp.team != selectingKingTeam)
        {
            UIManager.Instance.ShowMessage(
                "You cannot select the opponent's piece as King!",
                2f
            );
            return;
        }

        // 設為王
        cp.SetKing(true);
        if (KingMaterials != null && cp.team < KingMaterials.Length)
            cp.GetComponent<MeshRenderer>().material = KingMaterials[cp.team];

        if (isReselectingKing)
        {
            // 使用重選王卡牌流程：選完立即結束
            selectingKingTeam = -1;
            isReselectingKing = false;
            UIManager.Instance.ShowMessage(
                $"Team {cp.team + 1} has reselected their King!",
                2f
            );
            isReselectingKing = false;
            SwitchTurn(); // 選完後直接換回合
            return;
        }

        // 正常開局選王流程
        if (selectingKingTeam == 0)
        {
            selectingKingTeam = 1;
            UIManager.Instance.ShowMessage(
                "Team 2, please select a piece as your King!",
                2f
            );
        }
        else
        {
            selectingKingTeam = -1; // 兩隊都選完
            UIManager.Instance.ShowMessage(
                "Both teams have chosen their Kings. The game starts!",
                2f
            );
            UIManager.Instance.ShowCurrentTurn(0);
        }
    }
    private void CheckMate(int team)
    {
        UIManager.Instance.ShowVictory(team);
    }
    public void ResetBoard()
    {
        UIManager.Instance.HideVictory();

        currentlyDragging = null;
        availableMoves.Clear();
        availableAttacks.Clear();

        // 回收棋盤上的所有棋子
        for (int x = 0; x < Tile_COUNT_X; x++)
        {
            for (int y = 0; y < Tile_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    ChessPiecePool.Instance.ReturnPiece(chessPieces[x, y]);
                    chessPieces[x, y] = null;
                }
            }
        }

        // 回收死亡區棋子
        foreach (var piece in deadTeam_1)
            ChessPiecePool.Instance.ReturnPiece(piece);
        foreach (var piece in deadTeam_2)
            ChessPiecePool.Instance.ReturnPiece(piece);

        deadTeam_1.Clear();
        deadTeam_2.Clear();

        // 重新生成棋子（從 Pool 取得）
        SpawnAllPiece();
        PositionAllPiece();

        // 重置回合
        isTeam_1_Turn = true;

        // === 重置選王狀態 ===
        selectingKingTeam = 0;
        foreach (var piece in chessPieces)
        {
            if (piece != null)
                piece.isKing = false;
        }

        UIManager.Instance.HideRoundText();
        UIManager.Instance.ShowMessage("Team 1, please select a piece as your King!");

        // 重新發牌（如有卡牌系統）
        StartCoroutine(CardSystem.Instance.DealStartingCards());
    }

    // ===== Operations =====
    private Vector2Int GetHitTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < Tile_COUNT_X; x++) 
        {
            for (int y = 0; y < Tile_COUNT_Y; y++)
            {
                if (tiles[x,y]== hitInfo)
                    return new Vector2Int(x, y);
            }
        }
        return -Vector2Int.one; //it is actually invalid
    }
    public bool ContainsValidMove(List<Vector2Int> moves, Vector2Int pos)
    {
        //Contains 會使用 Vector2Int 的 Equals 方法 比較元素是否相同。
        return moves.Contains(pos);
    }
    public bool ContainsValidAttack(List<Vector2Int> attacks, Vector2Int pos)
    {
        //Contains 會使用 Vector2Int 的 Equals 方法 比較元素是否相同。
        return attacks.Contains(pos);
    }
    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        Vector2Int targetPos = new Vector2Int(x, y);
        ChessPiece targetPiece = chessPieces[x, y];

        // === 攻擊判斷要先做！ ===
        if (ContainsValidAttack(availableAttacks, targetPos))
        {
            // 攻擊格子上必須有敵方棋子
            if (targetPiece == null || targetPiece.team == cp.team)
                return false;

            // 吃掉敵方棋子（同你原本邏輯）
            if (targetPiece.team == 0)
            {
                if (targetPiece.isKing)
                    CheckMate(1);

                deadTeam_1.Add(targetPiece);
                targetPiece.SetScale(Vector3.one * deathSize);
                targetPiece.SetPosition(
                    new Vector3(8 * tileSize, yOffset, -1 * tileSize)
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing * deadTeam_1.Count * 0.5f)
                );
            }
            else
            {
                if (targetPiece.isKing)
                    CheckMate(0);

                deadTeam_2.Add(targetPiece);
                targetPiece.SetScale(Vector3.one * deathSize, true);
                targetPiece.SetPosition(
                    new Vector3(-1 * tileSize, yOffset, 8 * tileSize)
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing * deadTeam_2.Count * 0.5f)
                );
            }

            // 攻擊後移動棋子
            Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
            chessPieces[x, y] = cp;
            chessPieces[previousPosition.x, previousPosition.y] = null;
            PositionSinglePiece(x, y, true);

            isTeam_1_Turn = !isTeam_1_Turn;
            UIManager.Instance.ShowCurrentTurn(isTeam_1_Turn ? 0 : 1);
            return true;
        }

        // === 移動判斷（攻擊以外） ===
        if (ContainsValidMove(availableMoves, targetPos))
        {
            if (targetPiece == null)
            {
                Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
                chessPieces[x, y] = cp;
                chessPieces[previousPosition.x, previousPosition.y] = null;
                PositionSinglePiece(x, y, true);

                isTeam_1_Turn = !isTeam_1_Turn;
                UIManager.Instance.ShowCurrentTurn(isTeam_1_Turn ? 0 : 1);
                return true;
            }
            return false;
        }

        return false;
    }
    public void SwitchTurn()
    {
        isTeam_1_Turn = !isTeam_1_Turn;
        UIManager.Instance.ShowCurrentTurn(CurrentTurnTeam);
    }

    // ===== Cards =====
    public void ResetKingSelectionForCurrentTeam()
    {
        // 設定選王隊伍為當前回合隊伍
        selectingKingTeam = CurrentTurnTeam;
        isReselectingKing = true;

        // 清除原本的王
        for (int x = 0; x < Tile_COUNT_X; x++)
        {
            for (int y = 0; y < Tile_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null && chessPieces[x, y].team == selectingKingTeam)
                {
                    chessPieces[x, y].SetKing(false);
                    // 重置材質回原本隊伍材質
                    chessPieces[x, y].GetComponent<MeshRenderer>().material = teamMaterials[selectingKingTeam];
                }
            }
        }

        // 提示玩家重新選王
        UIManager.Instance.ShowMessage($"Team {selectingKingTeam + 1}, please select a new King!", 2f);
    }

}
