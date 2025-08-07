using UnityEngine;

public class CarAudioManager : MonoBehaviour
{
    public Car_script car;
    private bool useAudioManager = true; // Kích hoạt nếu muốn dùng AudioManager

    [Header("Engine Sound")]
    public AudioClip engineClip;
    public float engineVolume = 0.7f;
    public float pitchMin = 0.8f;
    public float pitchMax = 1.5f;

    [Header("Boost Sound")]
    public AudioClip boostClip;
    public float boostVolume = 1f;
    public float minSpeedForBoostSound = 0.1f;
    public float boostCooldown = 1f;
    public float boostExtendTime = 1.2f;

    [Header("Brake Sound")]
    public AudioClip brakeClip;
    public float brakeVolume = 1f;
    public float minSpeedForBrakeSound = 40f;

    private AudioSource engineSource;
    private AudioSource boostSource;
    private AudioSource brakeSource;

    private float boostTimer = 0f;

    private bool wasBoosting = false;
    private bool wasHandBraking = false;

    void Start()
    {
        if (car == null)
            car = GetComponent<Car_script>();

        // Engine AudioSource
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.clip = engineClip;
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.volume = engineVolume;
        engineSource.spatialBlend = 0f;

        if (engineClip != null)
            engineSource.Play();

        // Boost AudioSource
        boostSource = gameObject.AddComponent<AudioSource>();
        boostSource.clip = boostClip;
        boostSource.playOnAwake = false;
        boostSource.loop = false;
        boostSource.volume = boostVolume;
        boostSource.spatialBlend = 0f;

        // Brake AudioSource
        brakeSource = gameObject.AddComponent<AudioSource>();
        brakeSource.clip = brakeClip;
        brakeSource.playOnAwake = false;
        brakeSource.loop = true;
        brakeSource.volume = brakeVolume;
        brakeSource.spatialBlend = 0f;
    }

    void Update()
    {
        if (car == null) return;

        UpdateEngineSound();

        // Boost Sound
        if (car.isBoosting && !wasBoosting &&
            boostClip != null &&
            car.carSpeedConverted > minSpeedForBoostSound &&
            boostTimer <= 0f)
        {
            if (useAudioManager && Audio_Thanh_pho.Instance != null)
                Audio_Thanh_pho.Instance.PlayEffect(boostClip);

            boostTimer = boostCooldown;
        }

        boostTimer -= Time.deltaTime;

        // Brake Drift Sound
        bool isDrifting = car.handBrakeEffects && car.carSpeedConverted > minSpeedForBrakeSound;

        if (useAudioManager && Audio_Thanh_pho.Instance != null)
            Audio_Thanh_pho.Instance.PlayEffect(brakeClip);

        else
        {
            if (brakeSource.isPlaying)
                brakeSource.Stop();
        }

        wasBoosting = car.isBoosting;
        wasHandBraking = car.handBrakeEffects;
        if (useAudioManager && Audio_Thanh_pho.Instance != null)
        {
            float speedRatio = Mathf.Clamp01(car.carSpeedConverted / car.maximumSpeed);
            float pitch = Mathf.Lerp(pitchMin, pitchMax, speedRatio);

            // Dùng AudioManager phát engine loop
            Audio_Thanh_pho.Instance.PlayLoopingEngine(engineSource, engineClip, pitch);

            // Phát boost sound bằng AudioManager song song boostSource
            if (car.isBoosting && !wasBoosting &&
                boostClip != null &&
                car.carSpeedConverted > minSpeedForBoostSound &&
                boostTimer <= 0f)
            {
                Audio_Thanh_pho.Instance.PlayEffect(boostClip);
            }

            // Phát brake sound bằng AudioManager nếu drifting
            if (car.handBrakeEffects && car.carSpeedConverted > minSpeedForBrakeSound && !wasHandBraking)
            {
                Audio_Thanh_pho.Instance.PlayEffect(brakeClip);
            }
        }

    }

    void UpdateEngineSound()
    {
        float speedRatio = Mathf.Clamp01(car.carSpeedConverted / car.maximumSpeed);
        engineSource.pitch = Mathf.Lerp(pitchMin, pitchMax, speedRatio);
        engineSource.volume = Mathf.Max(0.4f, Mathf.Lerp(0.2f, engineVolume, speedRatio));
    }
}
