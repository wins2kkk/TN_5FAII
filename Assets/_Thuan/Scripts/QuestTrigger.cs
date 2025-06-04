using UnityEngine;

public class QuestTrigger : MonoBehaviour
{
    public QuestData quest;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            QuestManager.instance.ShowQuestPopup(quest);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            QuestManager.instance.HideQuestPopup();
        }
    }
}
