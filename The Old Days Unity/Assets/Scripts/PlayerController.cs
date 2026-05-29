using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    [Header("Interação")]
    [Tooltip("Distância máxima para poder interagir com as portas.")]
    public float interactionDistance = 3f;

    float xRotation = 0f;
    CharacterController controller;
    private IInteractable currentInteractable;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        Move();
        Look();
        CheckInteraction();
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

    void CheckInteraction()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        RaycastHit hit;
        IInteractable interactable = null;

        // Faz o Raycast para a frente da câmara
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Tenta encontrar um script que implemente IInteractable no objeto atingido (ou nos seus pais)
            interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null)
            {
                interactable = hit.collider.GetComponent<IInteractable>();
            }
        }

        // Se mudou o objeto com que podemos interagir
        if (interactable != currentInteractable)
        {
            currentInteractable = interactable;
            UpdateCrosshair();
        }

        bool interactPressed = false;

        // Teclado (Tecla E)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        // Comando / Gamepad (Botão Oeste - ex: X na Xbox, Quadrado na PlayStation)
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        if (interactPressed && currentInteractable != null)
        {
            // Ativa a interação do objeto
            currentInteractable.Interact(transform);
        }
    }

    /// <summary>
    /// Atualiza o estado visual da crosshair com base no objeto focado atualmente.
    /// </summary>
    void UpdateCrosshair()
    {
        if (CrosshairManager.Instance != null)
        {
            if (currentInteractable != null)
            {
                CrosshairManager.Instance.ShowInteractable();
            }
            else
            {
                CrosshairManager.Instance.ShowDefault();
            }
        }
    }
}