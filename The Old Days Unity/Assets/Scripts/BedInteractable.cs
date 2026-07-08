using System.Collections.Generic;
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
        // 1. Valida se o jogador já encontrou a lista e completou todas as tarefas de hoje
        if (TaskManager.Instance != null)
        {
            if (!TaskManager.Instance.IsListFound)
            {
                ShowSleepDeniedDialogue("Não posso ir dormir ainda. Tenho de encontrar a minha lista de tarefas primeiro.");
                return;
            }

            if (!TaskManager.Instance.AreAllTasksCompleted())
            {
                ShowSleepDeniedDialogue("Ainda não completei todas as tarefas de hoje... Não posso ir dormir.");
                return;
            }
        }

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

    /// <summary>
    /// Exibe um monólogo do jogador indicando o motivo pelo qual não pode dormir.
    /// </summary>
    private void ShowSleepDeniedDialogue(string text)
    {
        if (GameDialogueManager.Instance != null)
        {
            GameDialogueManager.DialogueLine line = new GameDialogueManager.DialogueLine
            {
                speakerName = "Jogador",
                textLine = text
            };
            GameDialogueManager.Instance.ShowDialogue(new List<GameDialogueManager.DialogueLine> { line });
        }
        else
        {
            Debug.LogWarning($"[Cama] Diálogo bloqueado: {text}");
        }
    }
}
