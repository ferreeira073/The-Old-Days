using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controla a exibição visual da lista de tarefas no ecrã (canto superior esquerdo).
/// Atualiza dinamicamente as caixas de seleção com base no progresso das tarefas.
/// </summary>
public class TaskUI : MonoBehaviour
{
    public static TaskUI Instance { get; private set; }

    [Header("Componentes de UI")]
    [Tooltip("Texto do TextMeshPro onde a lista de tarefas será escrita.")]
    [SerializeField] private TMP_Text taskListText;

    [Tooltip("O painel da UI que contém a lista (pode ser ativado/desativado de acordo com o estado).")]
    [SerializeField] private GameObject uiPanel;

    [Header("Configurações de Texto")]
    [Tooltip("Título exibido no topo da lista quando esta é encontrada (ex: 'TAREFAS:').")]
    [SerializeField] private string listTitle = "TAREFAS:";

    private void Awake()
    {
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
        UpdateUI();
    }

    /// <summary>
    /// Re-desenha e atualiza o conteúdo do texto da lista de tarefas no ecrã.
    /// </summary>
    public void UpdateUI()
    {
        if (taskListText == null) return;

        if (TaskManager.Instance == null)
        {
            if (uiPanel != null) uiPanel.SetActive(false);
            taskListText.text = "";
            return;
        }

        // Se o painel estiver definido, garante que está ativo
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }

        // Caso 1: A lista de tarefas física ainda não foi encontrada hoje
        if (!TaskManager.Instance.IsListFound)
        {
            taskListText.text = "<color=red>[ ] Find the list</color>";
            return;
        }

        // Caso 2: A lista foi encontrada, exibe os objetivos do dia
        string displayText = $"<color=yellow>{listTitle}</color>\n";
        bool allCompleted = true;

        foreach (var task in TaskManager.Instance.CurrentDayTasks)
        {
            if (task.isCompleted)
            {
                // Concluídas desaparecem da lista, por isso saltamos a exibição
                continue;
            }
            else
            {
                displayText += $"<color=white>[ ] {task.description}</color>\n";
                allCompleted = false;
            }
        }

        // Objetivo Extra: Se todas as tarefas estiverem feitas, instrui o jogador a ir dormir
        if (allCompleted)
        {
            displayText += "<color=cyan>[] Go to sleep</color>";

        }

        taskListText.text = displayText;
    }
}
