using UnityEngine;

public class FreeCameraController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 25f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    private void Start()
    {
        rotationX = transform.eulerAngles.y;
        rotationY = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        MoveCamera();
        LookAround();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void MoveCamera()
    {
        float speed =
            Input.GetKey(KeyCode.LeftShift)
                ? fastMoveSpeed
                : moveSpeed;

        float horizontal =
            Input.GetAxis("Horizontal");

        float vertical =
            Input.GetAxis("Vertical");

        Vector3 movement =
            transform.forward * vertical +
            transform.right * horizontal;

        transform.position +=
            movement * speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.E))
        {
            transform.position +=
                Vector3.up * speed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -=
                Vector3.up * speed * Time.deltaTime;
        }
    }

    private void LookAround()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity;

        rotationX += mouseX;
        rotationY -= mouseY;

        rotationY =
            Mathf.Clamp(rotationY, -89f, 89f);

        transform.rotation =
            Quaternion.Euler(
                rotationY,
                rotationX,
                0f
            );
    }
}