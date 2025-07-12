    using UnityEngine;

    public class TaxiNPC : MonoBehaviour
    {
        [Header("Animation")]
        public Animator animator;

        [Header("Audio")]
        public AudioClip[] greetingSounds;
        public AudioClip[] farewellSounds;
        public AudioSource audioSource;

        private void Start()
        {
            // Tự động tìm Animator nếu không được gán
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            // Tự động tìm AudioSource nếu không được gán
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = GetComponentInChildren<AudioSource>();
                }
            }

            // Đảm bảo NPC bắt đầu ở trạng thái Idle
            SetWalking(false);
            SetSitting(false);
        }

        public void SetWalking(bool isWalking)
        {
            if (animator != null)
            {
                animator.SetBool("Walking", isWalking);
                animator.SetBool("Sitting", false); // Đảm bảo không sitting khi walking

                Debug.Log($"NPC Animation: Walking = {isWalking}");
            }
            else
            {
                Debug.LogWarning("Animator not found on TaxiNPC!");
            }
        }

        public void SetSitting(bool isSitting)
        {
            if (animator != null)
            {
                animator.SetBool("Sitting", isSitting);
                animator.SetBool("Walking", false); // Đảm bảo không walking khi sitting

                Debug.Log($"NPC Animation: Sitting = {isSitting}");
            }
        }

        public void SetIdle()
        {
            if (animator != null)
            {
                animator.SetBool("Walking", false);
                animator.SetBool("Sitting", false);

                Debug.Log("NPC Animation: Idle");
            }
        }

        public void LookAtTarget(Transform target)
        {
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                direction.y = 0; // Chỉ xoay theo trục Y

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = targetRotation;
                }
            }
        }

        public void PlayGreeting()
        {
            if (audioSource != null && greetingSounds != null && greetingSounds.Length > 0)
            {
                AudioClip clip = greetingSounds[Random.Range(0, greetingSounds.Length)];
                audioSource.PlayOneShot(clip);
            }

            Debug.Log("NPC: Greeting played");
        }

        public void PlayFarewell()
        {
            if (audioSource != null && farewellSounds != null && farewellSounds.Length > 0)
            {
                AudioClip clip = farewellSounds[Random.Range(0, farewellSounds.Length)];
                audioSource.PlayOneShot(clip);
            }

            Debug.Log("NPC: Farewell played");
        }

        // Trigger animations từ Animator Events (nếu cần)
        public void OnWalkingStart()
        {
            Debug.Log("NPC: Walking animation started");
        }

        public void OnWalkingEnd()
        {
            Debug.Log("NPC: Walking animation ended");
        }

        public void OnSittingStart()
        {
            Debug.Log("NPC: Sitting animation started");
        }

        public void OnSittingEnd()
        {
            Debug.Log("NPC: Sitting animation ended");
        }
    }