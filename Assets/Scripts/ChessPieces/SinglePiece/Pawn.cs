using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new();
        int dir = team == 0 ? 1 : -1;

        Vector2Int forward = new(currentX, currentY + dir);
        if (IsInsideBoard(forward, tileCountX, tileCountY) && board[forward.x, forward.y] == null)
        {
            moves.Add(forward);
            // 初始位置可以走兩格
            Vector2Int doubleForward = new(currentX, currentY + 2 * dir);
            if ((team == 0 && currentY == 1 || team == 1 && currentY == 6) &&
                board[doubleForward.x, doubleForward.y] == null)
                moves.Add(doubleForward);
        }

        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new();
        int dir = team == 0 ? 1 : -1;

        Vector2Int[] attackOffsets = { new(1, dir), new(-1, dir) };
        foreach (var offset in attackOffsets)
        {
            Vector2Int pos = new(currentX + offset.x, currentY + offset.y);
            if (!IsInsideBoard(pos, tileCountX, tileCountY))
                continue;

            ChessPiece target = board[pos.x, pos.y];
            if (target != null && target.team != team)
                attacks.Add(pos);
        }

        return attacks;
    }

    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
