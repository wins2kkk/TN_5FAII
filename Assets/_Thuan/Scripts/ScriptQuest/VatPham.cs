using UnityEngine;

public class CollectableItem : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ThuThapVatPham quest = FindObjectOfType<ThuThapVatPham>(); // hoặc CollectQuest nếu bạn tách
            if (quest != null)
            {
                quest.CollectItem(this.gameObject);
            }
        }
    }
}
