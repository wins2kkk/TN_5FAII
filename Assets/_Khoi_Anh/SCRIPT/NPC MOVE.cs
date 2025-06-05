using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NPCMOVE : MonoBehaviour
{
    [Header("Cài đặt di chuyển")]
    public float moveSpeed = 3.5f;

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

    private NavMeshAgent agent;
    private bool hasFallen = false;

    private float ambientFadeSpeed = 1f;
    private float targetVolume = 0f;

    private Transform playerTransform;
    private bool isAmbientEnabled = true;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

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
        PickNewDestination();

        // Cài đặt âm thanh rên nhẹ
        if (ambientSound != null)
        {
            ambientAudioSource.clip = ambientSound;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0f;
            ambientAudioSource.Play();
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    void Update()
    {
        if (hasFallen) return;

        HandleAmbientSoundVolume();

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            PickNewDestination();
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

    void PickNewDestination()
    {
        NavMeshTriangulation navMeshData = NavMesh.CalculateTriangulation();
        int index = Random.Range(0, navMeshData.vertices.Length);
        Vector3 destination = navMeshData.vertices[index];
        agent.SetDestination(destination);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasFallen && collision.gameObject.CompareTag("Player"))
        {
            hasFallen = true;
            agent.enabled = false;
            animator.SetTrigger("fall");

            StartCoroutine(HandleFallSequence());
        }
    }

    IEnumerator HandleFallSequence()
    {
        // Tắt âm thanh ambient
        isAmbientEnabled = false;

        // Giảm âm lượng ambient về 0
        while (ambientAudioSource.volume > 0f)
        {
            ambientAudioSource.volume = Mathf.MoveTowards(ambientAudioSource.volume, 0f, ambientFadeSpeed * Time.deltaTime * 5);
            yield return null;
        }

        // Phát âm thanh đau nếu có
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

        // Bật lại ambient sound
        isAmbientEnabled = true;

        yield return new WaitForSeconds(1.5f); // thêm thời gian hồi phục
        hasFallen = false;
        agent.enabled = true;
        animator.Play("Walking");
        PickNewDestination();
    }
}
