using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource backgroundMusicSource;
    public AudioSource effectsSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float backgroundVolume = 1f;
    [Range(0f, 1f)] public float effectsVolume = 1f;

    [Header("UI Sliders")]
    public Slider backgroundVolumeSlider;
    public Slider effectsVolumeSlider;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ✅ KHÔNG dùng PlayerPrefs nữa → luôn mặc định 100%
        backgroundVolume = 1f;
        effectsVolume = 1f;

        if (effectsSource == null)
        {
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.playOnAwake = false;
            effectsSource.loop = false;
        }

        // ✅ Gán giá trị cho slider
        if (backgroundVolumeSlider != null)
        {
            backgroundVolumeSlider.value = backgroundVolume;
            backgroundVolumeSlider.onValueChanged.AddListener(SetBackgroundVolume);
        }

        if (effectsVolumeSlider != null)
        {
            effectsVolumeSlider.value = effectsVolume;
            effectsVolumeSlider.onValueChanged.AddListener(SetEffectsVolume);
        }

        // ✅ Áp dụng volume vào AudioSource
        UpdateVolume();

        // ✅ Phát nhạc nền từ đầu
        if (backgroundMusicSource != null && backgroundMusicSource.clip != null)
        {
            backgroundMusicSource.loop = true;
            backgroundMusicSource.time = 0f;
            backgroundMusicSource.volume = backgroundVolume;
            backgroundMusicSource.Play();
        }
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

    // NEW: dùng phát loop engine
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
        UpdateVolume();
    }

    public void SetEffectsVolume(float volume)
    {
        effectsVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("EffectsVolume", effectsVolume);
        UpdateVolume();
    }
}