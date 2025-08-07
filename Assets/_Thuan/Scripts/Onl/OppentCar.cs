using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class OppentCar : MonoBehaviour
{
    [Header("Car Engine")]
    public float maxSpeed;
    public float currentSpeed;
    public float acceleration = 1f;
    public float turningSpeed = 30f;
    public float breakSpeed = 12f;
    [Header("Destination Var")]
    public Vector3 destination;
    public bool destinationReached;

    private Rigidbody rb;

    [Header("Respawn")]
    public float respawnTimer = 0f;
    public const float respawnTimeThreshold = 10f;

    [Header("Lap")]
    public int maxLaps = 3;
    public int currentLap;

    [Header("Wheels")]
    public Transform frontLeftWheel;
    public Transform frontRightWheel;
    public Transform rearLeftWheel;
    public Transform rearRightWheel;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        maxLaps = FindObjectOfType<LapSystem>().maxLap;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

    }
    void FixedUpdate()
    {
        Drive();
        RotateWheels();

        if (!destinationReached)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnTimeThreshold)
            {
                RespawnAtDestiantion();
            }
        }
        else
        {
            respawnTimer = 0f;
        }
    }
    public void Drive()
    {
        if (!destinationReached)
        {
            Vector3 destinationDirection = destination - transform.position;
            destinationDirection.y = 0;
            float destinationDistance = destinationDirection.magnitude;
            if (destinationDistance >= breakSpeed)
            {
                Quaternion targetRotation = Quaternion.LookRotation(destinationDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turningSpeed * Time.deltaTime);
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
                rb.velocity = transform.forward * currentSpeed;
            }
            else 
            {
                destinationReached = true;
                rb.velocity = Vector3.zero;
            }
        }
    }
    private void RotateWheels()
    {
        float rotationSpeed = currentSpeed * 25f; // Hệ số quay, có thể tinh chỉnh

        // Quay theo trục X (xoay như xe thật)
        frontLeftWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        frontRightWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        rearLeftWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        rearRightWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }

    private void RespawnAtDestiantion()
    {
        respawnTimer = 0f;
        currentSpeed = 5f;

        //transform.position = destination;
        rb.MovePosition(new Vector3(destination.x, 0.2f, destination.z));
        rb.velocity = Vector3.zero;

        destinationReached = false;
    }
    public void LocateDestination ( Vector3 destination)
    {
        this.destination = destination;
        destinationReached = false;
    }
    public void ResetAcceleration()
    {
        currentSpeed = Random.Range(38f, 46f);
        acceleration = Random.Range(3.5f, 5f);
    }
    public void IncreaseLap()
    {
        currentLap++;
        Debug.Log("car "+ gameObject.name + "Lap: " + currentLap);
    }
}

