using UnityEngine;

/// <summary>
/// Controla o brilho e a iluminação de uma janela de acordo com o ciclo dia/noite do jogo.
/// Altera a cor e intensidade do material emissivo da janela e de uma luz projetada (opcional)
/// para simular o nascer do sol, meio-dia, pôr do sol e noite.
/// </summary>
public class WindowGlowController : MonoBehaviour
{
    [Header("Componentes Alvo")]
    [Tooltip("O Renderer que representa o vidro da janela (se vazio, tentará obter no próprio objeto).")]
    [SerializeField] private Renderer windowRenderer;

    [Tooltip("Uma fonte de luz (ex: Spot Light) associada à janela que projeta luz para o interior.")]
    [SerializeField] private Light targetLight;

    [Header("Configurações do Ciclo")]
    [Tooltip("Gradiente de cor que define o tom da luz da janela ao longo das 24 horas.")]
    [SerializeField] private Gradient windowGlowColor;

    [Tooltip("Curva que define a intensidade da luz e do brilho da janela ao longo das 24 horas (0 = Meia-noite, 1 = Fim do dia).")]
    [SerializeField] private AnimationCurve glowIntensity;

    [Header("Ajustes Finos")]
    [Tooltip("Multiplicador da intensidade da emissão do material da janela.")]
    [SerializeField] private float emissionMultiplier = 1.8f;

    [Tooltip("Se ativo, tenta habilitar a palavra-chave _EMISSION no material da janela em tempo de execução.")]
    [SerializeField] private bool forceEmissionKeyword = true;

    private MaterialPropertyBlock propertyBlock;
    private float lightBaseIntensity = 1.0f;

    // Caching de propriedades de shader para máxima performance
    private static readonly int ColorPropID = Shader.PropertyToID("_Color");
    private static readonly int BaseColorPropID = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorPropID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // Se não foi definido um Renderer, tenta obter do próprio GameObject
        if (windowRenderer == null)
        {
            windowRenderer = GetComponent<Renderer>();
        }

        // Se foi definida uma luz, guarda a sua intensidade base para servir de multiplicador
        if (targetLight != null)
        {
            lightBaseIntensity = targetLight.intensity;
        }

        propertyBlock = new MaterialPropertyBlock();

        // Se configurado, força a emissão a estar ativa no material
        if (forceEmissionKeyword && windowRenderer != null)
        {
            // Nota: Para usar MaterialPropertyBlock com emissão, o material original
            // deve ter a emissão ativa ou a keyword habilitada. Habilitamos no material partilhado.
            if (windowRenderer.sharedMaterial != null)
            {
                windowRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;

        // Calcula a hora decimal do dia normalizada entre 0.0 e 1.0
        float hour = TimeManager.Instance.CurrentHour;
        float minute = TimeManager.Instance.CurrentMinute;
        float time01 = (hour + (minute / 60f)) / 24f;

        // Obtém a cor e a intensidade correspondentes do ciclo
        Color currentColor = windowGlowColor.Evaluate(time01);
        float currentIntensity = glowIntensity.Evaluate(time01);

        // 1. Atualiza as propriedades visuais do Material da Janela (Glass)
        if (windowRenderer != null)
        {
            windowRenderer.GetPropertyBlock(propertyBlock);

            // Define cor principal e cor emissiva (HDR)
            propertyBlock.SetColor(ColorPropID, currentColor);
            propertyBlock.SetColor(BaseColorPropID, currentColor); // Para compatibilidade com URP Lit
            propertyBlock.SetColor(EmissionColorPropID, currentColor * currentIntensity * emissionMultiplier);

            windowRenderer.SetPropertyBlock(propertyBlock);
        }

        // 2. Atualiza a luz física que projeta para o interior
        if (targetLight != null)
        {
            targetLight.color = currentColor;
            targetLight.intensity = currentIntensity * lightBaseIntensity;
            
            // Desliga a luz se a intensidade for quase nula para poupar processamento
            targetLight.enabled = targetLight.intensity > 0.01f;
        }
    }

    /// <summary>
    /// Configura cores e curvas padrão quando o script é adicionado pela primeira vez no Editor.
    /// </summary>
    private void Reset()
    {
        windowGlowColor = new Gradient();

        // Criar gradiente de cor suave simulando o dia completo
        GradientColorKey[] colorKeys = new GradientColorKey[8];
        colorKeys[0] = new GradientColorKey(new Color(0.04f, 0.08f, 0.16f), 0.0f);   // 00:00 - Azul Escuro/Noite (Hex: #0B1528)
        colorKeys[1] = new GradientColorKey(new Color(0.17f, 0.10f, 0.30f), 0.22f);  // 05:16 - Violeta/Aurora (Hex: #2C1A4D)
        colorKeys[2] = new GradientColorKey(new Color(1.0f, 0.42f, 0.21f), 0.28f);   // 06:43 - Laranja/Nascer do Sol (Hex: #FF6B35)
        colorKeys[3] = new GradientColorKey(new Color(1.0f, 0.88f, 0.58f), 0.35f);   // 08:24 - Amarelo Suave (Hex: #FFE194)
        colorKeys[4] = new GradientColorKey(new Color(0.96f, 0.98f, 1.0f), 0.50f);   // 12:00 - Branco/Meio-Dia (Hex: #F4F9FF)
        colorKeys[5] = new GradientColorKey(new Color(1.0f, 0.96f, 0.88f), 0.70f);   // 16:48 - Marfim Quente (Hex: #FFF4E0)
        colorKeys[6] = new GradientColorKey(new Color(0.96f, 0.47f, 0.0f), 0.78f);   // 18:43 - Laranja/Pôr do Sol (Hex: #F77F00)
        colorKeys[7] = new GradientColorKey(new Color(0.11f, 0.21f, 0.34f), 0.85f);  // 20:24 - Crepúsculo Azul (Hex: #1D3557)

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1.0f, 0.0f);
        alphaKeys[1] = new GradientAlphaKey(1.0f, 1.0f);

        windowGlowColor.SetKeys(colorKeys, alphaKeys);

        // Criar curva de intensidade correspondente
        glowIntensity = new AnimationCurve(
            new Keyframe(0.0f,  0.15f),   // Noite
            new Keyframe(0.20f, 0.15f),   // Antes do amanhecer
            new Keyframe(0.28f, 1.50f),   // Nascer do sol
            new Keyframe(0.50f, 2.50f),   // Meio-dia (Pico de luz)
            new Keyframe(0.70f, 2.00f),   // Tarde
            new Keyframe(0.78f, 1.60f),   // Pôr do sol
            new Keyframe(0.85f, 0.15f),   // Pós-pôr do sol
            new Keyframe(1.0f,  0.15f)    // Noite
        );
    }
}
