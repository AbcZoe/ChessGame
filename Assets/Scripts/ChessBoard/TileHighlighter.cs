using System.Collections.Generic;
using UnityEngine;

public class TileHighlighter : MonoBehaviour
{
    private Dictionary<Vector2Int, TileHighlightType> highlightedTilesDict = new();
    private enum TileHighlightType { Move, Attack }

    public void HighlightTilesFromServer(List<Vector2Int> moves, List<Vector2Int> attacks)
    {
        highlightedTilesDict.Clear();

        if (moves != null)
            foreach (var pos in moves) highlightedTilesDict[pos] = TileHighlightType.Move;

        if (attacks != null)
            foreach (var pos in attacks) highlightedTilesDict[pos] = TileHighlightType.Attack;

        foreach (var kvp in highlightedTilesDict)
        {
            var renderer = ChessBoard.Instance.GetTileRenderer(kvp.Key.x, kvp.Key.y);
            if (renderer != null)
                renderer.material = kvp.Value == TileHighlightType.Move
                    ? ChessBoard.Instance.moveEreaMaterial
                    : ChessBoard.Instance.attackEreaMaterial;
        }
    }

    public void RemoveHighlight()
    {
        foreach (var kvp in highlightedTilesDict)
            ChessBoard.Instance.SetTileDefaultMaterial(kvp.Key.x, kvp.Key.y);

        highlightedTilesDict.Clear();
    }

    public bool IsHighlighted(Vector2Int pos)
    {
        return highlightedTilesDict.ContainsKey(pos);
    }
    public void RestoreTileMaterialIfHighlighted(Vector2Int pos)
    {
        if (highlightedTilesDict.TryGetValue(pos, out TileHighlightType type))
        {
            var renderer = ChessBoard.Instance.GetTileRenderer(pos.x, pos.y);
            if (renderer != null)
            {
                renderer.material = type == TileHighlightType.Move
                    ? ChessBoard.Instance.moveEreaMaterial
                    : ChessBoard.Instance.attackEreaMaterial;
            }
        }
        else
        {
            // 沒有高亮就還原預設材質
            ChessBoard.Instance.SetTileDefaultMaterial(pos.x, pos.y);
        }
    }
}