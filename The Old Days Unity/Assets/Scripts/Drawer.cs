using System.Collections;
using UnityEngine;

/// <summary>
/// Script para controlar a abertura e fecho de gavetas (ou outros objetos deslizantes).
/// Implementa a interface IInteractable para responder às ações do jogador.
/// </summary>
public class Drawer : MonoBehaviour, IInteractable
{
    public enum SlideDirection
    {
        Forward,
        Backward,
        Left,
        Right,
        Up,
        Down
    }

    [Header("Configurações do Comportamento")]
    [Tooltip("Direção para onde a gaveta desliza ao abrir (local).")]
    public SlideDirection slideDirection = SlideDirection.Forward;

    [Tooltip("A distância que a gaveta se move ao abrir.")]
    public float slideDistance = 0.5f;

    [Tooltip("Velocidade com que a gaveta se move.")]
    public float slideSpeed = 3f;

    [Header("Estado Atual")]
    [SerializeField] private bool isOpen = false;
    private bool isMoving = false;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private Coroutine slideCoroutine;

    void Start()
    {
        // Guarda a posição original da gaveta fechada (local para evitar problemas com rotações do móvel pai)
        closedPosition = transform.localPosition;
    }

    /// <summary>
    /// Método chamado quando o jogador interage com a gaveta.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador que iniciou a interação.</param>
    public void Interact(Transform playerTransform)
    {
        if (isMoving) return;

        if (isOpen)
        {
            // Fecha a gaveta voltando para a posição original
            StartMovement(closedPosition);
            isOpen = false;
        }
        else
        {
            // Calcula a posição de destino com base na direção local e distância
            Vector3 direction = GetDirectionVector();
            openPosition = closedPosition + direction * slideDistance;
            StartMovement(openPosition);
            isOpen = true;
        }
    }

    private void StartMovement(Vector3 targetPosition)
    {
        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideDrawer(targetPosition));
    }

    private IEnumerator SlideDrawer(Vector3 targetPosition)
    {
        isMoving = true;

        // Move suavemente até aproximar o suficiente do destino
        while (Vector3.Distance(transform.localPosition, targetPosition) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, Time.deltaTime * slideSpeed);
            yield return null;
        }

        // Garante que fica exatamente na posição alvo
        transform.localPosition = targetPosition;
        isMoving = false;
    }

    private Vector3 GetDirectionVector()
    {
        switch (slideDirection)
        {
            case SlideDirection.Forward:
                return Vector3.forward;
            case SlideDirection.Backward:
                return Vector3.back;
            case SlideDirection.Left:
                return Vector3.left;
            case SlideDirection.Right:
                return Vector3.right;
            case SlideDirection.Up:
                return Vector3.up;
            case SlideDirection.Down:
                return Vector3.down;
            default:
                return Vector3.forward;
        }
    }
}
