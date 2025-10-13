using System.Collections.Generic;
using UnityEngine;

public class Rook : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // 四個方向：上、下、左、右
        Vector2Int[] directions = {
            Vector2Int.up,     // (0, 1)
            Vector2Int.down,   // (0, -1)
            Vector2Int.left,   // (-1, 0)
            Vector2Int.right   // (1, 0)
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i < Mathf.Max(tileCountX, tileCountY); i++)
            {
                Vector2Int pos = new Vector2Int(currentX + dir.x * i, currentY + dir.y * i);
                if (!IsInsideBoard(pos, tileCountX, tileCountY))
                    break;

                ChessPiece target = board[pos.x, pos.y];
                if (target == null)
                {
                    // 空格可移動
                    moves.Add(pos);
                }
                else
                {
                    // 有棋子 -> 可攻擊不同陣營，但不能再繼續往該方向走
                    if (target.team != team)
                        moves.Add(pos);
                    break;
                }
            }
        }

        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new List<Vector2Int>();

        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i < Mathf.Max(tileCountX, tileCountY); i++)
            {
                Vector2Int pos = new Vector2Int(currentX + dir.x * i, currentY + dir.y * i);
                if (!IsInsideBoard(pos, tileCountX, tileCountY))
                    break;

                ChessPiece target = board[pos.x, pos.y];
                if (target != null)
                {
                    // 若遇到敵方棋子就可以攻擊
                    if (target.team != team)
                        attacks.Add(pos);
                    break; // 遇到棋子後不能再繼續
                }
            }
        }

        return attacks;
    }

    // 工具方法：檢查是否在棋盤內
    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
