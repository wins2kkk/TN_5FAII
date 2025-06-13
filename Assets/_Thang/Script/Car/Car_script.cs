using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_script : MonoBehaviour
{
    public enum CarType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }
    public CarType carType = CarType.FourWheelDrive;

    public enum ControlMode
    {
        Keyboard,
        Button
    }
    public ControlMode control;

    [Header("Wheel GameObject Meshes")]
    public GameObject FrontWheelLeft;
    public GameObject FrontWheelRight;
    public GameObject BackWheelLeft;
    public GameObject BackWheelRight;

    [Header("WheelCollider")]
    public WheelCollider FrontWheelLeftCollider;
    public WheelCollider FrontWheelRightCollider;
    public WheelCollider BackWheelLeftCollider;
    public WheelCollider BackWheelRightCollider;

    [Header("Movement, Steering and Braking")]
    private float currentSpeed;
    public float maximumMotorTorque;
    public float maximumSteeringAngle = 20f;
    public float maximumSpeed;
    public float brakePower;
    public Transform COM;
    float carSpeed;
    float carSpeedConverted;
    float motorTorque;
    float tireAngle;
    float vertical = 0f;
    float horizontal = 0f;
    bool handBrakeInput = false;
    bool handBrakeEffects = false;
    Rigidbody carRigidbody;

    [Header("Boost System")]
    public float boostMultiplier = 1.5f;
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyDrainRate = 20f; // mỗi giây giảm bao nhiêu
    public float energyRechargeRate = 100f / 60f; // = 1.666.../s
    public ParticleSystem boostEffect; // Hiệu ứng khi boost
    public UnityEngine.UI.Slider energySlider; // Slide UI hiển thị năng lượng
    private bool isBoosting = false;
    private bool wasBoostingLastFrame = false;

    [Header("Sounds & Effects")]
    public ParticleSystem[] smokeEffects;
    private bool smokeEffectEnabled;

    [Header("SkidMark Effects")]
    public GameObject skidMarkPrefab;
    public List<Transform> skidWheelPositions; // Gắn Transform bánh xe tạo hiệu ứng
    private List<TrailRenderer> skidTrails = new List<TrailRenderer>();

    [Header("Car Audio")]
    public AudioClip engineClip;     // Âm thanh khi xe chạy
    public AudioClip boostClip;      // Âm thanh khi tăng tốc (shift)
    public AudioClip brakeClip;      // Âm thanh khi phanh
    private AudioSource carAudioSource; // AudioSource cho âm thanh xe
    private float enginePitchMin = 0.8f; // Pitch tối thiểu cho âm thanh động cơ
    private float enginePitchMax = 1.5f; // Pitch tối đa cho âm thanh động cơ
    private float minVolume = 0.2f;      // Âm lượng khi xe đứng im
    private float maxVolume = 1f;        // Âm lượng khi xe chạy tối đa
    private const float minSpeedForBoost = 0.0f; // Ngưỡng tốc độ để boost hoạt động
    private const float minSpeedForBrakeEffects = 40f; // Ngưỡng tốc độ để hiệu ứng/âm thanh phanh hoạt động

    // Drift xe
    private WheelFrictionCurve originalSidewaysFrictionBackLeft;
    private WheelFrictionCurve originalSidewaysFrictionBackRight;

    [SerializeField] private float flipUpOffset = 3f;
    [SerializeField] private float flipBackOffset = 5f;
    [SerializeField] private float flipCooldown = 3f;
    private float lastFlipTime = -10f;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null)
        {
            carRigidbody.centerOfMass = COM.localPosition;
        }

        // Lưu lại friction gốc của bánh sau để phục hồi khi nhả phanh
        originalSidewaysFrictionBackLeft = BackWheelLeftCollider.sidewaysFriction;
        originalSidewaysFrictionBackRight = BackWheelRightCollider.sidewaysFriction;

        // Khởi tạo vệt trượt cho các bánh có trong danh sách
        foreach (Transform wheel in skidWheelPositions)
        {
            GameObject trailObj = Instantiate(skidMarkPrefab, wheel);
            trailObj.transform.localPosition = Vector3.zero; // hoặc Vector3.down * 0.05f nếu cần hạ thấp
            TrailRenderer trail = trailObj.GetComponent<TrailRenderer>();
            trail.emitting = false;
            skidTrails.Add(trail);
        }

        currentEnergy = maxEnergy;
        if (energySlider != null)
        {
            energySlider.maxValue = maxEnergy;
            energySlider.value = currentEnergy;
        }

        // Khởi tạo AudioSource cho xe
        carAudioSource = gameObject.AddComponent<AudioSource>();
        carAudioSource.loop = true; // Âm thanh động cơ lặp lại
        carAudioSource.playOnAwake = false;
        carAudioSource.clip = engineClip;
        carAudioSource.volume = minVolume; // Bắt đầu với âm lượng nhỏ
        // Kiểm tra AudioManager
        if (AudioManager.Instance == null)
            Debug.LogError("AudioManager.Instance is null! Ensure AudioManager exists in the scene.");
    }

    void Update()
    {
        GetInputs();
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();
        HandleBoost(); // Gọi xử lý boost
        UpdateCarAudio(); // Gọi xử lý âm thanh



    }

    void GetInputs()
    {
        if (control == ControlMode.Keyboard)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }
    }

    void CalculateCarMovement()
    {
        carSpeed = carRigidbody.velocity.magnitude;
        carSpeedConverted = Mathf.Round(carSpeed * 3.6f);

        // Kiểm tra input phanh
        handBrakeInput = Input.GetKey(KeyCode.Space);
        // Chỉ kích hoạt hiệu ứng phanh khi tốc độ > minSpeedForBrakeEffects
        handBrakeEffects = handBrakeInput && carSpeedConverted > minSpeedForBrakeEffects;
        if (handBrakeInput)
        {
            motorTorque = 0;
            ApplyBrake();
            DriftOn();

            if (carSpeedConverted > 10f) // <--- CHỈ hiệu ứng nếu tốc độ > 10km/h
            {
                if (!smokeEffectEnabled)
                {
                    EnableSmokeEffect(true);
                    smokeEffectEnabled = true;
                }
                EnableSkidTrails(true);
                // PlayBrakeSound();
            }
            else
            {
                EnableSmokeEffect(false);
                EnableSkidTrails(false);
                smokeEffectEnabled = false;
            }
        }
        else
        {
            ReleaseBrake();
            DriftOff();

            if (carSpeedConverted < maximumSpeed)
            {
                float boost = isBoosting ? boostMultiplier : 1f;
                motorTorque = maximumMotorTorque * vertical * boost;
            }
            else
            {
                motorTorque = 0;
            }

            if (smokeEffectEnabled)
            {
                EnableSmokeEffect(false);
                EnableSkidTrails(false);
                smokeEffectEnabled = false;
            }
        }


        ApplyMotorTorque();
        EnableSkidTrails(handBrakeEffects);
    }

    void EnableSkidTrails(bool enable)
    {
        foreach (TrailRenderer trail in skidTrails)
        {
            trail.emitting = enable;
        }
    }

    void CalculateSteering()
    {
        tireAngle = maximumSteeringAngle * horizontal;
        FrontWheelLeftCollider.steerAngle = tireAngle;
        FrontWheelRightCollider.steerAngle = tireAngle;
    }

    void ApplyMotorTorque()
    {
        if (carType == CarType.FrontWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = motorTorque;
            FrontWheelRightCollider.motorTorque = motorTorque;
        }
        else if (carType == CarType.RearWheelDrive)
        {
            BackWheelLeftCollider.motorTorque = motorTorque;
            BackWheelRightCollider.motorTorque = motorTorque;
        }
        else if (carType == CarType.FourWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = motorTorque;
            FrontWheelRightCollider.motorTorque = motorTorque;
            BackWheelLeftCollider.motorTorque = motorTorque;
            BackWheelRightCollider.motorTorque = motorTorque;
        }
    }

    void ApplyBrake()
    {
        FrontWheelLeftCollider.brakeTorque = brakePower;
        FrontWheelRightCollider.brakeTorque = brakePower;
        BackWheelLeftCollider.brakeTorque = brakePower;
        BackWheelRightCollider.brakeTorque = brakePower;
    }

    void ReleaseBrake()
    {
        FrontWheelLeftCollider.brakeTorque = 0;
        FrontWheelRightCollider.brakeTorque = 0;
        BackWheelLeftCollider.brakeTorque = 0;
        BackWheelRightCollider.brakeTorque = 0;
    }

    void UpdateWheelMeshes()
    {
        UpdateWheelOrientation(FrontWheelLeftCollider, FrontWheelLeft);
        UpdateWheelOrientation(FrontWheelRightCollider, FrontWheelRight);
        UpdateWheelOrientation(BackWheelLeftCollider, BackWheelLeft);
        UpdateWheelOrientation(BackWheelRightCollider, BackWheelRight);
    }

    void UpdateWheelOrientation(WheelCollider collider, GameObject mesh)
    {
        Vector3 pos;
        Quaternion rot;
        collider.GetWorldPose(out pos, out rot);
        mesh.transform.position = pos;
        mesh.transform.rotation = rot;
    }

    private void EnableSmokeEffect(bool enable)
    {
        foreach (ParticleSystem smoke in smokeEffects)
        {
            if (enable)
            {
                smoke.Play();
            }
            else
            {
                smoke.Stop();
            }
        }
    }

    void DriftOn()
    {
        WheelFrictionCurve wheelFriction = BackWheelLeftCollider.sidewaysFriction;
        wheelFriction.stiffness = 0.65f; // Giảm độ bám dính để drift
        BackWheelLeftCollider.sidewaysFriction = wheelFriction;

        wheelFriction = BackWheelRightCollider.sidewaysFriction;
        wheelFriction.stiffness = 0.65f;
        BackWheelRightCollider.sidewaysFriction = wheelFriction;

        float driftBrakePower = brakePower * 0.5f; // Lực phanh giảm khi drift
        FrontWheelLeftCollider.brakeTorque = 0;
        FrontWheelRightCollider.brakeTorque = 0;
        BackWheelLeftCollider.brakeTorque = driftBrakePower;
        BackWheelRightCollider.brakeTorque = driftBrakePower;
    }

    void DriftOff()
    {
        BackWheelLeftCollider.sidewaysFriction = originalSidewaysFrictionBackLeft;
        BackWheelRightCollider.sidewaysFriction = originalSidewaysFrictionBackRight;
        ReleaseBrake();
    }

    void HandleBoost()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Điều kiện: chỉ được boost khi nhấn Shift, đang đi tiến, năng lượng đầy, và xe đang di chuyển
        bool canStartBoost = shiftHeld && vertical > 0 && currentEnergy >= maxEnergy && carSpeedConverted > minSpeedForBoost;

        if (isBoosting)
        {
            currentEnergy -= energyDrainRate * Time.deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy);

            if (currentEnergy <= 0f)
            {
                isBoosting = false;
                carRigidbody.velocity *= 0.9f; // Giảm tốc nhẹ khi hết boost
            }
        }
        else
        {
            if (canStartBoost)
            {
                Debug.Log("Boost activated: currentEnergy = " + currentEnergy);
                isBoosting = true;
            }
            else
            {
                currentEnergy += energyRechargeRate * Time.deltaTime;
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy);
            }
        }

        if (boostEffect != null)
        {
            if (isBoosting && !boostEffect.isPlaying)
                boostEffect.Play();
            else if (!isBoosting && boostEffect.isPlaying)
                boostEffect.Stop();
        }

        if (energySlider != null)
        {
            energySlider.value = currentEnergy;
        }

        wasBoostingLastFrame = isBoosting;
    }

    //reset Tranfom
    public void FlipCarByButton()
    {
        if (Time.time - lastFlipTime < flipCooldown)
        {
            Debug.Log("⏳ Chờ cooldown lật xe...");
            return;
        }

        if (carRigidbody != null)
        {
            lastFlipTime = Time.time;

            carRigidbody.isKinematic = true;
            carRigidbody.velocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;

            Vector3 backDir = -transform.forward;
            Vector3 newPos = transform.position + backDir * flipBackOffset + Vector3.up * flipUpOffset;

            if (Physics.Raycast(newPos, Vector3.down, out RaycastHit hit, 10f))
            {
                newPos.y = hit.point.y + 1.5f;
            }

            transform.position = newPos;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            StartCoroutine(ReactivatePhysics(0.1f));
            Debug.Log("🚗 Xe đã được lật lại qua Button.");
        }
    }

    IEnumerator ReactivatePhysics(float delay)
    {
        yield return new WaitForSeconds(delay);
        carRigidbody.isKinematic = false;
    }



    void UpdateCarAudio()
    {
        if (engineClip == null || AudioManager.Instance == null) return;

        float speedRatio = carSpeedConverted / maximumSpeed;
        float targetPitch = Mathf.Lerp(enginePitchMin, enginePitchMax, speedRatio);

        // Đồng bộ engine với VFX volume
        AudioManager.Instance.PlayLoopingEngine(carAudioSource, engineClip, targetPitch);

        // Phanh
        if (handBrakeEffects && brakeClip != null && carSpeedConverted > minSpeedForBrakeEffects)
        {
            AudioManager.Instance.PlayEffect(brakeClip);
        }
        // Boost
        else if (isBoosting && boostClip != null && carSpeedConverted > minSpeedForBoost)
        {
            AudioManager.Instance.PlayEffect(boostClip);
        }
    }
}