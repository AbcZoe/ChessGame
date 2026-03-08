using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public Camera playerCamera;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            playerCamera.enabled = false;
        }
    }
}