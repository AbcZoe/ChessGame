using System.Collections.Generic;
using UnityEngine;

public class Beast : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int[,] knightMoves = {
        {2,1}, {2,-1}, {-2,1}, {-2,-1},
        {1,2}, {-1,2}, {1,-2}, {-1,-2}
    };

        for (int i = 0; i < knightMoves.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + knightMoves[i, 0], currentY + knightMoves[i, 1]);
            if (IsInsideBoard(pos, tileCountX, tileCountY))
                moves.Add(pos);
        }

        return moves;
    }
    public override List<Vector2Int> GetAvailableAttacks(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> attacks = new List<Vector2Int>();

        int[,] diagonalAttacks = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };

        for (int i = 0; i < diagonalAttacks.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + diagonalAttacks[i, 0], currentY + diagonalAttacks[i, 1]);
            if (IsInsideBoard(pos, tileCountX, tileCountY))
                attacks.Add(pos);
        }

        return attacks;
    }

    // 工具方法：檢查是否在棋盤內
    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
