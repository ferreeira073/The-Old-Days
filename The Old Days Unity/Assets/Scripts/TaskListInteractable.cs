using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente anexado ao objeto físico da lista de tarefas na cena.
/// Permite ao jogador interagir com o papel para o recolher e acionar as tarefas do dia.
/// </summary>
public class TaskListInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações do Diálogo ao Recolher")]
    [Tooltip("Nome que aparecerá na caixa de diálogo (ex: 'Jogador').")]
    [SerializeField] private string speakerName = "Jogador";

    [Tooltip("Texto do monólogo ao encontrar a lista pela primeira vez no dia.")]
    [SerializeField] private string dialogueLineText = "Found it...";

    /// <summary>
    /// Chamado quando o jogador olha para a lista e clica na tecla de interação.
    /// </summary>
    public void Interact(Transform playerTransform)
    {
        // Só interage se o TaskManager existir e a lista ainda não tiver sido encontrada hoje
        if (TaskManager.Instance != null && !TaskManager.Instance.IsListFound)
        {
            // 1. Desativa o papel físico da mesa imediatamente (ele é recolhido pelo jogador)
            // No dia seguinte, o TaskListManager voltará a reposicioná-lo e ativá-lo
            gameObject.SetActive(false);

            // 2. Cria a fala única para o monólogo
            GameDialogueManager.DialogueLine line = new GameDialogueManager.DialogueLine
            {
                speakerName = this.speakerName,
                textLine = this.dialogueLineText
            };

            List<GameDialogueManager.DialogueLine> lines = new List<GameDialogueManager.DialogueLine> { line };

            // Dispara o diálogo no gestor dinâmico
            if (GameDialogueManager.Instance != null)
            {
                GameDialogueManager.Instance.ShowDialogue(lines, () => {
                    // Quando o diálogo terminar (o texto fechar), ativa a lista no HUD
                    TaskManager.Instance.OnListFound();
                });
            }
            else
            {
                // Fallback caso não haja um GameDialogueManager na cena
                Debug.LogWarning("[TaskListInteractable] GameDialogueManager not found in the scene! Activating tasks directly.");
                TaskManager.Instance.OnListFound();
            }
        }
    }
}
