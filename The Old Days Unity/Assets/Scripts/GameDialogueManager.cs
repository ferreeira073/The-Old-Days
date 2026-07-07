using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Controla o monólogo ou diálogo automático que ocorre no início da cena do jogo.
/// Permite ao jogador mover-se livremente e ler o texto que passa de forma automática (Undertale style).
/// </summary>
public class GameDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueLine
    {
        [Tooltip("Nome de quem está a falar (ex: 'Jogador'). Deixe vazio se não quiser mostrar o nome.")]
        public string speakerName;

        [TextArea(3, 5)]
        [Tooltip("Linha de texto a ser exibida.")]
        public string textLine;
    }

    [Header("Elementos de UI (Legenda/Subtítulo)")]
    [Tooltip("O painel que contém a caixa de diálogo.")]
    [SerializeField] private GameObject dialoguePanel;

    [Tooltip("Texto do nome do orador.")]
    [SerializeField] private TMP_Text speakerNameText;

    [Tooltip("Texto da fala/monólogo.")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("Configurações de Velocidade de Escrita (Undertale Style)")]
    [Tooltip("Letras geradas por segundo.")]
    [SerializeField] private float charactersPerSecond = 45f;

    [Tooltip("Pausa em segundos numa vírgula.")]
    [SerializeField] private float commaPauseTime = 0.15f;

    [Tooltip("Pausa em segundos num ponto final, ponto de exclamação ou interrogação.")]
    [SerializeField] private float punctuationPauseTime = 0.35f;

    [Header("Configurações de Exibição")]
    [Tooltip("Tempo em segundos que cada linha fica visível após terminar de ser escrita antes de avançar automaticamente.")]
    [SerializeField] private float displayDurationAfterTyping = 2.5f;

    [Header("Falas")]
    [SerializeField] private List<DialogueLine> dialogueLines;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string activeText = "";
    private Coroutine dialogueCoroutine;

    private void Start()
    {
        // Garante que a UI de diálogo começa ativa ou é limpa
        if (dialogueText != null) dialogueText.text = "";
        if (speakerNameText != null) speakerNameText.text = "";

        if (dialogueLines != null && dialogueLines.Count > 0)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            dialogueCoroutine = StartCoroutine(PlayDialogueRoutine());
        }
        else
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            Debug.LogWarning("[GameDialogueManager] Nenhuma linha de diálogo configurada no Inspector.");
        }
    }

    private IEnumerator PlayDialogueRoutine()
    {
        currentLineIndex = 0;

        while (currentLineIndex < dialogueLines.Count)
        {
            DialogueLine line = dialogueLines[currentLineIndex];
            activeText = line.textLine;

            // Define o nome do orador
            if (speakerNameText != null)
            {
                speakerNameText.text = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;
            }

            // Escreve o texto com máquina de escrever
            yield return StartCoroutine(TypeText(activeText));

            // Espera o tempo de leitura configurado antes de avançar automaticamente
            yield return new WaitForSeconds(displayDurationAfterTyping);

            currentLineIndex++;
        }

        // Fim do diálogo: oculta a UI
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        Debug.Log("[GameDialogueManager] Diálogo de início de jogo concluído com sucesso.");
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        float timePerChar = 1f / charactersPerSecond;
        float timer = 0f;
        int charIndex = 0;
        char[] chars = text.ToCharArray();

        while (charIndex < chars.Length)
        {
            timer += Time.deltaTime;

            while (timer >= timePerChar && charIndex < chars.Length)
            {
                char letter = chars[charIndex];
                dialogueText.text += letter;
                timer -= timePerChar;

                // Identifica se faz parte de reticências (...) para evitar pausas longas entre pontos consecutivos
                bool isEllipsis = false;
                if (letter == '.')
                {
                    bool nextIsDot = (charIndex < chars.Length - 1 && chars[charIndex + 1] == '.');
                    bool prevIsDot = (charIndex > 0 && chars[charIndex - 1] == '.');
                    if (nextIsDot || prevIsDot) isEllipsis = true;
                }

                // Pausas dramáticas com base na pontuação
                if (!isEllipsis)
                {
                    if (letter == '.' || letter == '!' || letter == '?')
                    {
                        timer -= punctuationPauseTime;
                    }
                    else if (letter == ',')
                    {
                        timer -= commaPauseTime;
                    }
                }

                charIndex++;
            }

            yield return null;
        }

        isTyping = false;
    }
}
