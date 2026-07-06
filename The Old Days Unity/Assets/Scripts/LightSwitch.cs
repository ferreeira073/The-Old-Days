using UnityEngine;

/// <summary>
/// Controla o comportamento de um interruptor de luz físico e interativo.
/// Permite ligar/desligar componentes de luz, alterar o brilho emissivo das lâmpadas,
/// reproduzir feedback sonoro e rodar fisicamente o botão do interruptor.
/// </summary>
public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Estado Inicial")]
    [Tooltip("Estado inicial das luzes (ligadas ou desligadas).")]
    [SerializeField] private bool isOn = true;

    [Header("Luzes a Controlar")]
    [Tooltip("As luzes físicas que este interruptor controla.")]
    [SerializeField] private Light[] targetLights;

    [Header("Feedback Visual das Lâmpadas")]
    [Tooltip("Os renderers das lâmpadas/luminárias no teto para alterar o brilho emissivo do material.")]
    [SerializeField] private Renderer[] lightRenderers;

    [Tooltip("A cor de emissão quando a lâmpada está acesa.")]
    [SerializeField] private Color onEmissionColor = Color.white;

    [Tooltip("Intensidade da emissão quando acesa.")]
    [SerializeField] private float onEmissionIntensity = 1.5f;

    [Header("Feedback Físico do Interruptor")]
    [Tooltip("O Transform do botão ou alavanca física do interruptor para rodar ao interagir.")]
    [SerializeField] private Transform switchLever;

    [Tooltip("Rotação local do botão quando ligado.")]
    [SerializeField] private Vector3 localRotationOn = new Vector3(-15f, 0f, 0f);

    [Tooltip("Rotação local do botão quando desligado.")]
    [SerializeField] private Vector3 localRotationOff = new Vector3(15f, 0f, 0f);

    [Header("Feedback Sonoro")]
    [Tooltip("Efeito sonoro de clique do interruptor.")]
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    private MaterialPropertyBlock propertyBlock;
    private static readonly int EmissionColorPropID = Shader.PropertyToID("_EmissionColor");

    private void Start()
    {
        propertyBlock = new MaterialPropertyBlock();

        // Configura ou adiciona um componente AudioSource para o som de clique
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && clickSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Aplica o estado inicial sem tocar feedback sonoro
        UpdateLights(isOn, false);
    }

    /// <summary>
    /// Método chamado quando o jogador interage com o interruptor.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador que iniciou a interação.</param>
    public void Interact(Transform playerTransform)
    {
        isOn = !isOn;
        UpdateLights(isOn, true);
    }

    /// <summary>
    /// Atualiza as luzes, materiais, rotação física e som com base no estado atual.
    /// </summary>
    /// <param name="state">Novo estado das luzes (true = ligado, false = desligado).</param>
    /// <param name="playFeedback">Se verdadeiro, reproduz os feedbacks sonoros e visuais.</param>
    private void UpdateLights(bool state, bool playFeedback)
    {
        // 1. Alterna os componentes Light
        if (targetLights != null)
        {
            foreach (Light light in targetLights)
            {
                if (light != null)
                {
                    light.enabled = state;
                }
            }
        }

        // 2. Atualiza a emissão nos Renderers das lâmpadas/luminárias do teto
        if (lightRenderers != null)
        {
            Color targetColor = state ? (onEmissionColor * onEmissionIntensity) : Color.black;

            foreach (Renderer renderer in lightRenderers)
            {
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(EmissionColorPropID, targetColor);
                    renderer.SetPropertyBlock(propertyBlock);

                    // Garante que a keyword de emissão está ativa no material partilhado
                    if (state && renderer.sharedMaterial != null)
                    {
                        renderer.sharedMaterial.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        // 3. Altera a rotação local da alavanca/botão do interruptor
        if (switchLever != null)
        {
            switchLever.localRotation = Quaternion.Euler(state ? localRotationOn : localRotationOff);
        }

        // 4. Toca o som de clique
        if (playFeedback && audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}
