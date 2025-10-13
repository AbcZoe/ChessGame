using System.Collections.Generic;
using UnityEngine;

public class Bishop : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int[] directions = {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
        {
            for (int i = 1; i < Mathf.Max(tileCountX, tileCountY); i++)
            {
                Vector2Int pos = new Vector2Int(currentX + dir.x * i, currentY + dir.y * i);
                if (!IsInsideBoard(pos, tileCountX, tileCountY))
                    break;

                ChessPiece target = board[pos.x, pos.y];
                if (target == null)
                    moves.Add(pos);
                else
                {
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
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        foreach (var dir in directions)
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
                    break;
                }
            }
        }

        return attacks;
    }

    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
