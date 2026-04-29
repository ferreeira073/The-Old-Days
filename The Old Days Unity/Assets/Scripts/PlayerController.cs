using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    float xRotation = 0f;
    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Move();
        Look();
    }

    void Move()
    {
        float x = 0f;
        float z = 0f;

        if (Keyboard.current != null)
        {
            x = (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            z = (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f);
        }

        if (Gamepad.current != null)
        {
            x += Gamepad.current.leftStick.x.ReadValue();
            z += Gamepad.current.leftStick.y.ReadValue();
        }

        x = Mathf.Clamp(x, -1f, 1f);
        z = Mathf.Clamp(z, -1f, 1f);

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);
    }

    void Look()
    {
        float mouseX = 0f;
        float mouseY = 0f;

        // Read mouse input (scaled down because new Input System mouse delta is raw pixels)
        if (Mouse.current != null)
        {
            mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity * 0.05f * Time.deltaTime;
            mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity * 0.05f * Time.deltaTime;
        }

        // Read gamepad input
        if (Gamepad.current != null)
        {
            mouseX += Gamepad.current.rightStick.x.ReadValue() * mouseSensitivity * Time.deltaTime;
            mouseY += Gamepad.current.rightStick.y.ReadValue() * mouseSensitivity * Time.deltaTime;
        }

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}