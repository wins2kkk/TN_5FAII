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
    };
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
    bool handBrake = false;
    Rigidbody carRigidbody;

    //speed
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

    [Header("Brake Sound")]
    public AudioSource brakeAudioSource;
    public AudioClip brakeClip;


    // drift xe
    // Thêm bi?n lưu thông s? friction g?c
    private WheelFrictionCurve originalSidewaysFrictionBackLeft;
    private WheelFrictionCurve originalSidewaysFrictionBackRight;
    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null)
        {
            carRigidbody.centerOfMass = COM.localPosition;
        }

        // Lưu l?i friction g?c c?a bánh sau đ? ph?c h?i khi nh? phanh
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

    }

    void Update()
    {
        GetInputs();
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();

        HandleBoost(); // gọi xử lý boost


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
        carSpeed = Mathf.Round(carSpeed * 3.6f);

        // Apply Braking
        if (Input.GetKey(KeyCode.Space))
            handBrake = true;
        else
            handBrake = false;
        if (handBrake)
        {
            motorTorque = 0;
            ApplyBrake();

            // Gi?m friction bên đ? drift
            DriftOn();

            if (!smokeEffectEnabled)
            {
                EnableSmokeEffect(true);
                smokeEffectEnabled = true;
            }
            PlayBrakeSound();
        }
        else
        {
            ReleaseBrake();

            // Ph?c h?i friction
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
                smokeEffectEnabled = false;
            }
        }
        StopBrakeSound();
        ApplyMotorTorque();
        EnableSkidTrails(handBrake);


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
        UpdateWheelPose(FrontWheelLeftCollider, FrontWheelLeft);
        UpdateWheelPose(FrontWheelRightCollider, FrontWheelRight);
        UpdateWheelPose(BackWheelLeftCollider, BackWheelLeft);
        UpdateWheelPose(BackWheelRightCollider, BackWheelRight);
    }

    void UpdateWheelPose(WheelCollider collider, GameObject mesh)
    {
        Vector3 pos;
        Quaternion quat;
        collider.GetWorldPose(out pos, out quat);
        mesh.transform.position = pos;
        mesh.transform.rotation = quat; 
    }

    private void EnableSmokeEffect(bool enable)
    {
        foreach (ParticleSystem smokeEffect in smokeEffects)
        {
            if (enable)
            {
                smokeEffect.Play();
            }
            else
            {
                smokeEffect.Stop();
            }
        }
    }

    void DriftOn()
    {
        WheelFrictionCurve sidewaysFriction = BackWheelLeftCollider.sidewaysFriction;
        sidewaysFriction.stiffness = 0.65f;  // Giảm nhẹ để lết, không giảm quá sâu
        BackWheelLeftCollider.sidewaysFriction = sidewaysFriction;

        sidewaysFriction = BackWheelRightCollider.sidewaysFriction;
        sidewaysFriction.stiffness = 0.65f;
        BackWheelRightCollider.sidewaysFriction = sidewaysFriction;

        // Thay vì brakePower lớn, chỉ dùng brakePower nhỏ cho handbrake để bánh lết chứ ko đứng hẳn
        float driftBrakePower = brakePower * 10f;  // 50% lực thắng
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


    /// play stop thang oto
    /// 
    void PlayBrakeSound()
    {
        if (brakeAudioSource != null && brakeClip != null)
        {
            if (!brakeAudioSource.isPlaying)
            {
                brakeAudioSource.clip = brakeClip;
                brakeAudioSource.loop = true;
                brakeAudioSource.Play();
            }
        }
    }

    void StopBrakeSound()
    {
        if (brakeAudioSource != null && brakeAudioSource.isPlaying)
        {
            brakeAudioSource.Stop();
        }
    }

    void HandleBoost()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Điều kiện: chỉ được boost khi nhấn Shift, đang đi tiến, và năng lượng đã đầy
        bool canStartBoost = shiftHeld && vertical > 0 && currentEnergy >= maxEnergy;

        // Nếu đang boost
        if (isBoosting)
        {
            // Trừ năng lượng
            currentEnergy -= energyDrainRate * Time.deltaTime;
            currentEnergy = Mathf.Max(0, currentEnergy);

            // Khi hết năng lượng thì dừng boost
            if (currentEnergy <= 0f)
            {
                isBoosting = false;
                carRigidbody.velocity *= 0.9f; // Giảm tốc nhẹ khi hết boost
            }
        }
        else
        {
            // Nếu chưa boost mà điều kiện đủ, bắt đầu boost
            if (canStartBoost)
            {
                isBoosting = true;
            }
            else
            {
                // Hồi năng lượng khi không boost
                currentEnergy += energyRechargeRate * Time.deltaTime;
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy);
            }
        }

        // Cập nhật hiệu ứng boost
        if (boostEffect != null)
        {
            if (isBoosting && !boostEffect.isPlaying)
                boostEffect.Play();
            else if (!isBoosting && boostEffect.isPlaying)
                boostEffect.Stop();
        }

        // Cập nhật thanh năng lượng
        if (energySlider != null)
        {
            energySlider.value = currentEnergy;
        }

        wasBoostingLastFrame = isBoosting;
    }


}


