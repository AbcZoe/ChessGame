using System.Collections.Generic;
using UnityEngine;

public class Pawn : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1; // 白向上, 黑向下
        Vector2Int forward = new Vector2Int(currentX, currentY + direction);

        // 向前一步
        if (IsInsideBoard(forward, tileCountX, tileCountY) && board[forward.x, forward.y] == null)
        {
            moves.Add(forward);

            // 首次移動可走兩步
            if ((team == 0 && currentY == 1) || (team == 1 && currentY == tileCountY - 2))
            {
                Vector2Int doubleForward = new Vector2Int(currentX, currentY + 2 * direction);
                if (IsInsideBoard(doubleForward, tileCountX, tileCountY) && board[doubleForward.x, doubleForward.y] == null)
                    moves.Add(doubleForward);
            }
        }

        return moves;
    }

    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new List<Vector2Int>();
        int direction = (team == 0) ? 1 : -1;

        Vector2Int[] attackDirs = {
            new Vector2Int(currentX + 1, currentY + direction),
            new Vector2Int(currentX - 1, currentY + direction)
        };

        foreach (var pos in attackDirs)
        {
            if (!IsInsideBoard(pos, tileCountX, tileCountY)) continue;
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
