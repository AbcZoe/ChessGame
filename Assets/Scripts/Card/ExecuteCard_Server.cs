using Unity.Netcode;
using UnityEngine;

public static class CardServerExecutor
{
    public static bool ExecuteCard_Server(int cardId, int casterTeam, ulong? targetPieceNetId, Vector2Int? targetTile, ulong senderClientId)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsServer) return false;

        switch (cardId)
        {
            case 0: // ResetKingCard
                {
                    KingManager.Instance.HandleResetKingSelection_Server(casterTeam);
                    GameManager.Instance.ShowMessageToClient("Reset King_ToClient", 2f, senderClientId);
                    GameManager.Instance.ShowMessageToOthers("Reset King_ToOthers", 2f, senderClientId);
                    return false;  // 這張卡不會結束行動，因為它需要對方重新選王，等對方選完王才結束行動
                }

            case 1: // NothingCard
                {
                    GameManager.Instance.ShowMessageToClient("Nothing Happend_ToClient", 2f, senderClientId);
                    GameManager.Instance.ShowMessageToOthers("Nothing Happend_ToOthers", 2f, senderClientId);
                    return true;
                }

            case 2: // MutualDestructionCard (needs target)
                {
                    if (targetPieceNetId == null) return false;
                    if (!TryGetPiece(targetPieceNetId.Value, out var piece)) return false;

                    piece.ApplyMutualDestructionServer(2);
                    GameManager.Instance.ShowMessageToClient("Mutual Destruction_ToClient", 2f, senderClientId);
                    GameManager.Instance.ShowMessageToOthers("Mutual Destruction_ToOthers", 2f, senderClientId);
                    return true;
                }

            case 3: // IndestructibleCard (needs target)
                {
                    if (targetPieceNetId == null) return false;
                    if (!TryGetPiece(targetPieceNetId.Value, out var piece)) return false;

                    piece.ApplyIndestructibleServer(2);
                    GameManager.Instance.ShowMessageToClient("Indestructible_toClient", 2f, senderClientId);
                    GameManager.Instance.ShowMessageToOthers("Indestructible_ToOthers", 2f, senderClientId);
                    return true;
                }

            case 4: // DoubleActionCard
                { 
                GameManager.Instance.GrantExtraActionServer(1);
                GameManager.Instance.ShowMessageToClient("Double Action_ToClient", 2f, senderClientId);
                GameManager.Instance.ShowMessageToOthers("Double Action_ToOthers", 2f, senderClientId);
                return false;
                }

            default:
                Debug.LogWarning($"[CardServerExecutor] Unknown cardId={cardId}");
                return false;
        }
    }

    private static bool TryGetPiece(ulong pieceNetId, out ChessPiece piece)
    {
        piece = null;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pieceNetId, out var netObj))
            return false;

        piece = netObj.GetComponent<ChessPiece>();
        return piece != null;
    }

}