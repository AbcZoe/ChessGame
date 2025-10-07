using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Beast = 1,
    People = 2,
    Sword = 3,
    Wall = 4,
    Cup = 5,
    Flag = 6
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    public bool isKing = false;

    private Vector3 desiredPosition;
    private Vector3 desiredScale = Vector3.one;
    public float baseScale = 1f; // Inspector 可調比例大小

    private void Start()
    {
        desiredScale = transform.localScale * baseScale;
    }

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public void ResetPiece()
    {
        isKing = false;
        team = 0;
        type = ChessPieceType.None;
        desiredScale = Vector3.one * baseScale;
        // 如果有材質、顏色等可以在這裡重置
    }

    public void SetKing(bool value)
    {
        isKing = value;
    }

    // ==== 取得可移動、攻擊位置 (子類別覆寫) ====
    public virtual List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }
    public virtual List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return new List<Vector2Int>();
    }


    //virtual 表示這個方法「可以被子類別（繼承這個類別的類別）覆寫（override）」。
    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
        {
            transform.position = desiredPosition;
        }
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
        {
            transform.localScale = desiredScale;
        }
    }

}
