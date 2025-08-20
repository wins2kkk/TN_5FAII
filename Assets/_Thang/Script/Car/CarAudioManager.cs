using System.Collections;
using UnityEngine;

public class CarAudioManager : MonoBehaviour
{
    public Car_script car;
    private bool useAudioManager = false; // Dùng AudioSource trực tiếp nhưng sync volume với AudioManager

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

        // Debug AudioClips
        Debug.Log($"=== CarAudioManager Start Debug ===");
        Debug.Log($"Engine Clip: {(engineClip != null ? engineClip.name : "NULL")}");
        Debug.Log($"Boost Clip: {(boostClip != null ? boostClip.name : "NULL")}");
        Debug.Log($"Brake Clip: {(brakeClip != null ? brakeClip.name : "NULL")}");
        Debug.Log($"Use AudioManager: {useAudioManager}");
        Debug.Log($"AudioManager Instance: {(AudioManager.Instance != null ? "EXISTS" : "NULL")}");

        if (AudioManager.Instance != null)
        {
            Debug.Log($"AudioManager Effects Volume: {AudioManager.Instance.effectsVolume}");
            Debug.Log($"AudioManager Effects Source: {(AudioManager.Instance.effectsSource != null ? "EXISTS" : "NULL")}");
        }

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
        boostSource.outputAudioMixerGroup = null; // Đảm bảo output = Master

        // Brake AudioSource
        brakeSource = gameObject.AddComponent<AudioSource>();
        brakeSource.clip = brakeClip;
        brakeSource.playOnAwake = false;
        brakeSource.loop = true;
        brakeSource.volume = brakeVolume;
        brakeSource.spatialBlend = 0f;
        brakeSource.outputAudioMixerGroup = null; // Đảm bảo output = Master

        Debug.Log($"AudioSources created - Engine: {engineSource != null}, Boost: {boostSource != null}, Brake: {brakeSource != null}");

        // TEST NGAY TRONG START - phát âm thanh test
        //StartCoroutine(TestAudioAfterStart());
    }

    IEnumerator TestAudioAfterStart()
    {
        yield return new WaitForSeconds(1f); // Đợi 1 giây

        Debug.Log("=== TESTING AUDIO IMMEDIATELY ===");

        // Test 1: Direct AudioSource
        if (boostClip != null)
        {
            Debug.Log("Playing boost clip with direct AudioSource...");
            boostSource.PlayOneShot(boostClip, 1f);
            yield return new WaitForSeconds(2f);
        }

        // Test 2: AudioManager
        if (AudioManager.Instance != null && boostClip != null)
        {
            Debug.Log("Playing boost clip with AudioManager...");
            AudioManager.Instance.PlayEffect(boostClip);
            yield return new WaitForSeconds(2f);
        }

        // Test 3: Unity's AudioSource.PlayClipAtPoint
        if (boostClip != null)
        {
            Debug.Log("Playing boost clip with PlayClipAtPoint...");
            AudioSource.PlayClipAtPoint(boostClip, Camera.main.transform.position, 1f);
        }

        Debug.Log("=== AUDIO TESTS COMPLETED ===");
    }

    void Update()
    {
        if (car == null) return;

        // Debug trạng thái boost mỗi frame
        if (car.isBoosting || wasBoosting)
        {
            //  Debug.Log($"Frame Update - isBoosting: {car.isBoosting}, wasBoosting: {wasBoosting}");
        }

        UpdateEngineSound();
        HandleBoostSound();
        HandleBrakeSound();

        // Test âm thanh bằng phím (để debug)
        if (Input.GetKeyDown(KeyCode.B) && boostClip != null)
        {
            Debug.Log("=== MANUAL BOOST TEST ===");
            if (useAudioManager && AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect(boostClip);
            else
                boostSource.PlayOneShot(boostClip, boostVolume);
        }

        if (Input.GetKeyDown(KeyCode.N) && brakeClip != null)
        {
            Debug.Log("=== MANUAL BRAKE TEST ===");
            if (useAudioManager && AudioManager.Instance != null)
                AudioManager.Instance.PlayEffect(brakeClip);
            else
                brakeSource.PlayOneShot(brakeClip, brakeVolume);
        }

        // Cập nhật trạng thái trước đó
        wasBoosting = car.isBoosting;
        wasHandBraking = car.handBrakeEffects;
    }

    void UpdateEngineSound()
    {
        float speedRatio = Mathf.Clamp01(car.carSpeedConverted / car.maximumSpeed);

        if (useAudioManager && AudioManager.Instance != null)
        {
            float pitch = Mathf.Lerp(pitchMin, pitchMax, speedRatio);
            // Dùng AudioManager phát engine loop
            AudioManager.Instance.PlayLoopingEngine(engineSource, engineClip, pitch);
        }
        else
        {
            // Fallback: dùng AudioSource trực tiếp nhưng sync volume
            engineSource.pitch = Mathf.Lerp(pitchMin, pitchMax, speedRatio);
            float baseVolume = Mathf.Max(0.4f, Mathf.Lerp(0.2f, engineVolume, speedRatio));

            // Sync với AudioManager volume
            if (AudioManager.Instance != null)
                engineSource.volume = baseVolume * AudioManager.Instance.effectsVolume;
            else
                engineSource.volume = baseVolume;
        }
    }

    void HandleBoostSound()
    {
        // Chỉ phát boost nếu đang boosting thực sự
        bool shouldPlayBoost = car.isBoosting &&
                               !wasBoosting &&
                               boostClip != null &&
                               car.carSpeedConverted > minSpeedForBoostSound &&
                               boostTimer <= 0f;

        if (shouldPlayBoost)
        {
            Debug.Log("=== PLAYING BOOST SOUND ===");

            if (useAudioManager && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEffect(boostClip);
            }
            else
            {
                float finalVolume = boostVolume;
                if (AudioManager.Instance != null)
                    finalVolume *= AudioManager.Instance.effectsVolume;

                boostSource.PlayOneShot(boostClip, finalVolume);
            }

            boostTimer = boostCooldown;
        }

        // Không còn chơi boost âm thanh bằng phím hay test
        boostTimer -= Time.deltaTime;
    }


    void HandleBrakeSound()
    {
        // Điều kiện phát brake sound (khi bắt đầu drift)
        bool isDrifting = car.handBrakeEffects && car.carSpeedConverted > minSpeedForBrakeSound;
        bool shouldPlayBrake = isDrifting && !wasHandBraking;

        // TEST: Force play khi nhấn Space (debug)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Debug.Log("=== SPACE KEY TEST - BRAKE SOUND ===");

            float finalVolume = brakeVolume;
            if (AudioManager.Instance != null)
                finalVolume *= AudioManager.Instance.effectsVolume;

            brakeSource.PlayOneShot(brakeClip, finalVolume);
        }

        if (useAudioManager && AudioManager.Instance != null)
        {
            if (shouldPlayBrake)
            {
                //Debug.Log("=== PLAYING BRAKE SOUND via AudioManager ===");
                AudioManager.Instance.PlayEffect(brakeClip);
            }
        }
        else
        {
            // Dùng AudioSource trực tiếp với volume sync
            if (isDrifting && !brakeSource.isPlaying)
            {
                //Debug.Log("=== PLAYING BRAKE SOUND via AudioSource ===");

                // Sync volume với AudioManager
                float finalVolume = brakeVolume;
                if (AudioManager.Instance != null)
                    finalVolume *= AudioManager.Instance.effectsVolume;

                brakeSource.volume = finalVolume;
                brakeSource.Play();
            }
            else if (!isDrifting && brakeSource.isPlaying)
            {
                //Debug.Log("=== STOPPING BRAKE SOUND ===");
                brakeSource.Stop();
            }
            else if (brakeSource.isPlaying)
            {
                // Cập nhật volume liên tục khi đang phát
                float finalVolume = brakeVolume;
                if (AudioManager.Instance != null)
                    finalVolume *= AudioManager .Instance.effectsVolume;

                brakeSource.volume = finalVolume;
            }
        }
    }
}