using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCMOVE : MonoBehaviour
{
    private Rigidbody rb; // thêm dòng này vào đầu class

    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 3.5f;

    public Transform pointA;
    public Transform pointB;

    public Animator animator;
  

    private bool hasFallen = false;
    private bool isAmbientEnabled = true;
    private float ambientFadeSpeed = 1f;
    private float targetVolume = 0f;

    private Transform playerTransform;
    private Transform currentTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true; // để tránh bị vật lý đẩy khi di chuyển

        if (animator == null)
            animator = GetComponent<Animator>();

      

        animator.Play("Walking");

    

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;

        currentTarget = pointB; // bắt đầu từ A → B
    }

    void Update()
    {
        if (hasFallen || pointA == null || pointB == null) return;

        HandleAmbientSoundVolume();
        MoveBetweenPoints();
    }

    void MoveBetweenPoints()
    {
        Vector3 targetPos = currentTarget.position;
        targetPos.y = transform.position.y;

        Vector3 direction = targetPos - transform.position;
        direction.y = 0f; // KHÓA chiều xoay

        // Xoay trước
        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // Di chuyển sau
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Debug.DrawLine(transform.position, targetPos, Color.red); // Kiểm tra đường đi

        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            currentTarget = (currentTarget == pointA) ? pointB : pointA;
        }
    }




    void HandleAmbientSoundVolume()
    {
       

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float minDistance = float.MaxValue;

        foreach (GameObject player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        
    }


    bool IsPlayerNearby(float radius)
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(transform.position, playerTransform.position) <= radius;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasFallen && collision.gameObject.CompareTag("Player"))
        {
            hasFallen = true;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            animator.applyRootMotion = false; // Giữ animator hoạt động nhưng vô hiệu hóa root motion
            animator.SetTrigger("fall");

            StartCoroutine(HandleFallSequence());
        }

    }



    IEnumerator HandleFallSequence()
    {
        isAmbientEnabled = false;

       

        isAmbientEnabled = true;
        yield return new WaitForSeconds(1.5f);

        hasFallen = false; // Cho phép di chuyển lại
        animator.Play("Walking");

        // Reset hướng di chuyển đúng
        currentTarget = Vector3.Distance(transform.position, pointA.position) < Vector3.Distance(transform.position, pointB.position) ? pointB : pointA;

        rb.isKinematic = true;
    }
    void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }

}
