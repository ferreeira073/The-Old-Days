using UnityEngine;

/// <summary>
/// Interface para todos os objetos interativos no jogo (portas, itens, etc.).
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Chamado quando o jogador interage com o objeto.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador que iniciou a interação.</param>
    void Interact(Transform playerTransform);
}
