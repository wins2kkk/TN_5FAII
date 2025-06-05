  using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class carNPC : MonoBehaviour
{
    [Header("Wheel Visuals")]
    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelRearLeft;
    public Transform wheelRearRight;

    public float wheelSpinSpeed = 720f; // độ quay mỗi giây

    [Header("Waypoints")]
    public Transform[] waypoints;
    private int currentIndex = 0;

    [Header("Movement")]
    public float normalSpeed = 10f;
    public float escapeSpeed = 25f;
    public float turnSpeed = 5f;

    [Header("Escape Settings")]
    public float escapeDuration = 3f;
    private bool isEscaping = false;

    [Header("VFX & SFX")]
    public GameObject hitEffectObject; // dùng object có sẵn, không phải prefab
    public AudioClip hitSound;
    private AudioSource audioSource;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    void FixedUpdate()
    {
        // Quay bánh xe
        RotateWheels();


        if (waypoints.Length == 0 || currentIndex >= waypoints.Length) return;

        Vector3 direction = (waypoints[currentIndex].position - transform.position).normalized;
        float moveSpeed = isEscaping ? escapeSpeed : normalSpeed;

        // Di chuyển
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);

        // Quay đầu xe về hướng di chuyển
        Quaternion targetRot = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.fixedDeltaTime);

        // Đến gần waypoint thì chuyển sang cái tiếp theo
        float distance = Vector3.Distance(transform.position, waypoints[currentIndex].position);
        if (distance < 1.5f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
                currentIndex = 0;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            if (!isEscaping)
                StartCoroutine(EscapeRoutine());

            // VFX
            if (hitEffectObject)
            {
                // Di chuyển tới vị trí va chạm
                hitEffectObject.transform.position = transform.position + Vector3.up * 0.5f;

                // Chỉ dùng ParticleSystem.Play và Stop
                ParticleSystem ps = hitEffectObject.GetComponent<ParticleSystem>();
                if (ps)
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // reset trước khi play
                    ps.Play();
                    StartCoroutine(StopParticleAfterSeconds(ps, 1f)); // dừng sau 0.5s
                }
            }

            // SFX
            if (hitSound && audioSource)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
    }




    IEnumerator EscapeRoutine()
    {
        isEscaping = true;
        yield return new WaitForSeconds(escapeDuration);
        isEscaping = false;
    }

    void RotateWheels()
    {
        float rotationAmount = wheelSpinSpeed * Time.fixedDeltaTime;
        if (wheelFrontLeft) wheelFrontLeft.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (wheelFrontRight) wheelFrontRight.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (wheelRearLeft) wheelRearLeft.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (wheelRearRight) wheelRearRight.Rotate(Vector3.right, rotationAmount, Space.Self);
    }

    IEnumerator DisableHitEffectAfterSeconds(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (hitEffectObject)
            hitEffectObject.SetActive(false);
    }

    IEnumerator StopParticleAfterSeconds(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ps)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

}
