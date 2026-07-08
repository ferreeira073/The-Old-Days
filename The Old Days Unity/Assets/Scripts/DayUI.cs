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

            TimeManager.Instance.OnWeekEnded -= DisplayEndOfWeek;
            TimeManager.Instance.OnWeekEnded += DisplayEndOfWeek;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnDayChanged -= UpdateDayText;
            TimeManager.Instance.OnWeekEnded -= DisplayEndOfWeek;
        }
    }

    private void UpdateDayText(int day)
    {
        if (dayText == null) return;
        
        string newText = $"Day {day} / {TimeManager.Instance.maxDays}";
        
        if (gameObject.activeInHierarchy && fadeDuration > 0f)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTextRoutine(newText));
        }
        else
        {
            dayText.text = newText;
        }
    }

    private void DisplayEndOfWeek()
    {
        if (dayText == null) return;
        
        string newText = "Week Completed";
        
        if (gameObject.activeInHierarchy && fadeDuration > 0f)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeTextRoutine(newText));
        }
        else
        {
            dayText.text = newText;
        }
    }

    private IEnumerator FadeTextRoutine(string targetText)
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

        // Atualiza o conteúdo do texto
        dayText.text = targetText;

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
