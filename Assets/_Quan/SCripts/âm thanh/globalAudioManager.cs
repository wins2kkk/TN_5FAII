using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class globalAudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("UI")]
    public Slider soundSlider;
    public Slider musicSlider;

    [Header("Audio")]
    public AudioMixer audioMixer;
    public AudioSource musicSource;
    public AudioClip menuMusic;
    public AudioClip gameMusic;

    [Header("UI Panels")]
    public GameObject settingsPanel;
    public GameObject mainPanel;

    private float volumeStep = 0.05f;
    private enum ControlTarget { Sound, Music }
    private ControlTarget currentControl = ControlTarget.Sound;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
       // Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Load saved volumes
        float savedSound = PlayerPrefs.GetFloat("SoundVolume", 0.75f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);

        soundSlider.value = savedSound;
        musicSlider.value = savedMusic;

        SetSoundVolume(savedSound);
        SetMusicVolume(savedMusic);

        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) SwitchControlTarget();
        if (Input.GetKeyDown(KeyCode.F2)) AdjustVolume(-volumeStep);
        if (Input.GetKeyDown(KeyCode.F3)) AdjustVolume(volumeStep);
    }

    void SwitchControlTarget()
    {
        currentControl = (currentControl == ControlTarget.Sound) ? ControlTarget.Music : ControlTarget.Sound;
        Debug.Log("Switched to " + currentControl.ToString().ToUpper() + " control");
    }

    void AdjustVolume(float delta)
    {
        if (currentControl == ControlTarget.Sound)
        {
            soundSlider.value = Mathf.Clamp01(soundSlider.value + delta);
            SetSoundVolume(soundSlider.value);
        }
        else
        {
            musicSlider.value = Mathf.Clamp01(musicSlider.value + delta);
            SetMusicVolume(musicSlider.value);
        }
    }

    public void SetSoundVolume(float value)
    {
        audioMixer.SetFloat("SoundVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20);
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat("SoundVolume", soundSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.Save();

        CloseSettings();
    }

    public void CancelSettings()
    {
        float savedSound = PlayerPrefs.GetFloat("SoundVolume", 0.75f);
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.75f);

        soundSlider.value = savedSound;
        musicSlider.value = savedMusic;

        SetSoundVolume(savedSound);
        SetMusicVolume(savedMusic);

        CloseSettings();
    }

    void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    void PlayMusicForScene(string sceneName)
    {
        AudioClip clip = null;

        if (sceneName == "MenuScene") // Đổi tên theo scene menu thật của bạn
            clip = menuMusic;
        else if (sceneName == "GameScene") // Đổi theo scene game thật
            clip = gameMusic;

        if (clip != null && musicSource.clip != clip)
        {
            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
        }
    }
}
