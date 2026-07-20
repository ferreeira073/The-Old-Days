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

    [Header("Som de Passos")]
    [Tooltip("Clips de áudio dos passos. Adiciona vários para variar o som aleatoriamente.")]
    public AudioClip[] footstepClips;

    [Tooltip("Intervalo (em segundos) entre cada passo.")]
    [Range(0.1f, 1f)]
    public float footstepInterval = 0.45f;

    [Tooltip("Volume dos passos (0 a 1).")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.6f;

    float xRotation = 0f;
    CharacterController controller;
    private IInteractable currentInteractable;
    private float initialY;

    // --- Passos ---
    private AudioSource _footstepSource;
    private float _footstepTimer = 0f;

    /// <summary>
    /// Quando false, todos os inputs do jogador (movimento, câmara, interação) ficam bloqueados.
    /// </summary>
    public bool IsControlEnabled = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        initialY = transform.position.y;

        // Desativa a possibilidade de subir degraus ou rampas no CharacterController
        if (controller != null)
        {
            controller.stepOffset = 0f;
            controller.slopeLimit = 0f;
        }

        // Cria ou obtém o AudioSource dedicado aos passos
        _footstepSource = GetComponent<AudioSource>();
        if (_footstepSource == null)
            _footstepSource = gameObject.AddComponent<AudioSource>();

        _footstepSource.playOnAwake = false;
        _footstepSource.spatialBlend = 0f; // Som 2D (sem áudio espacial)
        _footstepSource.volume      = footstepVolume;
    }

    void Update()
    {
        if (!IsControlEnabled) return;
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

        bool isMoving = (x != 0f || z != 0f);

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * speed * Time.deltaTime);

        // Bloqueia a altura (Y) na posição inicial para evitar que o jogador suba degraus ou flutue por colisão
        Vector3 lockedPosition = transform.position;
        lockedPosition.y = initialY;
        transform.position = lockedPosition;

        // --- Passos ---
        HandleFootsteps(isMoving);
    }

    /// <summary>
    /// Toca sons de passos enquanto o jogador se move, com um intervalo fixo entre cada passo.
    /// </summary>
    void HandleFootsteps(bool isMoving)
    {
        if (!isMoving || footstepClips == null || footstepClips.Length == 0)
        {
            // Jogador parado: reinicia o timer para que o próximo passo não salte logo
            _footstepTimer = footstepInterval;
            return;
        }

        _footstepTimer -= Time.deltaTime;

        if (_footstepTimer <= 0f)
        {
            // Escolhe um clip aleatório da lista (evita repetir o mesmo)
            AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
            _footstepSource.PlayOneShot(clip, footstepVolume);
            _footstepTimer = footstepInterval;
        }
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

    /// <summary>
    /// Repõe a rotação vertical da câmara e roda o corpo do jogador horizontalmente.
    /// Útil para o acordar a olhar na direção oposta.
    /// </summary>
    public void ResetCameraRotation(float horizontalOffsetDegrees)
    {
        // Roda o corpo do jogador horizontalmente (eixo Y)
        transform.Rotate(0f, horizontalOffsetDegrees, 0f);

        // Repõe a rotação vertical (eixo X)
        xRotation = 0f;
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }
}