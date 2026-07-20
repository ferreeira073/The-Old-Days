using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Controla a exibição visual do dia atual no topo do ecrã com transições suaves.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class DayUI : MonoBehaviour
{
    private TextMeshProUGUI dayText;
    private CanvasGroup canvasGroup;

    [Header("Animação")]
    [Tooltip("Duração do efeito de fade-out e fade-in ao mudar de dia.")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("Segundos de espera antes de fechar o jogo após o fim da semana.")]
    [SerializeField] private float quitDelay = 3f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        dayText = GetComponent<TextMeshProUGUI>();
        
        // Tenta obter um CanvasGroup no próprio objeto ou nos pais.
        // Se não existir, adiciona um para podermos animar a opacidade do texto.
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void Start()
    {
        SubscribeToEvents();
        
        if (TimeManager.Instance != null)
        {
            UpdateDayText(TimeManager.Instance.CurrentDay);
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (TimeManager.Instance != null)
        {
            // Evita subscrições duplicadas desinscrevendo primeiro
            TimeManager.Instance.OnDayChanged -= UpdateDayText;
            TimeManager.Instance.OnDayChanged += UpdateDayText;

            TimeManager.Instance.OnWeekEnded -= DisplayWeekComplete;
            TimeManager.Instance.OnWeekEnded += DisplayWeekComplete;

            TimeManager.Instance.OnWeekFailed -= DisplayWeekFailed;
            TimeManager.Instance.OnWeekFailed += DisplayWeekFailed;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= UpdateDayText;
            TimeManager.Instance.OnWeekEnded -= DisplayWeekComplete;
            TimeManager.Instance.OnWeekFailed -= DisplayWeekFailed;
        }
    }

    private void UpdateDayText(int day)
    {
        if (dayText == null) return;
        
        string newText = $"Day {day} / {TimeManager.Instance.maxDays}";
        
        if (gameObject.activeInHierarchy && fadeDuration > 0f)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTextRoutine(newText, Color.white));
        }
        else
        {
            dayText.text = newText;
            dayText.color = Color.white;
        }
    }

    private void DisplayWeekComplete()
    {
        if (dayText == null) return;
        
        FreezePlayer();

        if (gameObject.activeInHierarchy && fadeDuration > 0f)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTextRoutine("Week Complete", Color.white));
        }
        else
        {
            dayText.text = "Week Complete";
            dayText.color = Color.white;
        }

        StartCoroutine(QuitAfterDelay());
    }

    private void DisplayWeekFailed()
    {
        if (dayText == null) return;

        FreezePlayer();

        if (gameObject.activeInHierarchy && fadeDuration > 0f)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTextRoutine("Week Failed", Color.red));
        }
        else
        {
            dayText.text = "Week Failed";
            dayText.color = Color.red;
        }

        StartCoroutine(QuitAfterDelay());
    }

    /// <summary>
    /// Bloqueia os controlos do jogador na cena atual.
    /// </summary>
    private void FreezePlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.IsControlEnabled = false;
        }

        // Liberta o cursor para não ficar preso
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Espera <see cref="quitDelay"/> segundos e depois fecha o jogo.
    /// </summary>
    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSeconds(quitDelay);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator FadeTextRoutine(string targetText, Color targetColor)
    {
        // Fade out (desaparece)
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Atualiza o conteúdo e a cor do texto
        dayText.text = targetText;
        dayText.color = targetColor;

        // Fade in (aparece)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
