using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 25f;

    [Header("Look")]
    public float lookSpeed = 60f;

    private float pitch = 0f;
    private float yaw = 0f;

    private void Start()
    {
        Vector3 angles = transform.eulerAngles;

        yaw = angles.y;
        pitch = angles.x;

        // Convert Unity's 0-360 pitch into -180 to 180
        if (pitch > 180f)
            pitch -= 360f;
    }

    private void Update()
    {
        MoveCamera();
        LookAround();
    }

    private void MoveCamera()
    {
        if (Keyboard.current == null)
            return;

        Vector3 movement = Vector3.zero;

        // WASD
        if (Keyboard.current.wKey.isPressed)
            movement += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            movement -= transform.forward;

        if (Keyboard.current.dKey.isPressed)
            movement += transform.right;

        if (Keyboard.current.aKey.isPressed)
            movement -= transform.right;

        float speed = Keyboard.current.leftShiftKey.isPressed
            ? fastMoveSpeed
            : moveSpeed;

        if (movement != Vector3.zero)
        {
            transform.position +=
                movement.normalized * speed * Time.deltaTime;
        }

        // Q / E = vertical movement
        if (Keyboard.current.eKey.isPressed)
        {
            transform.position +=
                Vector3.up * speed * Time.deltaTime;
        }

        if (Keyboard.current.qKey.isPressed)
        {
            transform.position -=
                Vector3.up * speed * Time.deltaTime;
        }
    }

    private void LookAround()
    {
        if (Keyboard.current == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        // Left / Right arrows
        if (Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;

        if (Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;

        // Up / Down arrows
        if (Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;

        yaw += horizontal * lookSpeed * Time.deltaTime;
        pitch -= vertical * lookSpeed * Time.deltaTime;

        // Prevent camera from flipping upside down
        pitch = Mathf.Clamp(pitch, -89f, 89f);

        transform.rotation =
            Quaternion.Euler(pitch, yaw, 0f);
    }
}