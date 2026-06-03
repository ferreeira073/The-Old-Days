using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controla o comportamento do Menu Principal (Main Menu).
/// Permite iniciar o jogo, alternar o painel de Opções e sair do jogo.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Configurações de Cena")]
    [Tooltip("Nome da cena do jogo a carregar quando o botão Play é premido.")]
    public string gameSceneName = "Scene";

    [Header("Painéis de UI (Canvas)")]
    [Tooltip("Painel de Opções (ex: volume, sensibilidade do rato, controlos).")]
    public GameObject optionsPanel;

    private void Awake()
    {
        Debug.Log($"[MainMenu] Script MainMenu carregado no GameObject '{gameObject.name}' com sucesso!");
    }

    private void Start()
    {
        // Garante que o cursor do rato está visível e desbloqueado no Menu Principal
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Garante que o painel de opções começa desativado
        if (optionsPanel != null)
        {
            // Avisa no console se o utilizador associou um botão no campo do painel por engano
            if (optionsPanel.GetComponent<UnityEngine.UI.Button>() != null)
            {
                Debug.LogError($"[MainMenu] ERRO: Associou o botão '{optionsPanel.name}' no campo 'Options Panel'. Deve associar o Painel de Opções (a janela que contém as configurações), não o botão em si!");
            }

            optionsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Carrega a cena do jogo.
    /// </summary>
    public void PlayGame()
    {
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogError($"[MainMenu] ERRO: Não foi possível carregar a cena '{gameSceneName}'.\n" +
                               $"1. Vá a 'File' -> 'Build Settings' no menu da Unity.\n" +
                               $"2. Arraste a cena '{gameSceneName}.unity' (da pasta Assets/Scenes) para a janela 'Scenes In Build'.");
            }
        }
        else
        {
            Debug.LogWarning("[MainMenu] O nome da cena do jogo não foi definido no MainMenu!");
        }
    }

    /// <summary>
    /// Abre ou fecha o painel de Opções (Alternadamente).
    /// </summary>
    public void ToggleOptions()
    {
        if (optionsPanel == null)
        {
            Debug.LogWarning("[MainMenu] O painel de Opções (Options Panel) não está associado no Inspector.");
            return;
        }

        // Alterna o estado ativo do painel (se está ativo, desativa; se está desativado, ativa)
        optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    /// <summary>
    /// Fecha o jogo.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("A fechar o jogo...");

        #if UNITY_EDITOR
        // Se estivermos a correr o jogo no editor da Unity, paramos a simulação
        EditorApplication.isPlaying = false;
        #else
        // Se for uma build real do jogo, fecha a aplicação
        Application.Quit();
        #endif
    }

    #region Métodos de Opções Úteis (Opcional para ligar a Sliders/Toggles/Dropdowns no painel de Opções)

    /// <summary>
    /// Ajusta o volume geral do jogo. Pode ser ligado a um Slider.
    /// </summary>
    /// <param name="volume">Valor entre 0.0f e 1.0f</param>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Define o estado de ecrã inteiro. Pode ser ligado a um Toggle.
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    /// <summary>
    /// Ajusta a qualidade gráfica do jogo. Pode ser ligado a um Dropdown.
    /// </summary>
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    #endregion
}
