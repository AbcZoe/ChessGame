using UnityEngine;

public class CamereSetup : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    public void SetupCamera(int team)
    {
        if (team == 1)
        {
            FlipCamera();
        }
    }

    private void FlipCamera()
    {
        mainCamera.transform.position = new Vector3(
            mainCamera.transform.position.x,
            mainCamera.transform.position.y,
            mainCamera.transform.position.z);

        mainCamera.transform.Rotate(Vector3.up, 180f, Space.World);
    }

}
