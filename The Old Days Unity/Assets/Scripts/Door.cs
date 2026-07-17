using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public enum DoorFacing
    {
        ForwardZ,
        BackwardZ,
        RightX,
        LeftX
    }

    public enum OpeningMode
    {
        SwingForward,
        SwingBackward,
        FlipUp,
        FlipDown
    }

    [Header("Configurações do Comportamento")]
    [Tooltip("Indica para que lado a face (frente) da porta está virada no seu eixo local.")]
    public DoorFacing doorFacing = DoorFacing.ForwardZ;

    [Tooltip("Escolha como a porta se abre.")]
    public OpeningMode openingMode = OpeningMode.SwingForward;

    [Tooltip("O ângulo em graus que a porta vai rodar ao abrir.")]
    public float openAngle = 90f;

    [Tooltip("Velocidade com que a porta roda.")]
    public float openSpeed = 3f;

    [Header("Estado Atual")]
    [SerializeField] private bool isOpen = false;
    private bool isRotating = false;

    [Header("Sons")]
    [Tooltip("Som tocado ao abrir a porta.")]
    public AudioClip openSound;

    [Tooltip("Som tocado ao fechar a porta.")]
    public AudioClip closeSound;

    [Tooltip("Volume dos sons da porta (0 a 1).")]
    [Range(0f, 1f)]
    public float doorVolume = 0.8f;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    private AudioSource _audioSource;

    void Start()
    {
        // Guarda a rotação original da porta fechada
        closedRotation = transform.localRotation;

        // Cria ou obtém o AudioSource da porta
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake  = false;
        _audioSource.spatialBlend = 1f; // Som 3D (vem da posição da porta no mundo)
        _audioSource.volume       = doorVolume;
    }

    /// <summary>
    /// Método chamado quando o jogador interage com a porta.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador para calcular a direção relativa.</param>
    public void Interact(Transform playerTransform)
    {
        if (isRotating) return;

        if (isOpen)
        {
            // Fecha a porta suavemente para a posição original
            PlayDoorSound(closeSound);
            StartRotation(closedRotation);
            isOpen = false;
        }
        else
        {
            // Abre a porta suavemente
            PlayDoorSound(openSound);
            Vector3 rotationAxis = Vector3.up; // Eixo padrão para swing
            float angle = openAngle;

            if (openingMode == OpeningMode.SwingForward)
            {
                rotationAxis = Vector3.up;
                angle = openAngle;
            }
            else if (openingMode == OpeningMode.SwingBackward)
            {
                rotationAxis = Vector3.up;
                angle = -openAngle;
            }
            else // FlipUp ou FlipDown
            {
                // Determina o eixo horizontal local com base na direção da face da porta
                switch (doorFacing)
                {
                    case DoorFacing.ForwardZ:
                        rotationAxis = Vector3.right; // Eixo X
                        angle = (openingMode == OpeningMode.FlipUp) ? -openAngle : openAngle;
                        break;

                    case DoorFacing.BackwardZ:
                        rotationAxis = Vector3.right; // Eixo X
                        angle = (openingMode == OpeningMode.FlipUp) ? openAngle : -openAngle;
                        break;

                    case DoorFacing.RightX:
                        rotationAxis = Vector3.forward; // Eixo Z
                        angle = (openingMode == OpeningMode.FlipUp) ? openAngle : -openAngle;
                        break;

                    case DoorFacing.LeftX:
                        rotationAxis = Vector3.forward; // Eixo Z
                        angle = (openingMode == OpeningMode.FlipUp) ? -openAngle : openAngle;
                        break;
                }
            }

            // Calcula a rotação alvo com base no eixo e ângulo calculados
            openRotation = closedRotation * Quaternion.AngleAxis(angle, rotationAxis);
            StartRotation(openRotation);
            isOpen = true;
        }
    }

    private void StartRotation(Quaternion targetRotation)
    {
        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
        }
        rotateCoroutine = StartCoroutine(RotateDoor(targetRotation));
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        isRotating = true;

        // Roda suavemente até atingir a rotação desejada
        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * openSpeed);
            yield return null;
        }

        // Garante que fica exatamente na rotação alvo
        transform.localRotation = targetRotation;
        isRotating = false;
    }

    /// <summary>
    /// Toca um clip de áudio da porta, se estiver definido.
    /// </summary>
    private void PlayDoorSound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip, doorVolume);
        }
    }
}
