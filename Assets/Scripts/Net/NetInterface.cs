using UnityEngine;

public interface NetInterface
{
    bool IsAuthoritative { get; }   // NGO: IsServer；Fusion: HasStateAuthority
    int LocalTeam { get; }          // 本機玩家隊伍（NGO 由 GameManager.AssignTeamClientRpc 設定）

    public void RequestMove(ulong pieceNetId, int targetX, int targetY);
    public void RequestHighlight(ulong pieceNetId);
    public void RequestSelectKing(ulong pieceNetId);
    public void RequestEndAction();
    void RequestRematch();

    void RequestUseCard(int cardId); // 無目標卡
    void RequestUseCardOnPiece(int cardId, ulong pieceNetId); // 需要選自己棋子的卡
}
