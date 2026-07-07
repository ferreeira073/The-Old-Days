using UnityEngine;

/// <summary>
/// Posiciona a lista física de objetivos (ex: folha/prancheta)
/// num local aleatório na casa no início do jogo e sempre que o dia muda.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    [Header("Objeto Físico e Spawns")]
    [Tooltip("O objeto 3D físico da lista de tarefas na cena (que o jogador procura).")]
    [SerializeField] private GameObject physicalListObject;

    [Tooltip("Pontos de spawn possíveis onde o objeto da lista pode ser colocado a cada dia.")]
    [SerializeField] private Transform[] spawnPoints;

    private TimeManager subscribedTimeManager;
    private GameObject activeListInstance;

    private void Awake()
    {
        // Se não foi associado nenhum objeto físico, assume que o script está no próprio objeto físico da lista
        if (physicalListObject == null)
        {
            physicalListObject = this.gameObject;
        }
    }

    private void Start()
    {
        // 1. Posiciona a lista imediatamente de forma incondicional no arranque do jogo
        RepositionList();

        // 2. Tenta subscrever no TimeManager
        subscribedTimeManager = TimeManager.Instance;
        if (subscribedTimeManager == null)
        {
            subscribedTimeManager = FindFirstObjectByType<TimeManager>();
        }

        if (subscribedTimeManager != null)
        {
            subscribedTimeManager.OnDayChanged += HandleDayChanged;
        }
        else
        {
            Debug.LogWarning("[Tarefas] TimeManager não foi encontrado na cena no arranque! O ciclo de dia/noite não moverá a lista automaticamente.");
        }
    }

    private void OnDestroy()
    {
        // Previne vazamentos de memória desinscrevendo o evento
        if (subscribedTimeManager != null)
        {
            subscribedTimeManager.OnDayChanged -= HandleDayChanged;
        }
    }

    /// <summary>
    /// Chamado automaticamente quando o dia muda no TimeManager.
    /// </summary>
    private void HandleDayChanged(int newDay)
    {
        // Move a lista física para um local aleatório no novo dia
        RepositionList();
    }

    /// <summary>
    /// Teletransporta o objeto físico da lista para um ponto de spawn aleatório.
    /// </summary>
    private void RepositionList()
    {
        if (physicalListObject == null)
        {
            Debug.LogError("[Tarefas] ERRO: O objeto físico da lista (Physical List Object) não foi associado e não pôde ser autoreferenciado!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[Tarefas] ERRO: Não foram configurados pontos de spawn (Spawn Points) no inspector de TaskListManager!");
            return;
        }

        // Seleciona um ponto aleatório de spawn
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform targetSpawn = spawnPoints[randomIndex];

        if (targetSpawn == null)
        {
            Debug.LogError($"[Tarefas] ERRO: O ponto de spawn no índice {randomIndex} é nulo (vazio)!");
            return;
        }

        // Verifica se o objeto associado é um Prefab (do diretório Assets) ou um objeto da cena física
        bool isPrefab = !physicalListObject.scene.IsValid();

        if (isPrefab)
        {
            // Se for um Prefab, precisamos de o instanciar na cena para que exista fisicamente
            if (activeListInstance == null)
            {
                activeListInstance = Instantiate(physicalListObject, targetSpawn.position, targetSpawn.rotation);
                Debug.Log($"[Tarefas] O objeto configurado era um Prefab. Instanciada uma nova cópia na cena: '{activeListInstance.name}'.");
            }
            else
            {
                activeListInstance.transform.position = targetSpawn.position;
                activeListInstance.transform.rotation = targetSpawn.rotation;
            }
        }
        else
        {
            // Se for um objeto já presente na cena, apenas o movemos
            activeListInstance = physicalListObject;
            activeListInstance.transform.position = targetSpawn.position;
            activeListInstance.transform.rotation = targetSpawn.rotation;
        }

        // Configurações e validações finais de segurança no objeto ativo
        if (activeListInstance != null)
        {
            // Garante que o objeto está ativo e visível na cena
            activeListInstance.SetActive(true);

            // Se possuir um Rigidbody (física ativa), anula velocidades residuais para não voar ou colidir
            Rigidbody rb = activeListInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Desenha um raio vermelho na vista Scene do Unity para ajudar a localizar visualmente onde o objeto apareceu
            Debug.DrawRay(activeListInstance.transform.position, Vector3.up * 10f, Color.red, 15f);

            Debug.Log($"[Tarefas] Lista posicionada com sucesso em: '{targetSpawn.name}' na posição {activeListInstance.transform.position}. (Linha vertical vermelha desenhada na vista Scene para depuração).");
        }
    }
}
