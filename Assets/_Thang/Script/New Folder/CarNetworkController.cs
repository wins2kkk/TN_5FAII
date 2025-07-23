using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarNetworkController : NetworkBehaviour
{
    public enum CarType { FrontWheelDrive, RearWheelDrive, FourWheelDrive }
    public CarType carType = CarType.FourWheelDrive;

    [Header("Wheel GameObject Meshes")]
    public GameObject FrontWheelLeft, FrontWheelRight, BackWheelLeft, BackWheelRight;

    [Header("WheelCollider")]
    public WheelCollider FrontWheelLeftCollider, FrontWheelRightCollider, BackWheelLeftCollider, BackWheelRightCollider;

    [Header("Movement, Steering and Braking")]
    public float maximumMotorTorque = 3000f;
    public float maximumSteeringAngle = 30f;
    public float maximumSpeed = 100f;
    public float brakePower = 5000f;
    public Transform COM;

    [Header("Boost System")]
    public float boostMultiplier = 1.5f;
    public float maxEnergy = 100f;
    public float energyDrainRate = 20f;
    public float energyRechargeRate = 15f;
    public ParticleSystem boostEffect;
    public UnityEngine.UI.Slider energySlider;

    [Header("Visual Effects")]
    public GameObject skidMarkPrefab;
    public List<Transform> skidWheelPositions;
    public ParticleSystem[] smokeEffects;

    [Header("Body Tilt Settings")]
    public Transform carBody;
    public float tiltAngle = 5f;
    public float tiltSpeed = 5f;

    [Header("Flip Settings")]
    public float flipUpOffset = 3f;
    public float flipBackOffset = 5f;
    public float flipCooldown = 3f;
    [Networked] public Vector3 NetworkedPosition { get; set; }
    [Networked] public Quaternion NetworkedRotation { get; set; }
    // ==== NETWORK PROPERTIES ====
    [Networked] public float NetworkedVertical { get; set; }
    [Networked] public float NetworkedHorizontal { get; set; }
    [Networked] public NetworkBool NetworkedIsHandbraking { get; set; }
    [Networked] public NetworkBool NetworkedIsBoosting { get; set; }
    [Networked] public float NetworkedCurrentEnergy { get; set; }
    [Networked] public float NetworkedCarSpeed { get; set; }
    [Networked] public NetworkBool NetworkedSmokeActive { get; set; }
    [Networked] public NetworkBool NetworkedSkidActive { get; set; }
    [Networked] public NetworkBool NetworkedBoostActive { get; set; }
    [Networked] public TickTimer FlipCooldownTimer { get; set; }

    // ==== PRIVATE VARIABLES ====
    private Rigidbody carRigidbody;
    private WheelFrictionCurve originalSidewaysFrictionBackLeft, originalSidewaysFrictionBackRight;
    private List<TrailRenderer> skidTrails = new List<TrailRenderer>();
    private float motorTorque;
    private float tireAngle;
    private bool smokeEffectEnabled = false;
    private bool skidEffectEnabled = false;
    private bool boostEffectEnabled = false;

    // ==== INITIALIZATION ====
    public override void Spawned()
    {
        // Khởi tạo rigidbody và center of mass
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null && COM != null)
            carRigidbody.centerOfMass = COM.localPosition;

        // Lưu friction curves gốc
        if (BackWheelLeftCollider != null)
            originalSidewaysFrictionBackLeft = BackWheelLeftCollider.sidewaysFriction;
        if (BackWheelRightCollider != null)
            originalSidewaysFrictionBackRight = BackWheelRightCollider.sidewaysFriction;

        // Khởi tạo skid trails (chỉ trên server)
        if (Object.HasStateAuthority)
        {
            InitializeSkidTrails();
            NetworkedCurrentEnergy = maxEnergy;
        }

        // Cấu hình UI cho người chơi local
        SetupLocalPlayerUI();
    }

    void InitializeSkidTrails()
    {
        if (skidMarkPrefab != null && skidWheelPositions != null)
        {
            foreach (Transform wheel in skidWheelPositions)
            {
                GameObject trailObj = Instantiate(skidMarkPrefab, wheel);
                trailObj.transform.localPosition = Vector3.zero;
                TrailRenderer trail = trailObj.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.emitting = false;
                    skidTrails.Add(trail);
                }
            }
        }
    }

    void SetupLocalPlayerUI()
    {
        if (Object.HasInputAuthority)
        {
            // Bật UI cho người chơi local
            if (energySlider != null)
                energySlider.gameObject.SetActive(true);

            Debug.Log("✅ Local player car spawned with UI enabled");
        }
        else
        {
            // Tắt UI cho những xe khác
            if (energySlider != null)
                energySlider.gameObject.SetActive(false);
        }
    }

    // ==== NETWORK UPDATE ====
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority && GetInput<CarInputData>(out var input))
        {
            ProcessNetworkInput(input);
        }

        if (Object.HasStateAuthority)
        {
            UpdatePhysics();
            UpdateEffectsState();

            // Cập nhật sync thủ công
            NetworkedPosition = transform.position;
            NetworkedRotation = transform.rotation;
        }
        else
        {
            // Client tự đồng bộ lại vị trí từ server
            float lerpSpeed = 10f;
            transform.position = Vector3.Lerp(transform.position, NetworkedPosition, Runner.DeltaTime * lerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, NetworkedRotation, Runner.DeltaTime * lerpSpeed);
        }
    }



    void ProcessNetworkInput(CarInputData input)
    {
        NetworkedVertical = input.vertical;
        NetworkedHorizontal = input.horizontal;
        NetworkedIsHandbraking = input.isHandbraking;

        // Xử lý boost input
        bool boostInput = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        HandleBoostInput(boostInput);

        // Xử lý flip input
        if (Input.GetKeyDown(KeyCode.R) && FlipCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            RPC_FlipCar();
            FlipCooldownTimer = TickTimer.CreateFromSeconds(Runner, flipCooldown);
        }
    }

    void HandleBoostInput(bool boostInput)
    {
        bool canStartBoost = boostInput && NetworkedVertical > 0 &&
                           NetworkedCurrentEnergy >= maxEnergy && NetworkedCarSpeed > 0f;

        if (NetworkedIsBoosting)
        {
            NetworkedCurrentEnergy -= energyDrainRate * Runner.DeltaTime;
            if (NetworkedCurrentEnergy <= 0f)
            {
                NetworkedIsBoosting = false;
                NetworkedBoostActive = false;
                // Giảm tốc độ khi hết boost
                if (carRigidbody != null)
                    carRigidbody.velocity *= 0.9f;
            }
        }
        else
        {
            if (canStartBoost)
            {
                NetworkedIsBoosting = true;
                NetworkedBoostActive = true;
            }
            else
            {
                NetworkedCurrentEnergy += energyRechargeRate * Runner.DeltaTime;
                NetworkedBoostActive = false;
            }

            NetworkedCurrentEnergy = Mathf.Clamp(NetworkedCurrentEnergy, 0, maxEnergy);
        }
    }

    void UpdatePhysics()
    {
        if (carRigidbody == null) return;

        // Tính toán tốc độ
        float speed = carRigidbody.velocity.magnitude;
        NetworkedCarSpeed = Mathf.Round(speed * 3.6f); // Chuyển đổi sang km/h

        // Xử lý handbrake
        if (NetworkedIsHandbraking)
        {
            motorTorque = 0;
            ApplyHandbrake();
        }
        else
        {
            ReleaseHandbrake();

            // Tính toán motor torque
            if (Mathf.Abs(NetworkedVertical) > 0.01f && NetworkedCarSpeed < maximumSpeed)
            {
                float boostMultiplier = NetworkedIsBoosting ? this.boostMultiplier : 1f;
                motorTorque = maximumMotorTorque * NetworkedVertical * boostMultiplier;
            }
            else
            {
                motorTorque = 0;
            }
        }

        // Áp dụng torque và steering
        ApplyMotorTorque();
        ApplySteering();
    }

    void UpdateEffectsState()
    {
        bool handbrakeEffects = NetworkedIsHandbraking && NetworkedCarSpeed > 40f;
        bool shouldShowEffects = handbrakeEffects && NetworkedCarSpeed > 10f;

        NetworkedSmokeActive = shouldShowEffects;
        NetworkedSkidActive = shouldShowEffects;
    }

    // ==== RENDER UPDATE ====
    public override void Render()
    {
        UpdateVisualEffects();
        UpdateWheelMeshes();
        UpdateBodyTilt();
        UpdateUI();
    }

    void UpdateVisualEffects()
    {
        // Cập nhật smoke effects
        if (NetworkedSmokeActive != smokeEffectEnabled)
        {
            EnableSmokeEffect(NetworkedSmokeActive);
            smokeEffectEnabled = NetworkedSmokeActive;
        }

        // Cập nhật skid effects
        if (NetworkedSkidActive != skidEffectEnabled)
        {
            EnableSkidTrails(NetworkedSkidActive);
            skidEffectEnabled = NetworkedSkidActive;
        }

        // Cập nhật boost effects
        if (NetworkedBoostActive != boostEffectEnabled)
        {
            EnableBoostEffect(NetworkedBoostActive);
            boostEffectEnabled = NetworkedBoostActive;
        }
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
        if (collider != null && mesh != null)
        {
            collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.transform.position = pos;
            mesh.transform.rotation = rot;
        }
    }

    void UpdateBodyTilt()
    {
        if (carBody == null) return;

        float targetZRotation = -NetworkedHorizontal * tiltAngle;
        Vector3 currentRotation = carBody.localEulerAngles;

        // Xử lý góc âm
        if (currentRotation.z > 180f) currentRotation.z -= 360f;

        float newZRotation = Mathf.Lerp(currentRotation.z, targetZRotation, Time.deltaTime * tiltSpeed);
        carBody.localEulerAngles = new Vector3(currentRotation.x, currentRotation.y, newZRotation);
    }

    void UpdateUI()
    {
        if (Object.HasInputAuthority && energySlider != null)
        {
            energySlider.value = NetworkedCurrentEnergy;
        }
    }

    // ==== PHYSICS METHODS ====
    void ApplyMotorTorque()
    {
        float appliedTorque = Mathf.Abs(NetworkedVertical) > 0.01f ? motorTorque : 0f;

        switch (carType)
        {
            case CarType.FrontWheelDrive:
                SetWheelTorque(appliedTorque, appliedTorque, 0f, 0f);
                break;
            case CarType.RearWheelDrive:
                SetWheelTorque(0f, 0f, appliedTorque, appliedTorque);
                break;
            case CarType.FourWheelDrive:
                SetWheelTorque(appliedTorque, appliedTorque, appliedTorque, appliedTorque);
                break;
        }
    }

    void SetWheelTorque(float frontLeft, float frontRight, float backLeft, float backRight)
    {
        if (FrontWheelLeftCollider != null) FrontWheelLeftCollider.motorTorque = frontLeft;
        if (FrontWheelRightCollider != null) FrontWheelRightCollider.motorTorque = frontRight;
        if (BackWheelLeftCollider != null) BackWheelLeftCollider.motorTorque = backLeft;
        if (BackWheelRightCollider != null) BackWheelRightCollider.motorTorque = backRight;
    }

    void ApplySteering()
    {
        tireAngle = maximumSteeringAngle * NetworkedHorizontal;
        if (FrontWheelLeftCollider != null) FrontWheelLeftCollider.steerAngle = tireAngle;
        if (FrontWheelRightCollider != null) FrontWheelRightCollider.steerAngle = tireAngle;
    }

    void ApplyHandbrake()
    {
        // Áp dụng phanh tay và drift
        SetWheelBrake(brakePower, brakePower, brakePower, brakePower);
        EnableDrift(true);
    }

    void ReleaseHandbrake()
    {
        SetWheelBrake(0f, 0f, 0f, 0f);
        EnableDrift(false);
    }

    void SetWheelBrake(float frontLeft, float frontRight, float backLeft, float backRight)
    {
        if (FrontWheelLeftCollider != null) FrontWheelLeftCollider.brakeTorque = frontLeft;
        if (FrontWheelRightCollider != null) FrontWheelRightCollider.brakeTorque = frontRight;
        if (BackWheelLeftCollider != null) BackWheelLeftCollider.brakeTorque = backLeft;
        if (BackWheelRightCollider != null) BackWheelRightCollider.brakeTorque = backRight;
    }

    void EnableDrift(bool enable)
    {
        if (enable)
        {
            // Giảm độ bám đường để drift
            SetWheelFriction(BackWheelLeftCollider, 0.65f);
            SetWheelFriction(BackWheelRightCollider, 0.65f);

            // Áp dụng phanh nhẹ ở bánh sau
            float driftBrake = brakePower * 0.5f;
            SetWheelBrake(0f, 0f, driftBrake, driftBrake);
        }
        else
        {
            // Khôi phục friction ban đầu
            if (BackWheelLeftCollider != null)
                BackWheelLeftCollider.sidewaysFriction = originalSidewaysFrictionBackLeft;
            if (BackWheelRightCollider != null)
                BackWheelRightCollider.sidewaysFriction = originalSidewaysFrictionBackRight;
        }
    }

    void SetWheelFriction(WheelCollider collider, float stiffness)
    {
        if (collider != null)
        {
            WheelFrictionCurve friction = collider.sidewaysFriction;
            friction.stiffness = stiffness;
            collider.sidewaysFriction = friction;
        }
    }

    // ==== VISUAL EFFECTS ====
    void EnableSmokeEffect(bool enable)
    {
        if (smokeEffects != null)
        {
            foreach (ParticleSystem smoke in smokeEffects)
            {
                if (smoke != null)
                {
                    if (enable) smoke.Play();
                    else smoke.Stop();
                }
            }
        }
    }

    void EnableSkidTrails(bool enable)
    {
        foreach (TrailRenderer trail in skidTrails)
        {
            if (trail != null)
                trail.emitting = enable;
        }
    }

    void EnableBoostEffect(bool enable)
    {
        if (boostEffect != null)
        {
            if (enable && !boostEffect.isPlaying)
                boostEffect.Play();
            else if (!enable && boostEffect.isPlaying)
                boostEffect.Stop();
        }
    }

    // ==== NETWORK RPC ====
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_FlipCar()
    {
        if (carRigidbody != null)
        {
            StartCoroutine(FlipCarCoroutine());
        }
    }

    IEnumerator FlipCarCoroutine()
    {
        // Tắt physics tạm thời
        carRigidbody.isKinematic = true;
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;

        // Tính toán vị trí mới
        Vector3 backDir = -transform.forward;
        Vector3 newPos = transform.position + backDir * flipBackOffset + Vector3.up * flipUpOffset;

        // Kiểm tra mặt đất
        if (Physics.Raycast(newPos, Vector3.down, out RaycastHit hit, 10f))
        {
            newPos.y = hit.point.y + 1.5f;
        }

        // Áp dụng vị trí và rotation mới
        transform.position = newPos;
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Chờ một chút rồi bật lại physics
        yield return new WaitForSeconds(0.1f);
        carRigidbody.isKinematic = false;

        Debug.Log("🔄 Car flipped successfully");
    }

    // ==== DEBUG INFO ====
    void OnGUI()
    {
        if (Object.HasInputAuthority)
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Speed: {NetworkedCarSpeed:F0} km/h");
            GUILayout.Label($"Energy: {NetworkedCurrentEnergy:F1}/{maxEnergy}");
            GUILayout.Label($"Boosting: {NetworkedIsBoosting}");
            GUILayout.Label($"Handbrake: {NetworkedIsHandbraking}");
            GUILayout.Label($"Input: V={NetworkedVertical:F2}, H={NetworkedHorizontal:F2}");
            GUILayout.EndArea();
        }
    }

}