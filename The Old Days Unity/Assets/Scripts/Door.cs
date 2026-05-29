using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public enum OpeningMode
    {
        FixedDirection,
        AwayFromPlayer
    }

    [Header("Configurações do Comportamento")]
    [Tooltip("Escolha se a porta abre sempre para o mesmo lado fixo ou se se afasta do jogador.")]
    public OpeningMode openingMode = OpeningMode.AwayFromPlayer;

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
            float angle = openAngle;

            if (openingMode == OpeningMode.AwayFromPlayer)
            {
                // Calcula a direção da porta em relação ao jogador
                Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, toPlayer);

                // Se o jogador estiver à frente da porta (dot > 0), a porta abre no sentido inverso (afasta-se do jogador)
                // Se estiver atrás (dot <= 0), abre no sentido normal
                if (dot > 0f)
                {
                    angle = -openAngle;
                }
            }

            // Calcula a rotação alvo com base no ângulo final
            openRotation = closedRotation * Quaternion.Euler(0f, angle, 0f);
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
