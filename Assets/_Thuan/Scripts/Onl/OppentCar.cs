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

    [Header("Physics Settings")]
    public float downForce = 100f; // Lực ép xuống
    public float maxYVelocity = 5f; // Giới hạn vận tốc Y
    public float groundCheckDistance = 1.5f;
    public LayerMask groundMask = -1;

    [Header("Collision Settings")]
    public float collisionForceLimit = 10f; // Giới hạn lực va chạm
    public float stabilityForce = 50f; // Lực ổn định

    private bool isGrounded;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;

   // public string botName = "Bot_A"; // Đặt tên riêng cho từng AI trong Inspector
    //public int currentLap = 0;
    public int currentCheckpoints = 0;
    public float finishTime = 0f;
    public bool finished = false;

    public string[] botNames = { "Bot_Sara", "Bot_Max", "Bot_Tom", "Bot_Eva", "Bot_Jin" };
    public string randomBotName;

    public bool hasFinished = false;
    public string botName;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;

        // Cải thiện center of mass để xe ổn định hơn
        rb.centerOfMass = new Vector3(0, -0.5f, 0);

        // Tăng drag để giảm độ trượt
        rb.drag = 1f;
        rb.angularDrag = 5f;

        maxLaps = FindObjectOfType<LapSystem>().maxLap;

        // Chỉ freeze rotation X và Z, cho phép Y rotation (steering)
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        lastPosition = transform.position;

        //if (string.IsNullOrEmpty(carName))
        //{
        //    carName = randomNames[Random.Range(0, randomNames.Length)];
        //}
        int randIndex = Random.Range(0, botNames.Length);
        randomBotName = botNames[randIndex];
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyDownForce();
        LimitYVelocity();
        Drive();
        RotateWheels();
        CheckIfStuck();

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

        lastPosition = transform.position;
    }

    private void CheckGrounded()
    {
        // Kiểm tra xe có chạm đất không
        RaycastHit hit;
        isGrounded = Physics.Raycast(transform.position, -transform.up, out hit, groundCheckDistance, groundMask);

        // Debug line để xem raycast
        Debug.DrawRay(transform.position, -transform.up * groundCheckDistance, isGrounded ? Color.green : Color.red);
    }

    private void ApplyDownForce()
    {
        // Áp dụng lực ép xuống khi xe đang di chuyển
        if (currentSpeed > 1f)
        {
            rb.AddForce(-transform.up * downForce * currentSpeed * Time.fixedDeltaTime);
        }
    }

    private void LimitYVelocity()
    {
        // Giới hạn vận tốc theo trục Y để tránh bay quá cao
        if (rb.velocity.y > maxYVelocity)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = maxYVelocity;
            rb.velocity = velocity;
        }

        // Nếu xe bay quá cao, kéo xuống
        if (rb.velocity.y < -maxYVelocity)
        {
            Vector3 velocity = rb.velocity;
            velocity.y = -maxYVelocity;
            rb.velocity = velocity;
        }
    }

    private void CheckIfStuck()
    {
        // Kiểm tra xe có bị kẹt không
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < 0.1f && currentSpeed > 1f)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer > 2f)
            {
                // Thêm lực để thoát khỏi tình trạng kẹt
                rb.AddForce(transform.forward * 500f + Vector3.up * 100f);
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
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

                // Cải thiện cách di chuyển
                Vector3 forwardVelocity = transform.forward * currentSpeed;
                Vector3 newVelocity = new Vector3(forwardVelocity.x, rb.velocity.y, forwardVelocity.z);
                rb.velocity = newVelocity;

                // Thêm lực ổn định nếu xe không chạm đất
                if (!isGrounded)
                {
                    rb.AddForce(Vector3.down * stabilityForce);
                }
            }
            else
            {
                destinationReached = true;
                // Chỉ dừng vận tốc X và Z, giữ nguyên Y
                Vector3 velocity = rb.velocity;
                velocity.x = 0;
                velocity.z = 0;
                rb.velocity = velocity;
            }
        }
    }

    private void RotateWheels()
    {
        float rotationSpeed = currentSpeed * 25f;

        frontLeftWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        frontRightWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        rearLeftWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
        rearRightWheel.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }

    private void RespawnAtDestiantion()
    {
        respawnTimer = 0f;
        currentSpeed = 5f;

        // Tìm vị trí respawn an toàn
        Vector3 spawnPosition = destination;
        RaycastHit hit;
        if (Physics.Raycast(destination + Vector3.up * 10f, Vector3.down, out hit, 20f, groundMask))
        {
            spawnPosition = hit.point + Vector3.up * 0.5f; // Cao hơn một chút
        }

        rb.MovePosition(spawnPosition);
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero; // Reset cả angular velocity

        destinationReached = false;
    }

    // Xử lý va chạm
    private void OnCollisionEnter(Collision collision)
    {
        // Giới hạn lực va chạm
        if (collision.relativeVelocity.magnitude > collisionForceLimit)
        {
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, collisionForceLimit);
        }

        // Nếu va chạm với xe khác
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<OppentCar>() != null)
        {
            // Giảm lực tác động lên trục Y
            Vector3 velocity = rb.velocity;
            velocity.y = Mathf.Clamp(velocity.y, -2f, 2f);
            rb.velocity = velocity;

            // Thêm lực đẩy nhẹ để tránh kẹt
            Vector3 pushDirection = (transform.position - collision.transform.position).normalized;
            pushDirection.y = 0; // Chỉ đẩy theo mặt phẳng ngang
            rb.AddForce(pushDirection * 100f, ForceMode.Impulse);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        // Nếu đang va chạm liên tục với xe khác
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<OppentCar>() != null)
        {
            // Giảm tốc độ để tránh xung đột
            currentSpeed = Mathf.Lerp(currentSpeed, maxSpeed * 0.5f, Time.fixedDeltaTime);

            // Kéo xe xuống đất
            rb.AddForce(Vector3.down * stabilityForce);
        }
    }

    public void LocateDestination(Vector3 destination)
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
    }
}