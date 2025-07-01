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

    private WheelFrictionCurve originalSidewaysFrictionBackLeft, originalSidewaysFrictionBackRight;

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
    }

    void Update()
    {
        GetInputs();
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();
        HandleBoost();
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

        handBrakeInput = Input.GetKey(KeyCode.Space);
        handBrakeEffects = handBrakeInput && carSpeedConverted > 40f;

        if (handBrakeInput)
        {
            motorTorque = 0;
            ApplyBrake();
            DriftOn();

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
        WheelFrictionCurve wheelFriction = BackWheelLeftCollider.sidewaysFriction;
        wheelFriction.stiffness = 0.65f;
        BackWheelLeftCollider.sidewaysFriction = wheelFriction;

        wheelFriction = BackWheelRightCollider.sidewaysFriction;
        wheelFriction.stiffness = 0.65f;
        BackWheelRightCollider.sidewaysFriction = wheelFriction;

        float driftBrakePower = brakePower * 0.5f;
        FrontWheelLeftCollider.brakeTorque = 0;
        FrontWheelRightCollider.brakeTorque = 0;
        BackWheelLeftCollider.brakeTorque = driftBrakePower;
        BackWheelRightCollider.brakeTorque = driftBrakePower;
    }

    void DriftOff()
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
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool canStartBoost = shiftHeld && vertical > 0 && currentEnergy >= maxEnergy && carSpeedConverted > 0f;

        if (isBoosting)
        {
            currentEnergy -= energyDrainRate * Time.deltaTime;
            if (currentEnergy <= 0f)
            {
                isBoosting = false;
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
