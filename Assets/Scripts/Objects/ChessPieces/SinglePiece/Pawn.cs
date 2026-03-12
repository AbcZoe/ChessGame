using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();
        int dir = Team.Value == 0 ? 1 : -1;

        Vector2Int forward = new(X.Value, Y.Value + dir);
        if (IsInsideBoard(forward, tileCountX, tileCountY) && board[forward.x, forward.y] == null)
        {
            moves.Add(forward);
            // 初始位置可以走兩格
            Vector2Int doubleForward = new(X.Value, Y.Value + 2 * dir);
            if ((Team.Value == 0 && Y.Value == 1 || Team.Value == 1 && Y.Value == 6) &&
                board[doubleForward.x, doubleForward.y] == null)
                moves.Add(doubleForward);
        }

        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new();
        int dir = Team.Value == 0 ? 1 : -1;

        Vector2Int[] attackOffsets = { new(1, dir), new(-1, dir) };
        foreach (var offset in attackOffsets)
        {
            Vector2Int pos = new(X.Value + offset.x, Y.Value + offset.y);
            if (!IsInsideBoard(pos, tileCountX, tileCountY))
                continue;

            ChessPiece target = board[pos.x, pos.y];
            if (target != null && target.Team.Value != Team.Value)
                attacks.Add(pos);
        }

        return attacks;
    }

    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
