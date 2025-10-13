using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        // 后的移動方向：上下左右 + 四個斜角
        Vector2Int[] directions = {
            Vector2Int.up,             // (0, 1)
            Vector2Int.down,           // (0, -1)
            Vector2Int.left,           // (-1, 0)
            Vector2Int.right,          // (1, 0)
            new Vector2Int(1, 1),      // 右上
            new Vector2Int(1, -1),     // 右下
            new Vector2Int(-1, 1),     // 左上
            new Vector2Int(-1, -1)     // 左下
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
                    // 空格可走
                    moves.Add(pos);
                }
                else
                {
                    // 敵方可攻擊、己方擋路
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
            Vector2Int.right,
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
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
                    if (target.team != team)
                        attacks.Add(pos);
                    break; // 遇到棋子後停止搜尋
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
