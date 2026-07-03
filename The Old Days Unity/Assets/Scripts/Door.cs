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

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;

    void Start()
    {
        // Guarda a rotação original da porta fechada
        closedRotation = transform.localRotation;
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
            StartRotation(closedRotation);
            isOpen = false;
        }
        else
        {
            // Abre a porta suavemente
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
}
