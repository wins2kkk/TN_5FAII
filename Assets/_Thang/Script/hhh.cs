using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class hhh : MonoBehaviour
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

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        if (carRigidbody  != null)
        {
            carRigidbody.centerOfMass = COM.localPosition;
        }
    }

    void Update()   
    {
        GetInputs();
        CalculateCarMovement();
        CalculateSteering();
        UpdateWheelMeshes();

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
        }
        else
        {
            ReleaseBrake();
            if (carSpeedConverted < maximumSpeed)
                motorTorque = maximumMotorTorque * vertical; 
            else
                motorTorque = 0;
        }
        ApplyMotorTorque();
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

}
