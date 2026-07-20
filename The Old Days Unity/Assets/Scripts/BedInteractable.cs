using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Permite ao jogador interagir com a cama para ir dormir e avançar o dia.
/// </summary>
public class BedInteractable : MonoBehaviour, IInteractable
{
    [Header("Configurações da Cama")]
    [Tooltip("Mensagem exibida na consola ao interagir.")]
    [SerializeField] private string sleepMessage = "A ir dormir... A avançar para o dia seguinte.";

    [Header("Configurações de Transição")]
    [Tooltip("Duração do zoom rápido em direção à cama (em segundos).")]
    [SerializeField] private float zoomDuration = 0.5f;

    [Tooltip("Field of View (FOV) alvo durante o zoom.")]
    [SerializeField] private float zoomFov = 25f;

    [Tooltip("Duração do fade-out (escurecer) e fade-in (clarear).")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Tooltip("Tempo de espera com o ecrã totalmente preto para simular o sono.")]
    [SerializeField] private float sleepWaitDuration = 1.0f;

    /// <summary>
    /// Chamado quando o jogador interage com a cama.
    /// </summary>
    /// <param name="playerTransform">O transform do jogador que iniciou a interação.</param>
    public void Interact(Transform playerTransform)
    {
        // 1. Valida se o jogador já encontrou a lista e completou todas as tarefas de hoje
        if (TaskManager.Instance != null)
        {
            if (!TaskManager.Instance.IsListFound)
            {
                ShowSleepDeniedDialogue("Não posso ir dormir ainda. Tenho de encontrar a minha lista de tarefas primeiro.");
                return;
            }

            if (!TaskManager.Instance.AreAllTasksCompleted())
            {
                ShowSleepDeniedDialogue("Ainda não completei todas as tarefas de hoje... Não posso ir dormir.");
                return;
            }
        }

        Debug.Log(sleepMessage);

        // Obtém o PlayerController e inicia o processo de transição do sono
        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player != null)
        {
            StartCoroutine(SleepSequenceRoutine(player));
        }
        else
        {
            // Fallback caso não seja possível aceder ao controlador
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.AdvanceDay();
            }
        }
    }

    private IEnumerator SleepSequenceRoutine(PlayerController player)
    {
        // 1. Bloqueia os controlos do jogador
        player.enabled = false;

        // 2. Garante que a instância do ScreenFader está ativa e criada na cena
        if (ScreenFader.Instance == null)
        {
            GameObject faderGo = new GameObject("ScreenFader");
            faderGo.AddComponent<ScreenFader>();
        }

        // 3. Obtém a câmara do jogador e inicia o zoom rápido
        Camera playerCamera = null;
        float originalFov = 60f;
        if (player.playerCamera != null)
        {
            playerCamera = player.playerCamera.GetComponent<Camera>();
        }

        if (playerCamera != null)
        {
            originalFov = playerCamera.fieldOfView;
            StartCoroutine(ZoomCameraRoutine(playerCamera, originalFov, zoomFov, zoomDuration));
        }

        // 4. Inicia o fade out para preto (completa a transição para o escuro)
        bool fadeCompleted = false;
        ScreenFader.Instance.FadeToBlack(fadeDuration, () => {
            fadeCompleted = true;
        });

        // Aguarda até o fade terminar de escurecer
        yield return new WaitUntil(() => fadeCompleted);

        // 5. O ecrã está agora totalmente preto. Avança o dia in-game.
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.AdvanceDay();
        }

        // 6. Repõe o FOV da câmara instantaneamente no escuro
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = originalFov;
        }

        // 7. Roda o jogador 180 graus para que acorde a olhar na direção oposta
        player.ResetCameraRotation(180f);

        // 8. Mantém o ecrã preto durante o tempo de sono simulado
        yield return new WaitForSeconds(sleepWaitDuration);

        // 9. Faz fade in de volta para revelar o jogo
        bool fadeClearCompleted = false;
        ScreenFader.Instance.FadeToClear(fadeDuration, () => {
            fadeClearCompleted = true;
        });

        // Aguarda a conclusão da revelação
        yield return new WaitUntil(() => fadeClearCompleted);

        // 10. Reativa os controlos do jogador
        player.enabled = true;
    }

    private IEnumerator ZoomCameraRoutine(Camera cam, float startFov, float targetFov, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cam.fieldOfView = Mathf.Lerp(startFov, targetFov, elapsed / duration);
            yield return null;
        }
        cam.fieldOfView = targetFov;
    }

    /// <summary>
    /// Exibe um monólogo do jogador indicando o motivo pelo qual não pode dormir.
    /// </summary>
    private void ShowSleepDeniedDialogue(string text)
    {
        if (GameDialogueManager.Instance != null)
        {
            GameDialogueManager.DialogueLine line = new GameDialogueManager.DialogueLine
            {
                speakerName = "Jogador",
                textLine = text
            };
            GameDialogueManager.Instance.ShowDialogue(new List<GameDialogueManager.DialogueLine> { line });
        }
        else
        {
            Debug.LogWarning($"[Cama] Diálogo bloqueado: {text}");
        }
    }
}
