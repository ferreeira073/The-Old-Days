using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gere o ciclo de vida das tarefas diárias, o estado de descoberta da lista de tarefas,
/// o limite de tempo do dia (curfew) e a condição de derrota se as tarefas falharem.
/// </summary>
public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance { get; private set; }

    [System.Serializable]
    public class TaskItem
    {
        [Tooltip("Identificador único da tarefa (ex: 'desligar_luzes', 'trancar_portas').")]
        public string taskId;
        
        [Tooltip("Descrição legível que será apresentada na UI do canto superior direito.")]
        public string description;
        
        [HideInInspector]
        public bool isCompleted;
    }

    [System.Serializable]
    public class DayTasksConfig
    {
        [Tooltip("Número do dia correspondente (1 a 7).")]
        public int dayNumber;

        [Tooltip("Lista de tarefas que têm de ser realizadas neste dia.")]
        public List<TaskItem> tasks;
    }

    [Header("Configurações das Tarefas")]
    [Tooltip("Configuração das tarefas para cada dia da semana.")]
    [SerializeField] private List<DayTasksConfig> dailyTasks;

    [Tooltip("Hora limite do dia em que o jogador perde se não terminar as tarefas (ex: 3 correspondente a 03:00 AM).")]
    [SerializeField] private int curfewHour = 3;

    [Tooltip("Nome da cena a carregar quando o jogador perde (normalmente o Menu Principal).")]
    [SerializeField] private string gameOverSceneName = "MainMenu";

    [Header("Estado Atual (Apenas Leitura no Editor)")]
    [SerializeField] private bool isListFound = false;
    [SerializeField] private List<TaskItem> currentDayTasks = new List<TaskItem>();

    public bool IsListFound => isListFound;
    public List<TaskItem> CurrentDayTasks => currentDayTasks;

    public delegate void TasksInitializedHandler(int day);
    public event TasksInitializedHandler OnTasksInitialized;

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
        if (TimeManager.Instance != null)
        {
            // Subscreve-se nas notificações do TimeManager
            TimeManager.Instance.OnDayChanged += InitializeDay;
            TimeManager.Instance.OnTimeChanged += HandleTimeChanged;
            InitializeDay(TimeManager.Instance.CurrentDay);
        }
        else
        {
            Debug.LogWarning("[TaskManager] TimeManager não encontrado na cena! Usando o Dia 1 por defeito.");
            InitializeDay(1);
        }
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= InitializeDay;
            TimeManager.Instance.OnTimeChanged -= HandleTimeChanged;
        }
    }

    /// <summary>
    /// Limpa o estado anterior e inicializa as tarefas do novo dia.
    /// </summary>
    /// <param name="day">Número do dia (1 a 7).</param>
    public void InitializeDay(int day)
    {
        isListFound = false;
        currentDayTasks.Clear();

        // Encontra a configuração correspondente ao dia
        DayTasksConfig config = dailyTasks.Find(d => d.dayNumber == day);
        if (config != null)
        {
            foreach (var task in config.tasks)
            {
                currentDayTasks.Add(new TaskItem 
                { 
                    // Apara espaços em branco antes ou depois para evitar erros de escrita
                    taskId = !string.IsNullOrEmpty(task.taskId) ? task.taskId.Trim() : "", 
                    description = task.description, 
                    isCompleted = false 
                });
            }
            Debug.Log($"[TaskManager] Day {day} initialized with {currentDayTasks.Count} tasks.");
        }
        else
        {
            Debug.LogWarning($"[TaskManager] No tasks configuration found for Day {day}.");
        }

        // Atualiza a interface gráfica
        if (TaskUI.Instance != null)
        {
            TaskUI.Instance.UpdateUI();
        }

        OnTasksInitialized?.Invoke(day);
    }

    /// <summary>
    /// Ativado quando o jogador encontra e interage com o papel físico da lista de tarefas.
    /// </summary>
    public void OnListFound()
    {
        isListFound = true;
        Debug.Log("[TaskManager] A lista de tarefas foi encontrada! UI de tarefas ativada.");

        if (TaskUI.Instance != null)
        {
            TaskUI.Instance.UpdateUI();
        }
    }

    /// <summary>
    /// Marca uma tarefa específica como concluída.
    /// </summary>
    /// <param name="taskId">O ID identificador da tarefa.</param>
    public void CompleteTask(string taskId)
    {
        // O jogador não pode concluir tarefas sem antes encontrar a lista física!
        if (!isListFound)
        {
            Debug.LogWarning($"[TaskManager] Tentativa de completar a tarefa '{taskId}' falhou. O jogador ainda não encontrou a lista de tarefas física!");
            return;
        }

        string targetId = !string.IsNullOrEmpty(taskId) ? taskId.Trim() : "";
        TaskItem task = currentDayTasks.Find(t => string.Equals(t.taskId.Trim(), targetId, System.StringComparison.OrdinalIgnoreCase));
        if (task != null)
        {
            if (!task.isCompleted)
            {
                task.isCompleted = true;
                Debug.Log($"[TaskManager] Tarefa concluída: {task.description} (ID: {targetId})");

                if (TaskUI.Instance != null)
                {
                    TaskUI.Instance.UpdateUI();
                }
            }
        }
        else
        {
            // Adiciona diagnóstico útil no console para o usuário
            List<string> activeTaskIds = currentDayTasks.ConvertAll(t => t.taskId);
            string activeTasksString = activeTaskIds.Count > 0 ? string.Join(", ", activeTaskIds) : "None";
            
            Debug.LogWarning($"[TaskManager] Tentativa de completar tarefa inexistente para hoje: '{targetId}'.\n" +
                             $"Tarefas configuradas para o dia atual no TaskManager: [{activeTasksString}].\n" +
                             $"Verifique se adicionou a tarefa com o ID correto na lista correspondente do TaskManager.");
        }
    }

    /// <summary>
    /// Verifica se todas as tarefas do dia atual foram concluídas com sucesso.
    /// </summary>
    public bool AreAllTasksCompleted()
    {
        if (!isListFound) return false;

        if (currentDayTasks.Count == 0) return true;

        foreach (var task in currentDayTasks)
        {
            if (!task.isCompleted) return false;
        }

        return true;
    }

    /// <summary>
    /// Escuta as mudanças de hora in-game para realizar tarefas baseadas em tempo e verificar derrota.
    /// </summary>
    private void HandleTimeChanged(int hour, int minute)
    {
        // 1. Tarefa Baseada em Tempo: Sobreviver até às 9:00 AM
        if (hour >= 9)
        {
            CompleteTask("survive_until_9");
        }

        // 2. Verifica a hora limite (Curfew) para derrota (ex: 3 AM)
        if (hour == curfewHour && minute == 0)
        {
            if (!AreAllTasksCompleted())
            {
                TriggerGameOver();
            }
        }
    }

    /// <summary>
    /// Lida com a condição de derrota e carrega a cena definida.
    /// </summary>
    private void TriggerGameOver()
    {
        Debug.LogWarning("[TaskManager] FIM DE TEMPO! O jogador falhou em realizar as tarefas de hoje e perdeu o jogo.");
        
        // Garante que o cursor é libertado para o menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (Application.CanStreamedLevelBeLoaded(gameOverSceneName))
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
        else
        {
            Debug.LogError($"[TaskManager] Não foi possível carregar a cena de Game Over '{gameOverSceneName}'. Certifique-se de que está nas Build Settings.");
        }
    }
}
