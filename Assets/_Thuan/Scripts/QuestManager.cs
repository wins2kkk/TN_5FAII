using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI References - Sẽ được tự động tìm lại")]
    public GameObject QuestlogoPanel;
    public GameObject PanelQuest;
    public GameObject PanelSucces;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI questNametext;
    public Button acceptButton;
    public Button declineButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI successRewardText;

    [Header("Quest Data - Được giữ lại")]
    private QuestData currentQuest;
    private float timeRemaining;
    private bool questActive = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Ngăn tạo bản sao khi load lại scene
        }
    }

    private void Start()
    {
        SetupUI();
    }

    // Được gọi mỗi khi load scene mới
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSetup());
    }

    // Delay một chút để đảm bảo UI đã được tạo
    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        FindUIReferences();
        SetupUI();
    }

    // Tự động tìm lại UI references
    private void FindUIReferences()
    {
        // Tìm theo tên GameObject
        if (QuestlogoPanel == null)
            QuestlogoPanel = GameObject.Find("QuestlogoPanel");

        if (PanelQuest == null)
            PanelQuest = GameObject.Find("PanelQuest");

        if (PanelSucces == null)
            PanelSucces = GameObject.Find("PanelSucces");

        // Tìm Text components
        if (descriptionText == null)
        {
            GameObject descObj = GameObject.Find("DescriptionText");
            if (descObj != null) descriptionText = descObj.GetComponent<TextMeshProUGUI>();
        }

        if (questNametext == null)
        {
            GameObject nameObj = GameObject.Find("QuestNameText");
            if (nameObj != null) questNametext = nameObj.GetComponent<TextMeshProUGUI>();
        }

        if (timerText == null)
        {
            GameObject timerObj = GameObject.Find("TimerText");
            if (timerObj != null) timerText = timerObj.GetComponent<TextMeshProUGUI>();
        }

        if (successRewardText == null)
        {
            GameObject rewardObj = GameObject.Find("SuccessRewardText");
            if (rewardObj != null) successRewardText = rewardObj.GetComponent<TextMeshProUGUI>();
        }

        // Tìm Buttons
        if (acceptButton == null)
        {
            GameObject acceptObj = GameObject.Find("AcceptButton");
            if (acceptObj != null) acceptButton = acceptObj.GetComponent<Button>();
        }

        if (declineButton == null)
        {
            GameObject declineObj = GameObject.Find("DeclineButton");
            if (declineObj != null) declineButton = declineObj.GetComponent<Button>();
        }
    }

    // Setup UI sau khi tìm được references
    private void SetupUI()
    {
        if (PanelQuest != null)
            PanelQuest.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        // Setup button listeners
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(AcceptQuest);
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(() => {
                if (QuestlogoPanel != null)
                    QuestlogoPanel.SetActive(false);
            });
        }
    }

    public void ShowQuestPopup(QuestData quest)
    {
        if (questActive) return;

        currentQuest = quest;

        if (questNametext != null)
            questNametext.text = quest.name;

        if (descriptionText != null)
            descriptionText.text = quest.description + "\nThưởng: " + quest.coinReward + " coin\nThời gian: " + quest.timeLimit + "s";

        if (PanelQuest != null)
            PanelQuest.SetActive(true);
    }

    public void HideQuestPopup()
    {
        if (PanelQuest != null)
            PanelQuest.SetActive(false);
    }

    private void AcceptQuest()
    {
        if (QuestlogoPanel != null)
            QuestlogoPanel.SetActive(false);
        StartQuest();
    }

    private void StartQuest()
    {
        if (currentQuest == null) return;

        questActive = true;
        timeRemaining = currentQuest.timeLimit;

        if (timerText != null)
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

        if (timerText != null)
            timerText.text = "Thời gian: " + Mathf.CeilToInt(timeRemaining) + "s";

        if (timeRemaining <= 0)
        {
            questActive = false;

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            if (WaypointManager.Instance != null)
                WaypointManager.Instance.RemoveWaypoint();

            Debug.Log("❌ Hết thời gian làm nhiệm vụ!");
        }
    }

    public void AcpQuestlogo()
    {
        if (QuestlogoPanel != null)
            QuestlogoPanel.SetActive(true);

        if (PanelQuest != null)
            PanelQuest.SetActive(false);
    }

    public void CompleteQuest()
    {
        if (!questActive || currentQuest == null) return;

        questActive = false;

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

            if (successRewardText != null)
            {
                successRewardText.text = "Bạn đã nhận được " + currentQuest.coinReward + " coin!";
            }
        }

        if (PanelSucces != null)
        {
            PanelSucces.SetActive(true);
            StartCoroutine(HideSuccessPanel());
        }

    }

    public bool IsQuestActive()
    {
        return questActive;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private IEnumerator HideSuccessPanel()
    {
        yield return new WaitForSeconds(2f);

        if (PanelSucces != null)
            PanelSucces.SetActive(false);
    }

}