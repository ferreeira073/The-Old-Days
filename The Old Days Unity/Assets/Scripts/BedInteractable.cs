using UnityEngine;

/// <summary>
/// Permite ao jogador interagir com a cama para ir dormir e avançar o dia.
/// </summary>
public class BedInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações da Cama")]
    [Tooltip("Mensagem exibida na consola ao interagir.")]
    [SerializeField] private string sleepMessage = "A ir dormir... A avançar para o dia seguinte.";

    /// <summary>
    /// Chamado quando o jogador interage com a cama.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador que iniciou a interação.</param>
    public void Interact(Transform playerTransform)
    {
        Debug.Log(sleepMessage);

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AdvanceDay();
        }
        else
        {
            Debug.LogWarning("[Cama] Não foi possível avançar o dia porque o TimeManager não existe na cena.");
        }
    }
}
