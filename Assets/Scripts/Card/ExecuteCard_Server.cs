using Unity.Netcode;
using UnityEngine;

public static class CardServerExecutor
{
    public static bool ExecuteCard_Server(int cardId, int casterTeam, ulong? targetPieceNetId)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsServer) return false;

        switch (cardId)
        {
            case 0: // ResetKingCard
                KingManager.Instance.HandleResetKingSelection_Server(casterTeam);
                GameManager.Instance.ShowMessageClientRpc("The enemy used the card \"Reset King\".", 2f);
                return false;  // 這張卡不會結束行動，因為它需要對方重新選王，等對方選完王才結束行動

            case 1: // NothingCard
                GameManager.Instance.ShowMessageClientRpc("The enemy used the card \"Nothing Happend\".", 2f);
                return true;

            case 2: // MutualDestructionCard (needs target)
                {
                    if (targetPieceNetId == null) return false;
                    if (!TryGetPiece(targetPieceNetId.Value, out var piece)) return false;

                    piece.ApplyMutualDestructionServer(2);
                    GameManager.Instance.ShowMessageClientRpc("The enemy used the card \"Mutual Destruction\".", 2f);
                    return true;
                }

            case 3: // IndestructibleCard (needs target)
                {
                    if (targetPieceNetId == null) return false;
                    if (!TryGetPiece(targetPieceNetId.Value, out var piece)) return false;

                    piece.ApplyIndestructibleServer(2);
                    GameManager.Instance.ShowMessageClientRpc("The enemy used the card \"Indestructible\".", 2f);
                    return true;
                }

            case 4: // DoubleActionCard
                GameManager.Instance.GrantExtraActionServer(1);
                GameManager.Instance.ShowMessageClientRpc("The enemy used the card \"Double Action\".", 2f);
                return false;

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