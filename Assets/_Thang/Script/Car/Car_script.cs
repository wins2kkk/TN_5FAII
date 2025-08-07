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
    public float maximumSteeringAngle = 18f; //Giảm góc cua
    public float maximumSpeed;
    public float brakePower;
    public Transform COM;

    [HideInInspector] public float carSpeed;
    [HideInInspector] public float carSpeedConverted;
    [HideInInspector] public bool handBrakeEffects = false;

    private float vertical = 0f, horizontal = 0f;
    private float motorTorque;
    private float tireAngle;
    private bool handBrakeInput = false;
    private bool smokeEffectEnabled;
    private Rigidbody carRigidbody;

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

    [Header("Body Tilt Settings")]
    public Transform carBody; // Trỏ đến phần thân xe (model) – thường là con của GameObject chính
    public float tiltAngle = 5f; // Góc nghiêng tối đa khi cua
    public float tiltSpeed = 5f; // Tốc độ nghiêng


    private WheelFrictionCurve originalSidewaysFrictionBackLeft, originalSidewaysFrictionBackRight;

    [SerializeField] private float flipUpOffset = 3f;
    [SerializeField] private float flipBackOffset = 5f;
    [SerializeField] private float flipCooldown = 3f;
    private float lastFlipTime = -10f;


    private float btnVertical = 0f, btnHorizontal = 0f;
    private bool btnBrake = false, btnBoost = false;

    [Header("Drift Settings")]
    //Giảm độ "nghiêng bánh xe" khi cua gấp (Drift Steer Multiplier) -> đuôi quá nhanh.
    public float driftSteerMultiplier = 1.8f; // từ 1.5 -> 2.0 hoặc cao hơn
    public float driftFriction = 0.5f;

    private bool isDrifting = false;

    // audio_nangluong
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip energyPickupSound;

    //tính vòng đua
    //[Header("Lap")]
    //public int maxLaps = 3;
    //private int currentLap;

    void Start()
    {
        if (Application.isMobilePlatform)
            control = ControlMode.Button;
        else
            control = ControlMode.Keyboard;

        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody != null)
            carRigidbody.centerOfMass = COM.localPosition;

        originalSidewaysFrictionBackLeft = BackWheelLeftCollider.sidewaysFriction;
        originalSidewaysFrictionBackRight = BackWheelRightCollider.sidewaysFriction;

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
            energySlider.maxValue = maxEnergy; energySlider.value = currentEnergy;
        }

        carRigidbody = GetComponent<Rigidbody>();

        // Tăng độ bám bánh trước khi cua để xe ổn định hơn
        WheelFrictionCurve frontFriction = FrontWheelLeftCollider.sidewaysFriction;
        frontFriction.stiffness = 1.3f; // Độ bám ngang, càng cao càng ít trượt
        FrontWheelLeftCollider.sidewaysFriction = frontFriction;
        FrontWheelRightCollider.sidewaysFriction = frontFriction;


       // maxLaps = FindObjectOfType<LapSystem>().maxLap;
    }

    void Update()
    {
        GetInputs();
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();
        HandleBoost();
        TiltCarBody();
    }
    public void SetButtonInputs(CarInputData data)
    {
        btnVertical = data.verticall;
        btnHorizontal = data.horizontall;
        btnBrake = data.brake;

        // Chỉ set true 1 frame duy nhất để HandleBoost() bắt được
        if (data.boost)
            btnBoost = true;
    }

    void GetInputs()
    {

        // Lấy input từ bàn phím
        float keyHorizontal = Input.GetAxis("Horizontal");
        float keyVertical = Input.GetAxisRaw("Vertical");
        bool keyBrake = Input.GetKey(KeyCode.Space);
        bool keyBoost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Tổng hợp input từ cả bàn phím + nút UI
        horizontal = Mathf.Clamp(keyHorizontal + btnHorizontal, -1f, 1f);
        vertical = Mathf.Clamp(keyVertical + btnVertical, -1f, 1f);
        handBrakeInput = keyBrake || btnBrake;

        //isBoosting = keyBoost || btnBoost;
    }



    void CalculateCarMovement()
    {
        carSpeed = carRigidbody.velocity.magnitude;
        carSpeedConverted = Mathf.Round(carSpeed * 3.6f);

        handBrakeEffects = handBrakeInput && carSpeedConverted > 30f;
        isDrifting = handBrakeInput && carSpeedConverted > 10f;

        if (handBrakeInput)
        {
            motorTorque = 0;
            ApplyBrake();
            HandleDrift(); // mới
            if (carSpeedConverted > 10f)
            {
                if (!smokeEffectEnabled)
                {
                    EnableSmokeEffect(true);
                    smokeEffectEnabled = true;
                }
                EnableSkidTrails(true);
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
            isDrifting = false;
            ReleaseBrake();
            RestoreFriction(); // mới

            if (Mathf.Abs(vertical) > 0.01f && carSpeedConverted < maximumSpeed * (isBoosting ? boostMultiplier : 1f))
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


    void CalculateSteering()
    {
        float steerMultiplier = isDrifting ? driftSteerMultiplier : 1f;
        float targetAngle = maximumSteeringAngle * horizontal * steerMultiplier;
        tireAngle = Mathf.Lerp(tireAngle, targetAngle, Time.deltaTime * 5f);

        FrontWheelLeftCollider.steerAngle = tireAngle;
        FrontWheelRightCollider.steerAngle = tireAngle;
    }



    void ApplyMotorTorque()
    {
        float appliedTorque = Mathf.Abs(vertical) > 0.01f ? motorTorque : 0f;

        if (carType == CarType.FrontWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = appliedTorque;
            FrontWheelRightCollider.motorTorque = appliedTorque;
            BackWheelLeftCollider.motorTorque = 0f;
            BackWheelRightCollider.motorTorque = 0f;
        }
        else if (carType == CarType.RearWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = 0f;
            FrontWheelRightCollider.motorTorque = 0f;
            BackWheelLeftCollider.motorTorque = appliedTorque;
            BackWheelRightCollider.motorTorque = appliedTorque;
        }
        else // FourWheelDrive
        {
            FrontWheelLeftCollider.motorTorque = appliedTorque;
            FrontWheelRightCollider.motorTorque = appliedTorque;
            BackWheelLeftCollider.motorTorque = appliedTorque;
            BackWheelRightCollider.motorTorque = appliedTorque;
        }
    }


    void ApplyBrake()
    {
        FrontWheelLeftCollider.brakeTorque = brakePower;
        FrontWheelRightCollider.brakeTorque = brakePower; BackWheelLeftCollider.brakeTorque = brakePower;
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

    void HandleDrift()
    {
        // Giảm ma sát ngang khi drift
        WheelFrictionCurve driftFrictionCurve = BackWheelLeftCollider.sidewaysFriction;
        driftFrictionCurve.stiffness = driftFriction;

        BackWheelLeftCollider.sidewaysFriction = driftFrictionCurve;
        BackWheelRightCollider.sidewaysFriction = driftFrictionCurve;

        // Giảm phanh bánh trước để xe quay mượt
        float driftBrakePower = brakePower * 0.5f;
        FrontWheelLeftCollider.brakeTorque = 0;
        FrontWheelRightCollider.brakeTorque = 0;
        BackWheelLeftCollider.brakeTorque = driftBrakePower;
        BackWheelRightCollider.brakeTorque = driftBrakePower;
    }

    void RestoreFriction()
    {
        BackWheelLeftCollider.sidewaysFriction = originalSidewaysFrictionBackLeft;
        BackWheelRightCollider.sidewaysFriction = originalSidewaysFrictionBackRight;
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
        // Phím boost được nhấn
        bool boostKeyDown = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        bool boostButtonDown = btnBoost;

        // Chỉ cho phép boost nếu chưa boost & năng lượng đã đầy 100%
        if (!isBoosting && (boostKeyDown || boostButtonDown) && Mathf.Approximately(currentEnergy, maxEnergy))
        {
            isBoosting = true;
            btnBoost = false; // Reset cờ UI
        }

        if (isBoosting)
        {
            currentEnergy -= energyDrainRate * Time.deltaTime;

            if (currentEnergy <= 0f)
            {
                currentEnergy = 0f;
                isBoosting = false;
            }
        }
        else
        {
            currentEnergy += energyRechargeRate * Time.deltaTime;
            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
        }

        // Quản lý hiệu ứng boost
        if (boostEffect != null)
        {
            if (isBoosting && !boostEffect.isPlaying)
                boostEffect.Play();
            else if (!isBoosting && boostEffect.isPlaying)
                boostEffect.Stop();
        }

        // Cập nhật thanh slider UI
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

            // Lùi xa hơn một chút
            Vector3 backDir = -transform.forward;
            Vector3 targetPos = transform.position + backDir * 2f + Vector3.up * 1.2f;

            // Kiểm tra mặt đất
            if (Physics.Raycast(targetPos, Vector3.down, out RaycastHit hit, 10f))
            {
                targetPos.y = hit.point.y + 0.5f;
            }

            Quaternion targetRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            StartCoroutine(FlipCarSmoothly(targetPos, targetRot, 0.6f));
        }
    }

    IEnumerator FlipCarSmoothly(Vector3 targetPos, Quaternion targetRot, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Di chuyển và xoay từ từ
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Đặt chính xác lại vị trí & rotation
        transform.position = targetPos;
        transform.rotation = targetRot;

        // Bật lại vật lý
        carRigidbody.isKinematic = false;
    }



    void TiltCarBody()
    {
        if (carBody == null) return;

        float targetYRotation = horizontal * 6f; //Giảm độ xoay thân xe khi cua

        Vector3 currentRotation = carBody.localEulerAngles;

        if (currentRotation.y > 180f) currentRotation.y -= 360f;

        float newY = Mathf.Lerp(currentRotation.y, targetYRotation, Time.deltaTime * tiltSpeed);

        // Chỉ giữ nguyên X và Z, không thay đổi nữa → bỏ nghiêng
        carBody.localEulerAngles = new Vector3(currentRotation.x, newY, 0f);
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Nangluong"))
        {
            Debug.Log("⚡ Nhặt năng lượng Boost!");

            currentEnergy = maxEnergy;
            isBoosting = false;

            if (energySlider != null)
                energySlider.value = currentEnergy;

            if (audioSource != null && energyPickupSound != null && Audio_Thanh_pho.Instance != null)
            {
                audioSource.volume = Audio_Thanh_pho.Instance.effectsVolume;
                audioSource.PlayOneShot(energyPickupSound);
            }


            // Ẩn vật phẩm và gọi Coroutine để xuất hiện lại
            other.gameObject.SetActive(false);
            StartCoroutine(RespawnEnergy(other.gameObject, 60f));
        }

    }
    private IEnumerator RespawnEnergy(GameObject energyObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        energyObject.SetActive(true);
        Debug.Log("⚡ Năng lượng đã xuất hiện lại!");
    }

    //public void IncreaseLap()
    //{
    //    currentLap++;
    //    Debug.Log("car " + gameObject.name + "Lap: " + currentLap);
    //}
}