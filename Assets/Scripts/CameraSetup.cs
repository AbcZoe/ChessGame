using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; 
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0f, 0.2f);
    private bool hasFlipped = false;

    public void SetupCamera(int playerTeam)
    {
        // Team 1 玩家翻轉相機
        if (!hasFlipped && playerTeam == 1)
        {
            FlipCamera();
            hasFlipped = true;
        }
    }

    private void FlipCamera()
    {
        // 假設棋盤中心在 (4, 0, 4)（8x8棋盤）
        Vector3 boardCenter = new Vector3(4f, 0f, 3.9f);
        mainCamera.transform.RotateAround(boardCenter, Vector3.up, 180f);

        mainCamera.transform.position += worldOffset;

    }

}
