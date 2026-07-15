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

    [Header("Identificação da Tarefa")]
    [Tooltip("ID da tarefa de recolha de roupa configurada no TaskManager.")]
    [SerializeField] private string clothesTaskId = "collect_clothes";

    private List<GameObject> physicalClothes = new List<GameObject>();
    private List<Color> clothesColors = new List<Color>();
    private bool isHoldingClothes = false;
    private Color heldColor;

    public bool IsHoldingClothes => isHoldingClothes;
    public Color HeldColor => heldColor;
    public int RemainingClothesCount => physicalClothes.Count;
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
        Debug.Log($"[LaundryBasket] Spawning {count} pieces of clothing at once in the basket.");

        // Instancia as peças de roupa todas de uma vez no mesmo frame
        for (int i = 0; i < count; i++)
        {
            // Spawn simplificado: empilha na vertical exata e na mesma rotação (sem física ativa para não escorregar)
            Vector3 offset = new Vector3(0f, i * heightOffset, 0f);
            GameObject piece = Instantiate(clothesPrefab, spawnPoint.position + offset, spawnPoint.rotation);
            
            // Configura a layer para "Ignore Raycast" (Layer 2) para que o clique do jogador passe pelas roupas e atinja o cesto
            piece.layer = 2;
            
            // Desativa a física para manter a pilha estática e perfeita
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }

            // Atribui uma cor de material aleatória
            Renderer renderer = piece.GetComponentInChildren<Renderer>();
            Color randomColor = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);
            if (renderer != null)
            {
                renderer.material.color = randomColor;
            }

            physicalClothes.Add(piece);
            clothesColors.Add(randomColor);
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
            ShowFeedbackDialogue("The basket is empty.");
        }
    }

    public void ClearHeldState()
    {
        isHoldingClothes = false;
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
