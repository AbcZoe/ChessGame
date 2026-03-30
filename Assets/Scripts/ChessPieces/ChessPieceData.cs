using UnityEngine;

[CreateAssetMenu(fileName = "NewChessPiece", menuName = "Pieces/ChessPieceData")]
public class ChessPieceData : ScriptableObject
{
    public string pieceName;       
    public ChessPiece prefab;    
    
}