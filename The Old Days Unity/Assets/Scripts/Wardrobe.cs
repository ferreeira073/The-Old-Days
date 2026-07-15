using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controla o armário/guarda-fatos no quarto. Trata de receber as roupas que o jogador
/// recolheu do cesto e verificar se a tarefa foi concluída.
/// </summary>
public class Wardrobe : MonoBehaviour, IInteractable
{
    [Header("Configurações do Armário")]
    [Tooltip("O prefab da peça de roupa a ser instanciado.")]
    [SerializeField] private GameObject clothesPrefab;

    [Tooltip("Ponto de spawn inicial onde a roupa será colocada no armário.")]
    [SerializeField] private Transform spawnPoint;

    private int placedCount = 0;
    private List<GameObject> placedClothes = new List<GameObject>();

    private void Start()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged += HandleDayChanged;
        }
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
        ClearPlacedClothes();
    }

    private void ClearPlacedClothes()
    {
        foreach (var piece in placedClothes)
        {
            if (piece != null) Destroy(piece);
        }
        placedClothes.Clear();
        placedCount = 0;
    }

    /// <summary>
    /// Chamado quando o jogador olha para o armário e pressiona o botão de interação.
    /// </summary>
    public void Interact(Transform playerTransform)
    {
        if (LaundryBasket.Instance == null)
        {
            Debug.LogError("[Wardrobe] ERRO: A instância do LaundryBasket não foi encontrada na cena!");
            return;
        }

        // 1. Verifica se o jogador está a carregar alguma roupa
        if (!LaundryBasket.Instance.IsHoldingClothes)
        {
            ShowFeedbackDialogue("This is the wardrobe. I need to bring clothes from the basket.");
            return;
        }

        // 2. Coloca a roupa no armário
        Color colorToApply = LaundryBasket.Instance.HeldColor;
        LaundryBasket.Instance.ClearHeldState();

        // Obtém o espaçamento vertical configurado no cesto para empilhar
        float spacing = LaundryBasket.Instance.HeightOffset;

        // Instancia no ponto de spawn do armário de forma estática (perfeita, sem física instável)
        Vector3 offset = new Vector3(0f, placedCount * spacing, 0f);
        GameObject placedPiece = Instantiate(clothesPrefab, spawnPoint.position + offset, spawnPoint.rotation);
        
        // Configura a layer para "Ignore Raycast" (Layer 2) para não interferir com cliques no armário
        placedPiece.layer = 2;

        // Desativa a física para manter a pilha perfeitamente ordenada no guarda-fatos
        Rigidbody rb = placedPiece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        Renderer renderer = placedPiece.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = colorToApply;
        }

        placedClothes.Add(placedPiece);
        placedCount++;
        ShowFeedbackDialogue("Placed the clothing item in the wardrobe.");

        // 3. Verifica se a tarefa foi completamente concluída
        CheckTaskCompletion();
    }

    private void CheckTaskCompletion()
    {
        if (TaskManager.Instance != null && LaundryBasket.Instance != null)
        {
            // Se o cesto está vazio de roupas físicas e o jogador não está a segurar nada, a tarefa está concluída!
            if (LaundryBasket.Instance.RemainingClothesCount == 0 && !LaundryBasket.Instance.IsHoldingClothes)
            {
                // Sincroniza o ID da tarefa directamente a partir da única fonte de verdade no LaundryBasket
                string targetTaskId = LaundryBasket.Instance.ClothesTaskId;
                
                Debug.Log($"[Wardrobe] All clothes placed. Completing task with ID: '{targetTaskId}'");
                TaskManager.Instance.CompleteTask(targetTaskId);
            }
        }
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
            Debug.Log($"[Wardrobe] {text}");
        }
    }
}
