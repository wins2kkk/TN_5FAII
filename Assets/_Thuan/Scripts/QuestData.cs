using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quests/Quest Data")]
public class QuestData : ScriptableObject
{
    public string questName = "Tên nhiệm vụ";      
    public QuestType questType;
    public float timeLimit = 60f;
    public int coinReward = 50;
    public string description = "Nhiệm vụ mặc định";
}
