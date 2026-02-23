using UnityEngine;

public class PlayerMove3D : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float gravity = 9.81f;
    public Transform cameraTransform; // 指向主攝影機
    private CharacterController controller;
    private Vector3 moveDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform; // 如果沒拖就用主相機
        }
    }

    void Update()
    {
        if (controller.isGrounded)
        {
            float h = Input.GetAxis("Horizontal"); // A,D / ←,→
            float v = Input.GetAxis("Vertical");   // W,S / ↑,↓

            // 取得相機的前、右方向 (忽略 Y 軸，避免角色上下亂跑)
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            // 計算移動方向 (根據相機朝向)
            moveDirection = (forward * v + right * h).normalized * moveSpeed;
        }

        // 重力
        moveDirection.y -= gravity * Time.deltaTime;

        // 移動角色
        controller.Move(moveDirection * Time.deltaTime);

        // 角色朝向移動方向（可選）
        Vector3 lookDir = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 0.15f);
        }
    }
}
