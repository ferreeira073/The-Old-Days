using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gere e exibe transições suaves de fade-in e fade-out pretas no ecrã.
/// Cria os elementos de UI dinamicamente no arranque para evitar dependências manuais na cena.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private Canvas faderCanvas;
    private CanvasGroup canvasGroup;
    private Image faderImage;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFader();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Configura e instancia programmaticamente o Canvas, Imagem e CanvasGroup do Fader.
    /// </summary>
    private void InitializeFader()
    {
        // 1. Cria o GameObject do Canvas
        GameObject canvasGo = new GameObject("ScreenFaderCanvas");
        canvasGo.transform.SetParent(this.transform);
        
        faderCanvas = canvasGo.AddComponent<Canvas>();
        faderCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        faderCanvas.sortingOrder = 9999; // Mantém no topo de toda a UI
        
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. Cria o GameObject da Imagem de fundo
        GameObject imageGo = new GameObject("FaderImage");
        imageGo.transform.SetParent(canvasGo.transform);

        faderImage = imageGo.AddComponent<Image>();
        faderImage.color = Color.black;

        // Estica a imagem para ocupar todo o ecrã
        RectTransform rectTransform = faderImage.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 3. Adiciona o CanvasGroup para controlo suave da opacidade
        canvasGroup = canvasGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; // Começa invisível
        canvasGroup.blocksRaycasts = false; // Não bloqueia interações quando invisível
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// Inicia uma transição suave para escurecer o ecrã (Preto).
    /// </summary>
    /// <param name="duration">Duração do fade em segundos.</param>
    /// <param name="onComplete">Callback executado ao terminar a transição.</param>
    public void FadeToBlack(float duration, Action onComplete = null)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        currentFadeCoroutine = StartCoroutine(FadeRoutine(1f, duration, true, onComplete));
    }

    /// <summary>
    /// Inicia uma transição suave para clarear o ecrã (Invisível).
    /// </summary>
    /// <param name="duration">Duração do fade em segundos.</param>
    /// <param name="onComplete">Callback executado ao terminar a transição.</param>
    public void FadeToClear(float duration, Action onComplete = null)
    {
        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }
        currentFadeCoroutine = StartCoroutine(FadeRoutine(0f, duration, false, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, bool blockRaycasts, Action onComplete)
    {
        // Bloqueia cliques e raycasts se estivermos a transitar para preto ou totalmente pretos
        canvasGroup.blocksRaycasts = blockRaycasts;
        canvasGroup.interactable = blockRaycasts;

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = targetAlpha;
        }

        currentFadeCoroutine = null;
        onComplete?.Invoke();
    }
}
