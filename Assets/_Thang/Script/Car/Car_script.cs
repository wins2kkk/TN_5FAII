using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_script : MonoBehaviour
{
    public enum CarType { FrontWheelDrive, RearWheelDrive, FourWheelDrive }
    public CarType carType = CarType.FourWheelDrive;

    public enum ControlMode { Keyboard, Button }
    public ControlMode control;

    [Header("Wheel GameObject Meshes")]
    public GameObject FrontWheelLeft, FrontWheelRight, BackWheelLeft, BackWheelRight;

    [Header("WheelCollider")]
    public WheelCollider FrontWheelLeftCollider, FrontWheelRightCollider, BackWheelLeftCollider, BackWheelRightCollider;

    [Header("Movement, Steering and Braking")]
    public float maximumMotorTorque;
    public float maximumSteeringAngle = 20f;
    public float maximumSpeed;
    public float brakePower;
    public Transform COM;

    [Header("Friction Settings")]
    public float normalGrip = 2.5f;
    public float driftGrip = 0.8f;
    public float frontWheelGrip = 3.0f;
    public float rearWheelGrip = 2.0f;

    //[Header("Speed Calculation Method")]
    public enum SpeedCalculationMethod { WheelRPM, PositionDifference, WheelVelocity }
    public SpeedCalculationMethod speedMethod = SpeedCalculationMethod.WheelRPM;

    [HideInInspector] public float carSpeed;
    [HideInInspector] public float carSpeedConverted;
    [HideInInspector] public bool handBrakeEffects = false;

    private float vertical = 0f, horizontal = 0f;
    private float motorTorque;
    private float tireAngle;
    private bool handBrakeInput = false;
    private bool smokeEffectEnabled;
    private Rigidbody carRigidbody;

    // Biến cho tính tốc độ
    private Vector3 lastPosition;
    private float lastTime;
    private float wheelRadius = 0.35f; // Bán kính bánh xe (có thể điều chỉnh)

    [Header("Boost System")]
    public float boostMultiplier = 1.5f;
    public float maxEnergy = 100f;
    public float currentEnergy;
    public float energyDrainRate = 20f;
    public float energyRechargeRate = 100f / 60f;
    public ParticleSystem boostEffect;
    public UnityEngine.UI.Slider energySlider;
    [HideInInspector] public bool isBoosting = false;

    [Header("SkidMark Effects")]
    public GameObject skidMarkPrefab;
    public List<Transform> skidWheelPositions;
    private List<TrailRenderer> skidTrails = new List<TrailRenderer>();

    [Header("Smoke Effects")]
    public ParticleSystem[] smokeEffects;

    private WheelFrictionCurve originalSidewaysFrictionBackLeft, originalSidewaysFrictionBackRight;
    private WheelFrictionCurve originalSidewaysFrictionFrontLeft, originalSidewaysFrictionFrontRight;
    private WheelFrictionCurve originalForwardFrictionBackLeft, originalForwardFrictionBackRight;
    private WheelFrictionCurve originalForwardFrictionFrontLeft, originalForwardFrictionFrontRight;

    [SerializeField] private float flipUpOffset = 3f;
    [SerializeField] private float flipBackOffset = 5f;
    [SerializeField] private float flipCooldown = 3f;
    private float lastFlipTime = -10f;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null)
            carRigidbody.centerOfMass = COM.localPosition;

        originalSidewaysFrictionBackLeft = BackWheelLeftCollider.sidewaysFriction;
        originalSidewaysFrictionBackRight = BackWheelRightCollider.sidewaysFriction;
        originalSidewaysFrictionFrontLeft = FrontWheelLeftCollider.sidewaysFriction;
        originalSidewaysFrictionFrontRight = FrontWheelRightCollider.sidewaysFriction;

        originalForwardFrictionBackLeft = BackWheelLeftCollider.forwardFriction;
        originalForwardFrictionBackRight = BackWheelRightCollider.forwardFriction;
        originalForwardFrictionFrontLeft = FrontWheelLeftCollider.forwardFriction;
        originalForwardFrictionFrontRight = FrontWheelRightCollider.forwardFriction;

        // Thiết lập friction ban đầu cho bánh xe
        SetupWheelFriction();

        foreach (Transform wheel in skidWheelPositions)
        {
            GameObject trailObj = Instantiate(skidMarkPrefab, wheel);
            trailObj.transform.localPosition = Vector3.zero;
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

        // Khởi tạo cho tính tốc độ
        lastPosition = transform.position;
        lastTime = Time.time;
    }

    void Update()
    {
        GetInputs();
        CalculateSpeedAlternative(); // Thay thế tính tốc độ mới
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();
        HandleBoost();

        // Điều chỉnh grip liên tục dựa trên tốc độ và góc cua
        AdjustGripDynamic();
    }

    void CalculateSpeedAlternative()
    {
        switch (speedMethod)
        {
            case SpeedCalculationMethod.WheelRPM:
                CalculateSpeedFromWheelRPM();
                break;
            case SpeedCalculationMethod.PositionDifference:
                CalculateSpeedFromPosition();
                break;
            case SpeedCalculationMethod.WheelVelocity:
                CalculateSpeedFromWheelVelocity();
                break;
        }

        carSpeedConverted = Mathf.Round(carSpeed * 3.6f);
    }

    void CalculateSpeedFromWheelRPM()
    {
        // Tính tốc độ từ RPM của bánh xe
        float avgRPM = 0f;
        int wheelCount = 0;

        if (FrontWheelLeftCollider.isGrounded)
        {
            avgRPM += FrontWheelLeftCollider.rpm;
            wheelCount++;
        }
        if (FrontWheelRightCollider.isGrounded)
        {
            avgRPM += FrontWheelRightCollider.rpm;
            wheelCount++;
        }
        if (BackWheelLeftCollider.isGrounded)
        {
            avgRPM += BackWheelLeftCollider.rpm;
            wheelCount++;
        }
        if (BackWheelRightCollider.isGrounded)
        {
            avgRPM += BackWheelRightCollider.rpm;
            wheelCount++;
        }

        if (wheelCount > 0)
        {
            avgRPM /= wheelCount;
            // Công thức: Speed = (RPM * 2π * radius) / 60
            carSpeed = Mathf.Abs((avgRPM * 2f * Mathf.PI * wheelRadius) / 60f);
        }
        else
        {
            carSpeed = 0f;
        }
    }

    void CalculateSpeedFromPosition()
    {
        // Tính tốc độ từ sự thay đổi vị trí
        float currentTime = Time.time;
        float deltaTime = currentTime - lastTime;

        if (deltaTime > 0.01f) // Cập nhật mỗi 0.01 giây
        {
            Vector3 deltaPosition = transform.position - lastPosition;
            carSpeed = deltaPosition.magnitude / deltaTime;

            lastPosition = transform.position;
            lastTime = currentTime;
        }
    }

    void CalculateSpeedFromWheelVelocity()
    {
        // Tính tốc độ từ vận tốc tại điểm tiếp xúc bánh xe
        Vector3 avgVelocity = Vector3.zero;
        int wheelCount = 0;

        if (FrontWheelLeftCollider.isGrounded)
        {
            FrontWheelLeftCollider.GetGroundHit(out WheelHit hit);
            avgVelocity += hit.sidewaysSlip * transform.right + hit.forwardSlip * transform.forward;
            wheelCount++;
        }
        if (FrontWheelRightCollider.isGrounded)
        {
            FrontWheelRightCollider.GetGroundHit(out WheelHit hit);
            avgVelocity += hit.sidewaysSlip * transform.right + hit.forwardSlip * transform.forward;
            wheelCount++;
        }
        if (BackWheelLeftCollider.isGrounded)
        {
            BackWheelLeftCollider.GetGroundHit(out WheelHit hit);
            avgVelocity += hit.sidewaysSlip * transform.right + hit.forwardSlip * transform.forward;
            wheelCount++;
        }
        if (BackWheelRightCollider.isGrounded)
        {
            BackWheelRightCollider.GetGroundHit(out WheelHit hit);
            avgVelocity += hit.sidewaysSlip * transform.right + hit.forwardSlip * transform.forward;
            wheelCount++;
        }

        if (wheelCount > 0)
        {
            avgVelocity /= wheelCount;
            carSpeed = avgVelocity.magnitude;
        }
        else
        {
            carSpeed = 0f;
        }
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
        handBrakeInput = Input.GetKey(KeyCode.Space);
        handBrakeEffects = handBrakeInput && carSpeedConverted > 40f;

        if (handBrakeInput)
        {
            motorTorque = 0;
            ApplyBrake();

            // Chỉ drift khi phanh tay và tốc độ đủ cao
            if (carSpeedConverted > 15f)
            {
                DriftOn();

                if (carSpeedConverted > 25f)
                {
                    if (!smokeEffectEnabled)
                    {
                        EnableSmokeEffect(true);
                        smokeEffectEnabled = true;
                    }
                    EnableSkidTrails(true);
                }
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

    void SetupWheelFriction()
    {
        // Bánh trước - grip cao hơn để tránh understeer
        WheelFrictionCurve frontSideways = FrontWheelLeftCollider.sidewaysFriction;
        frontSideways.stiffness = frontWheelGrip;
        frontSideways.extremumSlip = 0.4f;
        frontSideways.extremumValue = 1.0f;
        frontSideways.asymptoteSlip = 0.8f;
        frontSideways.asymptoteValue = 0.5f;

        FrontWheelLeftCollider.sidewaysFriction = frontSideways;
        FrontWheelRightCollider.sidewaysFriction = frontSideways;

        WheelFrictionCurve frontForward = FrontWheelLeftCollider.forwardFriction;
        frontForward.stiffness = frontWheelGrip;
        frontForward.extremumSlip = 0.4f;
        frontForward.extremumValue = 1.0f;
        frontForward.asymptoteSlip = 0.8f;
        frontForward.asymptoteValue = 0.5f;

        FrontWheelLeftCollider.forwardFriction = frontForward;
        FrontWheelRightCollider.forwardFriction = frontForward;

        // Bánh sau - grip thấp hơn để cho phép drift tự nhiên
        WheelFrictionCurve rearSideways = BackWheelLeftCollider.sidewaysFriction;
        rearSideways.stiffness = rearWheelGrip;
        rearSideways.extremumSlip = 0.3f;
        rearSideways.extremumValue = 1.0f;
        rearSideways.asymptoteSlip = 0.6f;
        rearSideways.asymptoteValue = 0.4f;

        BackWheelLeftCollider.sidewaysFriction = rearSideways;
        BackWheelRightCollider.sidewaysFriction = rearSideways;

        WheelFrictionCurve rearForward = BackWheelLeftCollider.forwardFriction;
        rearForward.stiffness = rearWheelGrip;
        rearForward.extremumSlip = 0.3f;
        rearForward.extremumValue = 1.0f;
        rearForward.asymptoteSlip = 0.6f;
        rearForward.asymptoteValue = 0.4f;

        BackWheelLeftCollider.forwardFriction = rearForward;
        BackWheelRightCollider.forwardFriction = rearForward;
    }

    void AdjustGripDynamic()
    {
        // Tính toán góc drift
        Vector3 velocityDirection = carRigidbody.velocity.normalized;
        float angle = Vector3.Angle(transform.forward, velocityDirection);

        // Tính toán input steering
        float steeringInput = Mathf.Abs(horizontal);

        // Điều chỉnh grip dựa trên tốc độ và góc cua
        float speedFactor = Mathf.Clamp01(carSpeedConverted / 80f);
        float gripReduction = 1f - (speedFactor * steeringInput * 0.3f);

        // Áp dụng grip reduction cho bánh sau khi cua nhanh
        if (steeringInput > 0.3f && carSpeedConverted > 30f && !handBrakeInput)
        {
            WheelFrictionCurve rearSideways = BackWheelLeftCollider.sidewaysFriction;
            rearSideways.stiffness = rearWheelGrip * gripReduction;
            BackWheelLeftCollider.sidewaysFriction = rearSideways;
            BackWheelRightCollider.sidewaysFriction = rearSideways;
        }
        else if (!handBrakeInput)
        {
            // Khôi phục grip bình thường
            WheelFrictionCurve rearSideways = BackWheelLeftCollider.sidewaysFriction;
            rearSideways.stiffness = rearWheelGrip;
            BackWheelLeftCollider.sidewaysFriction = rearSideways;
            BackWheelRightCollider.sidewaysFriction = rearSideways;
        }
    }

    void AdjustGripBasedOnSpeed()
    {
        // Chỉ dùng khi phanh tay
        float gripFactor = Mathf.Lerp(1.5f, 0.6f, carSpeedConverted / 120f);

        WheelFrictionCurve frontLeftFriction = FrontWheelLeftCollider.sidewaysFriction;
        frontLeftFriction.stiffness = frontWheelGrip * gripFactor;
        FrontWheelLeftCollider.sidewaysFriction = frontLeftFriction;

        WheelFrictionCurve frontRightFriction = FrontWheelRightCollider.sidewaysFriction;
        frontRightFriction.stiffness = frontWheelGrip * gripFactor;
        FrontWheelRightCollider.sidewaysFriction = frontRightFriction;

        WheelFrictionCurve backLeftFriction = BackWheelLeftCollider.sidewaysFriction;
        backLeftFriction.stiffness = driftGrip;
        BackWheelLeftCollider.sidewaysFriction = backLeftFriction;

        WheelFrictionCurve backRightFriction = BackWheelRightCollider.sidewaysFriction;
        backRightFriction.stiffness = driftGrip;
        BackWheelRightCollider.sidewaysFriction = backRightFriction;
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
        else
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

    void EnableSmokeEffect(bool enable)
    {
        foreach (ParticleSystem smoke in smokeEffects)
        {
            if (enable) smoke.Play();
            else smoke.Stop();
        }
    }

    void EnableSkidTrails(bool enable)
    {
        foreach (TrailRenderer trail in skidTrails)
        {
            trail.emitting = enable;
        }
    }

    void DriftOn()
    {
        // Drift mode - giảm grip bánh sau mạnh, giữ grip bánh trước
        WheelFrictionCurve frontSideways = FrontWheelLeftCollider.sidewaysFriction;
        frontSideways.stiffness = frontWheelGrip * 1.2f; // Tăng grip bánh trước
        FrontWheelLeftCollider.sidewaysFriction = frontSideways;
        FrontWheelRightCollider.sidewaysFriction = frontSideways;

        WheelFrictionCurve rearSideways = BackWheelLeftCollider.sidewaysFriction;
        rearSideways.stiffness = driftGrip; // Giảm grip bánh sau
        rearSideways.extremumSlip = 0.2f;
        rearSideways.asymptoteSlip = 0.4f;
        BackWheelLeftCollider.sidewaysFriction = rearSideways;
        BackWheelRightCollider.sidewaysFriction = rearSideways;

        // Điều chỉnh forward friction cho bánh sau
        WheelFrictionCurve rearForward = BackWheelLeftCollider.forwardFriction;
        rearForward.stiffness = driftGrip * 0.8f;
        BackWheelLeftCollider.forwardFriction = rearForward;
        BackWheelRightCollider.forwardFriction = rearForward;
    }

    void DriftOff()
    {
        // Khôi phục friction ban đầu
        BackWheelLeftCollider.sidewaysFriction = originalSidewaysFrictionBackLeft;
        BackWheelRightCollider.sidewaysFriction = originalSidewaysFrictionBackRight;
        FrontWheelLeftCollider.sidewaysFriction = originalSidewaysFrictionFrontLeft;
        FrontWheelRightCollider.sidewaysFriction = originalSidewaysFrictionFrontRight;

        BackWheelLeftCollider.forwardFriction = originalForwardFrictionBackLeft;
        BackWheelRightCollider.forwardFriction = originalForwardFrictionBackRight;
        FrontWheelLeftCollider.forwardFriction = originalForwardFrictionFrontLeft;
        FrontWheelRightCollider.forwardFriction = originalForwardFrictionFrontRight;
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
        collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.transform.position = pos;
        mesh.transform.rotation = rot;
    }

    void HandleBoost()
    {
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool canStartBoost = shiftHeld && vertical > 0 && currentEnergy >= maxEnergy && carSpeedConverted > 0f;

        if (isBoosting)
        {
            currentEnergy -= energyDrainRate * Time.deltaTime;
            if (currentEnergy <= 0f)
            {
                isBoosting = false;
                if (carRigidbody != null)
                    carRigidbody.velocity *= 0.9f;
            }
        }
        else
        {
            if (canStartBoost)
                isBoosting = true;
            else
                currentEnergy += energyRechargeRate * Time.deltaTime;

            currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        }

        if (boostEffect != null)
        {
            if (isBoosting && !boostEffect.isPlaying) boostEffect.Play();
            else if (!isBoosting && boostEffect.isPlaying) boostEffect.Stop();
        }

        if (energySlider != null)
            energySlider.value = currentEnergy;
    }

    public void FlipCarByButton()
    {
        if (Time.time - lastFlipTime < flipCooldown) return;

        if (carRigidbody != null)
        {
            lastFlipTime = Time.time;

            carRigidbody.isKinematic = true;
            carRigidbody.velocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;

            Vector3 backDir = -transform.forward;
            Vector3 newPos = transform.position + backDir * flipBackOffset + Vector3.up * flipUpOffset;

            if (Physics.Raycast(newPos, Vector3.down, out RaycastHit hit, 10f))
                newPos.y = hit.point.y + 1.5f;

            transform.position = newPos;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            StartCoroutine(ReactivatePhysics(0.1f));
        }
    }

    IEnumerator ReactivatePhysics(float delay)
    {
        yield return new WaitForSeconds(delay);
        carRigidbody.isKinematic = false;
    }
}