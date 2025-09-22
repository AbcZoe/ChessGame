using System.Collections.Generic;
using UnityEngine;

public class Cup : ChessPiece
{
    public override List<Vector2Int> GetAvailableMoves(ref ChessPiece[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int[,] crossMoves = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        for (int i = 0; i < crossMoves.GetLength(0); i++)
        {
            Vector2Int pos = new Vector2Int(currentX + crossMoves[i, 0], currentY + crossMoves[i, 1]);
            if (IsInsideBoard(pos, tileCountX, tileCountY))
                moves.Add(pos);
        }

        return moves;
    }

    // 工具方法：檢查是否在棋盤內
    private bool IsInsideBoard(Vector2Int pos, int tileCountX, int tileCountY)
    {
        return pos.x >= 0 && pos.x < tileCountX && pos.y >= 0 && pos.y < tileCountY;
    }
}
