using UnityEngine;
using TMPro;

/// <summary>
/// Controla a exibição visual de um relógio/despertador digital de cabeceira.
/// Exibe as horas e minutos em tempo real com a cor vermelha e efeitos de piscar intermitentes.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class DigitalClock : MonoBehaviour
{
    private TMP_Text textComponent;

    [Header("Configurações Visuais")]
    [Tooltip("A cor dos dígitos do display digital (predefinido para vermelho).")]
    [SerializeField] private Color displayColor = Color.red;

    [Header("Configurações do Efeito Piscar")]
    [Tooltip("Se ativo, apenas os dois pontos (:) piscam. Se inativo, os dois pontos ficam fixos.")]
    [SerializeField] private bool blinkColon = true;

    [Tooltip("Se ativo, o display inteiro das horas e minutos pisca ligando e desligando.")]
    [SerializeField] private bool blinkWholeDisplay = false;

    [Tooltip("Velocidade do piscar em segundos do mundo real.")]
    [SerializeField] private float blinkInterval = 0.5f;

    private float blinkTimer = 0f;
    private bool blinkState = true;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
        
        // Configura a cor inicial do texto para vermelho conforme pedido
        if (textComponent != null)
        {
            textComponent.color = displayColor;
        }
    }

    private void Update()
    {
        if (TimeManager.Instance == null || textComponent == null) return;

        int hour = TimeManager.Instance.CurrentHour;
        int minute = TimeManager.Instance.CurrentMinute;

        // Atualiza o temporizador do piscar com base no tempo real decorrido
        blinkTimer += Time.deltaTime;
        if (blinkTimer >= blinkInterval)
        {
            blinkTimer = 0f;
            blinkState = !blinkState;
        }

        // Define se exibe os dois pontos ou um espaço em branco
        string separator = (blinkColon && !blinkState) ? " " : ":";

        // Formata e exibe o tempo (ex: "08:00")
        textComponent.text = $"{hour:D2}{separator}{minute:D2}";

        // Controla se o display inteiro pisca
        if (blinkWholeDisplay)
        {
            textComponent.enabled = blinkState;
        }
        else
        {
            textComponent.enabled = true;
        }
    }
}
