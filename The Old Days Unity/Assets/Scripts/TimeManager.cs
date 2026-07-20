using UnityEngine;

/// <summary>
/// Gere o tempo e a progressão dos dias no jogo.
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Configurações do Tempo")]
    [Tooltip("Quantos segundos da vida real correspondem a 1 minuto no jogo.")]
    public float secondsPerInGameMinute = 2f;

    [Tooltip("Hora de início de cada dia.")]
    public int startHour = 8;
    [Tooltip("Minuto de início de cada dia.")]
    public int startMinute = 0;

    [Header("Configurações do Ciclo")]
    [Tooltip("O dia em que o jogo começa.")]
    public int startDay = 1;
    [Tooltip("O dia final do jogo.")]
    public int maxDays = 7;

    [Header("Estado Atual (Apenas Leitura no Editor)")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int currentHour = 8;
    [SerializeField] private int currentMinute = 0;

    private float timeAccumulator = 0f;
    private int lastLoggedHour = -1;

    // Eventos para outros sistemas (como UI e Tarefas)
    public delegate void DayChangedHandler(int newDay);
    public event DayChangedHandler OnDayChanged;

    public delegate void TimeChangedHandler(int hour, int minute);
    public event TimeChangedHandler OnTimeChanged;

    public delegate void WeekEndedHandler();
    public event WeekEndedHandler OnWeekEnded;

    public delegate void WeekFailedHandler();
    public event WeekFailedHandler OnWeekFailed;

    private bool weekFailed = false;

    // Propriedades de Acesso
    public int CurrentDay => currentDay;
    public int CurrentHour => currentHour;
    public int CurrentMinute => currentMinute;
    public bool IsWeekFinished => currentDay > maxDays;
    public bool IsWeekFailed => weekFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentDay = startDay;
        currentHour = startHour;
        currentMinute = startMinute;
    }

    private void Start()
    {
        // Notifica o estado inicial no arranque do jogo
        OnDayChanged?.Invoke(currentDay);
        OnTimeChanged?.Invoke(currentHour, currentMinute);
        
        LogCurrentTime(true);
    }

    private void Update()
    {
        if (IsWeekFinished || weekFailed) return;

        // Progressão do tempo
        timeAccumulator += Time.deltaTime;
        if (timeAccumulator >= secondsPerInGameMinute)
        {
            timeAccumulator -= secondsPerInGameMinute;
            AdvanceMinute();
        }
    }

    private void AdvanceMinute()
    {
        currentMinute++;
        if (currentMinute >= 60)
        {
            currentMinute = 0;
            currentHour++;
            
            if (currentHour >= 24)
            {
                currentHour = 0;
            }
        }

        // Invoca o evento de mudança de tempo
        OnTimeChanged?.Invoke(currentHour, currentMinute);

        // Regista nos logs sempre que muda a hora in-game
        if (currentHour != lastLoggedHour)
        {
            LogCurrentTime(false);
            lastLoggedHour = currentHour;
        }
    }

    /// <summary>
    /// Avança para o dia seguinte e repõe o relógio para a hora de início.
    /// </summary>
    public void AdvanceDay()
    {
        if (IsWeekFinished) return;

        currentDay++;
        currentHour = startHour;
        currentMinute = startMinute;
        timeAccumulator = 0f;
        lastLoggedHour = -1;

        Debug.Log($"[Tempo] O jogador foi dormir. Novo dia iniciado: Dia {currentDay} / {maxDays}.");

        if (currentDay > maxDays)
        {
            Debug.Log("[Tempo] Fim da semana! A idosa sobreviveu aos 7 dias.");
            OnWeekEnded?.Invoke();
        }
        else
        {
            OnDayChanged?.Invoke(currentDay);
            OnTimeChanged?.Invoke(currentHour, currentMinute);
            LogCurrentTime(true);
        }
    }

    private void LogCurrentTime(bool newDayStarted)
    {
        string header = newDayStarted ? $"--- INÍCIO DO DIA {currentDay} ---" : "[Tempo]";
        Debug.Log($"{header} Hora Atual no Jogo: {currentHour:D2}:{currentMinute:D2}");
    }

    /// <summary>
    /// Desencadeia a condição de semana falhada (chamado pelo TaskManager quando o curfew passa).
    /// Para o tempo e dispara o evento OnWeekFailed.
    /// </summary>
    public void TriggerWeekFailed()
    {
        if (weekFailed || IsWeekFinished) return;
        weekFailed = true;
        Debug.LogWarning("[Tempo] Semana falhada! O jogador não completou as tarefas a tempo.");
        OnWeekFailed?.Invoke();
    }
}
