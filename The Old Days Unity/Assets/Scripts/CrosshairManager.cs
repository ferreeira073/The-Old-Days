using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gere o aspeto visual da crosshair no centro do ecrã (troca de sprite e cor).
/// </summary>
public class CrosshairManager : MonoBehaviour
{
    public static CrosshairManager Instance { get; private set; }

    [Header("Componentes de UI")]
    [Tooltip("A imagem da crosshair no centro da tela.")]
    public Image crosshairImage;

    [Header("Sprites (Imagens)")]
    [Tooltip("A imagem padrão da crosshair (ex: um ponto ou mira simples).")]
    public Sprite defaultSprite;

    [Tooltip("A imagem de interação da crosshair (ex: uma mão ou círculo). Opcional - se nulo, mudará apenas de cor.")]
    public Sprite interactableSprite;

    [Header("Cores")]
    [Tooltip("A cor da crosshair em estado normal.")]
    public Color defaultColor = Color.white;

    [Tooltip("A cor da crosshair quando está apontada para algo interativo.")]
    public Color interactableColor = Color.yellow;

    private void Awake()
    {
        // Configuração do Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Inicializa a crosshair no estado padrão
        ShowDefault();
    }

    /// <summary>
    /// Ativa o visual de interação na crosshair (muda sprite e cor).
    /// </summary>
    public void ShowInteractable()
    {
        if (crosshairImage != null)
        {
            if (interactableSprite != null)
            {
                crosshairImage.sprite = interactableSprite;
            }
            crosshairImage.color = interactableColor;
        }
    }

    /// <summary>
    /// Restaura o visual padrão da crosshair (muda sprite e cor).
    /// </summary>
    public void ShowDefault()
    {
        if (crosshairImage != null)
        {
            if (defaultSprite != null)
            {
                crosshairImage.sprite = defaultSprite;
            }
            crosshairImage.color = defaultColor;
        }
    }
}
