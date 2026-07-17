using System.Collections;
using UnityEngine;

/// <summary>
/// Gere a música de fundo do Menu Principal.
/// Coloca este script num GameObject do Menu Principal que tenha um componente AudioSource.
/// Suporta fade-in ao iniciar e fade-out antes de carregar a cena do jogo.
/// O volume é guardado e restaurado via PlayerPrefs.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MainMenuMusic : MonoBehaviour
{
    [Header("Música de Fundo")]
    [Tooltip("Clip de áudio a tocar no Menu Principal.")]
    public AudioClip musicClip;

    [Header("Configurações de Fade")]
    [Tooltip("Duração do fade-in ao iniciar (em segundos). Coloca 0 para sem fade.")]
    [Range(0f, 5f)]
    public float fadeInDuration = 2f;

    [Tooltip("Duração do fade-out ao sair do menu (em segundos). Coloca 0 para sem fade.")]
    [Range(0f, 5f)]
    public float fadeOutDuration = 1.5f;

    [Header("Volume")]
    [Tooltip("Volume máximo da música (0 a 1). Será substituído pelo valor guardado em PlayerPrefs se existir.")]
    [Range(0f, 1f)]
    public float defaultVolume = 0.5f;

    // Referência interna ao AudioSource
    private AudioSource _audioSource;

    // Coroutine de fade actualmente a correr (para cancelar se necessário)
    private Coroutine _currentFade;

    // Chave usada para guardar o volume nas PlayerPrefs (a mesma usada em MainMenu.cs)
    private const string VolumeKey = "MasterVolume";

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        // Configura o AudioSource para música de fundo
        _audioSource.clip        = musicClip;
        _audioSource.loop        = true;
        _audioSource.playOnAwake = false;
        _audioSource.volume      = 0f; // Começa a 0 para o fade-in
    }

    private void Start()
    {
        if (musicClip == null)
        {
            Debug.LogWarning("[MainMenuMusic] Nenhum AudioClip atribuído no Inspector. " +
                             "Arrasta a tua música para o campo 'Music Clip'.");
            return;
        }

        // Restaura o volume guardado (ou usa o volume padrão)
        float savedVolume = PlayerPrefs.HasKey(VolumeKey)
            ? PlayerPrefs.GetFloat(VolumeKey)
            : defaultVolume;

        _audioSource.Play();

        // Inicia o fade-in
        if (fadeInDuration > 0f)
        {
            _currentFade = StartCoroutine(FadeTo(savedVolume, fadeInDuration));
        }
        else
        {
            _audioSource.volume = savedVolume;
        }
    }

    /// <summary>
    /// Para a música com um fade-out suave.
    /// Chama este método antes de carregar a cena do jogo.
    /// Devolve a duração do fade para que o chamador possa esperar se necessário.
    /// </summary>
    public float StopWithFadeOut()
    {
        if (_currentFade != null)
            StopCoroutine(_currentFade);

        if (fadeOutDuration > 0f)
        {
            _currentFade = StartCoroutine(FadeToAndStop(0f, fadeOutDuration));
            return fadeOutDuration;
        }

        _audioSource.Stop();
        return 0f;
    }

    /// <summary>
    /// Atualiza o volume da música em tempo real.
    /// Pode ser ligado ao mesmo Slider que o MainMenu.SetVolume().
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        // Cancela qualquer fade em curso para não sobrescrever o valor do slider
        if (_currentFade != null)
        {
            StopCoroutine(_currentFade);
            _currentFade = null;
        }

        _audioSource.volume = Mathf.Clamp01(volume);
    }

    // ──────────────────────────────────────────────────────────────
    // Coroutines de Fade
    // ──────────────────────────────────────────────────────────────

    /// <summary>Interpola o volume do AudioSource até 'targetVolume' ao longo de 'duration' segundos.</summary>
    private IEnumerator FadeTo(float targetVolume, float duration)
    {
        float startVolume = _audioSource.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        _audioSource.volume = targetVolume;
        _currentFade = null;
    }

    /// <summary>Interpola o volume até 0 e depois para o AudioSource.</summary>
    private IEnumerator FadeToAndStop(float targetVolume, float duration)
    {
        yield return FadeTo(targetVolume, duration);
        _audioSource.Stop();
        _currentFade = null;
    }
}
