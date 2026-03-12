using System.Collections.Generic;
using UnityEngine;

public class Queen : ChessPiece
{
    private static readonly Vector2Int[] directions =
    {
        new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
        new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
    };

    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return GetMovesOrAttacks(ref board, tileCountX, tileCountY, onlyMoves: true);
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        return GetMovesOrAttacks(ref board, tileCountX, tileCountY, onlyMoves: false);
    }

    private List<Vector2Int> GetMovesOrAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY, bool onlyMoves)
    {
        List<Vector2Int> result = new();
        foreach (var dir in directions)
        {
            for (int i = 1; i < Mathf.Max(tileCountX, tileCountY); i++)
            {
                Vector2Int pos = new(X.Value + dir.x * i, Y.Value + dir.y * i);
                if (!IsInsideBoard(pos, tileCountX, tileCountY))
                    break;

                ChessPiece target = board[pos.x, pos.y];

                if (target == null && onlyMoves)
                    result.Add(pos);
                else if (target != null && target.Team.Value != Team.Value && !onlyMoves)
                {
                    result.Add(pos);
                    break;
                }
                else if (target != null)
                    break;
            }
        }
        return result;
    }

    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
