using UnityEngine;

/// <summary>
/// Componente anexado dinamicamente a peças de roupa fora do cesto.
/// Permite ao jogador interagir diretamente com a roupa para a recolher.
/// </summary>
public class ClothingItem : MonoBehaviour, IInteractable
{
    private Color clothingColor;
    private LaundryBasket basketInstance;

    /// <summary>
    /// Configura a cor da roupa e a referência para o cesto principal.
    /// </summary>
    public void Setup(Color color, LaundryBasket basket)
    {
        clothingColor = color;
        basketInstance = basket;
    }

    /// <summary>
    /// Chamado quando o jogador interage com o objeto da roupa no ambiente.
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
        if (basketInstance != null && basketInstance.IsHoldingClothes)
        {
            ShowFeedbackDialogue("I am already carrying a piece of clothing. I should put it in the wardrobe.");
            return;
        }

        // 3. Recolhe a roupa fora do cesto
        if (basketInstance != null)
        {
            basketInstance.PickUpOutsideClothing(gameObject, clothingColor);
            ShowFeedbackDialogue("Picked up a piece of clothing. I need to place it in the wardrobe.");
        }
    }

    private void ShowFeedbackDialogue(string text)
    {
        if (GameDialogueManager.Instance != null)
        {
            var line = new GameDialogueManager.DialogueLine { speakerName = "Player", textLine = text };
            GameDialogueManager.Instance.ShowDialogue(new System.Collections.Generic.List<GameDialogueManager.DialogueLine> { line });
        }
        else
        {
            Debug.Log($"[ClothingItem] {text}");
        }
    }
}
