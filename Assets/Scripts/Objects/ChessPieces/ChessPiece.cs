using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Bishop = 1,
    Knight = 2,
    King = 3,
    Queen = 4,
    Pawn = 5,
    Rook = 6
}

public class ChessPiece : NetworkBehaviour
{
    public NetworkVariable<int> Team = new();
    public NetworkVariable<int> X = new();
    public NetworkVariable<int> Y = new();
    public ChessPieceType type;
    public NetworkVariable<bool> isKing = new(false);

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 100f; // Inspector 可調比例大小

    private MeshRenderer rend;

    private void Awake()
    {
        rend = GetComponent<MeshRenderer>();
        desiredScale = Vector3.one * baseScale;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale * baseScale;
        if (force)
            transform.localScale = desiredScale;
    }

    public void SetKing(bool value)
    {
        isKing.Value = value;
    }

    public void ResetPiece()
    {
        Team.Value = 0;
        X.Value = 0;
        Y.Value = 0;
        isKing.Value = false;
        desiredScale = Vector3.one * baseScale;
        transform.localScale = desiredScale;

        gameObject.SetActive(false);
    }

    #region Server Methods
    public NetworkObject NetObj => GetComponent<NetworkObject>();
    public ulong NetId => NetObj != null ? NetObj.NetworkObjectId : 0;

    public override void OnNetworkSpawn()
    {
        // 註冊 NetworkVariable 變化
        Team.OnValueChanged += (oldValue, newValue) => UpdateVisual();
        X.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        Y.OnValueChanged += (oldValue, newValue) => UpdatePosition();
        isKing.OnValueChanged += (oldValue, newValue) => UpdateKingVisual();        
        
        UpdatePosition();   // 立即對齊位置
        UpdateVisual();     // 立即更新隊伍顏色
        UpdateKingVisual(); // 如果是 King，更新 King 顏色

        // 登記到棋盤
        if (ChessBoard.Instance != null && ChessBoard.Instance.chessPieces != null)
        {
            ChessBoard.Instance.chessPieces[X.Value, Y.Value] = this;
        }
        // 強制刷新一次（避免初始值不觸發）
        StartCoroutine(ForceRefresh());

    }

    IEnumerator ForceRefresh()
    {
        yield return null;

        UpdateVisual();
        UpdateKingVisual();
    }

    public void UpdateVisual()
    {
        if (ChessBoard.Instance == null) return;

        if (rend != null)
            rend.material = ChessBoard.Instance.teamMaterials[Team.Value];
    }

    void UpdatePosition()
    {
        if (ChessBoard.Instance == null) return;

        Vector3 pos = ChessBoard.Instance.GetTileCenter(X.Value, Y.Value);
        SetPosition(pos, true);
    }

    private void UpdateKingVisual()
    {
        if (rend != null && isKing.Value)
            rend.material = ChessBoard.Instance.KingMaterials[Team.Value];
        else
            UpdateVisual();
    }

    #endregion
}
