using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o cesto de roupa no quarto. Trata da geração física e lógica das roupas,
/// e da remoção/recolha de cada peça pelo jogador de forma simples e direta.
/// </summary>
public class LaundryBasket : MonoBehaviour, IInteractable
{
    public static LaundryBasket Instance { get; private set; }

    [System.Serializable]
    public class DayClothesSpawnConfig
    {
        [Tooltip("Número do dia (noite) correspondente (ex: 2 para a 2ª noite).")]
        public int dayNumber;

        [Tooltip("Número de roupas a gerar fora do cesto neste dia.")]
        public int outsideClothesCount = 2;

        [Tooltip("Pontos de spawn possíveis para as roupas fora do cesto neste dia.")]
        public Transform[] spawnPoints;
    }

    [Header("Configuração de Spawn de Roupa")]
    [Tooltip("O prefab da peça de roupa a ser instanciado.")]
    [SerializeField] private GameObject clothesPrefab;

    [Tooltip("Ponto de spawn inicial da roupa no cesto.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Número mínimo de peças de roupa a gerar.")]
    [SerializeField] private int minClothes = 3;

    [Tooltip("Número máximo de peças de roupa a gerar.")]
    [SerializeField] private int maxClothes = 6;

    [Tooltip("Espaçamento vertical (altura) entre cada peça empilhada.")]
    [SerializeField] private float heightOffset = 0.12999988f;

    [Header("Configurações Dinâmicas por Noite")]
    [Tooltip("Configurações específicas de spawn de roupa para cada dia.")]
    [SerializeField] private List<DayClothesSpawnConfig> daySpawnConfigs = new List<DayClothesSpawnConfig>();

    [Header("Identificação da Tarefa")]
    [Tooltip("ID da tarefa de recolha de roupa configurada no TaskManager.")]
    [SerializeField] private string clothesTaskId = "collect_clothes";

    private List<GameObject> physicalClothes = new List<GameObject>();
    private List<Color> clothesColors = new List<Color>();
    private List<GameObject> outsideClothes = new List<GameObject>();
    private bool isHoldingClothes = false;
    private Color heldColor;

    public bool IsHoldingClothes => isHoldingClothes;
    public Color HeldColor => heldColor;
    public int RemainingClothesCount => physicalClothes.Count + outsideClothes.Count;
    public string ClothesTaskId => clothesTaskId;
    public float HeightOffset => heightOffset;

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
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
        
        // Spawna as roupas de forma incondicional no arranque do jogo
        SpawnClothes();
    }

    private void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        SpawnClothes();
    }

    /// <summary>
    /// Limpa qualquer roupa anterior e gera novas peças com cores aleatórias, empilhadas estaticamente.
    /// </summary>
    public void SpawnClothes()
    {
        ClearRemainingClothes();

        if (clothesPrefab == null)
        {
            Debug.LogError("[LaundryBasket] ERRO: O Prefab de Roupa (Clothes Prefab) não está associado no Inspector!");
            return;
        }

        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
        }

        int count = Random.Range(minClothes, maxClothes + 1);
        
        int currentDay = 1;
        if (TimeManager.Instance != null)
        {
            currentDay = TimeManager.Instance.CurrentDay;
        }
        
        Debug.Log($"[LaundryBasket] Spawning {count} pieces of clothing for Day {currentDay}.");

        // Determina se existem roupas fora do cesto para gerar hoje
        int targetOutsideCount = 0;
        DayClothesSpawnConfig config = null;

        if (daySpawnConfigs != null && daySpawnConfigs.Count > 0)
        {
            config = daySpawnConfigs.Find(c => c.dayNumber == currentDay);
            if (config == null)
            {
                // Fallback para o dia configurado mais alto que seja menor que o dia atual
                int maxDayFound = -1;
                foreach (var c in daySpawnConfigs)
                {
                    if (c.dayNumber <= currentDay && c.dayNumber > maxDayFound)
                    {
                        maxDayFound = c.dayNumber;
                        config = c;
                    }
                }
            }
        }

        if (config != null && config.spawnPoints != null && config.spawnPoints.Length > 0)
        {
            targetOutsideCount = Mathf.Min(config.outsideClothesCount, count);
        }

        int targetInsideCount = count - targetOutsideCount;

        // 1. Spawna as roupas que ficam DENTRO do cesto (empilhadas)
        for (int i = 0; i < targetInsideCount; i++)
        {
            Vector3 offset = new Vector3(0f, i * heightOffset, 0f);
            GameObject piece = Instantiate(clothesPrefab, spawnPoint.position + offset, spawnPoint.rotation);
            
            piece.layer = 2; // Ignore Raycast
            
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            Renderer renderer = piece.GetComponentInChildren<Renderer>();
            Color randomColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
            if (renderer != null)
            {
                renderer.material.color = randomColor;
            }

            physicalClothes.Add(piece);
            clothesColors.Add(randomColor);
        }

        // 2. Spawna as roupas que ficam FORA do cesto (em pontos aleatórios)
        if (targetOutsideCount > 0 && config != null && config.spawnPoints != null && config.spawnPoints.Length > 0)
        {
            List<Transform> availableSpawns = new List<Transform>(config.spawnPoints);

            for (int i = 0; i < targetOutsideCount; i++)
            {
                if (availableSpawns.Count == 0)
                {
                    Debug.LogWarning("[LaundryBasket] Não há pontos de spawn disponíveis suficientes para as roupas fora do cesto! Reutilizando pontos.");
                    availableSpawns = new List<Transform>(config.spawnPoints);
                }

                int randomIndex = Random.Range(0, availableSpawns.Count);
                Transform targetSpawn = availableSpawns[randomIndex];
                availableSpawns.RemoveAt(randomIndex);

                if (targetSpawn == null)
                {
                    Debug.LogError($"[LaundryBasket] Ponto de spawn no índice {randomIndex} para o dia {config.dayNumber} é nulo!");
                    continue;
                }

                GameObject piece = Instantiate(clothesPrefab, targetSpawn.position, targetSpawn.rotation);
                
                // Roupa fora do cesto deve ter colisão normal para poder ser detetada pelo Raycast do jogador
                piece.layer = 0; // Default layer
                
                Rigidbody rb = piece.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true; // Mantém estática
                }

                Renderer renderer = piece.GetComponentInChildren<Renderer>();
                Color randomColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
                if (renderer != null)
                {
                    renderer.material.color = randomColor;
                }

                // Adiciona o componente interactável de roupa
                ClothingItem itemComponent = piece.AddComponent<ClothingItem>();
                itemComponent.Setup(randomColor, this);

                outsideClothes.Add(piece);
                
                // Desenha linha vermelha no Scene view para debugging, igual à lista de tarefas
                Debug.DrawRay(piece.transform.position, Vector3.up * 10f, Color.red, 15f);
                Debug.Log($"[LaundryBasket] Spawned outside clothing piece at '{targetSpawn.name}' (Color: {randomColor}).");
            }
        }
    }

    private void ClearRemainingClothes()
    {
        foreach (var piece in physicalClothes)
        {
            if (piece != null) Destroy(piece);
        }
        physicalClothes.Clear();
        clothesColors.Clear();

        foreach (var piece in outsideClothes)
        {
            if (piece != null) Destroy(piece);
        }
        outsideClothes.Clear();

        isHoldingClothes = false;
    }

    /// <summary>
    /// Chamado quando o jogador interage com o cesto de roupa.
    /// </summary>
    public void Interact(Transform playerTransform)
    {
        // 1. O jogador deve ter a lista encontrada para interagir
        if (TaskManager.Instance == null || !TaskManager.Instance.IsListFound)
        {
            ShowFeedbackDialogue("I should look for my task list first.");
            return;
        }

        // 2. Se o jogador já estiver a carregar uma roupa
        if (isHoldingClothes)
        {
            ShowFeedbackDialogue("I am already carrying a piece of clothing. I should put it in the wardrobe.");
            return;
        }

        // 3. Retira a última roupa física do cesto
        if (physicalClothes.Count > 0)
        {
            int lastIndex = physicalClothes.Count - 1;
            GameObject topPiece = physicalClothes[lastIndex];
            heldColor = clothesColors[lastIndex];
            
            if (topPiece != null) Destroy(topPiece);
            physicalClothes.RemoveAt(lastIndex);
            clothesColors.RemoveAt(lastIndex);

            isHoldingClothes = true;
            ShowFeedbackDialogue("Picked up a piece of clothing. I need to place it in the wardrobe.");
        }
        else
        {
            if (outsideClothes.Count > 0)
            {
                ShowFeedbackDialogue("The basket is empty. I need to search the room for the remaining clothes.");
            }
            else
            {
                ShowFeedbackDialogue("The basket is empty.");
            }
        }
    }

    public void ClearHeldState()
    {
        isHoldingClothes = false;
    }

    /// <summary>
    /// Chamado pelo script ClothingItem para recolher uma roupa fora do cesto.
    /// </summary>
    public void PickUpOutsideClothing(GameObject piece, Color color)
    {
        if (outsideClothes.Contains(piece))
        {
            outsideClothes.Remove(piece);
        }
        Destroy(piece);
        isHoldingClothes = true;
        heldColor = color;
    }

    private void ShowFeedbackDialogue(string text)
    {
        if (GameDialogueManager.Instance != null)
        {
            var line = new GameDialogueManager.DialogueLine { speakerName = "Player", textLine = text };
            GameDialogueManager.Instance.ShowDialogue(new List<GameDialogueManager.DialogueLine> { line });
        }
        else
        {
            Debug.Log($"[LaundryBasket] {text}");
        }
    }
}
