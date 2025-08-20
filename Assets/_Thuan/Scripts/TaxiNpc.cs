using UnityEngine;

public class TaxiNPC : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("TaxiNPC: Không tìm thấy Animator component!");
        }
    }

    public void SetWalking(bool walking)
    {
        if (animator != null)
        {
            animator.SetBool("isWalking", walking);
        }
    }

    private TaxiMission taxiMission;
    private bool hasTriggered = false;

    public void Initialize(TaxiMission mission)
    {
        taxiMission = mission;
        hasTriggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        // ✅ Check nếu collision với xe và chưa trigger
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            //taxiMission?.OnNPCTouchedCar();
        }
    }

    // ✅ Backup method nếu dùng Collider thay vì Trigger
    void OnCollisionEnter(Collision collision)
    {
        if (!hasTriggered && collision.gameObject.CompareTag("Player"))
        {
            hasTriggered = true;
            //taxiMission?.OnNPCTouchedCar();
        }
    }

    // ✅ Reset trigger khi cần thiết
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}