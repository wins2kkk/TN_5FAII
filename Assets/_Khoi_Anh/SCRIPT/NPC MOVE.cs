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
    public AudioSource ambientAudioSource; // dành cho âm thanh rên nhẹ
    private AudioSource hurtAudioSource;   // dành cho âm thanh khi ngã

    [Header("Âm thanh khi ngã")]
    public List<AudioClip> hurtSounds = new List<AudioClip>();

    [Header("Âm thanh rên nhẹ khi có người tới gần")]
    public AudioClip ambientSound;

    [Header("Cài đặt âm lượng")]
    [Range(0f, 1f)] public float ambientMaxVolume = 0.7f;
    [Range(0f, 1f)] public float hurtVolume = 1.0f;

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

        if (ambientAudioSource == null)
            ambientAudioSource = GetComponent<AudioSource>();

        if (ambientAudioSource == null)
            Debug.LogError("Thiếu AudioSource cho AmbientSound trên NPC: " + gameObject.name);

        // Tạo thêm AudioSource để phát âm thanh hurt
        hurtAudioSource = gameObject.AddComponent<AudioSource>();
        hurtAudioSource.playOnAwake = false;
        hurtAudioSource.loop = false;

        animator.Play("Walking");

        // Cài đặt ambient sound
        if (ambientSound != null)
        {
            ambientAudioSource.clip = ambientSound;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0f;
            ambientAudioSource.Play();
        }

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
        if (ambientSound == null || ambientAudioSource == null || playerTransform == null || !isAmbientEnabled) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        targetVolume = distance <= 10f ? ambientMaxVolume : 0f;

        ambientAudioSource.volume = Mathf.MoveTowards(ambientAudioSource.volume, targetVolume, ambientFadeSpeed * Time.deltaTime);
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

            if (rb != null)
                rb.isKinematic = false; // cho vật lý hoạt động để bị đẩy lui

            animator.SetTrigger("fall");

            StartCoroutine(HandleFallSequence());
        }
    }


    IEnumerator HandleFallSequence()
    {
        isAmbientEnabled = false;

        while (ambientAudioSource.volume > 0f)
        {
            ambientAudioSource.volume = Mathf.MoveTowards(ambientAudioSource.volume, 0f, ambientFadeSpeed * Time.deltaTime * 5);
            yield return null;
        }

        if (hurtSounds.Count > 0 && IsPlayerNearby(10f))
        {
            int index = Random.Range(0, hurtSounds.Count);
            AudioClip clip = hurtSounds[index];
            hurtAudioSource.PlayOneShot(clip, hurtVolume);
            yield return new WaitForSeconds(clip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        isAmbientEnabled = true;

        yield return new WaitForSeconds(1.5f);

        hasFallen = false;
        animator.Play("Walking");

        if (rb != null)
            rb.isKinematic = true; // bật lại kinematic để tiếp tục di chuyển bằng transform
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
