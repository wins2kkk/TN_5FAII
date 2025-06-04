using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;
    WaypointManager waypointManager;

    public GameObject QuestlogoPanel;
    public GameObject PanelQuest;
    public GameObject PanelSucces;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI questNametext;
    public Button acceptButton;
    public Button declineButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI successRewardText; 


    private QuestData currentQuest;
    private float timeRemaining;
    private bool questActive = false;

    private void Awake()
    {
        instance = this;
        PanelQuest.SetActive(false);
        timerText.gameObject.SetActive(false);
    }

    public void ShowQuestPopup(QuestData quest)
    {
        if (questActive)
        {
           
            return;
        }

        currentQuest = quest;
        questNametext.text = quest.name;
        descriptionText.text = quest.description + "\nThưởng: " + quest.coinReward + " coin\nThời gian: " + quest.timeLimit + "s";
        PanelQuest.SetActive(true);
    }


    public void HideQuestPopup()
    {
        PanelQuest.SetActive(false);
    }

    private void Start()
    {
        acceptButton.onClick.AddListener(AcceptQuest);
        declineButton.onClick.AddListener(() => QuestlogoPanel.SetActive(false));
    }

    private void AcceptQuest()
    {
        QuestlogoPanel.SetActive(false);
        StartQuest();
    }

    private void StartQuest()
    {
        if (currentQuest == null) return;

        questActive = true;
        timeRemaining = currentQuest.timeLimit;
        timerText.gameObject.SetActive(true);

        switch (currentQuest.questType)
        {
            case QuestType.ParkCar:
                FindObjectOfType<ParkingMission>()?.StartMission();
                break;
            case QuestType.Delivery:
                FindObjectOfType<DeliveryQuest>()?.StartQuest();
                break;
        }
    }

    private void Update()
    {
        if (!questActive) return;

        timeRemaining -= Time.deltaTime;
        timerText.text = "Thời gian: " + Mathf.CeilToInt(timeRemaining) + "s";

        if (timeRemaining <= 0)
        {
            questActive = false;
            timerText.gameObject.SetActive(false);

            // Xoá point khi hết tg
            if (WaypointManager.Instance != null)
                WaypointManager.Instance.RemoveWaypoint();

            Debug.Log("❌ Hết thời gian làm nhiệm vụ!");
        }
    }
    
    public void AcpQuestlogo()
    {
        QuestlogoPanel.SetActive(true);
        PanelQuest.SetActive(false);
    }

    public void CompleteQuest()
    {
        if (!questActive || currentQuest == null) return;

        questActive = false;
        timerText.gameObject.SetActive(false);

        // Thêm coin thông qua CoinManager
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

            // Cập nhật text hiển thị phần thưởng
            if (successRewardText != null)
            {
                successRewardText.text = "Bạn đã nhận được " + currentQuest.coinReward + " coin!";
            }
        }

        // Xóa waypoint
        //if (WaypointManager.Instance != null)
        //{
        //    WaypointManager.Instance.RemoveWaypoint();
        //}

         //Hiển thị panel thành công
        PanelSucces.SetActive(true);

       // Debug.Log("✅ Hoàn thành nhiệm vụ: " + currentQuest.questName + " | Nhận " + currentQuest.coinReward + " coin");
    }
    public bool IsQuestActive()
    {
        return questActive;
    }
}
