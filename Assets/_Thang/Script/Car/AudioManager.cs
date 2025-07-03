using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource backgroundMusicSource;
    public AudioSource effectsSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float backgroundVolume = 1f;
    [Range(0f, 1f)] public float effectsVolume = 1f;

    [Header("UI Elements - Sẽ được tự động tìm kiếm")]
    public Slider backgroundVolumeSlider;
    public Slider effectsVolumeSlider;
    public GameObject settingsPanel;
    public Button showSettingsButton;
    public Button closeSettingsButton;

    private bool isSettingsPanelVisible = false;
    private bool hasInitializedCurrentScene = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Ép set volume = 1.0f mỗi khi mở game
            PlayerPrefs.SetFloat("BackgroundVolume", 1.0f);
            PlayerPrefs.SetFloat("EffectsVolume", 1.0f);
            PlayerPrefs.Save();

            InitializeAudioManager();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        SetupCurrentScene();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");
        hasInitializedCurrentScene = false;
        Invoke(nameof(SetupCurrentScene), 0.1f);
    }

    void SetupCurrentScene()
    {
        if (hasInitializedCurrentScene) return;

        ClearUIReferences();
        FindAndAssignUIElements();
        SetupUI();

        hasInitializedCurrentScene = true;
    }

    void ClearUIReferences()
    {
        backgroundVolumeSlider = null;
        effectsVolumeSlider = null;
        settingsPanel = null;
        showSettingsButton = null;
        closeSettingsButton = null;
    }

    void InitializeAudioManager()
    {
        backgroundVolume = PlayerPrefs.GetFloat("BackgroundVolume", 1.0f); // Luôn là 1.0f
        effectsVolume = PlayerPrefs.GetFloat("EffectsVolume", 1.0f);       // Luôn là 1.0f

        // Áp dụng volume
        UpdateVolume();

        // Phát nhạc nền nếu có
        if (backgroundMusicSource != null && backgroundMusicSource.clip != null)
        {
            backgroundMusicSource.loop = true;
            backgroundMusicSource.time = 0f;
            backgroundMusicSource.volume = backgroundVolume;

            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }
        }
    }


    void FindAndAssignUIElements()
    {
        settingsPanel = FindObjectByName("SettingsPanel");

        GameObject buttonObj = FindObjectByName("ShowSettingsButton") ?? FindObjectByName("SettingsButton");
        if (buttonObj != null) showSettingsButton = buttonObj.GetComponent<Button>();

        GameObject closeButtonObj = FindObjectByName("CloseSettingsButton");
        if (closeButtonObj != null) closeSettingsButton = closeButtonObj.GetComponent<Button>();

        GameObject bgSliderObj = FindObjectByName("BackgroundVolumeSlider");
        if (bgSliderObj != null) backgroundVolumeSlider = bgSliderObj.GetComponent<Slider>();

        GameObject fxSliderObj = FindObjectByName("EffectsVolumeSlider");
        if (fxSliderObj != null) effectsVolumeSlider = fxSliderObj.GetComponent<Slider>();
    }

    GameObject FindObjectByName(string name)
    {
        GameObject found = GameObject.Find(name);
        if (found != null) return found;

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name && obj.scene.IsValid())
            {
                return obj;
            }
        }

        return null;
    }

    void SetupUI()
    {
        if (backgroundVolumeSlider != null)
        {
            backgroundVolumeSlider.value = backgroundVolume;
            backgroundVolumeSlider.onValueChanged.RemoveAllListeners();
            backgroundVolumeSlider.onValueChanged.AddListener(SetBackgroundVolume);
        }

        if (effectsVolumeSlider != null)
        {
            effectsVolumeSlider.value = effectsVolume;
            effectsVolumeSlider.onValueChanged.RemoveAllListeners();
            effectsVolumeSlider.onValueChanged.AddListener(SetEffectsVolume);
        }

        if (showSettingsButton != null)
        {
            showSettingsButton.onClick.RemoveAllListeners();
            showSettingsButton.onClick.AddListener(ShowSettingsPanel);
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(HideSettingsPanel);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsPanelVisible = false;
        }

        if (backgroundMusicSource != null && backgroundMusicSource.clip != null)
        {
            if (!backgroundMusicSource.isPlaying)
            {
                backgroundMusicSource.Play();
            }
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            isSettingsPanelVisible = !isSettingsPanelVisible;
            settingsPanel.SetActive(isSettingsPanelVisible);
            SaveSettingsState();
        }
    }

    public void ShowSettingsPanel()
    {
        if (settingsPanel != null)
        {
            isSettingsPanelVisible = true;
            settingsPanel.SetActive(true);
            SaveSettingsState();
        }
    }

    public void HideSettingsPanel()
    {
        if (settingsPanel != null)
        {
            isSettingsPanelVisible = false;
            settingsPanel.SetActive(false);
            SaveSettingsState();
        }
    }

    void SaveSettingsState()
    {
        PlayerPrefs.SetFloat("BackgroundVolume", backgroundVolume);
        PlayerPrefs.SetFloat("EffectsVolume", effectsVolume);
        PlayerPrefs.Save();
    }

    public void UpdateVolume()
    {
        if (backgroundMusicSource != null)
            backgroundMusicSource.volume = backgroundVolume;
        if (effectsSource != null)
            effectsSource.volume = effectsVolume;
    }

    public void PlayEffect(AudioClip clip, float pitch = 1f)
    {
        if (clip == null || effectsSource == null) return;
        effectsSource.pitch = pitch;
        effectsSource.PlayOneShot(clip, effectsVolume);
    }

    public void PlayLoopingEngine(AudioSource engineSource, AudioClip clip, float pitch)
    {
        if (engineSource == null || clip == null) return;
        if (!engineSource.isPlaying || engineSource.clip != clip)
        {
            engineSource.clip = clip;
            engineSource.loop = true;
            engineSource.Play();
        }
        engineSource.volume = effectsVolume;
        engineSource.pitch = pitch;
    }

    public void SetBackgroundVolume(float volume)
    {
        backgroundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("BackgroundVolume", backgroundVolume);
        PlayerPrefs.Save();
        UpdateVolume();
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("EffectsVolume", effectsVolume);
        PlayerPrefs.Save();
        UpdateVolume();
    }

    public void RefreshUI()
    {
        hasInitializedCurrentScene = false;
        SetupCurrentScene();
    }

    public void ForceRefreshUI()
    {
        ClearUIReferences();
        FindAndAssignUIElements();
        SetupUI();
    }

    public void ResetToDefault()
    {
        backgroundVolume = 1.0f;
        effectsVolume = 1.0f;
        isSettingsPanelVisible = false;

        PlayerPrefs.SetFloat("BackgroundVolume", backgroundVolume);
        PlayerPrefs.SetFloat("EffectsVolume", effectsVolume);
        PlayerPrefs.Save();

        RefreshUI();
        UpdateVolume();
    }

    [ContextMenu("Debug UI Status")]
    public void DebugUIStatus()
    {
        Debug.Log($"=== AudioManager UI Status ===");
        Debug.Log($"Settings Panel: {(settingsPanel != null ? "Found" : "Missing")}");
        Debug.Log($"Show Settings Button: {(showSettingsButton != null ? "Found" : "Missing")}");
        Debug.Log($"Close Settings Button: {(closeSettingsButton != null ? "Found" : "Missing")}");
        Debug.Log($"Background Slider: {(backgroundVolumeSlider != null ? "Found" : "Missing")}");
        Debug.Log($"Effects Slider: {(effectsVolumeSlider != null ? "Found" : "Missing")}");
        Debug.Log($"Scene Initialized: {hasInitializedCurrentScene}");
    }
    ///ổn
}
