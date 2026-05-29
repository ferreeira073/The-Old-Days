using UnityEngine;

/// <summary>
/// Script de exemplo para itens que o jogador pode apanhar ou com os quais pode interagir.
/// </summary>
public class Item : MonoBehaviour, IInteractable
{
    [Header("Configurações do Item")]
    [Tooltip("Nome do item (útil para debug ou lógica interna).")]
    public string itemName = "Item";

    /// <summary>
    /// Lógica executada quando o jogador interage com o item.
    /// </summary>
    public void Interact(Transform playerTransform)
    {
        Debug.Log($"Jogador interagiu com o item: {itemName}");
        
        // Espaço reservado para futura integração com inventário:
        // Inventory.Instance.AddItem(this);

        // Destrói o item da cena após a interação
        Destroy(gameObject);
    }
}
