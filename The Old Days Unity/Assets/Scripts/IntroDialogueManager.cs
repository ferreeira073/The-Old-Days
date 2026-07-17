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
    [SerializeField] private float charactersPerSecond = 120f;

    [Tooltip("Pausa exata (em segundos) numa vírgula.")]
    [SerializeField] private float commaPauseTime = 0.04f;

    [Tooltip("Pausa exata (em segundos) num ponto final, ! ou ?")]
    [SerializeField] private float punctuationPauseTime = 0.08f;

    [Header("Falas")]
    [SerializeField] private List<DialogueLine> dialogueLines;

    [Header("Tempo de Espera Inicial")]
    [Tooltip("Segundos de espera no ecrã preto antes de o diálogo começar.")]
    [SerializeField] private float blackScreenWaitTime = 2f;

    [Header("Música da Intro")]
    [Tooltip("Clip de áudio a tocar durante a intro (diferente da música do menu).")]
    [SerializeField] private AudioClip introMusicClip;

    [Tooltip("Volume da música da intro (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float introMusicVolume = 0.5f;

    [Tooltip("Duração do fade-in da música da intro (em segundos).")]
    [Range(0f, 5f)]
    [SerializeField] private float introMusicFadeIn = 1.5f;

    [Tooltip("Duração do fade-out da música da intro quando a cena carrega (em segundos).")]
    [Range(0f, 5f)]
    [SerializeField] private float introMusicFadeOut = 1.5f;

    private int currentLineIndex = 0;
    private string targetSceneName;
    private bool isTyping = false;
    private string activeText = "";
    private string lastSpeakerName = "";

    private Coroutine typingCoroutine;
    private bool introStarted = false;

    // AudioSource criado dinamicamente para a música da intro
    private AudioSource _introAudioSource;
    private Coroutine _introMusicFadeCoroutine;

    private void Start()
    {
        if (blackScreenPanel != null) blackScreenPanel.SetActive(false);
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (speakerNameText != null) speakerNameText.text = "";

        // Cria o AudioSource para a música da intro (separado do resto)
        _introAudioSource = gameObject.AddComponent<AudioSource>();
        _introAudioSource.loop        = true;
        _introAudioSource.playOnAwake = false;
        _introAudioSource.volume      = 0f;
        _introAudioSource.clip        = introMusicClip;
    }

    public void StartIntro(string sceneToLoad)
    {
        targetSceneName = sceneToLoad;
        introStarted = false; // Só ficará true depois da espera inicial
        currentLineIndex = 0;
        lastSpeakerName = "";

        if (blackScreenPanel != null) blackScreenPanel.SetActive(true);
        if (continueHintText != null) continueHintText.gameObject.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
        if (speakerNameText != null) speakerNameText.text = "";

        StartCoroutine(IntroSequence());
    }

    /// <summary>
    /// Sequência completa da intro:
    /// 1. Inicia música da intro com fade-in
    /// 2. Aguarda o tempo no ecrã preto
    /// 3. Começa o diálogo
    /// </summary>
    private IEnumerator IntroSequence()
    {
        // --- Música da intro ---
        if (introMusicClip != null)
        {
            _introAudioSource.clip   = introMusicClip;
            _introAudioSource.volume = 0f;
            _introAudioSource.Play();

            if (_introMusicFadeCoroutine != null) StopCoroutine(_introMusicFadeCoroutine);
            _introMusicFadeCoroutine = StartCoroutine(FadeIntroMusic(introMusicVolume, introMusicFadeIn));
        }

        // --- Tempo de espera no ecrã preto ---
        if (blackScreenWaitTime > 0f)
            yield return new WaitForSeconds(blackScreenWaitTime);

        // --- Arranca o diálogo ---
        introStarted = true;
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
        StartCoroutine(EndIntroWithFadeOut());
    }

    /// <summary>
    /// Faz fade-out da música da intro e depois carrega a cena.
    /// </summary>
    private IEnumerator EndIntroWithFadeOut()
    {
        if (introMusicClip != null && _introAudioSource.isPlaying && introMusicFadeOut > 0f)
        {
            if (_introMusicFadeCoroutine != null) StopCoroutine(_introMusicFadeCoroutine);
            _introMusicFadeCoroutine = StartCoroutine(FadeIntroMusic(0f, introMusicFadeOut));
            yield return new WaitForSeconds(introMusicFadeOut);
            _introAudioSource.Stop();
        }

        if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Fade da Música da Intro
    // ──────────────────────────────────────────────────────────────

    private IEnumerator FadeIntroMusic(float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            _introAudioSource.volume = targetVolume;
            yield break;
        }

        float startVolume = _introAudioSource.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _introAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        _introAudioSource.volume = targetVolume;
        _introMusicFadeCoroutine = null;
    }
}