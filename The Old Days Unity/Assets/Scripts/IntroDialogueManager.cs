using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Controla um diálogo de introdução com um sistema de texto hiper-responsivo,
/// sem bloqueios de frames, com o verdadeiro passo de jogos como Undertale.
/// </summary>
public class IntroDialogueManager : MonoBehaviour
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string speakerName;

        [TextArea(3, 5)]
        public string textLine;
    }

    [Header("Elementos de UI")]
    [SerializeField] private GameObject blackScreenPanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text continueHintText;

    [Header("Configurações de Velocidade (Estilo Undertale)")]
    
    [Tooltip("Letras geradas por segundo. Undertale roda entre 30 a 60. Se meteres 90+, é incrivelmente rápido.")]
    [SerializeField] private float charactersPerSecond = 60f; 

    [Tooltip("Pausa exata (em segundos) numa vírgula.")]
    [SerializeField] private float commaPauseTime = 0.1f; 

    [Tooltip("Pausa exata (em segundos) num ponto final, ! ou ?")]
    [SerializeField] private float punctuationPauseTime = 0.25f;

    [Header("Falas")]
    [SerializeField] private List<DialogueLine> dialogueLines;

    private int currentLineIndex = 0;
    private string targetSceneName;
    private bool isTyping = false;
    private string activeText = "";
    private string lastSpeakerName = ""; 
    
    private Coroutine typingCoroutine;
    private bool introStarted = false;

    private void Start()
    {
        if (blackScreenPanel != null) blackScreenPanel.SetActive(false);
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (speakerNameText != null) speakerNameText.text = "";
    }

    public void StartIntro(string sceneToLoad)
    {
        targetSceneName = sceneToLoad;
        introStarted = true;
        currentLineIndex = 0;
        lastSpeakerName = "";

        if (blackScreenPanel != null) blackScreenPanel.SetActive(true);
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);

        // O texto arranca SEM esperas no ecrã preto. Rápido e direto.
        DisplayNextLine();
    }

    private void Update()
    {
        if (!introStarted) return;

        bool advancePressed = false;

        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            advancePressed = true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            advancePressed = true;
        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            advancePressed = true;

        if (advancePressed)
        {
            if (isTyping) CompleteLineInstantly();
            else DisplayNextLine();
        }
    }

    private void DisplayNextLine()
    {
        if (dialogueLines == null || currentLineIndex >= dialogueLines.Count)
        {
            EndIntro();
            return;
        }

        DialogueLine line = dialogueLines[currentLineIndex];
        activeText = line.textLine;

        if (speakerNameText != null)
        {
            // O nome aparece instantaneamente, sem fades demorados a empatar
            speakerNameText.text = string.IsNullOrEmpty(line.speakerName) ? "" : line.speakerName;
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(activeText));

        currentLineIndex++;
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);

        float timePerChar = 1f / charactersPerSecond; // Quanto tempo demora CADA letra
        float timer = 0f;
        int charIndex = 0;
        char[] chars = text.ToCharArray();

        // O loop corre a cada frame, garantindo a fluidez máxima
        while (charIndex < chars.Length)
        {
            timer += Time.deltaTime;

            // Se o jogo encravar ou for super rápido, isto imprime as letras todas necessárias num só frame
            while (timer >= timePerChar && charIndex < chars.Length)
            {
                char letter = chars[charIndex];
                dialogueText.text += letter;
                
                // Retira o tempo que esta letra demorou do acumulador
                timer -= timePerChar;

                // --- Lógica de Reticências ---
                bool isEllipsis = false;
                if (letter == '.')
                {
                    bool nextIsDot = (charIndex < chars.Length - 1 && chars[charIndex + 1] == '.');
                    bool prevIsDot = (charIndex > 0 && chars[charIndex - 1] == '.');
                    if (nextIsDot || prevIsDot) isEllipsis = true;
                }

                // --- Pausas de Pontuação ---
                // Retiramos tempo do timer. Assim o código vai ser obrigado a esperar uns frames até o timer voltar a ficar positivo.
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

                // Opcional: Aqui é onde em Undertale tocaria o som (AudioSource.PlayOneShot) a cada letra!

                charIndex++;
            }

            yield return null; // Espera estritamente apenas 1 frame
        }

        isTyping = false;
        if (continueHintText != null) continueHintText.gameObject.SetActive(true);
    }

    private void CompleteLineInstantly()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.text = activeText;
        isTyping = false;
        if (continueHintText != null) continueHintText.gameObject.SetActive(true);
    }

    private void EndIntro()
    {
        introStarted = false;
        if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}